param([Parameter(Mandatory=$true)][string]$OutputPath, [int]$Seconds = 45)
$ErrorActionPreference = 'Stop'
$taskWorkspace = (Resolve-Path -LiteralPath 'C:\Users\sdjsd\Desktop\Unity\Project_PA').Path
$taskOutput = [IO.Path]::GetFullPath($OutputPath)
if (-not $taskOutput.StartsWith($taskWorkspace + '\Logs\CodexOpeningDemoFinal\P11-Audio-20261005-001\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must stay in the dedicated audio log' }
if (Test-Path -LiteralPath $taskOutput) { throw 'Capture already exists' }
if ($Seconds -lt 1 -or $Seconds -gt 60) { throw 'Capture duration must be 1..60 seconds' }
Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

[ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDeviceEnumerator {
 [PreserveSig] int EnumAudioEndpoints(int flow, uint mask, out object devices);
 [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IDevice device);
 [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IDevice device);
 [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr callback);
 [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr callback);
}
[ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IDevice {
 [PreserveSig] int Activate(ref Guid iid, uint context, IntPtr args, [MarshalAs(UnmanagedType.IUnknown)] out object result);
 [PreserveSig] int OpenPropertyStore(uint access, out object store);
 [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
 [PreserveSig] int GetState(out uint state);
}
[ComImport, Guid("1CB9AD4C-DBFA-4C32-B178-C2F568A703B2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IClient {
 [PreserveSig] int Initialize(int shareMode, uint flags, long duration, long period, IntPtr format, ref Guid session);
 [PreserveSig] int GetBufferSize(out uint frames);
 [PreserveSig] int GetStreamLatency(out long latency);
 [PreserveSig] int GetCurrentPadding(out uint frames);
 [PreserveSig] int IsFormatSupported(int mode, IntPtr format, out IntPtr closest);
 [PreserveSig] int GetMixFormat(out IntPtr format);
 [PreserveSig] int GetDevicePeriod(out long normal, out long minimum);
 [PreserveSig] int Start();
 [PreserveSig] int Stop();
 [PreserveSig] int Reset();
 [PreserveSig] int SetEventHandle(IntPtr handle);
 [PreserveSig] int GetService(ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object service);
}
[ComImport, Guid("C8ADBD64-E71E-48A0-A4DE-185C395CD317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface ICapture {
 [PreserveSig] int GetBuffer(out IntPtr data, out uint frames, out uint flags, out ulong position, out ulong qpc);
 [PreserveSig] int ReleaseBuffer(uint frames);
 [PreserveSig] int GetNextPacketSize(out uint frames);
}
public static class PAOutputCapture {
 static void Check(int hr) { Marshal.ThrowExceptionForHR(hr); }
 public static string Capture(string path, int seconds) {
  var enumerator = (IDeviceEnumerator)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")));
  IDevice device = null; IClient client = null; ICapture capture = null; IntPtr format = IntPtr.Zero; bool started = false;
  var began = DateTimeOffset.Now;
  try {
   Check(enumerator.GetDefaultAudioEndpoint(0, 0, out device));
   var clientId = typeof(IClient).GUID; object activated;
   Check(device.Activate(ref clientId, 23, IntPtr.Zero, out activated)); client = (IClient)activated;
   Check(client.GetMixFormat(out format));
   int extra = (ushort)Marshal.ReadInt16(format, 16), align = (ushort)Marshal.ReadInt16(format, 12);
   var fmt = new byte[18 + extra]; Marshal.Copy(format, fmt, 0, fmt.Length);
   var session = Guid.Empty;
   Check(client.Initialize(0, 0x20000, 10000000, 0, format, ref session));
   var captureId = typeof(ICapture).GUID; object service;
   Check(client.GetService(ref captureId, out service)); capture = (ICapture)service;
   using (var samples = new MemoryStream()) {
    Check(client.Start()); started = true; var watch = Stopwatch.StartNew();
    while (watch.Elapsed.TotalSeconds < seconds) {
     uint next; Check(capture.GetNextPacketSize(out next));
     while (next != 0) {
      IntPtr data; uint frames, flags; ulong position, qpc;
      Check(capture.GetBuffer(out data, out frames, out flags, out position, out qpc));
      try {
       var bytes = new byte[checked((int)frames * align)];
       if ((flags & 2) == 0) Marshal.Copy(data, bytes, 0, bytes.Length);
       samples.Write(bytes, 0, bytes.Length);
      } finally { Check(capture.ReleaseBuffer(frames)); }
      Check(capture.GetNextPacketSize(out next));
     }
     Thread.Sleep(5);
    }
    Check(client.Stop()); started = false;
    using (var writer = new BinaryWriter(new FileStream(path, FileMode.CreateNew))) {
     writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write((uint)(20 + fmt.Length + samples.Length));
     writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write((uint)fmt.Length); writer.Write(fmt);
     writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write((uint)samples.Length); samples.Position = 0; samples.CopyTo(writer.BaseStream);
    }
    return "begin=" + began.ToString("o") + " wallSeconds=" + watch.Elapsed.TotalSeconds.ToString("F3") + " sampleBytes=" + samples.Length;
   }
  } finally {
   if (started && client != null) client.Stop();
   if (format != IntPtr.Zero) Marshal.FreeCoTaskMem(format);
   if (capture != null) Marshal.ReleaseComObject(capture);
   if (client != null) Marshal.ReleaseComObject(client);
   if (device != null) Marshal.ReleaseComObject(device);
   Marshal.ReleaseComObject(enumerator);
  }
 }
}
'@
[PAOutputCapture]::Capture($taskOutput, $Seconds)

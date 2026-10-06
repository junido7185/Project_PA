using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;

// Reads real compiler artifacts. It never loads game types or calls Unity APIs.
static class ReadPdbChecksums
{
    static string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    static int Main(string[] args)
    {
        try
        {
            if (args.Length < 4) throw new ArgumentException("root dll pdb relative-source...");
            string root = Path.GetFullPath(args[0]);
            string dllPath = Path.GetFullPath(args[1]), pdbPath = Path.GetFullPath(args[2]);
            byte[] dllBytes = File.ReadAllBytes(dllPath), pdbBytes = File.ReadAllBytes(pdbPath);
            using var dllStream = new MemoryStream(dllBytes, false);
            using var pe = new PEReader(dllStream);
            using var pdbStream = new MemoryStream(pdbBytes, false);
            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var reader = provider.GetMetadataReader();
            if (reader.DebugMetadataHeader == null) throw new InvalidDataException("No portable PDB header");
            var pdbId = new BlobContentId(reader.DebugMetadataHeader.Id);
            bool linked = pe.ReadDebugDirectory().Any(entry => entry.Type == DebugDirectoryEntryType.CodeView &&
                pe.ReadCodeViewDebugDirectoryData(entry).Guid == pdbId.Guid && entry.Stamp == pdbId.Stamp);
            string[] wanted = args.Skip(3).Select(s => s.Replace('\\', '/')).ToArray();
            var checks = new List<object>();
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool allMatch = true;
            foreach (var handle in reader.Documents)
            {
                var document = reader.GetDocument(handle);
                string name = reader.GetString(document.Name).Replace('\\', '/');
                string relative = wanted.FirstOrDefault(p => name.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith("/" + p, StringComparison.OrdinalIgnoreCase));
                if (relative == null) continue;
                string currentPath = Path.GetFullPath(Path.Combine(root, relative));
                if (!currentPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Source escaped the approved workspace");
                Guid algorithm = reader.GetGuid(document.HashAlgorithm);
                byte[] current = File.ReadAllBytes(currentPath);
                byte[] checksum = reader.GetBlobBytes(document.Hash);
                byte[] actual = algorithm == new Guid("8829d00f-11b8-4213-878b-770e8597ac16") ? SHA256.HashData(current) :
                    algorithm == new Guid("ff1816ec-aa5e-4d10-87f7-6f4963833460") ? SHA1.HashData(current) :
                    throw new InvalidDataException("Unsupported PDB checksum algorithm: " + algorithm);
                bool matches = checksum.SequenceEqual(actual);
                allMatch &= matches;
                found.Add(relative);
                checks.Add(new { source = relative, pdbDocument = name, hashAlgorithm = algorithm,
                    pdbChecksum = Convert.ToHexString(checksum).ToLowerInvariant(),
                    currentChecksum = Convert.ToHexString(actual).ToLowerInvariant(), matches });
            }
            string[] missing = wanted.Where(p => !found.Contains(p)).ToArray();
            bool stable = Digest(File.ReadAllBytes(dllPath)) == Digest(dllBytes) &&
                          Digest(File.ReadAllBytes(pdbPath)) == Digest(pdbBytes);
            bool pass = linked && allMatch && missing.Length == 0 && stable;
            Console.WriteLine(JsonSerializer.Serialize(new { success = pass, observedAt = DateTimeOffset.UtcNow,
                assembly = dllPath, assemblySha256 = Digest(dllBytes), pdb = pdbPath, pdbSha256 = Digest(pdbBytes),
                assemblyPdbLinked = linked, pdbGuid = pdbId.Guid, pdbStamp = pdbId.Stamp,
                artifactsStableDuringRead = stable, documents = checks, missing,
                scope = "Compiled source identity only; no gameplay, input, GameView or performance test." },
                new JsonSerializerOptions { WriteIndented = true }));
            return pass ? 0 : 2;
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { success = false, error = ex.Message }));
            return 1;
        }
    }
}

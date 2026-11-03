"""Foreground-only OS input and native client capture for the existing isolated player."""
import argparse
import ctypes as c
import ctypes.wintypes as w
import datetime
import json
from pathlib import Path
import struct
import time
import zlib

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / 'Logs/CodexOpeningDemoFinal/P11-FishingResume-20261005-001'
u, g = c.windll.user32, c.windll.gdi32
u.SetProcessDPIAware()
u.GetForegroundWindow.restype = w.HWND
u.GetDC.restype = w.HDC
u.GetDC.argtypes = [w.HWND]
u.ReleaseDC.argtypes = [w.HWND, w.HDC]
u.PrintWindow.argtypes = [w.HWND, w.HDC, w.UINT]
g.CreateCompatibleDC.argtypes = [w.HDC]
g.CreateCompatibleDC.restype = w.HDC
g.CreateCompatibleBitmap.argtypes = [w.HDC, c.c_int, c.c_int]
g.CreateCompatibleBitmap.restype = w.HBITMAP
g.SelectObject.argtypes = [w.HDC, w.HANDLE]
g.SelectObject.restype = w.HANDLE
g.DeleteObject.argtypes = [w.HANDLE]
g.DeleteDC.argtypes = [w.HDC]


class Mouse(c.Structure):
    _fields_ = [('dx', w.LONG), ('dy', w.LONG), ('mouseData', w.DWORD), ('dwFlags', w.DWORD), ('time', w.DWORD), ('extra', c.c_size_t)]


class Key(c.Structure):
    _fields_ = [('vk', w.WORD), ('scan', w.WORD), ('flags', w.DWORD), ('time', w.DWORD), ('extra', c.c_size_t)]


class InputValue(c.Union):
    _fields_ = [('mouse', Mouse), ('key', Key)]


class Input(c.Structure):
    _fields_ = [('type', w.DWORD), ('value', InputValue)]


class BitmapHeader(c.Structure):
    _fields_ = [('size', w.DWORD), ('width', w.LONG), ('height', w.LONG), ('planes', w.WORD),
                ('bits', w.WORD), ('compression', w.DWORD), ('imageSize', w.DWORD),
                ('xppm', w.LONG), ('yppm', w.LONG), ('used', w.DWORD), ('important', w.DWORD)]


g.GetDIBits.argtypes = [w.HDC, w.HBITMAP, w.UINT, w.UINT, c.c_void_p, c.POINTER(BitmapHeader), w.UINT]


def pid_of(hwnd):
    process = w.DWORD()
    u.GetWindowThreadProcessId(hwnd, c.byref(process))
    return process.value


def focus_window(hwnd, pid):
    """Attach the automation thread while requesting focus, then keep the existing PID guard."""
    thread = c.windll.kernel32.GetCurrentThreadId()
    foreground = u.GetForegroundWindow()
    foreground_thread = u.GetWindowThreadProcessId(foreground, None) if foreground else 0
    attached = []
    try:
        target_thread = u.GetWindowThreadProcessId(hwnd, None)
        for other in set((foreground_thread, target_thread)):
            if other and other != thread and u.AttachThreadInput(thread, other, True):
                attached.append(other)
        u.ShowWindow(hwnd, 9)
        u.BringWindowToTop(hwnd)
        u.SetForegroundWindow(hwnd)
        u.SetFocus(hwnd)
        time.sleep(.03)
    finally:
        for other in attached:
            u.AttachThreadInput(thread, other, False)
    guard(pid, hwnd)


def window(pid, editor_game_view=False):
    matches = []
    callback_type = c.WINFUNCTYPE(w.BOOL, w.HWND, w.LPARAM)
    @callback_type
    def callback(hwnd, _):
        if pid_of(hwnd) == pid:
            # A candidate launched with Start-Process -WindowStyle Hidden still
            # needs to be found before focus_window can restore it for input.
            if not u.IsWindowVisible(hwnd):
                if editor_game_view: return True
                window_class = c.create_unicode_buffer(128)
                u.GetClassNameW(hwnd, window_class, len(window_class))
                if window_class.value != 'UnityWndClass': return True
            title = c.create_unicode_buffer(512)
            u.GetWindowTextW(hwnd, title, len(title))
            matches.append((hwnd, title.value))
        return True
    u.EnumWindows(callback, 0)
    candidates = [h for h, title in matches if (title == 'Game' if editor_game_view else 'Project_PA' in title)]
    if len(candidates) != 1:
        raise RuntimeError('Expected one Project_PA player window: ' + repr(matches))
    return candidates[0]


def guard(pid, hwnd=None):
    foreground = u.GetForegroundWindow()
    if pid_of(foreground) != pid or (hwnd is not None and foreground != hwnd):
        raise RuntimeError('Target player lost foreground; no input sent')


def native_window_observation(pid, editor_game_view=False):
    hwnd=window(pid,editor_game_view);rect=w.RECT()
    if not u.GetClientRect(hwnd,c.byref(rect)) or u.IsIconic(hwnd): raise RuntimeError('Target client unavailable')
    title=c.create_unicode_buffer(512);u.GetWindowTextW(hwnd,title,len(title))
    kernel=c.windll.kernel32
    kernel.OpenProcess.argtypes=[w.DWORD,w.BOOL,w.DWORD];kernel.OpenProcess.restype=w.HANDLE
    kernel.GetProcessTimes.argtypes=[w.HANDLE]+[c.POINTER(w.FILETIME)]*4
    kernel.CloseHandle.argtypes=[w.HANDLE]
    handle=kernel.OpenProcess(0x1000,False,pid)
    if not handle: raise RuntimeError('Target process identity unknown')
    try:
        times=[w.FILETIME() for _ in range(4)]
        if not kernel.GetProcessTimes(handle,*[c.byref(x) for x in times]):raise RuntimeError('Target creation time unknown')
        created=str((times[0].dwHighDateTime<<32)|times[0].dwLowDateTime)
    finally:kernel.CloseHandle(handle)
    u.GetAsyncKeyState.restype=c.c_short
    released=not any(u.GetAsyncKeyState(k)&0x8000 for k in range(1,256))
    return {'window':int(hwnd),'windowPid':pid_of(hwnd),'windowTitle':title.value,
            'clientWidth':rect.right,'clientHeight':rect.bottom,'processCreationFileTime':created,'inputReleased':released}


def key_event(scan, up=False):
    send(Input(1,InputValue(key=Key(0,scan,10 if up else 8,0,0))))


def hold_keys(keys,duration):
    pressed=[]
    try:
        for key in keys:
            scan=u.MapVirtualKeyW(key,0);pressed.append(scan);key_event(scan)
        time.sleep(duration)
    finally:
        errors=[]
        for scan in reversed(pressed):
            try:key_event(scan,True)
            except Exception as exc:errors.append(type(exc).__name__)
        if errors:raise RuntimeError('Input release incomplete; inspect this operation before any new input')


def mouse_event(up=False):
    send(Input(0,InputValue(mouse=Mouse(0,0,0,4 if up else 2,0,0))))


def click_button():
    try:
        mouse_event();time.sleep(.12)
    finally:mouse_event(True)


def send(event):
    if u.SendInput(1, c.byref(event), c.sizeof(Input)) != 1:
        raise RuntimeError('OS rejected input')


def record(pid, action, detail):
    LOG.mkdir(parents=True, exist_ok=True)
    event = {'time': datetime.datetime.now().astimezone().isoformat(), 'pid': pid, 'action': action, **detail}
    with (LOG / 'input-events.jsonl').open('a', encoding='utf-8') as target:
        target.write(json.dumps(event, ensure_ascii=False) + '\n')
    print(json.dumps(event, ensure_ascii=True))


def capture(hwnd, label):
    if not label.replace('-', '').replace('_', '').isalnum():
        raise RuntimeError('Use a plain capture label')
    destination = LOG / (label + '.png')
    if destination.exists():
        raise RuntimeError('Do not overwrite evidence')
    rect = w.RECT()
    if not u.GetClientRect(hwnd, c.byref(rect)):
        raise RuntimeError('Client bounds unavailable')
    width, height = rect.right, rect.bottom
    if width < 100 or height < 100:
        raise RuntimeError('Player is minimized; restore before capture')
    dc = u.GetDC(hwnd)
    memory = g.CreateCompatibleDC(dc)
    bitmap = g.CreateCompatibleBitmap(dc, width, height)
    previous = g.SelectObject(memory, bitmap)
    try:
        if not u.PrintWindow(hwnd, memory, 3):
            raise RuntimeError('Native client capture failed')
        pixels = c.create_string_buffer(width * height * 4)
        header = BitmapHeader(c.sizeof(BitmapHeader), width, -height, 1, 32, 0, width * height * 4, 0, 0, 0, 0)
        if g.GetDIBits(memory, bitmap, 0, height, pixels, c.byref(header), 0) != height:
            raise RuntimeError('Native pixel read failed')
        raw = pixels.raw
        rows = []
        stride = width * 4
        for y in range(height):
            bgra = raw[y * stride:(y + 1) * stride]
            rgb = bytearray(width * 3)
            rgb[0::3], rgb[1::3], rgb[2::3] = bgra[2::4], bgra[1::4], bgra[0::4]
            rows.append(b'\0' + rgb)
        def chunk(kind, data):
            return struct.pack('>I', len(data)) + kind + data + struct.pack('>I', zlib.crc32(kind + data) & 0xffffffff)
        png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 2, 0, 0, 0))
        png += chunk(b'IDAT', zlib.compress(b''.join(rows), 6)) + chunk(b'IEND', b'')
        LOG.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(png)
        return {'path': destination.relative_to(ROOT).as_posix(), 'client': [width, height], 'nativeCapture': True}
    finally:
        g.SelectObject(memory, previous)
        g.DeleteObject(bitmap)
        g.DeleteDC(memory)
        u.ReleaseDC(hwnd, dc)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--pid', type=int, required=True)
    parser.add_argument('--editor-game-view', action='store_true', help='Target the actual floating Game View of this Editor PID')
    parser.add_argument('action', choices=['focus', 'shot', 'key', 'click', 'state'])
    parser.add_argument('values', nargs='*')
    args = parser.parse_args()
    hwnd = window(args.pid, args.editor_game_view)
    if args.action in ('focus','key','click') and native_window_observation(args.pid,args.editor_game_view)['inputReleased'] is not True:
        raise RuntimeError('Held input is not owned by this operation; no input sent')
    if args.action == 'focus':
        focus_window(hwnd, args.pid)
        record(args.pid, 'focus', {'window': hwnd, 'foreground': True})
    elif args.action == 'shot':
        record(args.pid, 'shot', capture(hwnd, args.values[0]))
    elif args.action == 'state':
        record(args.pid, 'state', {'window': hwnd, 'foregroundPid': pid_of(u.GetForegroundWindow()), 'iconic': bool(u.IsIconic(hwnd))})
    elif args.action == 'key':
        guard(args.pid, hwnd)
        aliases = {'ESC': 27, 'SPACE': 32, 'ENTER': 13, 'TAB': 9, 'SHIFT': 16}
        keys = [aliases[key] if key in aliases else ord(key) for key in args.values[0].upper().split('+')]
        duration = float(args.values[1]) if len(args.values) > 1 else .12
        if not 0 < duration <= 10 or any(not 0 < key < 256 for key in keys):
            raise RuntimeError('Invalid key duration/code')
        hold_keys(keys,duration)
        record(args.pid, 'key', {'keys': args.values[0], 'duration': duration, 'input': 'Windows SendInput scancode'})
    else:
        guard(args.pid, hwnd)
        point = w.POINT(int(args.values[0]), int(args.values[1]))
        rect = w.RECT(); u.GetClientRect(hwnd, c.byref(rect))
        if not 0 <= point.x < rect.right or not 0 <= point.y < rect.bottom:
            raise RuntimeError('Click outside client bounds')
        u.ClientToScreen(hwnd, c.byref(point)); u.SetCursorPos(point.x, point.y)
        click_button()
        record(args.pid, 'click', {'clientPosition': args.values, 'input': 'Windows SendInput'})


if __name__ == '__main__':
    main()

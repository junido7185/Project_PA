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


def window(pid):
    matches = []
    callback_type = c.WINFUNCTYPE(w.BOOL, w.HWND, w.LPARAM)
    @callback_type
    def callback(hwnd, _):
        if pid_of(hwnd) == pid and u.IsWindowVisible(hwnd):
            title = c.create_unicode_buffer(512)
            u.GetWindowTextW(hwnd, title, len(title))
            matches.append((hwnd, title.value))
        return True
    u.EnumWindows(callback, 0)
    candidates = [h for h, title in matches if 'Project_PA' in title]
    if len(candidates) != 1:
        raise RuntimeError('Expected one Project_PA player window: ' + repr(matches))
    return candidates[0]


def guard(pid):
    if pid_of(u.GetForegroundWindow()) != pid:
        raise RuntimeError('Target player lost foreground; no input sent')


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
    parser.add_argument('action', choices=['focus', 'shot', 'key', 'click', 'state'])
    parser.add_argument('values', nargs='*')
    args = parser.parse_args()
    hwnd = window(args.pid)
    if args.action == 'focus':
        u.ShowWindow(hwnd, 9)
        u.SetForegroundWindow(hwnd)
        time.sleep(.25)
        guard(args.pid)
        record(args.pid, 'focus', {'window': hwnd, 'foreground': True})
    elif args.action == 'shot':
        record(args.pid, 'shot', capture(hwnd, args.values[0]))
    elif args.action == 'state':
        record(args.pid, 'state', {'window': hwnd, 'foregroundPid': pid_of(u.GetForegroundWindow()), 'iconic': bool(u.IsIconic(hwnd))})
    elif args.action == 'key':
        guard(args.pid)
        aliases = {'ESC': 27, 'SPACE': 32, 'ENTER': 13, 'TAB': 9, 'SHIFT': 16}
        keys = [aliases[key] if key in aliases else ord(key) for key in args.values[0].upper().split('+')]
        duration = float(args.values[1]) if len(args.values) > 1 else .12
        if not 0 < duration <= 10 or any(not 0 < key < 256 for key in keys):
            raise RuntimeError('Invalid key duration/code')
        pressed = []
        try:
            for key in keys:
                scan = u.MapVirtualKeyW(key, 0)
                send(Input(1, InputValue(key=Key(0, scan, 8, 0, 0))))
                pressed.append(scan)
            time.sleep(duration)
        finally:
            for scan in reversed(pressed):
                send(Input(1, InputValue(key=Key(0, scan, 10, 0, 0))))
        record(args.pid, 'key', {'keys': args.values[0], 'duration': duration, 'input': 'Windows SendInput scancode'})
    else:
        guard(args.pid)
        point = w.POINT(int(args.values[0]), int(args.values[1]))
        rect = w.RECT(); u.GetClientRect(hwnd, c.byref(rect))
        if not 0 <= point.x < rect.right or not 0 <= point.y < rect.bottom:
            raise RuntimeError('Click outside client bounds')
        u.ClientToScreen(hwnd, c.byref(point)); u.SetCursorPos(point.x, point.y)
        send(Input(0, InputValue(mouse=Mouse(0, 0, 0, 2, 0, 0))))
        time.sleep(.12)
        send(Input(0, InputValue(mouse=Mouse(0, 0, 0, 4, 0, 0))))
        record(args.pid, 'click', {'clientPosition': args.values, 'input': 'Windows SendInput'})


if __name__ == '__main__':
    main()

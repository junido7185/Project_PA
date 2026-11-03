"""Process inactivity detection, independent of task duration or attempt budgets.

Output/owned-file observations renew activity; periodic snapshots and quota polls do not.
This detects a stalled transport, not gameplay progress or successful verification.
"""
import math
from pathlib import Path
import subprocess
import time


class InactivityWatchdog:
    def __init__(self, seconds, clock=None):
        if not math.isfinite(seconds) or seconds <= 0:
            raise ValueError('Inactivity timeout must be finite and positive')
        self.seconds, self.clock = seconds, clock or time.monotonic
        self.last_activity = self.clock()

    def touch(self):
        self.last_activity = self.clock()

    def expired(self):
        return self.clock() - self.last_activity >= self.seconds


def file_observation(paths):
    result = []
    for path in paths:
        try:
            s = Path(path).stat()
            result.append((str(path), s.st_size, s.st_mtime_ns))
        except FileNotFoundError:
            result.append((str(path), None, None))
    return tuple(result)


class StopRequested(Exception):
    """A caller-owned stop condition (for example paid overage) fired; termination stays with the owner."""


def communicate_active(process, payload, *, idle_seconds, paths=(), checkpoint=None,
                       checkpoint_seconds=120, clock=None, poll_seconds=1, stop_when=None):
    """Keep a progressing subprocess alive. On a stall, leave termination to its owner.

    Callers must preserve/check the exact child and in-flight operation before replay.
    Logs must be real child output, not observer-generated heartbeat/snapshot files.
    """
    clock = clock or time.monotonic
    watch = InactivityWatchdog(idle_seconds, clock)
    previous = file_observation(paths)
    last_checkpoint = clock()
    while True:
        try:
            return process.communicate(payload, timeout=min(poll_seconds, watch.seconds))
        except subprocess.TimeoutExpired:
            payload = None
            if stop_when and stop_when():
                raise StopRequested('Caller stop condition observed; preserve output and stop the exact child')
            current = file_observation(paths)
            if current != previous:
                watch.touch()
                previous = current
            if checkpoint and clock() - last_checkpoint >= checkpoint_seconds:
                checkpoint()
                last_checkpoint = clock()
            if watch.expired():
                raise subprocess.TimeoutExpired(process.args, idle_seconds,
                    stderr='No child output or owned-file activity; preserve checkpoint and diagnose')

"""Small stdio client for the installed Codex app-server.

No launcher, state store, credential store, or automatic model turn is created.
The owner decides when to start/interrupt its isolated work. Usage reads never
create or resume a thread. Do not log raw config/account responses.
"""
from __future__ import annotations

import datetime as dt
import hashlib
import json
import math
import os
from pathlib import Path
import queue
import re
import shutil
import subprocess
import threading
import time
import traceback


class CodexBridgeError(RuntimeError):
    """A sanitized protocol/runtime failure; raw account/config text is private."""

    def __init__(self, category, message, code=None, method=None):
        super().__init__(message)
        self.category = category
        self.code = code
        self.method = method


def resolve_codex_executable():
    """Reuse an installed native CLI when a Claude supervisor lacks the IDE PATH.

    No installs, environment changes, credentials, thread or model calls.
    Only the user's known VS Code extension roots are considered as a fallback.
    """
    executable = shutil.which('codex')
    if executable:
        return executable
    if os.name == 'nt':
        candidates = []
        for extensions in (Path.home()/'.vscode/extensions', Path.home()/'.vscode-insiders/extensions'):
            for binary in extensions.glob('openai.chatgpt-*/bin/windows-x86_64/codex.exe'):
                try:
                    binary.resolve().relative_to(extensions.resolve())
                    if binary.is_file():
                        candidates.append(binary)
                except (OSError, ValueError):
                    continue
        def version_key(path):
            return tuple(int(x) for x in re.findall(r'\d+', path.parents[2].name))
        for binary in sorted(candidates, key=version_key, reverse=True):
            try:
                check = subprocess.run([str(binary), '--version'], capture_output=True, text=True,
                    timeout=5, creationflags=getattr(subprocess, 'CREATE_NO_WINDOW', 0))
                if check.returncode == 0 and re.fullmatch(r'codex-cli\s+\S+\s*', check.stdout):
                    return str(binary)
            except (OSError, subprocess.TimeoutExpired):
                continue
    raise CodexBridgeError('cli_unavailable', 'Codex executable was not found', method='executable/resolve')


def failure_observation(exc, stage):
    """Keep exact program-controlled bridge messages and stack locations, no raw RPC data."""
    known = isinstance(exc, CodexBridgeError)
    return {'observedAt': dt.datetime.now(dt.timezone.utc).isoformat(), 'status': 'ERROR',
            'stage': stage, 'type': type(exc).__name__, 'category': exc.category if known else 'unexpected_error',
            'message': str(exc) if known else 'Unexpected provider failure; inspect listed code location',
            'code': exc.code if known else None, 'method': exc.method if known else None,
            'frames': [{'file': Path(f.filename).name, 'line': f.lineno, 'function': f.name}
                       for f in traceback.extract_tb(exc.__traceback__)],
            'claudeResumeBlocked': False, 'codexRelayAttemptSkipped': True,
            'writerTransfer': 'separate_adapter_gates_not_executed', 'gameCompletion': False}


def _error_category(message):
    text = str(message).lower()
    if any(word in text for word in ["rate limit", "rate_limit", "usage limit", "quota", "credits depleted"]):
        return "usage_limit"
    if any(word in text for word in ["unauthorized", "authentication", "not authenticated", "login required"]):
        return "authentication_required"
    if any(word in text for word in ["read-only", "read only", "blocked by policy", "permission denied"]):
        return "runtime_permissions"
    if any(word in text for word in ["config", "missing field", "unknown field"]):
        return "configuration"
    return "rpc_error"


class CodexAppServer:
    """Own one fresh stdio server with plugins, hooks, apps and MCP disabled.

    request() returns the RPC result and raises CodexBridgeError on failure.
    next_event() returns a queued notification/server request or None on timeout.
    It does not attach to a shared daemon or implicitly resume any thread.
    """

    def __init__(self, cwd):
        self.cwd = Path(cwd).resolve()
        if not self.cwd.is_dir():
            raise ValueError("Codex app-server working directory does not exist")
        self._process = None
        self._responses = queue.Queue()
        self._events = queue.Queue()
        self._request_lock = threading.Lock()
        self._sequence = 0
        self._closed = True
        self._verified = False
        self._stderr_count = 0
        self._stderr_digest = hashlib.sha256()
        self._diag_lock = threading.Lock()
        self._notification_methods = set()
        self.disabled_mcp_servers = []

    def __enter__(self):
        try:
            # An empty CLI TOML table merges with inherited servers. Discover the
            # effective keys using read endpoints before any thread is created.
            self._launch([])
            config = self.request("config/read", {"cwd": str(self.cwd), "includeLayers": False})
            servers = (config.get("config") or {}).get("mcp_servers") or {}
            if not isinstance(servers, dict):
                raise CodexBridgeError("configuration", "Invalid effective MCP configuration shape")
            names = sorted(servers)
            if any(not re.fullmatch(r"[A-Za-z0-9_-]+", name) for name in names):
                raise CodexBridgeError("configuration", "MCP server identifier needs unsupported dotted-key quoting")
            self.close()
            self._launch(names)
            effective = self.request("config/read", {"cwd": str(self.cwd), "includeLayers": False})
            current = (effective.get("config") or {}).get("mcp_servers") or {}
            if any(not isinstance(item, dict) or item.get("enabled", True) is not False for item in current.values()):
                raise CodexBridgeError("configuration", "An effective MCP entry remained enabled; no thread may start")
            if sorted(current) != names:
                raise CodexBridgeError("configuration", "Effective MCP configuration changed during isolation setup")
            features = (effective.get("config") or {}).get("features") or {}
            if any(features.get(key) is not False for key in ["plugins", "apps", "hooks", "multi_agent"]):
                raise CodexBridgeError("configuration", "Optional runtime capabilities remained enabled")
            self.disabled_mcp_servers = names
            self._verified = True
            return self
        except Exception:
            self.close()
            raise

    def __exit__(self, exc_type, exc, tb):
        self.close()

    def _launch(self, server_names):
        executable = resolve_codex_executable()
        # app-server 0.160.1 has no --ignore-user-config flag. Default compatible
        # parsing retains existing auth and requirements without changing them.
        command = [executable, "-C", str(self.cwd), "app-server", "--listen", "stdio://"]
        for value in ["mcp_servers={}", "features.plugins=false", "features.apps=false", "features.hooks=false", "features.multi_agent=false", "analytics.enabled=false"]:
            command.extend(["-c", value])
        for name in server_names:
            command.extend(["-c", "mcp_servers." + name + ".enabled=false"])
        self._responses = queue.Queue()
        self._events = queue.Queue()
        self._process = subprocess.Popen(
            command, cwd=self.cwd, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.PIPE, text=True, encoding="utf-8", errors="replace",
            creationflags=getattr(subprocess, "CREATE_NO_WINDOW", 0),
        )
        self._closed = False
        threading.Thread(target=self._read_stdout, args=(self._process,), daemon=True).start()
        threading.Thread(target=self._read_stderr, args=(self._process,), daemon=True).start()
        self.request("initialize", {
            "clientInfo": {"name": "project_pa_production", "title": "Project PA Production Relay", "version": "0.1.0"},
            "capabilities": {"experimentalApi": True},
        })
        self._send({"method": "initialized", "params": {}})

    def _read_stdout(self, process):
        responses = self._responses
        events = self._events
        for line in process.stdout:
            try:
                message = json.loads(line)
            except (ValueError, TypeError):
                continue
            if not isinstance(message, dict):
                continue
            if "id" in message and "method" not in message:
                responses.put(message)
            else:
                method = message.get("method")
                if method:
                    with self._diag_lock:
                        self._notification_methods.add(method)
                # These notifications may expose account/config details and are
                # irrelevant to the production event stream.
                if method in {"account/updated", "configWarning"}:
                    events.put({"method": method})
                else:
                    events.put(message)
        responses.put({"_processEnded": True})
        events.put({"method": "bridge/processEnded", "params": {"exitCode": process.poll()}})

    def _read_stderr(self, process):
        for line in process.stderr:
            with self._diag_lock:
                self._stderr_count += 1
                self._stderr_digest.update(line.encode("utf-8", errors="replace"))

    def _send(self, message):
        if self._closed or self._process is None or self._process.poll() is not None:
            raise CodexBridgeError("process_exited", "Owned app-server process is not running")
        try:
            self._process.stdin.write(json.dumps(message, ensure_ascii=False) + "\n")
            self._process.stdin.flush()
        except (BrokenPipeError, OSError, ValueError) as exc:
            raise CodexBridgeError("process_exited", "Owned app-server stdin was closed") from exc

    def request(self, method, params=None, timeout=20):
        """Send an explicit RPC; initialization and quota reads use no model.

        Destructive/auth/config/credit methods are intentionally unavailable.
        The parent holds file ownership and must validate any thread it resumes.
        """
        read_methods = {"initialize", "account/rateLimits/read", "config/read", "configRequirements/read", "permissionProfile/list", "thread/read", "thread/turns/list", "thread/items/list", "model/list"}
        thread_methods = {"thread/start", "thread/resume", "thread/fork", "thread/unsubscribe", "turn/start", "turn/steer", "turn/interrupt"}
        if method not in read_methods | thread_methods:
            raise CodexBridgeError("method_not_allowed", "App-server method is outside the production bridge scope")
        if method in thread_methods and not self._verified:
            raise CodexBridgeError("isolation_unverified", "Thread operations require verified MCP/runtime isolation")
        if not isinstance(timeout, (int, float)) or isinstance(timeout, bool) or not math.isfinite(timeout) or timeout <= 0 or timeout > 300:
            raise ValueError("RPC timeout must be positive and at most 300 seconds")
        with self._request_lock:
            self._sequence += 1
            request_id = self._sequence
            request = {"id": request_id, "method": method}
            if params is not None:
                request["params"] = params
            self._send(request)
            deadline = time.monotonic() + timeout
            while True:
                remaining = deadline - time.monotonic()
                if remaining <= 0:
                    raise CodexBridgeError("timeout", "Codex app-server RPC timed out: " + method, method=method)
                try:
                    response = self._responses.get(timeout=remaining)
                except queue.Empty as exc:
                    raise CodexBridgeError("timeout", "Codex app-server RPC timed out: " + method, method=method) from exc
                if response.get("_processEnded"):
                    raise CodexBridgeError("process_exited", "Owned app-server ended before its RPC response")
                if response.get("id") != request_id:
                    continue  # Late response to a previously timed-out request.
                if "error" in response:
                    error = response.get("error") or {}
                    category = _error_category(error.get("message"))
                    raise CodexBridgeError(category, "Codex app-server RPC failed: " + method + " (" + category + ")", error.get("code"), method=method)
                return response.get("result") or {}

    def next_event(self, timeout=20):
        if not isinstance(timeout, (int, float)) or isinstance(timeout, bool) or not math.isfinite(timeout) or timeout < 0 or timeout > 300:
            raise ValueError("Event timeout must be between zero and 300 seconds")
        try:
            return self._events.get(timeout=timeout)
        except queue.Empty:
            return None

    @property
    def diagnostics(self):
        with self._diag_lock:
            return {"stderrLineCount": self._stderr_count, "stderrSha256": self._stderr_digest.hexdigest(), "notificationMethods": sorted(self._notification_methods), "disabledMcpServers": list(self.disabled_mcp_servers), "isolationVerified": self._verified}

    def close(self):
        """Close only this object's fresh process; never stop an existing daemon."""
        process = self._process
        self._closed = True
        self._verified = False
        if process is None:
            return
        try:
            if process.stdin and not process.stdin.closed:
                process.stdin.close()
            if process.poll() is None:
                try:
                    process.wait(timeout=3)
                except subprocess.TimeoutExpired:
                    process.terminate()
                    try:
                        process.wait(timeout=3)
                    except subprocess.TimeoutExpired:
                        process.kill()
                        process.wait(timeout=3)
        finally:
            self._process = None


def normalize_usage(result, observed_at=None):
    """Project only quota facts. Never return account IDs, tokens or credits."""
    observed_at = observed_at or dt.datetime.now(dt.timezone.utc).isoformat()
    allowed = result.get("ordinaryUsageAllowed")
    if allowed is not None and not isinstance(allowed, bool):
        allowed = None
    normalized = {"source": "codex_app_server", "observedAt": observed_at, "ordinaryUsageAllowed": allowed, "rateLimits": [], "blockReasonCode": None}
    buckets = result.get("rateLimitsByLimitId")
    if not isinstance(buckets, dict) or not buckets:
        legacy = result.get("rateLimits")
        buckets = {legacy.get("limitId") or "codex": legacy} if isinstance(legacy, dict) else {}
    for limit_id, bucket in buckets.items():
        if not isinstance(bucket, dict):
            continue
        if bucket.get("rateLimitReachedType") in {"rate_limit_reached", "workspace_owner_credits_depleted", "workspace_member_credits_depleted", "workspace_owner_usage_limit_reached", "workspace_member_usage_limit_reached"}:
            normalized["blockReasonCode"] = bucket["rateLimitReachedType"]
        if bucket.get("spendControlReached") is True:
            normalized["blockReasonCode"] = "spend_control_reached"
        for kind in ["primary", "secondary"]:
            window = bucket.get(kind)
            if not isinstance(window, dict):
                continue
            percent = window.get("usedPercent")
            if isinstance(percent, bool) or not isinstance(percent, (int, float)) or not math.isfinite(percent):
                continue
            reset = window.get("resetsAt")
            try:
                reset_iso = dt.datetime.fromtimestamp(reset, dt.timezone.utc).isoformat() if isinstance(reset, (int, float)) and not isinstance(reset, bool) else None
            except (OverflowError, OSError, ValueError):
                reset_iso = None
            normalized["rateLimits"].append({"limitId": limit_id, "kind": kind, "percentUsed": percent, "windowDurationMins": window.get("windowDurationMins"), "resetsAt": reset_iso})
    if normalized["ordinaryUsageAllowed"] is False and not normalized["blockReasonCode"]:
        normalized["blockReasonCode"] = "ordinary_usage_not_allowed"
    # None is unknown. Neither low percentages nor elapsed reset times prove
    # recovery; the scheduler must retain that distinction.
    return normalized


def read_usage(cwd):
    """Model-free authenticated read using the existing installed CLI account."""
    with CodexAppServer(cwd) as server:
        result = server.request("account/rateLimits/read", {"excludeResetCreditDetails": True, "supportsLunaReserve": False})
        return normalize_usage(result)

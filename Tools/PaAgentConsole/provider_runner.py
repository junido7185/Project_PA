"""Finite, subscription-oriented CLI fallback for read-only support work.

This is not a production game-development supervisor. It neither grants work
approval nor changes loop-state, Unity ownership, budgets or CODEX_HANDOFF.
Claude plan mode is a requested policy, not an OS-level file-write guarantee.
The local PC still needs its owner/approval/gate integration before production.
"""
from __future__ import annotations

import argparse
from dataclasses import asdict, dataclass, field
from datetime import datetime, timezone
import hashlib
import json
import math
import os
from pathlib import Path
import queue
import re
import shutil
import subprocess
import sys
import threading
import time
import tomllib
from typing import Callable, Mapping, Sequence
import uuid


PROVIDERS = ("claude", "codex")
FALLBACK_REASONS = {"QUOTA_EXHAUSTED", "AUTH_REQUIRED", "AUTH_UNVERIFIED", "AUTH_ROUTE_REJECTED",
                    "CLI_NOT_FOUND", "CLI_SHIM_UNSUPPORTED", "CLI_ENTRYPOINT_UNSUPPORTED", "CLI_START_FAILED",
                    "NETWORK_FAILURE", "RATE_LIMIT_UNCONFIRMED"}
CHILD_FLAG = "PA_AGENT_CONSOLE_CHILD"
SUPPORT_SCOPE = "support-read-only"
MAX_DISPATCHES = 2
MAX_LINE = 262144
DEFAULT_LOG_ROOT = Path(__file__).resolve().parents[2] / "Logs" / "AgentConsole"


class PolicyError(ValueError):
    pass


class DiscoveryError(RuntimeError):
    def __init__(self, reason: str):
        super().__init__(reason)
        self.reason = reason


def utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


class Redactor:
    """Remove inherited secret values without ever returning their names/values."""

    def __init__(self, environment: Mapping[str, str]):
        self.secrets = sorted({value for key, value in environment.items()
                               if len(value) >= 6 and re.search(
                                   r"KEY|TOKEN|SECRET|PASSWORD|CREDENTIAL|AUTHORIZATION", key, re.I)},
                              key=len, reverse=True)

    def text(self, value: str) -> str:
        for secret in self.secrets:
            value = value.replace(secret, "[REDACTED]")
        value = re.sub(r"(?i)\bBearer\s+\S+", "Bearer [REDACTED]", value)
        value = re.sub(r"\bsk-[A-Za-z0-9_-]{8,}", "[REDACTED]", value)
        value = re.sub(r'(?i)(["\']?(?:access_token|refresh_token|api_key|authorization)["\']?\s*[:=]\s*)(["\']?)[^\s,}\n]+',
                       r"\1[REDACTED]", value)
        return value

    def value(self, value):
        if isinstance(value, str):
            return self.text(value)
        if isinstance(value, list):
            return [self.value(item) for item in value]
        if isinstance(value, dict):
            return {str(key): ("[REDACTED]" if re.search(
                r"^(?:access_token|refresh_token|api_key|authorization|password|secret)$", str(key), re.I)
                else self.value(item)) for key, item in value.items()}
        return value


def child_environment(source: Mapping[str, str]) -> dict[str, str]:
    """Only sanitize the new process environment; never alter the user's globals."""
    exact = {"OPENAI_API_KEY", "CODEX_API_KEY", "OPENAI_BASE_URL", "OPENAI_ORG_ID",
             "OPENAI_ORGANIZATION", "OPENAI_PROJECT_ID", "CLAUDE_CODE_OAUTH_TOKEN",
             "CLAUDE_CODE_USE_BEDROCK", "CLAUDE_CODE_USE_VERTEX", "CLAUDE_CODE_USE_FOUNDRY",
             "CLAUDE_CODE_API_KEY_HELPER_TTL_MS"}
    prefixes = ("ANTHROPIC_", "AZURE_OPENAI_", "AWS_", "BEDROCK_", "VERTEX_",
                "GOOGLE_APPLICATION_CREDENTIALS", "AZURE_AI_")
    result = {key: value for key, value in source.items()
              if key.upper() not in exact and not key.upper().startswith(prefixes)}
    result[CHILD_FLAG] = "1"
    return result


def authority_snapshot(project_root: Path) -> dict:
    path = project_root / "Automation/LoopEngineering/State/loop-state.json"
    raw = path.read_bytes()
    state = json.loads(raw.decode("utf-8-sig"))
    route = state.get("demoProductionRouting", {})
    selected = route.get("selectedApproval")
    approval = state.get(selected, {}) if isinstance(selected, str) else {}
    ticket = approval.get("activeTicket", route.get("activeTicket"))
    result = approval.get("results", {}).get(ticket, {})
    handoff = project_root / "Docs/00_CURRENT/CODEX_HANDOFF.md"
    return {
        "authorityPath": str(path), "authoritySha256": digest(raw),
        "handoffPath": str(handoff),
        "handoffSha256": digest(handoff.read_bytes()) if handoff.is_file() else None,
        "selectedApproval": selected, "activeTicket": ticket,
        "approvalStatus": approval.get("status", "UNKNOWN"),
        "ticketStatus": approval.get("ticketStatus", {}).get(ticket, "UNKNOWN"),
        "unityEditorOwner": route.get("unityEditorOwner"),
        "stopAtMilestoneReached": route.get("stopAtMilestoneReached", False),
        "usedBudget": {"compileFix": result.get("compileCycles"),
                       "targetedPlay": result.get("targetedPlayRuns"),
                       "dedicatedValidators": result.get("dedicatedValidatorsAdded")},
        "approvedBudget": result.get("approvedTotalBudget", approval.get("perTicketBudget", {})),
        "productionAllowedByThisRunner": False,
        "note": "Observed task authority only. Provider switching never resets this gate.",
    }


def validate_policy(scope: str, environment: Mapping[str, str]) -> None:
    if scope != SUPPORT_SCOPE:
        raise PolicyError("Only support-read-only is implemented; production dispatch is disabled.")
    if environment.get(CHILD_FLAG):
        raise PolicyError("Nested provider dispatch is forbidden.")


def build_argv(provider: str, command: Sequence[str]) -> list[str]:
    if provider == "codex":
        return [*command, "exec", "--ignore-user-config", "--json", "--sandbox", "read-only",
                "-c", 'forced_login_method="chatgpt"',
                "-c", "features.plugins=false", "-c", "features.apps=false",
                "-c", "mcp_servers={}", "-"]
    if provider == "claude":
        return [*command, "-p", "--output-format", "stream-json", "--verbose",
                "--permission-mode", "plan", "--strict-mcp-config",
                "--tools", "Read,Grep,Glob",
                "--mcp-config", '{"mcpServers":{}}']
    raise PolicyError("Unknown provider.")


def codex_mcp_overrides(project_root: Path, environment: Mapping[str, str]) -> list[str]:
    """Disable configured MCPs, including case-variant aliases, for support children.

    Read configuration keys only. Values, env blocks and credentials are never
    logged. Managed requirements can still reject a run; that is not bypassed.
    """
    codex_directory = Path(environment.get('CODEX_HOME', str(Path.home()/'.codex')))
    paths = [codex_directory/'config.toml']
    paths += [directory/'.codex/config.toml' for directory in [project_root, *project_root.parents]]
    # A disabled table still needs a valid transport. Do not invent absent case aliases.
    names = set()
    for path in paths:
        if not path.is_file():
            continue
        try:
            data = tomllib.loads(path.read_text(encoding='utf-8-sig'))
        except (OSError, ValueError):
            raise DiscoveryError('MCP_CONFIG_UNVERIFIED') from None
        servers = data.get('mcp_servers', {})
        if not isinstance(servers, dict):
            raise DiscoveryError('MCP_CONFIG_UNVERIFIED')
        names.update(servers.keys())
    # Exec can ignore user config while keeping its normal subscription auth. Replace the entire
    # MCP map, rather than making transport-less alias tables for project configs not loaded by exec.
    flags = ['-c', 'mcp_servers={}']
    selected = {}
    for path in paths:
        if path.is_file(): selected.update(tomllib.loads(path.read_text(encoding='utf-8-sig')))
    for key in ('model','model_reasoning_effort'):
        if isinstance(selected.get(key),str): flags += ['-c', key + '=' + json.dumps(selected[key])]
    return flags


def resolve_command(provider: str, override: str | None = None, *, windows: bool | None = None,
                    lookup: Callable[[str], str | None] = shutil.which) -> list[str]:
    """Never run a .cmd/.bat with shell=True or reinterpret its contents."""
    windows = os.name == "nt" if windows is None else windows
    found = override or lookup(provider)
    if not found:
        raise DiscoveryError("CLI_NOT_FOUND")
    path = Path(found).resolve()
    if not path.is_file():
        raise DiscoveryError("CLI_NOT_FOUND")
    suffix = path.suffix.lower()
    if suffix in {".cmd", ".bat"}:
        # Recognize only the fixed installed npm entrypoint, without executing or
        # parsing arbitrary batch text. A different installation fails clearly.
        if not windows or path.stem.lower() != provider:
            raise DiscoveryError("CLI_SHIM_UNSUPPORTED")
        native = path.with_suffix(".exe")
        if native.is_file():
            return [str(native)]
        package = {"codex": ("@openai", "codex", "bin", "codex.js"),
                   "claude": ("@anthropic-ai", "claude-code", "cli.js")}[provider]
        script = (path.parent / "node_modules").joinpath(*package).resolve()
        try:
            script.relative_to((path.parent / "node_modules").resolve())
        except ValueError:
            raise DiscoveryError("CLI_SHIM_UNSUPPORTED") from None
        nearby_node = path.parent / "node.exe"
        node = str(nearby_node) if nearby_node.is_file() else lookup("node.exe")
        if not script.is_file() or not node or not Path(node).is_file() or Path(node).suffix.lower() != ".exe":
            raise DiscoveryError("CLI_SHIM_UNSUPPORTED")
        return [str(Path(node).resolve()), str(script)]
    if suffix in {".ps1", ".sh", ".js"} or (windows and suffix != ".exe"):
        raise DiscoveryError("CLI_ENTRYPOINT_UNSUPPORTED")
    return [str(path)]


def support_prompt(task: str, authority: dict) -> str:
    return (
        "Project P.A. READ-ONLY SUPPORT task. Do not edit repository files, task state, budgets, "
        "Handoff, assets or user settings. Do not launch or control Unity, builds, tests or a game. "
        "Do not install packages, use paid APIs, read credentials, print environment values, "
        "or invoke any other agent/LLM CLI (including through Bash/PowerShell). "
        "Nested dispatch is forbidden. Do not claim production completion or a runtime PASS. "
        "Return findings or a candidate patch as text only, with evidence and unverified limits. "
        "The following gate is observed, not changed. Subscription availability does not grant "
        "task approval or replenish the compile/Play budget.\n"
        + json.dumps(authority, ensure_ascii=False) + "\nUSER SUPPORT REQUEST:\n" + task
    )


@dataclass
class AuthCheck:
    allowed: bool
    status: str
    method: str = "unknown"


def verify_subscription(provider: str, command: Sequence[str], environment: dict[str, str],
                        cwd: Path, *, run_command: Callable = subprocess.run) -> AuthCheck:
    """Use only auth-status metadata. Never save/echo its account or token fields."""
    suffix = ["auth", "status"] if provider == "claude" else ["login", "status"]
    try:
        result = run_command([*command, *suffix], cwd=str(cwd), env=environment,
                             shell=False, capture_output=True, text=True,
                             encoding="utf-8", errors="replace", timeout=15)
    except (OSError, subprocess.TimeoutExpired):
        return AuthCheck(False, "AUTH_UNVERIFIED")
    if provider == "claude":
        try:
            value = json.loads(result.stdout)
        except (ValueError, TypeError):
            return AuthCheck(False, "AUTH_UNVERIFIED")
        if not isinstance(value, dict):
            return AuthCheck(False, "AUTH_UNVERIFIED")
        method = value.get("authMethod")
        known = {"none", "claude.ai", "oauth_token", "api_key", "api_key_helper", "third_party"}
        method = method if method in known else "unknown"
        if result.returncode == 0 and value.get("loggedIn") is True and method == "claude.ai":
            return AuthCheck(True, "SUBSCRIPTION_VERIFIED", method)
        if method in {"api_key", "api_key_helper", "third_party"}:
            return AuthCheck(False, "AUTH_ROUTE_REJECTED", method)
        if method == "none" or value.get("loggedIn") is False:
            return AuthCheck(False, "AUTH_REQUIRED", method)
        # An injected OAuth token is not automatically proven to be a Pro subscription.
        return AuthCheck(False, "AUTH_UNVERIFIED", method)
    text = str(result.stdout) + "\n" + str(result.stderr)
    if result.returncode == 0 and re.search(r"\bAPI\s+key\b", text, re.I):
        return AuthCheck(False, "AUTH_ROUTE_REJECTED", "api_key")
    if result.returncode == 0 and re.search(r"\bChatGPT\b", text, re.I):
        return AuthCheck(True, "SUBSCRIPTION_VERIFIED", "chatgpt")
    if re.search(r"not logged in|login required|please (?:log|sign) in", text, re.I):
        return AuthCheck(False, "AUTH_REQUIRED", "none")
    return AuthCheck(False, "AUTH_UNVERIFIED")


def classify_failure(returncode: int | None, error_text: str, *, timed_out: bool = False,
                     terminal_failed: bool = False) -> str:
    if timed_out:
        return "TIMEOUT"
    if returncode == 0 and not terminal_failed:
        return "SUCCESS"
    text = error_text.lower()
    if re.search(r"credit balance|billing|payment required", text):
        return "BILLING_ROUTE_REJECTED"
    if "insufficient_quota" in text:
        return "QUOTA_TYPE_UNCONFIRMED"
    if re.search(r"usage_limit_reached|quota_exhausted|"
                 r"(?:usage|subscription|weekly|daily|monthly|plan)\s+limit.{0,70}"
                 r"(?:reached|exceeded|exhausted|reset)|you(?:'ve| have) hit your (?:usage )?limit", text):
        return "QUOTA_EXHAUSTED"
    if re.search(r"authentication_error|not logged in|please (?:log|sign) in|login required|"
                 r"authentication required|invalid authentication|expired (?:access )?token|"
                 r"unauthorized|\b401\b", text):
        return "AUTH_REQUIRED"
    if re.search(r"econnreset|econnrefused|enotfound|etimedout|connection refused|"
                 r"network (?:error|timeout)|tls handshake|dns (?:error|failure)|connection error", text):
        return "NETWORK_FAILURE"
    if re.search(r"\b429\b|rate_limit_exceeded|rate limit", text):
        return "RATE_LIMIT_UNCONFIRMED"
    if "max_turns" in text or "maximum number of turns" in text:
        return "TURN_LIMIT_REACHED"
    return "PROVIDER_FAILURE"


@dataclass
class Outcome:
    provider: str
    reason: str
    returncode: int | None = None
    pid: int | None = None
    session_ids: list[str] = field(default_factory=list)
    started_at: str = field(default_factory=utc_now)
    ended_at: str = field(default_factory=utc_now)
    managed_process_exited: bool = True
    descendants_verified: bool = False
    report_path: str | None = None
    detail: str = ""
    authentication: dict = field(default_factory=dict)
    model_dispatched: bool = True
    verified_reset_at: str | None = None


class StreamCollector:
    def __init__(self, directory: Path, provider: str, redactor: Redactor):
        self.directory, self.provider, self.redactor = directory, provider, redactor
        self.buffers = {"stdout": "", "stderr": ""}
        self.stderr = ""
        self.errors: list[str] = []
        self.sessions: list[str] = []
        self.terminal: str | None = None
        self.report: list[str] = []
        self.overage = False
        self.reset_epochs: list[float] = []

    def feed(self, channel: str, chunk: str) -> None:
        self.buffers[channel] += chunk
        while "\n" in self.buffers[channel]:
            line, self.buffers[channel] = self.buffers[channel].split("\n", 1)
            self.line(channel, line[:MAX_LINE])
        if len(self.buffers[channel]) > MAX_LINE:
            self.line(channel, self.buffers[channel][:MAX_LINE] + " [TRUNCATED]")
            self.buffers[channel] = ""

    def line(self, channel: str, raw: str) -> None:
        if not raw.strip():
            return
        try:
            event = json.loads(raw)
        except ValueError:
            event = {"type": "unstructured", "text": raw}
        if not isinstance(event, dict):
            event = {"type": "unstructured", "text": raw}
        kind = str(event.get("type", "unknown"))
        if channel == "stdout":
            session = event.get("thread_id") or event.get("session_id")
            if isinstance(session, str) and session not in self.sessions:
                self.sessions.append(session[:200])
            if kind == "turn.completed":
                self.terminal = "success"
            elif kind == "turn.failed":
                self.terminal = "failed"
                self.errors.append(raw)
            elif kind == "error":
                self.errors.append(raw)
            elif kind == "rate_limit_event":
                info = event.get("rate_limit_info")
                if isinstance(info, dict):
                    if info.get("isUsingOverage") is True:
                        self.overage = True
                    if info.get("status") == "rejected" or info.get("isUsingOverage") is True:
                        windows = [info, *[w for w in (info.get("unifiedWindows") or {}).values()
                                           if isinstance(w, dict) and isinstance(w.get("utilization"), (int, float))
                                           and w["utilization"] >= 1]]
                        self.reset_epochs += [w["resetsAt"] for w in windows
                                              if isinstance(w.get("resetsAt"), (int, float))
                                              and not isinstance(w.get("resetsAt"), bool)
                                              and math.isfinite(w["resetsAt"])]
            elif kind == "result":
                failed = event.get("is_error") is True or str(event.get("subtype", "")).startswith("error")
                self.terminal = "failed" if failed else "success"
                if failed:
                    self.errors.append(raw)
                elif isinstance(event.get("result"), str):
                    self.report.append(event["result"])
            item = event.get("item", {})
            if kind == "item.completed" and isinstance(item, dict) and item.get("type") == "agent_message":
                self.report.append(str(item.get("text", "")))
        if channel == "stderr":
            self.stderr = (self.stderr + "\n" + raw)[-MAX_LINE:]
        record = self.redactor.value({"observedAt": utc_now(), "stream": channel, "event": event})
        with (self.directory / f"{self.provider}.{channel}.jsonl").open("a", encoding="utf-8") as target:
            target.write(json.dumps(record, ensure_ascii=False) + "\n")

    def finish(self) -> None:
        for channel, pending in self.buffers.items():
            if pending:
                self.line(channel, pending[:MAX_LINE])
        if self.report:
            path = self.directory / f"{self.provider}.report.txt"
            path.write_text(self.redactor.text("\n\n".join(self.report)), encoding="utf-8")


def stop_managed_process(process) -> None:
    """Reap only the exact process created by this runner, including on errors."""
    if process.poll() is not None:
        return
    try:
        process.terminate()
        process.wait(timeout=2)
    except subprocess.TimeoutExpired:
        try:
            process.kill()
            process.wait(timeout=2)
        except (OSError, subprocess.TimeoutExpired):
            pass
    except OSError:
        pass


def launch_cli(provider: str, argv: list[str], prompt: str, environment: dict[str, str],
               cwd: Path, timeout: float, directory: Path, redactor: Redactor,
               *, popen_factory: Callable = subprocess.Popen) -> Outcome:
    started = utc_now()
    collector = StreamCollector(directory, provider, redactor)
    try:
        process = popen_factory(argv, cwd=str(cwd), env=environment, shell=False,
                                stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                                text=True, encoding="utf-8", errors="replace")
    except OSError:
        return Outcome(provider, "CLI_START_FAILED", started_at=started, detail="Process creation failed.")
    stop = threading.Event()
    try:
        messages: queue.Queue = queue.Queue(maxsize=128)

        def pump(channel, stream):
            try:
                while not stop.is_set():
                    chunk = stream.readline(4096)
                    if not chunk:
                        break
                    while not stop.is_set():
                        try:
                            messages.put((channel, chunk), timeout=.1)
                            break
                        except queue.Full:
                            continue
            finally:
                # Completion respects the same bounded queue and shutdown event.
                while not stop.is_set():
                    try:
                        messages.put((channel, None), timeout=.1)
                        break
                    except queue.Full:
                        continue

        threads = [threading.Thread(target=pump, args=(name, stream), daemon=True)
                   for name, stream in (("stdout", process.stdout), ("stderr", process.stderr))]
        for thread in threads:
            thread.start()
        # Writing stdin concurrently also bounds a CLI which exits without consuming it.
        def write_prompt():
            try:
                process.stdin.write(prompt)
                process.stdin.close()
            except (OSError, ValueError):
                pass
        writer = threading.Thread(target=write_prompt, daemon=True)
        writer.start()
        ended, timed_out = set(), False
        deadline = time.monotonic() + timeout
        while len(ended) < 2 or process.poll() is None:
            remaining = deadline - time.monotonic()
            if remaining <= 0:
                timed_out = True
                break
            try:
                channel, chunk = messages.get(timeout=min(.1, remaining))
            except queue.Empty:
                continue
            if chunk is None:
                ended.add(channel)
            else:
                collector.feed(channel, chunk)
                deadline = time.monotonic() + timeout
                if collector.overage:
                    # Paid overage is never an automatic route around a subscription limit.
                    break
        if timed_out or collector.overage:
            stop_managed_process(process)
        stop.set()
        while True:
            try:
                channel, chunk = messages.get_nowait()
            except queue.Empty:
                break
            if chunk is not None:
                collector.feed(channel, chunk)
        for thread in threads:
            thread.join(timeout=.2)
        collector.finish()
        code = process.poll()
        failed = collector.terminal == "failed" or (bool(collector.errors) and collector.terminal != "success")
        reason = classify_failure(code, "\n".join(collector.errors) + collector.stderr,
                                  timed_out=timed_out, terminal_failed=failed)
        if reason == 'SUCCESS' and collector.terminal != 'success':
            reason = 'INCOMPLETE_STREAM'
        detail = "CLI result only; GUI/Unity ownership and descendant cleanup are not verified."
        if collector.overage:
            # The plan window was exhausted; paid overage was stopped rather than used.
            reason, detail = "QUOTA_EXHAUSTED", "Paid overage use observed and stopped. " + detail
        future = [epoch for epoch in collector.reset_epochs if epoch / (1000 if epoch > 1e11 else 1) > time.time()]
        reset = (datetime.fromtimestamp(max(future) / (1000 if max(future) > 1e11 else 1), timezone.utc).isoformat()
                 if future and reason == "QUOTA_EXHAUSTED" else None)
        report = directory / f"{provider}.report.txt"
        return Outcome(provider, reason, code, getattr(process, "pid", None), collector.sessions,
                       started, utc_now(), code is not None, False, str(report) if report.exists() else None,
                       detail, verified_reset_at=reset)
    finally:
        stop.set()
        stop_managed_process(process)


def write_json(path: Path, value, redactor: Redactor) -> None:
    temporary = path.with_name(path.name + '.' + uuid.uuid4().hex + '.tmp')
    temporary.write_text(json.dumps(redactor.value(value), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    os.replace(temporary, path)


def execute_job(project_root: Path, task: str, *, primary: str = "claude", run: bool = False,
                scope: str = SUPPORT_SCOPE, log_root: Path = DEFAULT_LOG_ROOT, timeout: float = 300,
                environment: Mapping[str, str] | None = None,
                overrides: Mapping[str, str] | None = None,
                executor: Callable = launch_cli, resolver: Callable | None = None,
                auth_checker: Callable = verify_subscription) -> dict:
    environment = dict(os.environ if environment is None else environment)
    validate_policy(scope, environment)
    if primary not in PROVIDERS or not math.isfinite(timeout) or timeout <= 0:
        raise PolicyError("Invalid provider or inactivity timeout (finite positive seconds required).")
    project_root = Path(project_root).resolve()
    authority = authority_snapshot(project_root)
    redactor = Redactor(environment)
    run_id = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ") + "-" + uuid.uuid4().hex[:10]
    directory = Path(log_root).resolve() / run_id
    directory.mkdir(parents=True, exist_ok=False)
    order = [primary, next(provider for provider in PROVIDERS if provider != primary)]
    observation = {"schemaVersion": 1, "runId": run_id, "scope": scope,
                   "productionAutomation": False, "authoritativeTaskGate": authority,
                   "taskSha256": digest(task.encode("utf-8")), "providerOrder": order,
                   "maxDispatches": MAX_DISPATCHES, "dispatches": [], "stage": "PREPARED",
                   "authentication": "Every run requires a successful subscription auth-status preflight; prepare does not call a CLI.",
                   "guiWritersControlled": False, "childProcessDescendantsVerified": False,
                   "observationDirectory": str(directory),
                   "proposedArgv": {provider: build_argv(provider, [f"<{provider}-cli>"]) for provider in order}}
    # These are operation observations, not a second task queue or a model-authored handoff.
    checkpoint = directory / "checkpoint.json"
    write_json(checkpoint, observation, redactor)
    if not run:
        return observation
    prompt = support_prompt(task, authority)
    child_env = child_environment(environment)
    for provider in order[:MAX_DISPATCHES]:
        current = authority_snapshot(project_root)
        if current != authority:
            observation["stage"] = "AUTHORITY_CHANGED_REVIEW_REQUIRED"
            break
        observation["stage"] = "DISPATCHING"
        observation["currentProvider"] = provider
        write_json(checkpoint, observation, redactor)
        try:
            command = resolver(provider) if resolver else resolve_command(provider, (overrides or {}).get(provider))
            auth = auth_checker(provider, command, child_env, project_root)
            if not auth.allowed:
                outcome = Outcome(provider, auth.status, authentication=asdict(auth), model_dispatched=False)
            elif authority_snapshot(project_root) != authority:
                outcome = Outcome(provider, "AUTHORITY_CHANGED_REVIEW_REQUIRED", model_dispatched=False)
            else:
                argv = build_argv(provider, command)
                if provider == 'codex':
                    # Keep '-' as the final stdin-prompt sentinel.
                    argv = argv[:-1] + codex_mcp_overrides(project_root, child_env) + argv[-1:]
                outcome = executor(provider, argv, prompt, child_env, project_root, timeout, directory, redactor)
                outcome.authentication = asdict(auth)
        except DiscoveryError as error:
            outcome = Outcome(provider, error.reason, model_dispatched=False)
        observation["dispatches"].append(asdict(outcome))
        observation["stage"] = "SUPPORT_COMPLETED" if outcome.reason == "SUCCESS" else outcome.reason
        write_json(checkpoint, observation, redactor)
        if outcome.reason not in FALLBACK_REASONS or not outcome.managed_process_exited:
            break
    observation.pop("currentProvider", None)
    exhausted = [item for item in observation["dispatches"] if item["reason"] == "QUOTA_EXHAUSTED"]
    if len(exhausted) == len(PROVIDERS):
        resets = [item["verified_reset_at"] for item in exhausted]
        # A support run is one-shot: record the wait, never schedule a model retry from here.
        observation["quotaWait"] = {
            "allProvidersExhausted": True,
            "earliestVerifiedResetAt": min(resets) if all(resets) else None,
            "resetsByProvider": {item["provider"]: item["verified_reset_at"] for item in exhausted},
            "automaticResume": False,
            "nextAction": "Re-run this support task manually after a recorded reset. Unknown resets are not guessed; "
                          "a reset grants no task approval and renews no budget."}
    observation["completedAt"] = utc_now()
    observation["modelDispatchCount"] = sum(bool(item["model_dispatched"]) for item in observation["dispatches"])
    observation["requiresPcOwnerApprovalGateIntegrationForProduction"] = True
    write_json(checkpoint, observation, redactor)
    return observation


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--task-file", type=Path, required=True)
    parser.add_argument("--primary", choices=PROVIDERS, default="claude")
    modes = parser.add_mutually_exclusive_group()
    modes.add_argument("--prepare", action="store_true", help="Prepare only (the default); invoke no CLI.")
    modes.add_argument("--run", action="store_true", help="Run read-only support work only.")
    parser.add_argument("--scope", default=SUPPORT_SCOPE)
    parser.add_argument("--timeout-seconds", "--idle-timeout-seconds", dest="timeout_seconds", type=float, default=300, help="Inactivity seconds; progressing work has no total time cap")
    parser.add_argument("--log-root", type=Path, default=DEFAULT_LOG_ROOT)
    parser.add_argument("--claude-cli")
    parser.add_argument("--codex-cli")
    args = parser.parse_args(argv)
    try:
        result = execute_job(args.project_root, args.task_file.read_text(encoding="utf-8-sig"),
                             primary=args.primary, run=args.run, scope=args.scope,
                             log_root=args.log_root, timeout=args.timeout_seconds,
                             overrides={key: value for key, value in
                                        (("claude", args.claude_cli), ("codex", args.codex_cli)) if value})
        print(json.dumps({"runId": result["runId"], "stage": result["stage"],
                          "scope": result["scope"], "productionAutomation": False,
                          "checkpoint": str(Path(result["observationDirectory"]) / "checkpoint.json"),
                          "dispatchCount": len(result["dispatches"])}, ensure_ascii=False))
        return 0 if result["stage"] in {"PREPARED", "SUPPORT_COMPLETED"} else 2
    except (PolicyError, OSError, ValueError) as error:
        # Do not echo arbitrary exception text, task input or environment values.
        print(json.dumps({"stage": "PREPARATION_FAILED", "errorType": type(error).__name__,
                          "productionAutomation": False}))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())

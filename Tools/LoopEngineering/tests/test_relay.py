"""Independent relay isolation and review tests, using only temporary fixtures.

No real loop state, config, process, repository operation, or Unity is touched.
Run: python -B -X utf8 Tools/LoopEngineering/tests/test_relay.py -v
"""
import contextlib
import copy
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

sys.dont_write_bytecode = True
TEST_DIR = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("pa_relay_under_test", TEST_DIR.parent / "Invoke-ProjectPAProduction.py")
production = importlib.util.module_from_spec(spec)
spec.loader.exec_module(production)
SID = "12345678-1234-1234-1234-123456789abc"
SOURCE = "Assets/Scripts/Presentation/CoastalVegetationField.cs"


class RelayFixture(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix=".relay-test-", dir=TEST_DIR)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.logs = self.root / "Logs/worker"
        self.wt = self.logs / "worktree"
        self.wt.mkdir(parents=True)
        self.baseline = "// user's existing dirty work\nconst float Density = 0.75f;\n// keep authority\n"
        self.candidate = self.baseline.replace("0.75f", "0.80f")
        self.put(self.root / SOURCE, self.baseline)
        self.put(self.wt / SOURCE, self.candidate)
        self.put(self.logs / "source-baseline" / SOURCE, self.baseline)
        self.put(self.root / ".codex/config.toml", '''model = "requested-root-model"
sandbox_mode = "danger-full-access"
[mcp_servers.unityMCP]
enabled = true
url = "http://127.0.0.1:8080/mcp"
[mcp_servers.serena]
enabled = true
command = "uvx"
args = ["--project", "C:/original-root"]
[mcp_servers."other-server"]
enabled = true
''')
        self.job = {"id": "relay-fixture", "provider": "codex", "role": "builder",
            "worktree": str(self.wt), "logDirectory": str(self.logs), "status": "ready_for_review",
            "baseHead": "current-local-head", "requestedModel": "gpt-6.1-sol", "requestedEffort": "xhigh",
            "timeoutMinutes": 2, "allowedFiles": [SOURCE],
            "originalHashes": {SOURCE: production.digest(self.root / SOURCE)},
            "worktreeBaseline": {SOURCE: production.digest(self.root / SOURCE)}}
        self.patches = contextlib.ExitStack()
        self.addCleanup(self.patches.close)
        self.patches.enter_context(mock.patch.object(production, "ROOT", self.root))
        self.patches.enter_context(mock.patch.object(production, "PRIMARY_ROOT", self.root))
        self.patches.enter_context(mock.patch.object(production, "STATE", self.root / "state.json"))
        self.patches.enter_context(mock.patch.object(production, "RUNS", self.root / "Logs"))
        actual_safe_path = production.safe_path
        self.patches.enter_context(mock.patch.object(production, "safe_path",
            side_effect=lambda relative, base=None: actual_safe_path(relative, self.root if base is None else base)))
        self.patches.enter_context(contextlib.redirect_stdout(io.StringIO()))
        self.which = self.patches.enter_context(mock.patch.object(production.shutil, "which", return_value="codex"))
        self.popen = self.patches.enter_context(mock.patch.object(production.subprocess, "Popen",
            side_effect=AssertionError("unmocked process launch forbidden")))
        self.record = self.patches.enter_context(mock.patch.object(production, "record"))

    @staticmethod
    def put(path, text):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def review(self, paths=None, *, reviewed=True, integrate=True, head="current-local-head"):
        paths = [SOURCE] if paths is None else paths
        def git(*args, cwd=None):
            if args[:2] == ("rev-parse", "HEAD"):
                return head
            if args[0] == "diff":
                return "\n".join(paths)
            if args[0] == "ls-files":
                return "\n".join(paths)
            raise AssertionError("unexpected git call: " + repr(args))
        with mock.patch.object(production, "get_job", return_value=self.job), \
             mock.patch.object(production, "git", side_effect=git):
            return production.review(SimpleNamespace(id=self.job["id"], reviewed=reviewed, integrate=integrate))


class CodexCommandTests(RelayFixture):
    def test_builder_uses_only_worktree_write_and_explicit_model_effort(self):
        command = production.codex_worker_command(self.job)
        self.assertEqual(command[command.index("--sandbox") + 1], "workspace-write")
        self.assertEqual(command[command.index("-C") + 1], str(self.wt))
        self.assertEqual(command[command.index("--model") + 1], "gpt-6.1-sol")
        self.assertIn('model_reasoning_effort="xhigh"', command)
        self.assertIn("--ignore-user-config", command)
        self.assertIn("--strict-config", command)
        self.assertIn("--json", command)
        self.assertEqual(command[:3], ["codex", "--no-daemon", "exec"])
        self.assertEqual(command[-1], "-")

    def test_root_danger_mode_and_server_endpoints_are_not_reused(self):
        command = production.codex_worker_command(self.job)
        joined = " ".join(command)
        self.assertNotIn("danger-full-access", joined)
        self.assertNotIn("dangerously-bypass", joined)
        self.assertNotIn("127.0.0.1", joined)
        self.assertNotIn("C:/original-root", joined)
        self.assertIn("mcp_servers={}", command)
        for name in ("plugins", "apps", "hooks", "multi_agent"):
            self.assertIn("features." + name + "=false", command)

    def test_critics_and_verifiers_are_read_only(self):
        for role in ("critic", "verifier"):
            with self.subTest(role=role):
                self.job["role"] = role
                command = production.codex_worker_command(self.job)
                self.assertEqual(command[command.index("--sandbox") + 1], "read-only")

    def test_missing_cli_fails_without_starting_a_process(self):
        self.which.return_value = None
        with self.assertRaisesRegex(RuntimeError, "Codex CLI unavailable"):
            production.codex_worker_command(self.job)
        self.popen.assert_not_called()


class RuntimeMetadataTests(RelayFixture):
    def test_no_session_or_response_model_stays_unverified(self):
        with mock.patch.object(production.Path, "home") as home:
            self.assertEqual(production.codex_runtime(None, []), ([], "UNVERIFIED", None))
        home.assert_not_called()

    def test_session_id_reads_only_matching_runtime_turn_context(self):
        home = self.root / "fake-home"
        session = home / ".codex/sessions/2026/10/11" / ("rollout-2026-10-11-" + SID + ".jsonl")
        self.put(session, json.dumps({"type": "turn_context", "payload": {
            "model": "gpt-6.1-sol", "effort": "xhigh"}}) + "\n")
        self.put(session.parent / "unrelated-session.jsonl", json.dumps({"type": "turn_context",
            "payload": {"model": "must-not-read", "effort": "max"}}) + "\n")
        self.put(home / ".codex/auth.json", "invalid and must not be read")
        with mock.patch.object(production.Path, "home", return_value=home):
            models, effort, metadata = production.codex_runtime(SID, [])
        self.assertEqual(models, ["gpt-6.1-sol"])
        self.assertEqual(effort, "xhigh")
        self.assertEqual(metadata, str(session))

    def test_malformed_rows_and_absent_model_are_not_requested_model_evidence(self):
        home = self.root / "fake-home"
        session = home / ".codex/sessions/2026/10/11" / ("rollout-" + SID + ".jsonl")
        self.put(session, "not json\n" + json.dumps({"type": "session_meta",
            "payload": {"requestedModel": "gpt-6.1-sol"}}) + "\n")
        with mock.patch.object(production.Path, "home", return_value=home):
            self.assertEqual(production.codex_runtime(SID, []), ([], "UNVERIFIED", None))

    def test_actual_turn_started_model_is_kept_without_inventing_effort(self):
        self.assertEqual(production.codex_runtime(None, [{"type": "turn.started", "model": "gpt-6.1-sol"}]),
                         (["gpt-6.1-sol"], "UNVERIFIED", None))


class CandidateAndIntegrationTests(RelayFixture):
    def test_untracked_source_has_a_nonempty_actual_candidate_diff(self):
        with mock.patch.object(production, "git") as git:
            patch = production.candidate_patch(self.job, [SOURCE])
        git.assert_not_called()
        self.assertIn("--- baseline/" + SOURCE, patch)
        self.assertIn("+++ candidate/" + SOURCE, patch)
        self.assertIn("-const float Density = 0.75f;", patch)
        self.assertIn("+const float Density = 0.80f;", patch)

    def test_dirty_user_work_is_context_and_is_not_reported_as_new_worker_changes(self):
        patch = production.candidate_patch(self.job, [SOURCE])
        self.assertNotIn("-// user's existing dirty work", patch)
        self.assertNotIn("+// user's existing dirty work", patch)
        self.assertIn(" // user's existing dirty work", patch)

    def test_unchanged_candidate_produces_no_diff(self):
        self.put(self.wt / SOURCE, self.baseline)
        self.assertEqual(production.candidate_patch(self.job, [SOURCE]), "")

    def test_mutated_immutable_baseline_is_rejected(self):
        self.put(self.logs / "source-baseline" / SOURCE, "corrupted baseline\n")
        with self.assertRaisesRegex(RuntimeError, "baseline unavailable or changed"):
            production.candidate_patch(self.job, [SOURCE])
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_primary_changed_after_staging_does_not_replace_review_baseline(self):
        self.put(self.root / SOURCE, "new user edits after worker launch\n")
        patch = production.candidate_patch(self.job, [SOURCE])
        self.assertIn("-const float Density = 0.75f;", patch)
        self.assertNotIn("new user edits", patch)

    def test_new_owned_file_is_reviewed_against_empty_baseline(self):
        relative = "Assets/Scripts/Presentation/NewOwned.cs"
        self.job["originalHashes"][relative] = None
        self.put(self.wt / relative, "class NewOwned {}\n")
        patch = production.candidate_patch(self.job, [relative])
        self.assertIn("+class NewOwned {}", patch)

    def test_legacy_baseline_fallback_requires_unchanged_primary_content(self):
        (self.logs / "source-baseline" / SOURCE).unlink()
        self.assertIn("+const float Density", production.candidate_patch(self.job, [SOURCE]))
        self.put(self.root / SOURCE, "user changed primary\n")
        with self.assertRaisesRegex(RuntimeError, "baseline unavailable or changed"):
            production.candidate_patch(self.job, [SOURCE])

    def test_review_only_keeps_original_and_writes_actual_diff(self):
        self.review(integrate=False)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertIn("+const float Density", (self.logs / "review.patch").read_text(encoding="utf-8"))
        self.record.assert_not_called()

    def test_user_mutation_blocks_integration(self):
        self.put(self.root / SOURCE, "new user edit\n")
        with self.assertRaisesRegex(RuntimeError, "unchanged originals"):
            self.review()
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), "new user edit\n")
        self.record.assert_not_called()

    def test_outside_scope_new_file_blocks_integration(self):
        outside = "Assets/Scripts/Unowned.cs"
        self.put(self.wt / outside, "class Unowned {}\n")
        with self.assertRaisesRegex(RuntimeError, "in-scope diff"):
            self.review(paths=[SOURCE, outside])
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertFalse((self.root / outside).exists())

    def test_new_head_blocks_integration_without_branch_switch(self):
        with self.assertRaisesRegex(RuntimeError, "Primary HEAD changed"):
            self.review(head="different-head")
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_failed_partial_worker_cannot_be_integrated_as_success(self):
        self.job["status"] = "failed_preserved"
        with self.assertRaisesRegex(RuntimeError, "successful worker"):
            self.review()
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_explicit_diff_review_is_required(self):
        with self.assertRaisesRegex(RuntimeError, "explicit lead diff review"):
            self.review(reviewed=False)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_source_deletion_is_rejected(self):
        (self.wt / SOURCE).unlink()
        with self.assertRaisesRegex(RuntimeError, "Deletion is not authorized"):
            self.review()
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_successful_fixture_integration_is_pending_actual_unity_verification(self):
        self.review()
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.candidate)
        self.assertEqual((self.logs / "integration-backup" / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual(self.job["status"], "integrated_pending_unity_validation")
        self.assertNotEqual(self.job["status"], "PASS")


class CodexSupervisorTests(RelayFixture):
    def simulate(self, events, *, exit_code=0, answer="actual worker handoff", timeout=False):
        self.put(self.logs / "assignment.md", "Edit only owned source. No Unity.\n")
        job = self.job
        job["status"] = "assigned"
        job["goal"] = "Repair the assigned source only"
        self.patches.enter_context(mock.patch.object(production, "codex_worker_command", return_value=["codex", "exec"]))
        self.patches.enter_context(mock.patch.object(production, "codex_runtime", return_value=([], "UNVERIFIED", None)))
        child = SimpleNamespace(pid=987, args=["codex"], returncode=exit_code, terminate=mock.Mock(), wait=mock.Mock())
        elapsed = [0]
        if timeout:
            self.patches.enter_context(mock.patch.object(production,"relay_checkpoint",return_value={"safePoint":False}))
            def clock():
                elapsed[0] += 60
                return elapsed[0]
            self.patches.enter_context(mock.patch.object(production.time, "monotonic", side_effect=clock))
        delivered = [False]
        def start(command, **kwargs):
            def communicate(prompt, timeout=None):
                if answer is not None:
                    self.put(self.logs / "HANDOFF.md", answer)
                if not delivered[0]:
                    for event in events:
                        kwargs["stdout"].write(json.dumps(event) + "\n")
                    delivered[0] = True
                kwargs["stdout"].flush()
                if timeout_requested:
                    raise production.subprocess.TimeoutExpired(command, timeout)
            child.communicate = communicate
            return child
        timeout_requested = timeout
        self.popen.side_effect = start
        production.supervise_codex(job)
        return child

    def test_real_completed_event_and_handoff_stop_at_ready_for_review(self):
        self.simulate([{"type": "thread.started", "thread_id": SID},
            {"type": "turn.completed", "usage": {"input_tokens": 12, "output_tokens": 8}}])
        self.assertEqual(self.job["sessionId"], SID)
        self.assertEqual(self.job["status"], "ready_for_review")
        self.assertEqual(self.job["usage"], {"input_tokens": 12, "output_tokens": 8})
        self.assertEqual(self.job["modelVerification"], "UNVERIFIED")
        self.assertEqual(self.job["actualModels"], [])
        self.assertEqual(self.job["appliedEffort"], "UNVERIFIED")
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_exit_zero_without_completed_event_is_not_success(self):
        self.simulate([{"type": "thread.started", "thread_id": SID}])
        self.assertEqual(self.job["status"], "failed_preserved")

    def test_missing_handoff_is_not_success(self):
        self.simulate([{"type": "turn.completed", "usage": {}}], answer=None)
        self.assertEqual(self.job["status"], "failed_preserved")

    def test_failed_turn_is_preserved_even_with_zero_cli_exit(self):
        self.simulate([{"type": "turn.completed", "usage": {}},
            {"type": "turn.failed", "error": {"message": "account limit reached"}}])
        self.assertEqual(self.job["status"], "failed_preserved")
        self.assertTrue(production.read(self.logs / "result.json")["is_error"])

    def test_completed_turn_with_transient_reconnecting_notice_is_reviewable(self):
        notice = {"type": "error", "message": "Reconnecting... 1/5"}
        self.simulate([{"type": "thread.started", "thread_id": SID}, notice,
            {"type": "turn.completed", "usage": {"output_tokens": 8}}])
        self.assertEqual(self.job["status"], "ready_for_review")
        result = production.read(self.logs / "result.json")
        self.assertFalse(result["is_error"])
        self.assertEqual(result["errors"], [notice])

    def test_reconnecting_notice_with_final_failed_turn_blocks_review(self):
        self.simulate([{"type": "error", "message": "Reconnecting... 1/5"},
            {"type": "turn.completed", "usage": {}},
            {"type": "turn.failed", "error": {"message": "stream disconnected"}}])
        self.assertEqual(self.job["status"], "failed_preserved")
        self.assertTrue(production.read(self.logs / "result.json")["is_error"])

    def test_completed_turn_with_nonzero_exit_still_blocks_review(self):
        self.simulate([{"type": "error", "message": "Reconnecting... 1/5"},
            {"type": "turn.completed", "usage": {}}], exit_code=1)
        self.assertEqual(self.job["status"], "failed_preserved")
        self.assertTrue(production.read(self.logs / "result.json")["is_error"])

    def test_timeout_preserves_partial_result_and_never_marks_ready(self):
        child = self.simulate([{"type": "thread.started", "thread_id": SID}], timeout=True, exit_code=-1)
        child.terminate.assert_called_once()
        self.assertEqual(self.job["status"], "timeout_preserved")
        self.assertTrue((self.logs / "codex.jsonl").is_file())
        self.assertTrue((self.logs / "HANDOFF.md").is_file())

    def test_provider_routing_uses_codex_supervisor(self):
        self.job["status"] = "assigned"
        production.write(self.root / "state.json", {"productionEnvironment": {"workers": {self.job["id"]: self.job}}})
        with mock.patch.object(production, "state_lock", contextlib.nullcontext), \
             mock.patch.object(production, "process_alive", return_value=False), \
             mock.patch.object(production, "supervise_codex") as supervisor:
            production.supervise(SimpleNamespace(id=self.job["id"]))
        supervisor.assert_called_once()
        claimed = supervisor.call_args.args[0]
        self.assertEqual(claimed["id"], self.job["id"])
        self.assertEqual(claimed["status"], "starting")
        self.assertTrue(claimed["attemptToken"])
        self.popen.assert_not_called()

    def test_native_owner_rejects_duplicate_cli_before_logs_or_process(self):
        self.job.update(executionMethod="native_subagent", nativeAgent="/root/dune_builder",
                        status="running", sessionId=SID)
        before = copy.deepcopy(self.job)
        production.write(self.root / "state.json", {"productionEnvironment": {"workers": {self.job["id"]: self.job}}})
        with mock.patch.object(production, "state_lock", contextlib.nullcontext), \
             mock.patch.object(production, "supervise_codex") as supervisor, \
             mock.patch.object(production, "codex_worker_command") as command:
            with self.assertRaisesRegex(RuntimeError, "Native worker owns this job"):
                production.supervise(SimpleNamespace(id=self.job["id"]))
        supervisor.assert_not_called()
        command.assert_not_called()
        self.popen.assert_not_called()
        self.record.assert_not_called()
        self.assertEqual(self.job, before)
        self.assertFalse((self.logs / "codex.jsonl").exists())


class WorkerStagingTests(RelayFixture):
    def setUp(self):
        super().setUp()
        self.state_path = self.root / "state.json"
        production.write(self.state_path, {"productionEnvironment": {
            "unityEditorOwner": "claude-code", "approvalSource": "approved relay fixture", "workers": {}}})
        self.patches.enter_context(mock.patch.object(production, "state_lock", contextlib.nullcontext))
        self.args = SimpleNamespace(id="fresh-relay-worker", files=[SOURCE], role="builder", provider="codex",
            model=None, effort="xhigh", serena=False, timeout=2, goal="bounded coast fix",
            reference_group="none", evidence=[])
        def record(job):
            state = production.read(self.state_path)
            state["productionEnvironment"]["workers"][job["id"]] = copy.deepcopy(job)
            production.write(self.state_path, state)
        self.record.side_effect = record
        self.mutate_during_staging = False
        def git(*args, cwd=None):
            if args[:2] == ("rev-parse", "HEAD"):
                if self.mutate_during_staging and (self.root / "Logs/fresh-relay-worker/source-baseline" / SOURCE).exists():
                    self.put(self.root / SOURCE, "user changed original during staging\n")
                return "current-local-head"
            if args[0] == "branch":
                return "milestone/current-local-branch"
            if args[:2] == ("worktree", "add"):
                Path(args[-2]).mkdir(parents=True)
                return ""
            if args[0] in ("sparse-checkout", "checkout"):
                return ""
            if args[0] == "diff":
                return ""
            if args[0] == "ls-files":
                if "Assets/Scripts" in args:
                    return SOURCE
                if cwd is None:
                    return ""
                return "\n".join(str(p.relative_to(cwd)).replace("\\", "/")
                                 for p in Path(cwd).rglob("*") if p.is_file())
            raise AssertionError("unexpected git call: " + repr(args))
        self.patches.enter_context(mock.patch.object(production, "git", side_effect=git))

    def test_staging_copies_actual_untracked_baseline_and_codex_root_restrictions(self):
        def started(command, **kwargs):
            kwargs["stdout"].close()
            return SimpleNamespace(pid=51300, stdin=mock.Mock())
        self.popen.side_effect = started
        production.worker_start(self.args)
        state = production.read(self.state_path)
        job = state["productionEnvironment"]["workers"][self.args.id]
        self.assertEqual(job["baseHead"], "current-local-head")
        self.assertEqual(job["baseBranch"], "milestone/current-local-branch")
        self.assertEqual(job["provider"], "codex")
        self.assertEqual(job["requestedModel"], "gpt-6.1-sol")
        self.assertEqual((Path(job["worktree"]) / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual((Path(job["logDirectory"]) / "source-baseline" / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual(production.read(Path(job["worktree"]) / ".mcp.json"), {"mcpServers": {}})
        self.assertTrue(production.read(Path(job["worktree"]) / ".claude/settings.local.json")["disableAllHooks"])
        self.assertIn("No Unity MCP", (Path(job["worktree"]) / "AGENTS.override.md").read_text(encoding="utf-8"))
        self.assertFalse(job["unityMcpConnected"])

    def test_original_mutation_during_staging_prevents_any_worker_launch(self):
        self.mutate_during_staging = True
        with self.assertRaisesRegex(RuntimeError, "Original baseline changed during staging"):
            production.worker_start(self.args)
        self.popen.assert_not_called()
        state = production.read(self.state_path)
        self.assertEqual(state["productionEnvironment"]["workers"][self.args.id]["status"], "setup_failed")
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), "user changed original during staging\n")

    def test_parent_pid_update_does_not_revert_fast_child_state(self):
        def child_started(command, **kwargs):
            state = production.read(self.state_path)
            state["productionEnvironment"]["workers"][self.args.id].update(
                status="running", sessionId=SID, codexPid=456)
            production.write(self.state_path, state)
            kwargs["stdout"].close()
            return SimpleNamespace(pid=51300, stdin=mock.Mock())
        self.popen.side_effect = child_started
        production.worker_start(self.args)
        job = production.read(self.state_path)["productionEnvironment"]["workers"][self.args.id]
        self.assertEqual(job["status"], "running")
        self.assertEqual(job["sessionId"], SID)
        self.assertEqual(job["codexPid"], 456)
        self.assertEqual(job["supervisorPid"], 51300)

    def test_existing_owner_blocks_duplicate_relay_work_on_same_source(self):
        state = production.read(self.state_path)
        state["productionEnvironment"]["workers"]["existing"] = {
            "id": "existing", "status": "ready_for_review", "allowedFiles": [SOURCE]}
        production.write(self.state_path, state)
        with self.assertRaisesRegex(RuntimeError, "File already owned"):
            production.worker_start(self.args)
        self.popen.assert_not_called()

    def test_codex_worker_cannot_enable_serena_or_unsupported_max_effort(self):
        for field in ("serena", "effort"):
            with self.subTest(field=field):
                args = copy.copy(self.args)
                setattr(args, field, True if field == "serena" else "max")
                with self.assertRaisesRegex(ValueError, "Codex worker uses no MCP"):
                    production.worker_start(args)
        self.popen.assert_not_called()

    def test_known_runtime_read_only_blocks_cli_builders_and_revisers_before_staging(self):
        state = production.read(self.state_path)
        state["productionEnvironment"]["relaySupport"] = {
            "externalCodexWriteCapability": "blocked_runtime_read_only",
            "blockerEvidence": "prior actual runtime preserved"}
        production.write(self.state_path, state)
        before = self.state_path.read_bytes()
        with mock.patch.object(production, "git") as git, \
             mock.patch.object(production, "codex_worker_command") as command:
            for role in ("builder", "reviser"):
                with self.subTest(role=role):
                    args = copy.copy(self.args)
                    args.role = role
                    with self.assertRaisesRegex(RuntimeError, "Standalone Codex write capability is blocked"):
                        production.worker_start(args)
                    self.assertFalse((self.root / "Logs" / args.id).exists())
                    self.assertEqual(self.state_path.read_bytes(), before)
        git.assert_not_called()
        command.assert_not_called()
        self.which.assert_not_called()
        self.popen.assert_not_called()
        self.record.assert_not_called()


class MonitorTests(RelayFixture):
    def test_codex_stream_retains_thread_model_and_command_action(self):
        stream = self.logs / "codex.jsonl"
        events = [{"type": "thread.started", "thread_id": SID},
                  {"type": "turn.started", "model": "gpt-6.1-sol"},
                  {"type": "item.completed", "item": {"type": "command_execution", "command": "rg Density file.cs"}}]
        self.put(stream, "\n".join(json.dumps(event) for event in events) + "\n")
        detail = production.monitor_tail(stream)
        self.assertEqual(detail["sessionId"], SID)
        self.assertEqual(detail["model"], "gpt-6.1-sol")
        self.assertIsNotNone(detail["action"])

    def test_monitor_selects_provider_stream_and_detects_exited_codex_pid(self):
        self.job.update(status="running", codexPid=222, actualModels=["gpt-6.1-sol"], sessionId=SID)
        state = {"productionEnvironment": {"workers": {self.job["id"]: self.job},
            "coordinator": {}, "coordinatorLease": {}, "autonomous": {}}}
        seen = []
        def tail(path):
            seen.append(Path(path))
            return {"sessionId": None, "model": None, "action": None}
        with mock.patch.object(production, "monitor_tail", side_effect=tail), \
             mock.patch.object(production, "process_alive", return_value=False):
            frame = production.monitor_frame(state)
        self.assertIn(self.logs / "codex.jsonl", seen)
        self.assertIn("recorded running / PID exited", frame)
        self.assertIn(self.job["id"], frame)

    def test_native_monitor_does_not_expose_preserved_cli_session_or_dead_pid(self):
        old_sid = "OLDCLISID-MUST-NOT-BE-ACTIVE"
        native_agent = "/root/dune_builder"
        self.job.update(status="running", executionMethod="native_subagent", nativeAgent=native_agent,
                        codexPid=222, sessionId=old_sid, actualModels=[])
        state = {"productionEnvironment": {"workers": {self.job["id"]: self.job},
            "coordinator": {}, "coordinatorLease": {}, "autonomous": {}}}
        def tail(path):
            return {"sessionId": old_sid, "model": "old-cli-model", "action": "old CLI action"} \
                if Path(path).name == "codex.jsonl" else {"sessionId": None, "model": None, "action": None}
        with mock.patch.object(production, "monitor_tail", side_effect=tail), \
             mock.patch.object(production, "process_alive", return_value=False) as alive:
            frame = production.monitor_frame(state)
        self.assertIn(native_agent[:10], frame)
        self.assertIn("native Codex subagent; scoped worktree", frame)
        self.assertNotIn(old_sid[:10], frame)
        self.assertNotIn("old-cli-model", frame)
        self.assertNotIn("recorded running / PID exited", frame)
        self.assertNotIn(mock.call(222), alive.call_args_list)


if __name__ == "__main__":
    unittest.main()

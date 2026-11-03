"""Quota-aware relay regression tests; every file/process fixture is isolated.

Run: python -B -X utf8 Tools/LoopEngineering/tests/test_relay_handoff.py -v
These tests do not read or mutate the actual state, sessions, saves or Unity.
"""
import contextlib
import copy
import datetime as dt
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
spec = importlib.util.spec_from_file_location(
    "pa_handoff_under_test", TEST_DIR.parent / "Invoke-ProjectPAProduction.py")
production = importlib.util.module_from_spec(spec)
spec.loader.exec_module(production)
CURRENT = dt.datetime(2026, 10, 11, 15, 0, tzinfo=production.KST)
SOURCE = "Assets/Scripts/Presentation/CoastalVegetationField.cs"
SESSION = "12345678-1234-1234-1234-123456789abc"
GENERATION = "relay-generation-1"


def observation(percent=25, *, source="claude_mods", age=0, reset_hours=2, **extra):
    result = {"observedAt": (CURRENT - dt.timedelta(seconds=age)).isoformat(), "source": source,
        "rateLimits": [{"kind": "five_hour", "percentUsed": percent,
                        "resetsAt": (CURRENT + dt.timedelta(hours=reset_hours)).isoformat()}],
        "context": {"percent": 10}}
    result.update(extra)
    return result


def relay_state():
    return {"demoProductionRouting": {"unityEditorOwner": "claude-code"},
        "preservedRejections": ["grip HOLD"], "preservedUsage": [14, 42],
        "productionEnvironment": {"unityEditorOwner": "claude-code",
            "approvalSource": "approved relay fixture", "workers": {},
            "autonomous": {"enabled": True, "stopRequested": False, "generation": GENERATION},
            "coordinatorLease": {"owner": "claude-code", "status": "active",
                "generation": GENERATION, "sessionId": SESSION, "cliPid": 777},
            "coordinator": {"status": "running", "sessionId": SESSION, "cliPid": 777},
            "relaySupport": {"autoHandoff": {"enabled": True},
                "teams": {"claude-code": {"capability": "verified"},
                          "codex": {"capability": "verified"}},
                "observations": {"claude-code": observation(),
                    "codex": observation(source="codex_runtime", ordinaryUsageAllowed=True)}}}}


class UsageStateTests(unittest.TestCase):
    def usage(self, value):
        return production.relay_usage_state(value, CURRENT)

    def test_fresh_verified_claude_mods_reading_establishes_availability(self):
        self.assertEqual(self.usage(observation())["status"], "available")

    def test_near_limit_is_distinct_from_available_and_exhausted(self):
        for percent in (90, 99.9):
            with self.subTest(percent=percent):
                self.assertEqual(self.usage(observation(percent))["status"], "near_limit")

    def test_multiple_exhausted_windows_require_latest_reset_for_that_team(self):
        item = observation(100)
        weekly = CURRENT + dt.timedelta(days=5)
        item["rateLimits"].append({"kind": "seven_day", "percentUsed": 100,
                                   "resetsAt": weekly.isoformat()})
        result = self.usage(item)
        self.assertEqual(result["status"], "exhausted")
        self.assertEqual(dt.datetime.fromisoformat(result["resetAt"]), weekly)

    def test_nonexhausted_weekly_window_does_not_extend_exhausted_session_wait(self):
        item = observation(100)
        item["rateLimits"].append({"kind": "seven_day", "percentUsed": 17,
            "resetsAt": (CURRENT + dt.timedelta(days=5)).isoformat()})
        result = self.usage(item)
        self.assertEqual(dt.datetime.fromisoformat(result["resetAt"]), CURRENT + dt.timedelta(hours=2))

    def test_old_or_future_observations_cannot_establish_current_headroom(self):
        for age in (301, 3600, -30):
            with self.subTest(age=age):
                self.assertEqual(self.usage(observation(age=age))["status"], "unknown")

    def test_exact_freshness_boundary_is_accepted(self):
        self.assertEqual(self.usage(observation(age=300))["status"], "available")

    def test_no_mods_reading_or_naive_timestamp_is_unknown(self):
        for item in (None, {}, observation(observedAt="unknown"),
                     observation(observedAt="2026-10-11T15:00:00")):
            with self.subTest(item=item):
                self.assertEqual(self.usage(item)["status"], "unknown")

    def test_unknown_ordinary_usage_permission_never_proves_codex_availability(self):
        for permitted in (None, False):
            with self.subTest(permitted=permitted):
                result = self.usage(observation(source="codex_runtime", ordinaryUsageAllowed=permitted))
                self.assertNotEqual(result["status"], "available")
                if permitted is None:
                    self.assertEqual(result["status"], "unknown")

    def test_verified_codex_permission_and_fresh_windows_establish_availability(self):
        self.assertEqual(self.usage(observation(source="codex_runtime",
            ordinaryUsageAllowed=True))["status"], "available")

    def test_malformed_percentages_cannot_be_treated_as_zero_usage(self):
        for percent in (None, "unknown", float("nan"), float("inf"), -1, False, True):
            with self.subTest(percent=percent):
                self.assertEqual(self.usage(observation(percent))["status"], "unknown")

    def test_exhaustion_without_actual_future_reset_never_guesses_a_window(self):
        for reset in (None, "unknown", "2026-10-12T15:00:00",
                      (CURRENT - dt.timedelta(seconds=1)).isoformat()):
            with self.subTest(reset=reset):
                item = observation(100)
                item["rateLimits"][0]["resetsAt"] = reset
                result = self.usage(item)
                self.assertNotEqual(result["status"], "available")
                self.assertIsNone(result.get("resetAt"))

    def test_one_unknown_exhausted_window_blocks_use_of_another_known_reset(self):
        item = observation(100)
        item["rateLimits"].append({"kind": "seven_day", "percentUsed": 100, "resetsAt": None})
        result = self.usage(item)
        self.assertNotEqual(result["status"], "available")
        self.assertIsNone(result.get("resetAt"))


class SelectionTests(unittest.TestCase):
    def setUp(self):
        self.state = relay_state()

    def select(self):
        before = copy.deepcopy(self.state)
        result = production.relay_choice(self.state, CURRENT)
        self.assertEqual(self.state, before, "selection must not transfer leases or erase history")
        return result

    @property
    def support(self):
        return self.state["productionEnvironment"]["relaySupport"]

    def test_available_team_may_be_selected_without_unity_lease_transfer(self):
        result = self.select()
        self.assertEqual(result["action"], "available")
        self.assertIn(result["provider"], ("claude-code", "codex"))

    def test_exhausted_claude_selects_capable_available_codex(self):
        self.support["observations"]["claude-code"] = observation(100)
        result = self.select()
        self.assertEqual(result["action"], "available")
        self.assertEqual(result["provider"], "codex")

    def test_both_exhausted_choose_earliest_team_after_its_latest_window(self):
        claude = observation(100, reset_hours=1)
        claude["rateLimits"].append({"kind": "seven_day", "percentUsed": 100,
            "resetsAt": (CURRENT + dt.timedelta(days=5)).isoformat()})
        self.support["observations"] = {"claude-code": claude,
            "codex": observation(100, source="codex_runtime", reset_hours=3,
                                  ordinaryUsageAllowed=True)}
        result = self.select()
        self.assertEqual(result["action"], "wait")
        self.assertEqual(result["provider"], "codex")
        self.assertEqual(dt.datetime.fromisoformat(result["resumeAt"]), CURRENT + dt.timedelta(hours=3))

    def test_permission_blocked_codex_cannot_win_earlier_verified_reset(self):
        self.support["teams"]["codex"]["capability"] = "blocked_runtime_read_only"
        self.support["observations"] = {"claude-code": observation(100, reset_hours=7),
            "codex": observation(100, source="codex_runtime", reset_hours=1, ordinaryUsageAllowed=True)}
        result = self.select()
        self.assertEqual(result["action"], "wait")
        self.assertEqual(result["provider"], "claude-code")
        self.assertEqual(dt.datetime.fromisoformat(result["resumeAt"]), CURRENT + dt.timedelta(hours=7))

    def test_blocked_capability_cannot_launch_despite_low_observed_usage(self):
        self.support["teams"]["codex"]["capability"] = "blocked_runtime_read_only"
        self.support["observations"]["claude-code"] = observation(100, reset_hours=7)
        result = self.select()
        self.assertEqual(result["action"], "wait")
        self.assertEqual(result["provider"], "claude-code")

    def test_unknown_usage_does_not_become_an_available_team(self):
        self.support["observations"] = {"claude-code": {}, "codex": {}}
        self.assertEqual(self.select()["action"], "blocked")

    def test_missing_capability_is_not_assumed_verified(self):
        self.support["teams"] = {"codex": {}}
        self.assertNotEqual(self.select()["action"], "available")

    def test_near_limit_selection_preserves_live_owner_and_active_generation(self):
        self.support["observations"]["claude-code"] = observation(95)
        before = copy.deepcopy(self.state["productionEnvironment"]["coordinatorLease"])
        result = self.select()
        self.assertNotEqual(result.get("provider"), "claude-code")
        self.assertEqual(self.state["productionEnvironment"]["coordinatorLease"], before)

    def test_user_pause_prevents_all_selection_even_with_available_capacity(self):
        self.state["productionEnvironment"]["autonomous"].update(enabled=False, stopRequested=True)
        self.assertEqual(self.select()["action"], "paused")

    def test_disabled_handoff_prevents_selection(self):
        self.support["autoHandoff"]["enabled"] = False
        self.assertEqual(self.select()["action"], "paused")

    def test_human_review_terminal_status_is_preserved(self):
        self.state["productionEnvironment"]["autonomous"]["status"] = "ready_for_human_review"
        self.assertEqual(self.select()["action"], "paused")

    def test_human_review_checkpoint_is_preserved_even_before_status_settles(self):
        self.state["productionEnvironment"]["autonomous"]["checkpoint"] = {
            "sessionId": SESSION, "disposition": "human-review", "nextAction": "human review"}
        self.assertEqual(self.select()["action"], "paused")


class HandoffFixture(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix=".handoff-test-", dir=TEST_DIR)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.state_path = self.root / "state.json"
        self.logs = self.root / "Logs/ParallelProduction/relay-worker"
        self.wt = self.logs / "worktree"
        self.baseline = "// existing approved local work\nconst int Quantity = 3;\n"
        self.partial = self.baseline.replace("Quantity = 3", "Quantity = 6")
        self.put(self.root / SOURCE, self.baseline)
        self.put(self.wt / SOURCE, self.partial)
        self.put(self.logs / "source-baseline" / SOURCE, self.baseline)
        self.put(self.logs / "claude.jsonl", '{"type":"assistant","partial":"preserve"}\n')
        self.put(self.logs / "claude.stderr", "retained error evidence\n")
        self.put(self.logs / "HANDOFF.md", "unfinished partial; no Unity check\n")
        self.job = {"id": "relay-worker", "provider": "claude-code", "role": "builder",
            "goal": "Improve bounded coastal dune layout", "status": "handoff_pending",
            "baseHead": "approved-current-local-head", "allowedFiles": [SOURCE],
            "worktree": str(self.wt), "logDirectory": str(self.logs), "sessionId": SESSION,
            "originalHashes": {SOURCE: production.digest(self.root / SOURCE)},
            "worktreeBaseline": {SOURCE: production.digest(self.root / SOURCE)},
            "relayGeneration": GENERATION,
            "relayContinuation": {"status": "pending", "generation": GENERATION}}
        self.state = relay_state()
        self.state["productionEnvironment"]["workers"][self.job["id"]] = copy.deepcopy(self.job)
        production.write(self.state_path, self.state)
        self.patches = contextlib.ExitStack()
        self.addCleanup(self.patches.close)
        for name, value in (("ROOT", self.root), ("PRIMARY_ROOT", self.root),
                            ("STATE", self.state_path), ("RUNS", self.root / "Logs/ParallelProduction")):
            self.patches.enter_context(mock.patch.object(production, name, value))
        self.patches.enter_context(mock.patch.object(production, "state_lock", contextlib.nullcontext))
        actual_safe_path = production.safe_path
        self.patches.enter_context(mock.patch.object(production, "safe_path", side_effect=lambda relative, base=None:
            actual_safe_path(relative, self.root if base is None else base)))
        self.patches.enter_context(contextlib.redirect_stdout(io.StringIO()))
        self.git = self.patches.enter_context(mock.patch.object(production, "git", return_value="approved-current-local-head"))
        self.popen = self.patches.enter_context(mock.patch.object(production.subprocess, "Popen",
            side_effect=AssertionError("real process launches are forbidden in tests")))
        self.run = self.patches.enter_context(mock.patch.object(production.subprocess, "run",
            side_effect=AssertionError("real processes are forbidden in tests")))
        self.alive = self.patches.enter_context(mock.patch.object(production, "process_alive", return_value=False))
        actual_choice = production.relay_choice
        actual_usage = production.relay_usage_state
        self.patches.enter_context(mock.patch.object(production, "relay_choice", side_effect=lambda state, current=None:
            actual_choice(state, CURRENT if current is None else current)))
        self.patches.enter_context(mock.patch.object(production, "relay_usage_state", side_effect=lambda value, current=None:
            actual_usage(value, CURRENT if current is None else current)))

    @staticmethod
    def put(path, text):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def current_state(self):
        return production.read(self.state_path)

    def mutate(self, callback):
        state = self.current_state()
        callback(state)
        production.write(self.state_path, state)

    def checkpoint(self, **kwargs):
        arguments = {"provider": "claude-code", "job_id": self.job["id"],
            "reason": "unexpected_quota", "next_action": "Continue only unfinished dune allocation",
            "session_id": SESSION, "generation": GENERATION, "safe_point": False}
        arguments.update(kwargs)
        return production.relay_checkpoint(**arguments)


class CheckpointTests(HandoffFixture):
    def test_unexpected_quota_preserves_partial_source_logs_and_authorities(self):
        old_state = self.current_state()
        old_logs = {name: (self.logs / name).read_bytes()
                    for name in ("claude.jsonl", "claude.stderr", "HANDOFF.md")}
        original_handoff = self.root / "Docs/00_CURRENT/CODEX_HANDOFF.md"
        self.put(original_handoff, "existing actual validation must be preserved\n")
        result = self.checkpoint()
        self.assertEqual(result["reason"], "unexpected_quota")
        self.assertEqual(result["goal"], self.job["goal"])
        self.assertEqual(result["changedFiles"], [SOURCE])
        self.assertTrue(result["validation"].startswith("UNVERIFIED"))
        self.assertFalse(result["safePoint"])
        self.assertEqual(len(result["files"]), 1)
        entry = result["files"][0]
        self.assertEqual(entry["path"], SOURCE)
        self.assertEqual(entry["sha256"], production.digest(self.wt / SOURCE))
        self.assertEqual(Path(entry["snapshot"]).read_bytes(), (self.wt / SOURCE).read_bytes())
        self.assertTrue(Path(result["manifest"]).is_file())
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual((self.logs / "source-baseline" / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual(original_handoff.read_text(encoding="utf-8"), "existing actual validation must be preserved\n")
        for name, value in old_logs.items():
            self.assertEqual((self.logs / name).read_bytes(), value)
        after = self.current_state()
        self.assertEqual(after["demoProductionRouting"], old_state["demoProductionRouting"])
        self.assertEqual(after["productionEnvironment"]["coordinatorLease"],
                         old_state["productionEnvironment"]["coordinatorLease"])
        self.assertEqual(after["preservedUsage"], [14, 42])
        self.assertEqual(after["preservedRejections"], ["grip HOLD"])
        self.popen.assert_not_called()
        self.run.assert_not_called()

    def test_changed_original_preserves_snapshot_but_blocks_promotion(self):
        self.put(self.root / SOURCE, "new user edit after worker started\n")
        result = self.checkpoint()
        self.assertFalse(result["promotable"])
        self.assertTrue(any("original changed" in item for item in result["conflicts"]))
        self.assertEqual(Path(result["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), "new user edit after worker started\n")

    def test_mutated_immutable_baseline_preserves_partial_but_blocks_promotion(self):
        self.put(self.logs / "source-baseline" / SOURCE, "corrupted recovery baseline\n")
        result = self.checkpoint()
        self.assertFalse(result["promotable"])
        self.assertTrue(any("immutable baseline" in item for item in result["conflicts"]))
        self.assertEqual(Path(result["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_changed_head_blocks_promotion_without_losing_candidate(self):
        self.git.return_value = "new-current-head"
        result = self.checkpoint()
        self.assertFalse(result["promotable"])
        self.assertEqual(result["baseHead"], "approved-current-local-head")
        self.assertEqual(result["currentHead"], "new-current-head")
        self.assertEqual(Path(result["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)

    def test_source_changing_during_copy_is_not_promotable(self):
        actual_digest = production.digest
        with mock.patch.object(production, "digest", side_effect=lambda path:
                "changing-source-hash" if Path(path) == self.wt / SOURCE else actual_digest(path)):
            result = self.checkpoint()
        self.assertFalse(result["promotable"])
        self.assertFalse(result["files"][0]["stable"])
        self.assertTrue(any("changing while copied" in item for item in result["conflicts"]))
        self.assertEqual(Path(result["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)

    def test_missing_owned_source_is_not_a_promotable_deletion(self):
        (self.wt / SOURCE).unlink()
        result = self.checkpoint()
        self.assertFalse(result["promotable"])
        self.assertTrue(result["files"][0]["missing"])
        self.assertIsNone(result["files"][0]["snapshot"])
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_checkpoint_copies_only_assigned_source(self):
        unowned = "Assets/Scripts/Unowned.cs"
        self.put(self.wt / unowned, "must not become relay-owned\n")
        result = self.checkpoint()
        self.assertEqual([item["path"] for item in result["files"]], [SOURCE])
        snapshot_values = [p.read_text(encoding="utf-8")
                           for p in (self.logs / "relay-snapshots/objects").glob("*.bin")]
        self.assertNotIn("must not become relay-owned\n", snapshot_values)

    def test_stale_generation_cannot_replace_shared_checkpoint(self):
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "generation changed"):
            self.checkpoint(generation="old-generation")
        self.assertEqual(self.state_path.read_bytes(), before)
        self.assertFalse((self.logs / "relay-snapshots").exists())

    def test_stale_worker_cannot_publish_under_current_caller_generation(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            relayGeneration="expired-worker-generation"))
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "generation"):
            self.checkpoint(generation=GENERATION)
        self.assertEqual(self.state_path.read_bytes(), before)
        self.assertFalse((self.logs / "relay-snapshots").exists())

    def test_foreign_provider_and_session_cannot_publish_another_workers_checkpoint(self):
        for fields, message in (({"provider": "codex"}, "Foreign worker provider"),
                                ({"session_id": "foreign-session"}, "Foreign worker session")):
            with self.subTest(fields=fields):
                before = self.state_path.read_bytes()
                with self.assertRaisesRegex(RuntimeError, message):
                    self.checkpoint(**fields)
                self.assertEqual(self.state_path.read_bytes(), before)
        self.assertFalse((self.logs / "relay-snapshots").exists())

    def test_unknown_job_is_not_given_source_or_ownership(self):
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "Registered job required"):
            self.checkpoint(job_id="not-registered")
        self.assertEqual(self.state_path.read_bytes(), before)

    def test_foreign_lead_cannot_snapshot_original_production(self):
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "Only current lead"):
            self.checkpoint(provider="codex", job_id=None, session_id="foreign-session")
        self.assertEqual(self.state_path.read_bytes(), before)

    def test_missing_validation_evidence_never_turns_checkpoint_into_pass(self):
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "Evidence unavailable"):
            self.checkpoint(evidence=["Logs/not-run.png"])
        self.assertEqual(self.state_path.read_bytes(), before)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_existing_validation_evidence_is_hashed_without_inventing_a_new_result(self):
        evidence = self.root / "Logs/actual-verification.json"
        self.put(evidence, '{"result":"limited","unrun":["standalone"]}\n')
        result = self.checkpoint(evidence=["Logs/actual-verification.json"])
        self.assertTrue(result["validation"].startswith("UNVERIFIED"))
        self.assertEqual(result["evidence"], [{"path": "Logs/actual-verification.json",
                                               "sha256": production.digest(evidence)}])
        self.assertNotIn("completionReached", self.current_state()["demoProductionRouting"])

    def test_near_limit_checkpoint_does_not_transfer_live_unity_owner(self):
        self.alive.return_value = True
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"]["observations"].update(
            {"claude-code": observation(95)}))
        before = self.current_state()["productionEnvironment"]["coordinatorLease"]
        result = self.checkpoint(reason="near_limit", safe_point=True)
        self.assertEqual(result["unityOwnerUnchanged"], "claude-code")
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"], before)
        self.popen.assert_not_called()


class TickSelectionTests(HandoffFixture):
    def test_ready_for_review_candidate_is_not_rerun(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            status="ready_for_review"))
        with mock.patch.object(production, "resume_relay_worker") as resume:
            result = production.relay_tick(CURRENT)
        resume.assert_not_called()
        self.assertFalse(result["dispatched"])
        self.popen.assert_not_called()

    def test_only_registered_pending_continuation_can_be_dispatched(self):
        with mock.patch.object(production, "resume_relay_worker", return_value={"dispatched": "mock"}) as resume:
            result = production.relay_tick(CURRENT)
        self.assertEqual(result, {"dispatched": "mock"})
        self.assertEqual(resume.call_count, 1)
        resumed_job, provider = resume.call_args.args
        self.assertEqual(resumed_job["id"], self.job["id"])
        self.assertEqual(provider, "claude-code")
        self.popen.assert_not_called()

    def test_nonpending_portable_status_cannot_be_dispatched(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]][
            "relayContinuation"].update(status="consumed"))
        with mock.patch.object(production, "resume_relay_worker") as resume:
            result = production.relay_tick(CURRENT)
        resume.assert_not_called()
        self.assertFalse(result["dispatched"])

    def test_both_exhausted_wait_without_process_attempt(self):
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"]["observations"].update({
            "claude-code": observation(100, reset_hours=4),
            "codex": observation(100, source="codex_runtime", reset_hours=2, ordinaryUsageAllowed=True)}))
        with mock.patch.object(production, "resume_relay_worker") as resume:
            result = production.relay_tick(CURRENT)
        resume.assert_not_called()
        self.assertEqual(result["action"], "wait")
        self.assertEqual(result["provider"], "codex")
        self.popen.assert_not_called()

    def test_unverified_usage_requests_no_model_or_process_attempt(self):
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"].update(
            observations={"claude-code": {}, "codex": {}}))
        before = self.current_state()
        with mock.patch.object(production, "resume_relay_worker") as resume:
            result = production.relay_tick(CURRENT)
        resume.assert_not_called()
        self.assertEqual(result["action"], "blocked")
        self.assertEqual(self.current_state(), before)
        self.popen.assert_not_called()

    def test_human_final_stop_does_not_dispatch_pending_job(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            status="ready_for_human_review"))
        with mock.patch.object(production, "resume_relay_worker") as resume:
            result = production.relay_tick(CURRENT)
        resume.assert_not_called()
        self.assertEqual(result["action"], "paused")
        self.popen.assert_not_called()


class ResumeGuardFixture(HandoffFixture):
    def setUp(self):
        super().setUp()
        production.write(self.logs / "worker-mcp.json", {"mcpServers": {}})
        production.write(self.logs / "worker-settings.json", {"disableAllHooks": True})
        self.put(self.logs / "assignment.md", "Only edit owned coastal source. No Unity or original files.\n")
        self.checkpoint(safe_point=True)

    def stored_job(self):
        return self.current_state()["productionEnvironment"]["workers"][self.job["id"]]

    def assert_no_dispatch(self):
        self.popen.assert_not_called()
        self.run.assert_not_called()


class AppServerCheckpointTests(HandoffFixture):
    def simulate(self, events, usage):
        """Exercise real supervisor control flow with a process-free RPC double."""
        job = self.current_state()["productionEnvironment"]["workers"][self.job["id"]]
        job.update(provider="codex", sessionId=None, status="assigned", executionMethod="app_server",
                   requestedModel="requested-codex-model", requestedEffort="high", timeoutMinutes=10)
        self.mutate(lambda state: state["productionEnvironment"]["workers"].update({job["id"]: copy.deepcopy(job)}))
        self.put(self.logs / "assignment.md", "Only finish the owned dune method. No Unity or original edits.\n")
        elapsed = [0]
        pending = list(events)
        requests = []
        fixture = self
        class FakeServer:
            _process = SimpleNamespace(pid=999)
            diagnostics = {"fixture": "no process or network", "mcpServers": []}
            closed = False

            def __enter__(self):
                return self

            def __exit__(self, *args):
                self.closed = True
                return False

            def request(self, method, params):
                requests.append((method, copy.deepcopy(params)))
                if method == "thread/start":
                    fixture.assertEqual(params["cwd"], str(fixture.wt))
                    fixture.assertEqual(params["runtimeWorkspaceRoots"], [str(fixture.wt)])
                    fixture.assertEqual(params["sandbox"], "workspace-write")
                    return {"thread": {"id": SESSION}, "model": "actual-response-model",
                        "reasoningEffort": "high", "sandbox": "workspace-write",
                        "runtimeWorkspaceRoots": [str(fixture.wt)]}
                if method == "turn/start":
                    fixture.assertEqual(params["sandboxPolicy"]["writableRoots"], [str(fixture.wt)])
                    fixture.assertFalse(params["sandboxPolicy"]["networkAccess"])
                    return {"turn": {"id": "fixture-turn"}}
                if method == "account/rateLimits/read":
                    return {"fixture": "usage response"}
                if method == "turn/interrupt":
                    return {}
                raise AssertionError("unexpected RPC method: " + method)

            def next_event(self, timeout):
                fixture.assertEqual(timeout, 3)
                if not pending:
                    raise AssertionError("supervisor exceeded bounded fixture events")
                elapsed[0], event = pending.pop(0)
                return copy.deepcopy(event)

        server = FakeServer()
        module = SimpleNamespace(CodexAppServer=lambda worktree: server,
                                 normalize_usage=lambda result: copy.deepcopy(usage))
        actual_checkpoint = production.relay_checkpoint
        self.checkpoint_closed_states = []
        def save_checkpoint(*args, **kwargs):
            self.checkpoint_closed_states.append((args[2], server.closed))
            return actual_checkpoint(*args, **kwargs)
        with mock.patch.object(production, "codex_bridge_module", return_value=module), \
             mock.patch.object(production.time, "monotonic", side_effect=lambda: elapsed[0]), \
             mock.patch.object(production, "relay_checkpoint", side_effect=save_checkpoint) as checkpoint:
            production.supervise_codex_appserver(job)
        self.popen.assert_not_called()
        self.run.assert_not_called()
        return self.current_state()["productionEnvironment"]["workers"][job["id"]], requests, checkpoint.call_args_list

    @staticmethod
    def message():
        return {"method": "item/completed", "params": {"item": {
            "type": "agentMessage", "text": "Owned partial preserved; Unity validation not run."}}}

    @staticmethod
    def completed(status="completed"):
        return {"method": "turn/completed", "params": {"turn": {"id": "fixture-turn", "status": status}}}

    def test_unknown_usage_still_takes_periodic_source_checkpoint_without_interrupting(self):
        before = self.current_state()["productionEnvironment"]["coordinatorLease"]
        job, requests, checkpoints = self.simulate([(1, self.message()), (121, None), (122, self.completed())],
            observation(source="codex_runtime", ordinaryUsageAllowed=None))
        self.assertIn("periodic", [call.args[2] for call in checkpoints])
        self.assertNotIn("turn/interrupt", [method for method, _ in requests])
        self.assertEqual(job["status"], "ready_for_review")
        self.assertTrue(job["relayCheckpoint"]["validation"].startswith("UNVERIFIED"))
        self.assertEqual(Path(job["relayCheckpoint"]["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"], before)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_unexpected_verified_quota_preserves_partial_stream_and_registers_handoff(self):
        failed = self.completed("failed")
        failed["params"]["turn"]["error"] = {"message": "fixture usage limit", "codexErrorInfo": "usageLimitExceeded"}
        job, requests, checkpoints = self.simulate([(1, self.message()), (2, failed)],
            observation(100, source="codex_runtime", ordinaryUsageAllowed=False))
        self.assertEqual(job["status"], "handoff_pending")
        self.assertEqual(job["relayContinuation"]["status"], "pending")
        self.assertIn("usage_boundary", [call.args[2] for call in checkpoints])
        self.assertEqual(Path(job["relayCheckpoint"]["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)
        self.assertIn("usageLimitExceeded", (self.logs / "codex-app-server.jsonl").read_text(encoding="utf-8"))
        self.assertIn("Owned partial preserved", (self.logs / "HANDOFF.md").read_text(encoding="utf-8"))
        self.assertEqual((self.logs / "source-baseline" / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual(self.current_state()["demoProductionRouting"]["unityEditorOwner"], "claude-code")

    def test_near_limit_interrupts_at_a_completed_nonunity_tool_boundary(self):
        boundary = {"method": "item/completed", "params": {"item": {"type": "commandExecution"}}}
        job, requests, checkpoints = self.simulate(
            [(1, self.message()), (61, boundary), (62, self.completed("interrupted"))],
            observation(95, source="codex_runtime", ordinaryUsageAllowed=True))
        interrupts = [(method, params) for method, params in requests if method == "turn/interrupt"]
        self.assertEqual(interrupts, [("turn/interrupt", {"threadId": SESSION, "turnId": "fixture-turn"})])
        self.assertEqual(job["status"], "handoff_pending")
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"]["owner"], "claude-code")

    def test_observed_near_limit_waits_for_next_completed_boundary_not_the_next_poll(self):
        started = {"method": "item/started", "params": {"item": {"type": "commandExecution"}}}
        boundary = {"method": "item/completed", "params": {"item": {"type": "commandExecution"}}}
        job, requests, checkpoints = self.simulate(
            [(1, self.message()), (61, started), (62, boundary), (63, self.completed("interrupted"))],
            observation(95, source="codex_runtime", ordinaryUsageAllowed=True))
        self.assertEqual([method for method, _ in requests].count("turn/interrupt"), 1)
        self.assertEqual(job["status"], "handoff_pending")

    def test_fatal_auth_model_context_and_permission_errors_are_not_quota_retries(self):
        # Official app-server schema exposes camelCase enum values, not snake_case only:
        # github.com/openai/codex/blob/main/codex-rs/app-server-protocol/schema/json/v2/ThreadStartResponse.json
        errors = [
            {"codexErrorInfo": "unauthorized", "message": "fixture authentication failure"},
            {"codexErrorInfo": "contextWindowExceeded", "message": "fixture nonquota failure"},
            {"codexErrorInfo": "sandboxError", "message": "fixture nonquota failure"},
            {"codexErrorInfo": "badRequest", "message": "Model not found"},
            {"code": "model_not_found", "message": "fixture nonquota failure"},
            {"codexErrorInfo": {"httpConnectionFailed": {"httpStatusCode": 401}}, "message": "fixture"},
            {"codexErrorInfo": {"httpConnectionFailed": {"httpStatusCode": 403}}, "message": "fixture"},
        ]
        for error in errors:
            with self.subTest(error=error):
                failed = self.completed("failed")
                failed["params"]["turn"]["error"] = error
                job, requests, checkpoints = self.simulate([(1, self.message()), (2, failed)],
                    observation(100, source="codex_runtime", ordinaryUsageAllowed=False))
                self.assertEqual(job["status"], "failed_preserved")
                self.assertNotIn("usage_boundary", [call.args[2] for call in checkpoints])
                self.assertIn("worker_finished", [call.args[2] for call in checkpoints])
                self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)

    def test_timeout_with_terminal_ack_is_portable_only_after_server_closes(self):
        job, requests, checkpoints = self.simulate(
            [(1, self.message()), (601.1, None), (601, self.completed("interrupted"))],
            observation(source="codex_runtime", ordinaryUsageAllowed=True))
        self.assertEqual([method for method, _ in requests].count("turn/interrupt"), 1)
        self.assertEqual(job["status"], "handoff_pending")
        self.assertEqual(job["relayContinuation"]["reason"], "inactivity_checkpoint")
        self.assertTrue(job["relayCheckpoint"]["safePoint"])
        self.assertEqual(self.checkpoint_closed_states[-1], ("time_boundary", True))
        self.assertEqual(Path(job["relayCheckpoint"]["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)

    def test_timeout_without_terminal_ack_preserves_partial_but_cannot_resume(self):
        job, requests, checkpoints = self.simulate(
            [(1, self.message()), (601.1, None), (620, None)],
            observation(source="codex_runtime", ordinaryUsageAllowed=True))
        self.assertEqual([method for method, _ in requests].count("turn/interrupt"), 1)
        self.assertEqual(job["status"], "failed_preserved")
        self.assertFalse(job["relayCheckpoint"]["safePoint"])
        self.assertEqual(self.checkpoint_closed_states[-1], ("worker_finished", True))
        self.assertEqual(Path(job["relayCheckpoint"]["files"][0]["snapshot"]).read_text(encoding="utf-8"), self.partial)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)


class ObserveControlTests(HandoffFixture):
    def setUp(self):
        super().setUp()
        self.control_time = [CURRENT]
        time_reference = self.control_time
        actual_datetime = dt.datetime
        class Clock(actual_datetime):
            @classmethod
            def now(cls, tz=None):
                return time_reference[0].astimezone(tz) if tz is not None else time_reference[0].replace(tzinfo=None)
        self.patches.enter_context(mock.patch.object(production.dt, "datetime", Clock))
        self.patches.enter_context(mock.patch.object(production, "now", side_effect=lambda: self.control_time[0].isoformat()))
        def git(*args, **kwargs):
            if args[:2] == ("rev-parse", "HEAD"):
                return "approved-current-local-head"
            if args[:2] == ("diff", "--name-only"):
                return SOURCE
            if args[0] == "ls-files":
                return ""
            raise AssertionError("unexpected git operation: " + repr(args))
        self.git.side_effect = git

    def observe(self, *, percent=None, context_percent=10, session_id=SESSION):
        limits = [] if percent is None else observation(percent)["rateLimits"]
        return production.autonomy_control(SimpleNamespace(operation="observe", session_id=session_id,
            observation=json.dumps({"rateLimits": limits, "context": {"percent": context_percent},
                                    "model": "actual-mods-model", "version": "fixture"})))

    def test_unknown_usage_still_requests_twenty_minute_safe_checkpoint(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            cycleStartedAt=(CURRENT-dt.timedelta(minutes=21)).isoformat()))
        lease = self.current_state()["productionEnvironment"]["coordinatorLease"]
        self.observe()
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertEqual(control["relayCheckpointRequest"]["reason"], "periodic_safe_boundary")
        self.assertEqual(production.relay_usage_state(
            self.current_state()["productionEnvironment"]["relaySupport"]["observations"]["claude-code"], CURRENT
        )["status"], "unknown")
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"], lease)
        self.popen.assert_not_called()

    def test_observation_snapshots_follow_cadence_without_repeated_identical_copies(self):
        actual_checkpoint = production.relay_checkpoint
        with mock.patch.object(production, "relay_checkpoint", wraps=actual_checkpoint) as checkpoint:
            self.observe()
            self.control_time[0] += dt.timedelta(seconds=30)
            self.observe()
            self.control_time[0] += dt.timedelta(seconds=91)
            self.observe()
        self.assertEqual(checkpoint.call_count, 2)
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.popen.assert_not_called()

    def test_near_limit_requests_boundary_without_transferring_live_unity(self):
        self.alive.return_value = True
        lease = self.current_state()["productionEnvironment"]["coordinatorLease"]
        self.observe(percent=95)
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertEqual(control["relayCheckpointRequest"]["reason"], "near_usage_limit")
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"], lease)
        self.popen.assert_not_called()

    def test_context_pressure_requests_handoff_at_safe_boundary(self):
        self.observe(percent=20, context_percent=85)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"][
            "relayCheckpointRequest"]["reason"], "context_pressure")
        self.assertEqual(self.current_state()["demoProductionRouting"]["unityEditorOwner"], "claude-code")
        self.popen.assert_not_called()

    def test_foreign_session_cannot_request_checkpoint_or_snapshot(self):
        before = self.state_path.read_bytes()
        with mock.patch.object(production, "relay_checkpoint") as checkpoint:
            self.observe(percent=100, session_id="unrelated-session")
        checkpoint.assert_not_called()
        self.assertEqual(self.state_path.read_bytes(), before)
        self.popen.assert_not_called()

    def test_existing_pause_is_preserved_during_near_limit_observation(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            enabled=False, stopRequested=True, status="pause_requested"))
        self.observe(percent=100)
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertFalse(control["enabled"])
        self.assertTrue(control["stopRequested"])
        self.assertEqual(control["status"], "pause_requested")
        self.assertEqual(production.relay_choice(self.current_state(), CURRENT)["action"], "paused")
        self.popen.assert_not_called()

    def test_malformed_prior_timestamps_do_not_break_optional_observation(self):
        for previous in ("unknown", "2026-10-11T14:00:00"):
            with self.subTest(previous=previous):
                self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                    lastRelaySnapshotAt=previous, lastSafeCheckpointAt=previous))
                self.observe()
                self.assertTrue(self.current_state()["productionEnvironment"]["autonomous"]["modLoaded"])
                self.popen.assert_not_called()


class ResumeGuardTests(ResumeGuardFixture):
    def test_running_snapshot_without_confirmed_safe_point_cannot_resume(self):
        self.checkpoint(safe_point=False)
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "preserved|promotable"):
            production.resume_relay_worker(self.stored_job(), "codex")
        self.assertEqual(self.state_path.read_bytes(), before)
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_blocked_codex_cannot_launch_or_allocate_attempt(self):
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"]["teams"]["codex"].update(
            capability="blocked_runtime_read_only"))
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "capability"):
            production.resume_relay_worker(self.stored_job(), "codex")
        self.assertEqual(self.state_path.read_bytes(), before)
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_no_verified_team_headroom_prevents_attempt(self):
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"]["observations"].update({
            "claude-code": observation(100),
            "codex": observation(100, source="codex_runtime", ordinaryUsageAllowed=True)}))
        with self.assertRaisesRegex(RuntimeError, "headroom"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_near_limit_team_cannot_resume_even_if_other_team_available(self):
        self.mutate(lambda state: state["productionEnvironment"]["relaySupport"]["observations"].update(
            {"claude-code": observation(95)}))
        lease = self.current_state()["productionEnvironment"]["coordinatorLease"]
        with self.assertRaisesRegex(RuntimeError, "headroom"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"], lease)
        self.assert_no_dispatch()

    def test_running_prior_worker_blocks_duplicate_launch(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(claudePid=888))
        self.alive.side_effect = lambda pid: pid == 888
        with self.assertRaisesRegex(RuntimeError, "Previous worker still running"):
            production.resume_relay_worker(self.stored_job(), "codex")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_worker_generation_mismatch_stops_resume(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            relayGeneration="expired-generation"))
        with self.assertRaisesRegex(RuntimeError, "generation changed"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_original_hash_mismatch_stops_resume_without_overwrite(self):
        self.put(self.root / SOURCE, "user edit preserved during handoff\n")
        with self.assertRaisesRegex(RuntimeError, "Original baseline changed"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), "user edit preserved during handoff\n")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_head_mismatch_stops_resume_without_switching_branches(self):
        self.git.return_value = "new-user-head"
        with self.assertRaisesRegex(RuntimeError, "Original baseline changed"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_other_reserved_file_owner_prevents_resume(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"].update({"other": {
            "id": "other", "status": "ready_for_review", "allowedFiles": [SOURCE]}}))
        with self.assertRaisesRegex(RuntimeError, "File owned by another worker"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_consumed_continuation_cannot_be_claimed_again_after_selector_race(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]][
            "relayContinuation"].update(status="consumed"))
        with self.assertRaisesRegex(RuntimeError, "pending|preserved"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_current_immutable_baseline_is_rechecked_before_process_launch(self):
        self.put(self.logs / "source-baseline" / SOURCE, "baseline changed after checkpoint\n")
        with self.assertRaisesRegex(RuntimeError, "Immutable baseline changed"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assert_no_dispatch()

    def test_human_final_stop_cannot_be_bypassed_by_direct_resume(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            status="ready_for_human_review"))
        with self.assertRaisesRegex(RuntimeError, "headroom"):
            production.resume_relay_worker(self.stored_job(), "claude-code")
        self.assertFalse((self.logs / "attempts").exists())
        self.assert_no_dispatch()

    def test_mocked_cross_team_resume_retains_logs_baseline_and_live_unity_lease(self):
        before = self.current_state()
        old_logs = {name: (self.logs / name).read_bytes()
                    for name in ("claude.jsonl", "claude.stderr", "HANDOFF.md")}
        def start(command, **kwargs):
            self.assertEqual(command[-2:], ["--id", self.job["id"]])
            self.assertIn('--registered-start', command)
            self.assertEqual(kwargs["cwd"], self.root)
            kwargs["stdout"].close()
            return SimpleNamespace(pid=51300, stdin=mock.Mock())
        self.popen.side_effect = start
        result = production.resume_relay_worker(self.stored_job(), "codex")
        self.assertEqual(result["action"], "resumed")
        self.assertEqual(result["provider"], "codex")
        after = self.current_state()
        job = after["productionEnvironment"]["workers"][self.job["id"]]
        self.assertEqual(job["provider"], "codex")
        self.assertIsNone(job["sessionId"], "Claude UUID must not be reused as a Codex session")
        self.assertEqual(job["actualModels"], [])
        self.assertEqual(job["appliedEffort"], "UNVERIFIED")
        self.assertEqual(job["supervisorPid"], 51300)
        self.assertEqual(job["worktree"], str(self.wt))
        self.assertEqual(job["baseHead"], "approved-current-local-head")
        self.assertEqual(job["originalHashes"], self.job["originalHashes"])
        self.assertEqual(job["attempts"][-1]["provider"], "claude-code")
        self.assertEqual(job["attempts"][-1]["sessionId"], SESSION)
        self.assertEqual(job["attempts"][-1]["logDirectory"], str(self.logs))
        self.assertEqual(after["productionEnvironment"]["coordinatorLease"],
                         before["productionEnvironment"]["coordinatorLease"])
        self.assertEqual(after["demoProductionRouting"], before["demoProductionRouting"])
        self.assertEqual((self.root / SOURCE).read_text(encoding="utf-8"), self.baseline)
        self.assertEqual((self.wt / SOURCE).read_text(encoding="utf-8"), self.partial)
        self.assertEqual((Path(job["logDirectory"]) / "source-baseline" / SOURCE).read_text(encoding="utf-8"),
                         self.baseline)
        self.assertEqual(production.read(Path(job["logDirectory"]) / "worker-mcp.json"), {"mcpServers": {}})
        for name, value in old_logs.items():
            self.assertEqual((self.logs / name).read_bytes(), value)
        self.run.assert_not_called()


class LeadMaintenanceTests(HandoffFixture):
    def setUp(self):
        super().setUp()
        self.lead_time = [CURRENT]
        time_reference = self.lead_time
        actual_datetime = dt.datetime
        class Clock(actual_datetime):
            @classmethod
            def now(cls, tz=None):
                return time_reference[0].astimezone(tz) if tz is not None else time_reference[0].replace(tzinfo=None)
        self.patches.enter_context(mock.patch.object(production.dt, "datetime", Clock))
        self.patches.enter_context(mock.patch.object(production, "now", side_effect=lambda: self.lead_time[0].isoformat()))
        self.patches.enter_context(mock.patch.object(production, "MOD", self.root / "project-mod"))
        self.patches.enter_context(mock.patch.object(production, "doctor", return_value=SimpleNamespace(
            selection=lambda state, owner: {"nextTicket": "approved-quality-work"})))
        self.patches.enter_context(mock.patch.object(production, "actual_effort", return_value="high"))
        self.fingerprint = self.patches.enter_context(mock.patch.object(production, "game_fingerprint", return_value="same"))
        self.patches.enter_context(mock.patch.object(production.shutil, "which", return_value="claude"))
        self.maintenance = self.patches.enter_context(mock.patch.object(production, "relay_tick", return_value={"action": "blocked"}))
        self.observe_usage = self.patches.enter_context(mock.patch.object(production, "observe_codex_usage", return_value={}))
        # Authority/approval re-verification is covered by test_autonomy.ResumeGateTests.
        self.patches.enter_context(mock.patch.object(production, "resume_gate_errors", return_value=[]))
        self.patches.enter_context(mock.patch.object(production, "schedule_local_resume", return_value={"registered": True}))
        self.patches.enter_context(mock.patch.object(production, "unschedule_local_resume"))
        self.patches.enter_context(mock.patch.object(production.time, "monotonic", return_value=60))
        self.run.side_effect = None
        self.run.return_value = SimpleNamespace(returncode=0, stdout="[]")
        production.write(self.root / ".claude/settings.local.json", {"disableAllHooks": True, "enabledPlugins": {}})
        production.write(self.root / ".mcp.json", {"mcpServers": {}})
        self.args = SimpleNamespace(owner="claude-code", headless=False, intake_check=False,
            resume_session=SESSION, foreground=True, lease_token=GENERATION, prepare_session=True, effort="high")
        self.report = {"routing": {"nextTicket": "approved-quality-work"}}
        def git(*args, **kwargs):
            if args[:2] == ("rev-parse", "HEAD"):
                return "approved-current-local-head"
            if args[:2] == ("diff", "--name-only"):
                return SOURCE
            if args[0] == "ls-files":
                return ""
            raise AssertionError("unexpected git operation: " + repr(args))
        self.git.side_effect = git

    def start(self):
        return production.autonomous_start(self.args, self.report)

    def mock_lead(self, callback):
        commands = []
        fixture = self
        def popen(command, **kwargs):
            commands.append(copy.deepcopy(command))
            result = {"type": "result", "is_error": False, "result": "Fixture safe checkpoint; no product claim"}
            kwargs["stdout"].write(json.dumps(result) + "\n")
            kwargs["stdout"].flush()
            class Prompt(io.StringIO):
                def write(self, text):
                    callback(len(commands), command)
                    return super().write(text)
            return SimpleNamespace(pid=999, returncode=0, stdin=Prompt(), poll=lambda: 0)
        self.popen.side_effect = popen
        return commands

    def test_adopted_verified_wait_preserves_reset_without_premature_claude_launch(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            status="waiting_usage_reset", nextResumeAt=(CURRENT+dt.timedelta(hours=2)).isoformat()))
        def pause(_):
            self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                enabled=False, stopRequested=True, status="pause_requested"))
        with mock.patch.object(production.time, "sleep", side_effect=pause):
            self.start()
        self.popen.assert_not_called()
        self.assertGreaterEqual(self.maintenance.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"], "paused_by_user")

    def test_final_human_boundary_is_not_restarted_by_foreground_supervisor(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(status="ready_for_human_review"))
        self.start()
        self.popen.assert_not_called()
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"], "ready_for_human_review")

    def test_generation_change_during_registry_prevents_old_lead_launch(self):
        def registry(*args, **kwargs):
            self.mutate(lambda state: (
                state["productionEnvironment"]["autonomous"].update(generation="new-generation"),
                state["productionEnvironment"]["coordinatorLease"].update(generation="new-generation")))
            return SimpleNamespace(returncode=0, stdout="[]")
        self.run.side_effect = registry
        self.start()
        self.popen.assert_not_called()
        self.assertEqual(self.current_state()["productionEnvironment"]["coordinatorLease"]["generation"], "new-generation")

    def test_old_result_cannot_overwrite_a_newer_lease_coordinator(self):
        def replaced_during_turn(count, command):
            self.mutate(lambda state: (
                state["productionEnvironment"]["autonomous"].update(generation="new-generation"),
                state["productionEnvironment"]["coordinatorLease"].update(generation="new-generation", sessionId="new-owner"),
                state["productionEnvironment"]["coordinator"].update(status="new-owner-running", sessionId="new-owner")))
        self.mock_lead(replaced_during_turn)
        self.start()
        coordinator = self.current_state()["productionEnvironment"]["coordinator"]
        self.assertEqual(coordinator["status"], "new-owner-running")
        self.assertEqual(coordinator["sessionId"], "new-owner")
        self.assertTrue(list((self.root / "Logs/ParallelProduction").glob("ClaudeLead-*/result.json")))

    def test_context_renewal_uses_new_session_and_retains_prior_session_history(self):
        self.fingerprint.side_effect = ["before-change", "after-change", "next-cycle-baseline"]
        def checkpoint(count, command):
            active_sid = self.current_state()["productionEnvironment"]["coordinatorLease"]["sessionId"]
            self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(checkpoint={
                "sessionId": active_sid, "recordedAt": "2099-01-01T00:00:00+09:00",
                "disposition": "continue" if count == 1 else "blocked", "nextAction": "bounded unfinished step",
                "reason": "context_pressure" if count == 1 else None, "evidence": []}))
            self.lead_time[0] += dt.timedelta(seconds=1)
        commands = self.mock_lead(checkpoint)
        self.start()
        self.assertEqual(len(commands), 2)
        self.assertIn("--resume", commands[0])
        self.assertEqual(commands[0][commands[0].index("--resume")+1], SESSION)
        self.assertIn("--session-id", commands[1])
        self.assertNotIn("--resume", commands[1])
        new_sid = commands[1][commands[1].index("--session-id")+1]
        self.assertNotEqual(new_sid, SESSION)
        state = self.current_state()
        self.assertEqual(state["productionEnvironment"]["autonomous"]["priorSessions"][-1]["sessionId"], SESSION)
        self.assertEqual(state["productionEnvironment"]["coordinatorLease"]["sessionId"], new_sid)
        self.assertEqual(state["productionEnvironment"]["coordinatorLease"]["generation"], GENERATION)
        self.assertEqual(state["demoProductionRouting"]["unityEditorOwner"], "claude-code")


class AtomicWorkerClaimTests(HandoffFixture):
    def setUp(self):
        super().setUp()
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            status="assigned", provider="codex", executionMethod="app_server", attemptToken="active-attempt"))

    def stored_job(self):
        return self.current_state()["productionEnvironment"]["workers"][self.job["id"]]

    def test_duplicate_supervisor_entry_cannot_claim_same_attempt_twice(self):
        with mock.patch.object(production, "supervise_codex_appserver") as supervisor:
            production.supervise(SimpleNamespace(id=self.job["id"]))
            first = self.stored_job()
            self.assertEqual(first["status"], "starting")
            self.assertEqual(first["attemptToken"], "active-attempt")
            with self.assertRaisesRegex(RuntimeError, "already claimed|duplicate"):
                production.supervise(SimpleNamespace(id=self.job["id"]))
        supervisor.assert_called_once()
        self.assertEqual(self.stored_job(), first)
        self.popen.assert_not_called()

    def test_stale_generation_supervisor_cannot_claim_or_launch(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            relayGeneration="stale-generation"))
        before = self.state_path.read_bytes()
        with mock.patch.object(production, "supervise_codex_appserver") as supervisor:
            with self.assertRaisesRegex(RuntimeError, "generation changed"):
                production.supervise(SimpleNamespace(id=self.job["id"]))
        supervisor.assert_not_called()
        self.assertEqual(self.state_path.read_bytes(), before)
        self.popen.assert_not_called()

    def test_record_preserves_parent_pid_while_merging_child_result(self):
        self.mutate(lambda state: state["productionEnvironment"]["workers"][self.job["id"]].update(
            status="running", supervisorPid=51300))
        incoming = self.stored_job()
        incoming.update(supervisorPid=None, usage={"output_tokens": 23}, status="ready_for_review")
        production.record(incoming)
        actual = self.stored_job()
        self.assertEqual(actual["supervisorPid"], 51300)
        self.assertEqual(actual["usage"], {"output_tokens": 23})
        self.assertEqual(actual["status"], "ready_for_review")

    def test_old_attempt_result_cannot_overwrite_new_attempt(self):
        incoming = self.stored_job()
        incoming.update(attemptToken="old-attempt", status="ready_for_review")
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "attempt changed"):
            production.record(incoming)
        self.assertEqual(self.state_path.read_bytes(), before)

    def test_missing_attempt_token_cannot_overwrite_claimed_attempt(self):
        incoming = self.stored_job()
        incoming.pop("attemptToken")
        incoming["status"] = "ready_for_review"
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "attempt changed"):
            production.record(incoming)
        self.assertEqual(self.state_path.read_bytes(), before)

    def test_record_from_old_production_generation_cannot_replace_current_state(self):
        incoming = self.stored_job()
        self.mutate(lambda state: state["productionEnvironment"]["coordinatorLease"].update(
            generation="new-generation"))
        before = self.state_path.read_bytes()
        with self.assertRaisesRegex(RuntimeError, "generation changed"):
            production.record(incoming)
        self.assertEqual(self.state_path.read_bytes(), before)


if __name__ == "__main__":
    unittest.main()

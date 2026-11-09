"""Independent autonomy checks; fixtures stay under this test directory.

These tests never read/write the real loop state, start a process, or touch Unity.
Run: python -B -X utf8 Tools/LoopEngineering/tests/test_autonomy.py -v
"""
import contextlib
import copy
import datetime as dt
import importlib.util
import io
import json
import math
import os
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

sys.dont_write_bytecode = True
TEST_DIR = Path(__file__).resolve().parent
HELPER = TEST_DIR.parent / "Invoke-ProjectPAProduction.py"
spec = importlib.util.spec_from_file_location("pa_autonomy_under_test", HELPER)
production = importlib.util.module_from_spec(spec)
spec.loader.exec_module(production)
CURRENT = dt.datetime(2026, 10, 11, 10, 0, tzinfo=production.KST)
SESSION = "3f66c90c-d5eb-4a51-9c99-44d7744d2310"


class QuotaResetTests(unittest.TestCase):
    def test_latest_exhausted_server_reset_includes_weekly_limit(self):
        limits = [
            {"kind": "session", "percentUsed": 100, "resetsAt": "2026-10-11T15:00:00+09:00"},
            {"kind": "weekly", "percentUsed": 100, "resetsAt": "2026-10-15T02:00:00Z"},
            {"kind": "other", "percentUsed": 42, "resetsAt": "2026-11-01T00:00:00Z"},
        ]
        self.assertEqual(production.future_quota_reset("", limits, CURRENT),
                         dt.datetime(2026, 10, 15, 11, tzinfo=production.KST))

    def test_unexhausted_limit_does_not_schedule_retry(self):
        self.assertIsNone(production.future_quota_reset("", [{"percentUsed": 99.9,
            "resetsAt": "2026-10-12T01:00:00Z"}], CURRENT))

    def test_server_epoch_seconds_and_milliseconds(self):
        future = CURRENT + dt.timedelta(hours=3)
        for reset in (future.timestamp(), int(future.timestamp() * 1000)):
            with self.subTest(reset=reset):
                self.assertEqual(production.future_quota_reset("", [{"percentUsed": 100,
                    "resetsAt": reset}], CURRENT), future)

    def test_past_naive_and_unknown_server_resets_are_not_guessed(self):
        for reset in ("2026-10-11T09:00:00+09:00", "2026-10-12T10:00:00",
                      "next week", None, -1, math.inf):
            with self.subTest(reset=reset):
                self.assertIsNone(production.future_quota_reset("", [{"percentUsed": 100,
                    "resetsAt": reset}], CURRENT))

    def test_unknown_exhaustion_cannot_establish_a_reset(self):
        for percent in ("unknown", "NaN", float("nan"), "Infinity", float("inf")):
            with self.subTest(percent=percent):
                self.assertIsNone(production.future_quota_reset("", [{"percentUsed": percent,
                    "resetsAt": "2026-10-12T01:00:00Z"}], CURRENT))

    def test_current_timezone_banner_is_used_without_rolling_to_tomorrow(self):
        self.assertEqual(production.future_quota_reset(
            "You've hit your session limit · resets 3:45pm (Asia/Seoul)", [], CURRENT),
            CURRENT.replace(hour=15, minute=45))
        self.assertIsNone(production.future_quota_reset(
            "You've hit your session limit · resets 3am (Asia/Seoul)", [], CURRENT))

    def test_midnight_noon_and_invalid_banner_hours(self):
        early = CURRENT.replace(hour=0, minute=0)
        self.assertEqual(production.future_quota_reset("resets 12pm (Asia/Seoul)", [], early),
                         early.replace(hour=12))
        self.assertIsNone(production.future_quota_reset("resets 12am (Asia/Seoul)", [], CURRENT))
        for text in ("resets 13pm (Asia/Seoul)", "resets 3:75pm (Asia/Seoul)",
                     "resets 3pm", "resets Monday", "resets 3pm (UTC)"):
            with self.subTest(text=text):
                self.assertIsNone(production.future_quota_reset(text, [], CURRENT))


class QuotaEventTests(unittest.TestCase):
    @staticmethod
    def rejected(reset=1791612000, windows=None):
        return {"type": "rate_limit_event", "rate_limit_info": {"status": "rejected",
            "rateLimitType": "five_hour", "resetsAt": reset,
            "unifiedWindows": windows or {"five_hour": {"utilization": 1, "resetsAt": reset},
                                          "seven_day": {"utilization": .17, "resetsAt": 1791907200}}}}

    def test_recorded_october_tenth_server_epoch_is_used_only_before_reset(self):
        event = self.rejected()
        expected = dt.datetime.fromtimestamp(1791612000, production.KST)
        self.assertEqual(production.quota_reset_from_events({"is_error": True}, [event], {},
            expected - dt.timedelta(hours=1)), expected)
        self.assertIsNone(production.quota_reset_from_events({"is_error": True}, [event], {},
            expected + dt.timedelta(seconds=1)))

    def test_weekly_exhaustion_is_not_replaced_by_earlier_session_reset(self):
        hourly = (CURRENT + dt.timedelta(hours=2)).timestamp()
        weekly = (CURRENT + dt.timedelta(days=5)).timestamp()
        event = self.rejected(hourly, {"five_hour": {"utilization": 1, "resetsAt": hourly},
                                     "seven_day": {"utilization": 1, "resetsAt": weekly}})
        self.assertEqual(production.quota_reset_from_events({}, [event], {}, CURRENT),
                         CURRENT + dt.timedelta(days=5))

    def test_prose_about_quota_does_not_establish_an_actual_api_error(self):
        result = {"is_error": True, "result": "Earlier: You've hit your session limit · resets 3pm (Asia/Seoul)"}
        observation = {"rateLimits": [{"percentUsed": 100, "resetsAt": "2026-10-12T01:00:00Z"}]}
        self.assertIsNone(production.quota_reset_from_events(result, [], observation, CURRENT))

    def test_authentication_model_and_credit_errors_override_stale_quota_data(self):
        future = (CURRENT + dt.timedelta(days=1)).timestamp()
        for code in ("authentication_error", "invalid_api_key", "insufficient_credits",
                     "context_length_exceeded", "model_not_found"):
            with self.subTest(code=code):
                self.assertIsNone(production.quota_reset_from_events({"api_error": code,
                    "result": "usage limit resets 3pm (Asia/Seoul)"}, [self.rejected(future)], {}, CURRENT))

    def test_allowed_stream_rate_limit_event_is_not_exhaustion(self):
        event = self.rejected((CURRENT + dt.timedelta(hours=2)).timestamp())
        event["rate_limit_info"]["status"] = "allowed_warning"
        self.assertIsNone(production.quota_reset_from_events({}, [event], {}, CURRENT))

    def test_actual_synthetic_api_error_can_use_verified_mod_reset(self):
        event = {"type": "assistant", "is_api_error_message": True, "error": "rate_limit",
                 "message": {"model": "<synthetic>"}}
        observation = {"rateLimits": [{"percentUsed": 100, "resetsAt": "2026-10-12T01:00:00Z"}]}
        self.assertEqual(production.quota_reset_from_events({}, [event], observation, CURRENT),
                         dt.datetime(2026, 10, 12, 10, tzinfo=production.KST))

    def test_malformed_window_does_not_discard_known_rejected_reset(self):
        reset = (CURRENT + dt.timedelta(hours=2)).timestamp()
        event = self.rejected(reset, {"unknown_window": {"utilization": "unknown", "resetsAt": reset}})
        self.assertEqual(production.quota_reset_from_events({}, [event], {}, CURRENT),
                         CURRENT + dt.timedelta(hours=2))

    def test_null_error_text_and_limits_do_not_guess_reset(self):
        self.assertIsNone(production.quota_reset_from_events({"api_error": "usage_limit_reached",
            "result": None}, [], {"rateLimits": None}, CURRENT))

    def test_paid_overage_event_waits_for_the_plan_reset(self):
        reset = (CURRENT + dt.timedelta(hours=2)).timestamp()
        event = self.rejected(reset)
        event["rate_limit_info"].update(status="allowed", isUsingOverage=True)
        self.assertEqual(production.quota_reset_from_events({}, [event], {}, CURRENT),
                         CURRENT + dt.timedelta(hours=2))


class OverageWatchTests(unittest.TestCase):
    def test_partial_lines_and_allowed_events_are_not_overage(self):
        with tempfile.TemporaryDirectory(prefix=".autonomy-test-", dir=TEST_DIR) as folder:
            stream = Path(folder) / "claude.jsonl"
            watch = production.OverageWatch(stream)
            self.assertIsNone(watch.check())
            allowed = {"type": "rate_limit_event", "rate_limit_info": {"status": "allowed_warning",
                       "isUsingOverage": False, "overageEnabled": True}}
            overage = {"type": "rate_limit_event", "rate_limit_info": {"status": "allowed", "isUsingOverage": True}}
            line = json.dumps(overage)
            stream.write_text(json.dumps(allowed) + "\n" + line[:20], encoding="utf-8")
            self.assertIsNone(watch.check())
            with stream.open("a", encoding="utf-8") as target:
                target.write(line[20:] + "\n")
            self.assertTrue(watch.check()["rate_limit_info"]["isUsingOverage"])


class ResumeGateTests(unittest.TestCase):
    def setUp(self):
        self.state = {"productionEnvironment": {
            "coordinatorLease": {"owner": "claude-code", "generation": "g1", "sessionId": SESSION, "cliPid": 7},
            "relaySupport": {"checkpoints": {"claude-code": {"generation": "g1",
                "recordedAt": "2026-10-11T10:05:00+09:00"}}},
            "productionDispatch": {"ledger": [{"status": "completed"}]}}}
        self.guard = SimpleNamespace(authority=lambda: (self.state, {"owner": "claude-code"}),
            authorization_errors=lambda state, pin, request: [], budget_errors=lambda state: [])
        stack = contextlib.ExitStack(); self.addCleanup(stack.close)
        stack.enter_context(mock.patch.object(production, "production_guard", return_value=self.guard))
        stack.enter_context(mock.patch.object(production, "process_alive", return_value=False))

    def errors(self, cycle="2026-10-11T10:00:00+09:00"):
        return production.resume_gate_errors("g1", SESSION, cycle)

    def test_current_owner_lease_and_checkpoint_pass(self):
        self.assertEqual(self.errors(), [])

    def test_other_provider_writer_blocks_duplicate_resume(self):
        self.state["productionEnvironment"]["coordinatorLease"].update(owner="codex", generation="codex-g")
        self.assertIn("WRITER_OWNER_OR_LEASE_CHANGED", self.errors())

    def test_in_flight_operation_and_live_child_block_resume(self):
        self.state["productionEnvironment"]["productionDispatch"]["ledger"].append({"status": "in_flight"})
        with mock.patch.object(production, "process_alive", return_value=True):
            self.assertEqual(self.errors(), ["IN_FLIGHT_OPERATION_UNRESOLVED", "PREVIOUS_MODEL_CHILD_STILL_RUNNING"])

    def test_missing_or_stale_checkpoint_blocks_resume(self):
        self.assertIn("RECOVERY_CHECKPOINT_MISSING", self.errors("2026-10-11T10:10:00+09:00"))
        self.state["productionEnvironment"]["relaySupport"]["checkpoints"] = {}
        self.assertIn("RECOVERY_CHECKPOINT_MISSING", self.errors())

    def test_approval_and_budget_errors_are_preserved(self):
        self.guard.authorization_errors = lambda state, pin, request: ["USER_02_PRODUCTION_RESUME_REQUIRED"]
        self.guard.budget_errors = lambda state: ["BUDGET_POLICY_REVIEW_REQUIRED"]
        self.assertEqual(self.errors(), ["BUDGET_POLICY_REVIEW_REQUIRED", "USER_02_PRODUCTION_RESUME_REQUIRED"])


class AtomicWriteTests(unittest.TestCase):
    def test_retry_keeps_original_until_success_and_uses_unique_temp_names(self):
        with tempfile.TemporaryDirectory(prefix=".autonomy-test-", dir=TEST_DIR) as folder:
            target = Path(folder) / "state.json"
            original = {"oldApproval": {"rejected": True}, "usage": [42], "save": "preserve"}
            target.write_text(json.dumps(original), encoding="utf-8")
            replacement = {**original, "autonomy": {"status": "waiting_usage_reset"}}
            actual_replace = os.replace
            attempts = []

            def intermittent_lock(source, destination):
                attempts.append(Path(source))
                if len(attempts) < 3:
                    self.assertEqual(production.read(target), original)
                    raise PermissionError("simulated Windows reader lock")
                actual_replace(source, destination)

            with mock.patch.object(production.os, "replace", side_effect=intermittent_lock), \
                 mock.patch.object(production.time, "sleep") as sleep:
                production.write(target, replacement)
            self.assertEqual(production.read(target), replacement)
            self.assertEqual(sleep.call_count, 2)
            self.assertEqual(len(set(attempts)), 1)
            prior_temp = attempts[0]
            with mock.patch.object(production.os, "replace", wraps=actual_replace) as replace:
                production.write(target, replacement)
            self.assertNotEqual(Path(replace.call_args.args[0]), prior_temp)

    def test_persistent_lock_preserves_original_and_complete_recovery_file(self):
        with tempfile.TemporaryDirectory(prefix=".autonomy-test-", dir=TEST_DIR) as folder:
            target = Path(folder) / "state.json"
            original = {"preservedApproval": "거절 유지", "count": 14}
            target.write_text(json.dumps(original), encoding="utf-8")
            replacement = {**original, "waiting": True}
            with mock.patch.object(production.os, "replace", side_effect=PermissionError("locked")), \
                 mock.patch.object(production.time, "monotonic", side_effect=[100, 113]), \
                 mock.patch.object(production.time, "sleep"):
                with self.assertRaisesRegex(RuntimeError, "original and temporary backup preserved"):
                    production.write(target, replacement)
            self.assertEqual(production.read(target), original)
            backups = list(Path(folder).glob("state.json.*.tmp"))
            self.assertEqual(len(backups), 1)
            self.assertEqual(production.read(backups[0]), replacement)


class ControlAndSupervisorTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix=".autonomy-test-", dir=TEST_DIR)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        self.state_path = self.root / "state.json"
        self.state = {
            "demoProductionRouting": {"unityEditorOwner": "claude-code"},
            "preservedRejections": ["grip HOLD"],
            "productionEnvironment": {
                "autonomous": {"enabled": True, "stopRequested": False, "generation": "generation-1"},
                "coordinatorLease": {"owner": "claude-code", "status": "active",
                    "generation": "generation-1", "sessionId": SESSION, "launcherPid": 101},
                "coordinator": {"sessionId": SESSION, "status": "idle", "usage": {"old": 44}},
                "workers": {},
            },
        }
        production.write(self.state_path, self.state)
        (self.root / ".claude").mkdir()
        production.write(self.root / ".claude/settings.local.json", {"disableAllHooks": True,
            "permissions": {"deny": ["Bash(git push *)"]}, "enabledPlugins": {"old-plugin": False}})
        self.patches = contextlib.ExitStack()
        self.addCleanup(self.patches.close)
        self.patches.enter_context(mock.patch.object(production, "ROOT", self.root))
        self.patches.enter_context(mock.patch.object(production, "STATE", self.state_path))
        self.patches.enter_context(mock.patch.object(production, "RUNS", self.root / "Logs"))
        self.patches.enter_context(mock.patch.object(production, "MOD", self.root / "project-mod"))
        self.patches.enter_context(mock.patch.object(production, "state_lock", contextlib.nullcontext))
        actual_safe_path = production.safe_path
        self.patches.enter_context(mock.patch.object(production, "safe_path",
            side_effect=lambda relative, base=None: actual_safe_path(relative, self.root if base is None else base)))
        self.output = self.patches.enter_context(contextlib.redirect_stdout(io.StringIO()))
        self.args = SimpleNamespace(owner="claude-code", headless=False, intake_check=False,
            resume_session=SESSION, foreground=True, lease_token="generation-1",
            prepare_session=True, effort="high")
        self.report = {"routing": {"nextTicket": "D7"}}
        self.patches.enter_context(mock.patch.object(production, "doctor", return_value=SimpleNamespace(
            selection=lambda state, owner: {"nextTicket": "D7" if
                state["demoProductionRouting"]["unityEditorOwner"] == owner else None})))
        self.patches.enter_context(mock.patch.object(production, "git", return_value="current-head"))
        self.patches.enter_context(mock.patch.object(production, "game_fingerprint", return_value="same-product"))
        self.patches.enter_context(mock.patch.object(production, "actual_effort", return_value="high"))
        self.patches.enter_context(mock.patch.object(production.shutil, "which", return_value="claude"))
        self.registry = self.patches.enter_context(mock.patch.object(production.subprocess, "run",
            return_value=SimpleNamespace(returncode=0, stdout="[]")))
        self.popen = self.patches.enter_context(mock.patch.object(production.subprocess, "Popen",
            side_effect=AssertionError("unexpected process launch")))
        self.gate = self.patches.enter_context(mock.patch.object(production, "resume_gate_errors", return_value=[]))
        self.schedule = self.patches.enter_context(mock.patch.object(production, "schedule_local_resume",
            return_value={"registered": True, "task": "\\ProjectPA\\UsageResume"}))
        self.unschedule = self.patches.enter_context(mock.patch.object(production, "unschedule_local_resume"))

    def control(self, operation, **kwargs):
        values = {"operation": operation, "session_id": SESSION, "observation": "{}",
                  "evidence": [], "disposition": "continue", "next_action": "correct coast"}
        values.update(kwargs)
        return production.autonomy_control(SimpleNamespace(**values))

    def current_state(self):
        return production.read(self.state_path)

    def mutate(self, callback):
        state = self.current_state()
        callback(state)
        production.write(self.state_path, state)

    def stream_result(self, *, is_error=False, text="safe checkpoint", checkpoint=None, api_error=None):
        def start(command, **kwargs):
            fixture = self
            class PromptInput(io.StringIO):
                def write(self, text):
                    if checkpoint is not None:
                        fixture.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                            checkpoint={"sessionId": SESSION, "recordedAt": "9999-01-01T00:00:00+09:00",
                                        **checkpoint}))
                    return super().write(text)
            result = {"type": "result", "result": text, "is_error": is_error, "usage": {"input_tokens": 3}}
            if api_error is not None:
                result["api_error"] = api_error
            kwargs["stdout"].write(json.dumps(result) + "\n")
            kwargs["stdout"].flush()
            return SimpleNamespace(pid=999, returncode=1 if is_error else 0,
                                   stdin=PromptInput(), poll=lambda: 1 if is_error else 0)
        self.popen.side_effect = start

    def test_pause_is_persisted_and_does_not_erase_history(self):
        self.control("pause")
        current = self.current_state()
        control = current["productionEnvironment"]["autonomous"]
        self.assertTrue(control["stopRequested"])
        self.assertFalse(control["enabled"])
        self.assertIn("cancelledAt", control)
        self.assertEqual(current["preservedRejections"], ["grip HOLD"])
        self.assertEqual(current["productionEnvironment"]["coordinator"]["usage"], {"old": 44})
        self.popen.assert_not_called()

    def test_foreign_session_observation_is_not_marked_loaded(self):
        self.control("observe", session_id="foreign", observation=json.dumps({"rateLimits": [
            {"percentUsed": 100, "resetsAt": "2099-01-01T00:00:00Z"}]}))
        self.assertNotIn("modLoaded", self.current_state()["productionEnvironment"]["autonomous"])

    def test_actual_lead_observation_keeps_only_supported_fields(self):
        self.control("observe", observation=json.dumps({"rateLimits": [], "context": {"percent": 4},
            "version": "2.1.296", "model": "observed-model", "unrelated": "not stored"}))
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertTrue(control["modLoaded"])
        self.assertEqual(control["modObservation"]["sessionId"], SESSION)
        self.assertNotIn("unrelated", control["modObservation"])

    def test_foreign_session_cannot_checkpoint(self):
        before = self.current_state()
        with self.assertRaisesRegex(RuntimeError, "recorded lead"):
            self.control("checkpoint", session_id="foreign")
        self.assertEqual(self.current_state(), before)

    def test_missing_checkpoint_evidence_is_rejected_without_state_write(self):
        before = self.current_state()
        with self.assertRaisesRegex(RuntimeError, "evidence must exist"):
            self.control("checkpoint", evidence=["Logs/does-not-exist.png"])
        self.assertEqual(self.current_state(), before)

    def test_outside_root_checkpoint_path_is_rejected(self):
        before = self.current_state()
        with self.assertRaises((ValueError, RuntimeError)):
            self.control("checkpoint", evidence=["../outside.png"])
        self.assertEqual(self.current_state(), before)

    def test_cancelled_session_does_not_launch(self):
        self.control("pause")
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()
        self.assertFalse(self.current_state()["productionEnvironment"]["autonomous"]["enabled"])

    def test_changed_generation_is_left_untouched(self):
        self.mutate(lambda state: state["productionEnvironment"]["coordinatorLease"].update(
            generation="new-owner-generation"))
        before = self.current_state()
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()
        self.assertEqual(self.current_state(), before)

    def test_changed_editor_owner_prevents_launch(self):
        self.mutate(lambda state: state["demoProductionRouting"].update(unityEditorOwner="codex"))
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()

    def test_codex_cannot_become_autonomous_production_owner(self):
        self.args.owner = "codex"
        with self.assertRaisesRegex(RuntimeError, "Claude production owner"):
            production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()

    def test_live_existing_lease_cannot_launch_second_supervisor(self):
        self.args.foreground = False
        before = self.current_state()
        with mock.patch.object(production, "process_alive", return_value=True):
            with self.assertRaisesRegex(RuntimeError, "Existing lead owns Unity"):
                production.autonomous_start(self.args, self.report)
        self.assertEqual(self.current_state(), before)
        self.popen.assert_not_called()

    def test_other_working_session_pauses_without_termination_or_launch(self):
        self.registry.return_value.stdout = json.dumps([{"sessionId": "user-session", "status": "working"}])
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "paused_other_session_or_unverified_registry")

    def test_registry_failure_never_assumes_unity_is_unowned(self):
        self.registry.return_value.returncode = 1
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()

    def test_auth_error_cannot_be_turned_into_a_quota_retry(self):
        self.stream_result(is_error=True, text="Authentication failed. Please login.")
        with mock.patch.object(production, "future_quota_reset") as parser:
            production.autonomous_start(self.args, self.report)
        parser.assert_not_called()
        self.assertEqual(self.popen.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "blocked_account_or_tool_no_verified_reset")

    def test_quota_without_actual_reset_stops_after_one_invocation(self):
        self.stream_result(is_error=True, text="You've hit your session limit")
        production.autonomous_start(self.args, self.report)
        self.assertEqual(self.popen.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "blocked_account_or_tool_no_verified_reset")

    def test_cancel_during_verified_quota_wait_prevents_resumed_child(self):
        self.stream_result(is_error=True, text="You've hit your session limit", api_error="usage_limit_reached")
        def cancel_wait(_):
            self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                enabled=False, stopRequested=True, cancelledAt="cancelled by test"))
        with mock.patch.object(production, "future_quota_reset",
                return_value=dt.datetime.now(production.KST) + dt.timedelta(minutes=2)), \
             mock.patch.object(production.time, "sleep", side_effect=cancel_wait):
            production.autonomous_start(self.args, self.report)
        self.assertEqual(self.popen.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "paused_by_user")
        self.assertEqual(self.current_state()["preservedRejections"], ["grip HOLD"])

    def test_continue_without_product_or_validation_progress_pauses(self):
        self.stream_result(checkpoint={"disposition": "continue", "nextAction": "same task", "evidence": []})
        production.autonomous_start(self.args, self.report)
        self.assertEqual(self.popen.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "paused_no_product_or_validation_progress")

    def test_old_evidence_cannot_make_a_noop_look_like_progress(self):
        evidence = self.root / "Logs/old-verification.json"
        evidence.parent.mkdir(parents=True)
        evidence.write_text('{"old":true}', encoding="utf-8")
        os.utime(evidence, (946684800, 946684800))
        self.stream_result(checkpoint={"disposition": "continue", "nextAction": "same task",
            "evidence": ["Logs/old-verification.json"]})
        production.autonomous_start(self.args, self.report)
        self.assertEqual(self.popen.call_count, 1)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "paused_no_product_or_validation_progress")

    def test_cli_success_without_checkpoint_is_not_completion(self):
        self.stream_result(text="Done")
        production.autonomous_start(self.args, self.report)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "paused_checkpoint_or_blocker")
        self.assertNotIn("completionReached", self.current_state()["demoProductionRouting"])

    def test_human_review_readiness_does_not_mark_game_complete(self):
        self.stream_result(checkpoint={"disposition": "human-review", "nextAction": "human review", "evidence": []})
        production.autonomous_start(self.args, self.report)
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"],
                         "ready_for_human_review")
        self.assertNotIn("completionReached", self.current_state()["demoProductionRouting"])

    def test_resume_gate_block_prevents_model_launch(self):
        self.gate.return_value = ["USER_02_PRODUCTION_RESUME_REQUIRED"]
        production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertEqual(control["status"], "paused_resume_gate_blocked")
        self.assertEqual(control["resumeGate"]["errors"], ["USER_02_PRODUCTION_RESUME_REQUIRED"])

    def test_verified_reset_rechecks_gate_before_relaunch(self):
        self.stream_result(is_error=True, text="You've hit your session limit", api_error="usage_limit_reached")
        self.gate.side_effect = [[], ["IN_FLIGHT_OPERATION_UNRESOLVED"]]
        past = dt.datetime.now(production.KST) - dt.timedelta(minutes=5)
        with mock.patch.object(production, "future_quota_reset", return_value=past):
            production.autonomous_start(self.args, self.report)
        self.assertEqual(self.popen.call_count, 1)
        self.assertIsNotNone(self.gate.call_args_list[1].args[2])
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertEqual(control["status"], "paused_resume_gate_blocked")
        self.assertEqual(control["resumeReservation"]["resetAt"], past.isoformat())
        self.assertIn("waitCompletedAt", control["resumeReservation"])
        self.schedule.assert_called_once_with(control["resumeReservation"]["resumeAt"])
        self.assertTrue(control["resumeReservation"]["scheduledTask"]["registered"])
        self.unschedule.assert_called()

    def test_restart_honours_recorded_reset_without_a_model_call(self):
        future = (dt.datetime.now(production.KST) + dt.timedelta(hours=1)).isoformat()
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            status="starting", resumeReservation={"resetAt": future, "generation": "older"}))
        def cancel_wait(_):
            self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                enabled=False, stopRequested=True))
        with mock.patch.object(production.time, "sleep", side_effect=cancel_wait):
            production.autonomous_start(self.args, self.report)
        self.popen.assert_not_called()
        self.gate.assert_not_called()
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["status"], "paused_by_user")

    def test_pause_cancels_a_pending_reset_reservation(self):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
            resumeReservation={"resetAt": "2099-01-01T00:00:00+09:00"}))
        self.control("pause")
        self.assertIn("cancelledAt", self.current_state()["productionEnvironment"]["autonomous"]["resumeReservation"])
        self.unschedule.assert_called_once()

    def due_state(self, **reservation):
        self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(resumeReservation={
            "resetAt": "2026-10-11T09:00:00+09:00", "resumeAt": "2026-10-11T09:01:00+09:00",
            "effort": "high", **reservation}))

    def resume_due(self, alive=False, gate=()):
        guard = SimpleNamespace(authority=lambda: (None, {"owner": "claude-code"}),
            authorization_errors=lambda state, pin, request: list(gate), budget_errors=lambda state: [])
        spawned = mock.Mock(return_value=SimpleNamespace(pid=4321))
        with mock.patch.object(production, "production_guard", return_value=guard), \
             mock.patch.object(production, "process_alive", return_value=alive), \
             mock.patch.object(production, "RESUME_LOG", self.root / "Logs/resume-due.jsonl"):
            outcome = production.resume_due(CURRENT, spawn=spawned)
        return outcome, spawned

    def test_resume_due_dispatches_one_gated_supervisor_after_reboot(self):
        self.due_state()
        outcome, spawned = self.resume_due()
        self.assertEqual(outcome["reasons"], [])
        self.assertIn("--autonomous", spawned.call_args.args[0])
        self.assertEqual(self.current_state()["productionEnvironment"]["autonomous"]["resumeReservation"]["launcherPid"], 4321)
        again, spawned_again = self.resume_due()
        self.assertIn("NO_PENDING_RESERVATION", again["reasons"])
        spawned_again.assert_not_called()

    def test_resume_due_refuses_early_paused_live_foreign_or_unapproved_resume(self):
        cases = [({"resumeAt": "2026-10-11T11:00:00+09:00"}, {}, "NOT_DUE"),
                 ({}, {"alive": True}, "SUPERVISOR_OR_CHILD_ALIVE"),
                 ({}, {"gate": ["USER_02_PRODUCTION_RESUME_REQUIRED"]}, "USER_02_PRODUCTION_RESUME_REQUIRED")]
        for reservation, options, reason in cases:
            with self.subTest(reason=reason):
                self.due_state(**reservation)
                outcome, spawned = self.resume_due(**options)
                self.assertIn(reason, outcome["reasons"]); spawned.assert_not_called()
        self.due_state()
        self.mutate(lambda state: state["productionEnvironment"]["coordinatorLease"].update(owner="codex"))
        outcome, spawned = self.resume_due()
        self.assertIn("OTHER_WRITER_OWNS_PRODUCTION", outcome["reasons"]); spawned.assert_not_called()
        self.control("pause")
        outcome, spawned = self.resume_due()
        self.assertIn("PAUSED_BY_USER", outcome["reasons"]); spawned.assert_not_called()

    def test_paid_overage_stops_child_and_waits_for_plan_reset(self):
        reset = (dt.datetime.now(production.KST) + dt.timedelta(hours=2)).timestamp()
        process = SimpleNamespace(pid=999, returncode=None, stdin=io.StringIO())
        process.poll = lambda: process.returncode
        process.terminate = mock.Mock(side_effect=lambda: setattr(process, "returncode", 1))
        process.wait = lambda timeout=None: process.returncode
        def start(command, **kwargs):
            kwargs["stdout"].write(json.dumps({"type": "rate_limit_event", "rate_limit_info": {
                "status": "allowed", "isUsingOverage": True, "rateLimitType": "five_hour", "resetsAt": reset,
                "unifiedWindows": {"five_hour": {"utilization": 1.02, "resetsAt": reset}}}}) + "\n")
            kwargs["stdout"].flush()
            return process
        self.popen.side_effect = start
        def cancel_wait(_):
            self.mutate(lambda state: state["productionEnvironment"]["autonomous"].update(
                enabled=False, stopRequested=True))
        with mock.patch.object(production.time, "sleep", side_effect=cancel_wait):
            production.autonomous_start(self.args, self.report)
        process.terminate.assert_called_once()
        self.assertEqual(self.popen.call_count, 1)
        control = self.current_state()["productionEnvironment"]["autonomous"]
        self.assertIn("overageStopped", control)
        self.assertEqual(control["resumeReservation"]["resetAt"],
                         dt.datetime.fromtimestamp(reset, production.KST).isoformat())


if __name__ == "__main__":
    unittest.main()

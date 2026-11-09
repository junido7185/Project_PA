"""Offline tests: no installed CLI, network, model, Unity, sleep or API is used."""
from datetime import datetime, timezone
import io
import json
import time
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
from types import SimpleNamespace

import provider_runner as runner


class RunnerTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name) / "project"
        state_dir = self.root / "Automation/LoopEngineering/State"
        state_dir.mkdir(parents=True)
        self.state_path = state_dir / "loop-state.json"
        self.state_path.write_text(json.dumps({
            "demoProductionRouting": {"selectedApproval": "chosen", "unityEditorOwner": "codex", "activeTicket": "D2"},
            "chosen": {"status": "blocked_budget", "activeTicket": "D2",
                       "ticketStatus": {"D2": "INCOMPLETE_BLOCKED_BUDGET"},
                       "results": {"D2": {"compileCycles": 4, "targetedPlayRuns": 5,
                                           "dedicatedValidatorsAdded": 1,
                                           "approvedTotalBudget": {"compileFix": 4, "targetedPlay": 5, "dedicatedValidators": 1}}}}
        }), encoding="utf-8")
        handoff = self.root / "Docs/00_CURRENT/CODEX_HANDOFF.md"
        handoff.parent.mkdir(parents=True)
        handoff.write_text("D2 blocked. Do not reset or borrow D3 budget.", encoding="utf-8")
        self.logs = Path(self.temp.name) / "logs"
        self.environment = {"PATH": "/no-real-cli", "HOME": str(self.root)}

    def job(self, outcomes, **kwargs):
        calls = []
        def fake(provider, argv, prompt, environment, cwd, timeout, directory, redactor):
            calls.append(provider)
            self.assertIn(runner.CHILD_FLAG, environment)
            self.assertIn("Nested dispatch is forbidden", prompt)
            self.assertFalse(json.loads(self.state_path.read_text())["chosen"]["status"] != "blocked_budget")
            return runner.Outcome(provider, outcomes[provider], session_ids=[provider + "-session"])
        result = runner.execute_job(self.root, "Read and audit the current D2 contract.", run=True,
                                    log_root=self.logs, environment=self.environment,
                                    executor=fake, resolver=lambda provider: [provider + "-fake"],
                                    auth_checker=lambda *args: runner.AuthCheck(True, "SUBSCRIPTION_VERIFIED", "fake"), **kwargs)
        return result, calls

    def test_prepare_never_resolves_or_invokes_a_cli(self):
        def forbidden(*args):
            raise AssertionError("CLI discovery/execution must not run in prepare")
        result = runner.execute_job(self.root, "audit", log_root=self.logs,
                                    environment=self.environment, executor=forbidden, resolver=forbidden)
        self.assertEqual(result["stage"], "PREPARED")
        self.assertEqual(result["dispatches"], [])

    def test_symmetric_quota_failover(self):
        for primary in runner.PROVIDERS:
            other = next(name for name in runner.PROVIDERS if name != primary)
            result, calls = self.job({primary: "QUOTA_EXHAUSTED", other: "SUCCESS"}, primary=primary)
            self.assertEqual(calls, [primary, other])
            self.assertEqual(result["stage"], "SUPPORT_COMPLETED")

    def test_symmetric_auth_failover(self):
        result, calls = self.job({"codex": "AUTH_REQUIRED", "claude": "SUCCESS"}, primary="codex")
        self.assertEqual(calls, ["codex", "claude"])
        self.assertEqual(result["stage"], "SUPPORT_COMPLETED")

    def test_both_exhausted_never_retries_a_provider(self):
        result, calls = self.job({"claude": "QUOTA_EXHAUSTED", "codex": "QUOTA_EXHAUSTED"})
        self.assertEqual(calls, ["claude", "codex"])
        self.assertEqual(result["stage"], "QUOTA_EXHAUSTED")
        self.assertEqual(len(result["dispatches"]), 2)

    def test_auth_and_quota_both_fail(self):
        result, calls = self.job({"claude": "AUTH_REQUIRED", "codex": "QUOTA_EXHAUSTED"})
        self.assertEqual(len(calls), 2)
        self.assertNotEqual(result["stage"], "SUPPORT_COMPLETED")

    def test_uncertain_timeout_and_billing_do_not_trigger_fallback(self):
        for reason in ("TIMEOUT", "BILLING_ROUTE_REJECTED"):
            result, calls = self.job({"claude": reason, "codex": "SUCCESS"})
            self.assertEqual(calls, ["claude"])
            self.assertEqual(result["stage"], reason)

    def test_terminal_network_or_429_failure_can_use_other_subscription(self):
        for reason in ('NETWORK_FAILURE','RATE_LIMIT_UNCONFIRMED'):
            result,calls=self.job({'claude':reason,'codex':'SUCCESS'})
            self.assertEqual(calls,['claude','codex'])
            self.assertEqual(result['dispatches'][0]['reason'],reason)
            self.assertEqual(result['stage'],'SUPPORT_COMPLETED')

    def test_success_does_not_call_second_provider(self):
        result, calls = self.job({"claude": "SUCCESS", "codex": "SUCCESS"})
        self.assertEqual(calls, ["claude"])
        self.assertEqual(result["dispatches"][0]["session_ids"], ["claude-session"])

    def test_missing_primary_cli_can_use_other_provider_without_claiming_quota(self):
        calls=[]
        def resolver(provider):
            if provider=='claude': raise runner.DiscoveryError('CLI_NOT_FOUND')
            return ['fake-codex']
        def execute(provider,*args):
            calls.append(provider);return runner.Outcome(provider,'SUCCESS')
        result=runner.execute_job(self.root,'audit',run=True,log_root=self.logs,environment=self.environment,
            resolver=resolver,executor=execute,auth_checker=lambda *args:runner.AuthCheck(True,'SUBSCRIPTION_VERIFIED'))
        self.assertEqual(calls,['codex'])
        self.assertEqual(result['dispatches'][0]['reason'],'CLI_NOT_FOUND')
        self.assertEqual(result['stage'],'SUPPORT_COMPLETED')

    def test_support_child_disables_case_aliases_and_all_configured_mcps(self):
        p=self.root/'.codex/config.toml';p.parent.mkdir();p.write_text('[mcp_servers.MixedUnity]\nurl="http://private.invalid"\n[mcp_servers.other]\ncommand="never-run"\n')
        flags=runner.codex_mcp_overrides(self.root,{'CODEX_HOME':str(self.root/'missing-user-config')})
        self.assertIn('mcp_servers={}',flags)
        self.assertFalse(any('.enabled=' in value for value in flags))
        self.assertNotIn('private.invalid',str(flags))

    def test_environment_sanitized_without_global_mutation(self):
        source = {"OPENAI_API_KEY": "secret-value", "CODEX_API_KEY": "secret2", "ANTHROPIC_API_KEY": "secret3",
                  "ANTHROPIC_BASE_URL": "https://private.invalid", "CLAUDE_CODE_USE_BEDROCK": "1",
                  "AWS_ACCESS_KEY_ID": "aws-secret", "AZURE_OPENAI_ENDPOINT": "private", "PATH": "keep",
                  "HOME": "keep-home", "SYSTEMROOT": "keep-windows"}
        child = runner.child_environment(source)
        self.assertEqual(child, {"PATH": "keep", "HOME": "keep-home", "SYSTEMROOT": "keep-windows", runner.CHILD_FLAG: "1"})
        self.assertEqual(source["OPENAI_API_KEY"], "secret-value")

    def test_nested_dispatch_is_rejected(self):
        with self.assertRaises(runner.PolicyError):
            runner.execute_job(self.root, "audit", environment={runner.CHILD_FLAG: "1"}, log_root=self.logs)

    def test_production_scope_cannot_ignore_d2_gate(self):
        before = self.state_path.read_bytes()
        with self.assertRaises(runner.PolicyError):
            runner.execute_job(self.root, "finish D2", run=True, scope="production", log_root=self.logs)
        self.assertEqual(self.state_path.read_bytes(), before)
        result, _ = self.job({"claude": "SUCCESS", "codex": "SUCCESS"})
        gate = result["authoritativeTaskGate"]
        self.assertEqual(gate["approvalStatus"], "blocked_budget")
        self.assertEqual(gate["usedBudget"]["compileFix"], 4)
        self.assertFalse(gate["productionAllowedByThisRunner"])
        self.assertEqual(self.state_path.read_bytes(), before)

    def test_authority_drift_prevents_a_second_dispatch(self):
        calls = []
        def fake(provider, *args):
            calls.append(provider)
            self.state_path.write_text(self.state_path.read_text() + "\n", encoding="utf-8")
            return runner.Outcome(provider, "QUOTA_EXHAUSTED")
        result = runner.execute_job(self.root, "audit", run=True, log_root=self.logs,
                                    environment=self.environment, executor=fake,
                                    resolver=lambda provider: [provider],
                                    auth_checker=lambda *args: runner.AuthCheck(True, "SUBSCRIPTION_VERIFIED", "fake"))
        self.assertEqual(calls, ["claude"])
        self.assertEqual(result["stage"], "AUTHORITY_CHANGED_REVIEW_REQUIRED")

    def test_codex_argv_requests_subscription_readonly_and_disables_unity(self):
        argv = runner.build_argv("codex", ["path with spaces/codex.exe"])
        self.assertIn('forced_login_method="chatgpt"', argv)
        self.assertIn("mcp_servers={}", argv)
        self.assertIn("read-only", argv)
        self.assertEqual(argv[-1], "-")
        self.assertNotIn("--full-auto", argv)
        self.assertNotIn("--yolo", argv)

    def test_claude_argv_uses_plan_strict_empty_mcp_without_turn_cap(self):
        argv = runner.build_argv("claude", ["claude.exe"])
        for flag in ("-p", "--verbose", "--strict-mcp-config"):
            self.assertIn(flag, argv)
        self.assertEqual(argv[argv.index("--permission-mode") + 1], "plan")
        self.assertNotIn("--max-turns", argv)
        self.assertEqual(json.loads(argv[-1]), {"mcpServers": {}})

    def test_error_classification_does_not_call_429_quota(self):
        cases = {"Usage limit reached; resets later": "QUOTA_EXHAUSTED",
                 "usage_limit_reached": "QUOTA_EXHAUSTED", "Please log in": "AUTH_REQUIRED",
                 "authentication_error": "AUTH_REQUIRED", "HTTP 429 rate_limit_exceeded": "RATE_LIMIT_UNCONFIRMED",
                 "ECONNRESET": "NETWORK_FAILURE", "credit balance too low": "BILLING_ROUTE_REJECTED"}
        for text, reason in cases.items():
            self.assertEqual(runner.classify_failure(1, text), reason)
        self.assertEqual(runner.classify_failure(0, "report discusses usage limit reached"), "SUCCESS")
        self.assertEqual(runner.classify_failure(1, "", timed_out=True), "TIMEOUT")

    def test_api_billing_quota_is_not_a_subscription_limit(self):
        self.assertEqual(runner.classify_failure(1,'insufficient_quota: check your plan and billing details'), 'BILLING_ROUTE_REJECTED')
        self.assertEqual(runner.classify_failure(1,'insufficient_quota'), 'QUOTA_TYPE_UNCONFIRMED')

    def test_stderr_json_cannot_supply_terminal_success_or_session(self):
        directory=Path(self.temp.name)/'stderr-json';directory.mkdir()
        collector=runner.StreamCollector(directory,'codex',runner.Redactor({}))
        collector.feed('stderr','{"type":"thread.started","thread_id":"fake"}\n{"type":"turn.completed"}\n')
        collector.finish()
        self.assertIsNone(collector.terminal);self.assertEqual(collector.sessions,[])

    def test_log_failure_reaps_only_the_process_started_by_this_runner(self):
        directory=Path(self.temp.name)/'cleanup';directory.mkdir()
        class Process:
            pid=123
            stdin=io.StringIO();stdout=io.StringIO('event\n');stderr=io.StringIO('')
            code=None;terminated=False
            def poll(self):return self.code
            def terminate(self):self.terminated=True;self.code=-15
            def wait(self,timeout):return self.code
        process=Process()
        with patch.object(runner.StreamCollector,'feed',side_effect=OSError('log failed')):
            with self.assertRaises(OSError):
                runner.launch_cli('codex',['not-run'],'audit',{},self.root,2,directory,
                    runner.Redactor({}),popen_factory=lambda *args,**kwargs:process)
        self.assertTrue(process.terminated)

    def test_claude_auth_only_allows_verified_claude_ai_subscription(self):
        for method in ("claude.ai", "oauth_token", "api_key", "api_key_helper", "third_party", "new-unknown-mode"):
            raw = json.dumps({"loggedIn": True, "authMethod": method, "email": "private@example.invalid", "token": "private-token"})
            def status(argv, **kwargs):
                self.assertEqual(argv[-2:], ["auth", "status"])
                self.assertFalse(kwargs["shell"])
                return SimpleNamespace(returncode=0, stdout=raw, stderr="")
            result = runner.verify_subscription("claude", ["not-executed"], {}, self.root, run_command=status)
            self.assertEqual(result.allowed, method == "claude.ai")
            self.assertNotIn("private", str(result))

    def test_codex_auth_method_must_be_chatgpt(self):
        for text, expected in (("Logged in using ChatGPT", True), ("Logged in using an API key", False), ("Logged in", False)):
            def status(argv, **kwargs):
                self.assertEqual(argv[-2:], ["login", "status"])
                return SimpleNamespace(returncode=0, stdout=text, stderr="")
            result = runner.verify_subscription("codex", ["not-executed"], {}, self.root, run_command=status)
            self.assertEqual(result.allowed, expected)

    def test_old_auth_status_is_unverified_and_calls_no_model_for_that_provider(self):
        for text in ("unknown command auth", '{"loggedIn": true}'):
            result = runner.verify_subscription("claude", ["not-executed"], {}, self.root,
                                                run_command=lambda *a, **k: SimpleNamespace(returncode=1, stdout=text, stderr=""))
            self.assertFalse(result.allowed)
            self.assertEqual(result.status, "AUTH_UNVERIFIED")
        calls = []
        def fake(provider, *args):
            calls.append(provider)
            return runner.Outcome(provider, "SUCCESS")
        result = runner.execute_job(self.root, "audit", run=True, log_root=self.logs,
                                    environment=self.environment, executor=fake, resolver=lambda provider: [provider],
                                    auth_checker=lambda provider, *args: runner.AuthCheck(
                                        provider == "codex", "SUBSCRIPTION_VERIFIED" if provider == "codex" else "AUTH_UNVERIFIED"))
        self.assertEqual(calls, ["codex"])
        self.assertEqual(result["modelDispatchCount"], 1)
        self.assertEqual(result["dispatches"][0]["reason"], "AUTH_UNVERIFIED")

    def test_redaction_in_stream_records_and_report(self):
        directory = Path(self.temp.name) / "events"
        directory.mkdir()
        collector = runner.StreamCollector(directory, "codex", runner.Redactor({"OPENAI_API_KEY": "private-secret-value"}))
        collector.feed("stdout", json.dumps({"type": "item.completed", "item": {"type": "agent_message", "text": "private-secret-value"}}) + "\n")
        collector.feed("stderr", 'access_token="private-secret-value"\n')
        collector.finish()
        for path in directory.iterdir():
            self.assertNotIn("private-secret-value", path.read_text())

    def test_known_windows_npm_shim_discovered_without_running_it(self):
        base = Path(self.temp.name) / "npm"
        base.mkdir()
        shim = base / "codex.cmd"
        shim.write_text("untrusted batch contents are never interpreted")
        node = base / "node.exe"
        node.write_bytes(b"not executed")
        script = base / "node_modules/@openai/codex/bin/codex.js"
        script.parent.mkdir(parents=True)
        script.write_text("not executed")
        self.assertEqual(runner.resolve_command("codex", str(shim), windows=True, lookup=lambda _: None),
                         [str(node.resolve()), str(script.resolve())])

    def test_unknown_shim_is_an_installation_error_not_quota(self):
        shim = Path(self.temp.name) / "claude.cmd"
        shim.write_text("never executed")
        with self.assertRaises(runner.DiscoveryError) as raised:
            runner.resolve_command("claude", str(shim), windows=True, lookup=lambda _: None)
        self.assertEqual(raised.exception.reason, "CLI_SHIM_UNSUPPORTED")

    def test_two_streams_are_collected_with_shell_false_and_no_real_cli(self):
        directory = Path(self.temp.name) / "streams"
        directory.mkdir()
        observed = {}
        class FakeProcess:
            pid = 123
            stdin = io.StringIO()
            stdout = io.StringIO('{"type":"thread.started","thread_id":"test-session"}\n'
                                 '{"type":"turn.completed"}\n')
            stderr = io.StringIO("progress only\n")
            def poll(self):
                return 0
        def fake_popen(argv, **kwargs):
            observed.update(kwargs)
            return FakeProcess()
        result = runner.launch_cli("codex", ["never-executed"], "support only", self.environment,
                                   self.root, 2, directory, runner.Redactor({}), popen_factory=fake_popen)
        self.assertEqual(result.reason, "SUCCESS")
        self.assertEqual(result.session_ids, ["test-session"])
        self.assertFalse(observed["shell"])
        self.assertTrue((directory / "codex.stdout.jsonl").is_file())
        self.assertTrue((directory / "codex.stderr.jsonl").is_file())

    def test_unstructured_stdout_cannot_fake_a_terminal_quota_event(self):
        directory = Path(self.temp.name) / "plain"
        directory.mkdir()
        collector = runner.StreamCollector(directory, "codex", runner.Redactor({}))
        collector.feed("stdout", "The report discusses usage limit reached.\n")
        collector.finish()
        self.assertEqual(collector.errors, [])
        self.assertEqual(runner.classify_failure(0, "\n".join(collector.errors)), "SUCCESS")

    def test_zero_exit_without_a_terminal_event_is_incomplete(self):
        directory=Path(self.temp.name)/'incomplete';directory.mkdir()
        class Process:
            pid=1
            stdin=io.StringIO();stdout=io.StringIO('plain text only\n');stderr=io.StringIO('')
            def poll(self):return 0
        result=runner.launch_cli('codex',['never-run'],'audit',{},self.root,2,directory,
            runner.Redactor({}),popen_factory=lambda *args,**kwargs:Process())
        self.assertEqual(result.reason,'INCOMPLETE_STREAM')

    def test_paid_overage_is_stopped_and_treated_as_plan_exhaustion(self):
        directory=Path(self.temp.name)/'overage';directory.mkdir()
        reset=int(time.time())+7200
        event={'type':'rate_limit_event','rate_limit_info':{'status':'allowed','isUsingOverage':True,
               'resetsAt':reset,'unifiedWindows':{'five_hour':{'utilization':1.02,'resetsAt':reset}}}}
        class Process:
            pid=2;returncode=None;terminated=False
            stdin=io.StringIO();stdout=io.StringIO(json.dumps(event)+'\n'+'{"type":"result","result":"paid"}\n')
            stderr=io.StringIO('')
            def poll(self):return self.returncode
            def terminate(self):self.terminated=True;self.returncode=1
            def wait(self,timeout=None):return self.returncode
        process=Process()
        result=runner.launch_cli('claude',['never-run'],'audit',{},self.root,2,directory,
            runner.Redactor({}),popen_factory=lambda *args,**kwargs:process)
        self.assertTrue(process.terminated)
        self.assertEqual(result.reason,'QUOTA_EXHAUSTED')
        self.assertIn('overage',result.detail)
        self.assertEqual(result.verified_reset_at,datetime.fromtimestamp(reset,timezone.utc).isoformat())

    def test_both_exhausted_records_wait_without_automatic_retry(self):
        def fake(provider, *args):
            reset={'claude':'2099-01-01T05:00:00+00:00','codex':None}[provider]
            return runner.Outcome(provider,'QUOTA_EXHAUSTED',verified_reset_at=reset)
        result=runner.execute_job(self.root,'audit',run=True,log_root=self.logs,environment=self.environment,
            executor=fake,resolver=lambda provider:[provider],
            auth_checker=lambda *args:runner.AuthCheck(True,'SUBSCRIPTION_VERIFIED','fake'))
        wait=result['quotaWait']
        self.assertTrue(wait['allProvidersExhausted'])
        self.assertFalse(wait['automaticResume'])
        self.assertIsNone(wait['earliestVerifiedResetAt'])
        self.assertEqual(len(result['dispatches']),2)


if __name__ == "__main__":
    unittest.main()

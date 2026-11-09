import contextlib
import copy
import datetime as dt
import hashlib
import json
import os
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import production_controls as controls

SID = 'd1ff77d7-03aa-4167-8dfe-576673db1b47'
OTHER = 'd9c1ccb8-cf6e-422e-b670-8beca13cb3b8'
NOW = dt.datetime.fromisoformat('2026-10-23T12:00:00+09:00')


class ControlTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name).resolve()
        self.calls = []
        self.locked = False
        self.inventory = {'available': True, 'processes': []}
        self.pin = {'owner': 'claude-code', 'approvalKey': 'approval', 'ticket': 'D7', 'baselineHead': 'b'*40,
                    'workspaceSha256': 'w', 'stateSha256': 's', 'handoffSha256': 'h', 'contractSha256': 'c'}
        proof = 'Logs/AgentConsole/resume/user.md'
        self.file(proof, 'actual user instruction')
        self.file('contract.md', '2026-10-09 policy amendment: adaptive repair; per-ticket counts are not current execution gates\n')
        self.file(controls.HELPER, 'def resume_due(): pass\ndef schedule_local_resume(): pass\n')
        self.file(controls.PROMPT, 'existing owner support prompt')
        self.state = {'demoProductionRouting': {'unityEditorOwner': 'claude-code'},
                      'approval': {'status': 'active', 'policyId': 'PREAPPROVED_MILESTONE_CONTINUATION',
                                   'approvalSource': 'direct user', 'stopAtMilestone': 'FINAL_WINDOWS_HUMAN_REVIEW',
                                   'pushAllowed': False, 'approvedTickets': ['D7'], 'preapprovedThrough': 'D7',
                                   'ticketStatus': {'D7': 'IN_PROGRESS'}, 'activeTicket': 'D7',
                                   'executionPolicy': 'APPROVED_GOAL_ADAPTIVE_PRODUCTION', 'contract': 'contract.md'},
                      'productionEnvironment': {'baseHead': self.pin['baselineHead'],
                          'coordinatorLease': {'owner': 'claude-code', 'sessionId': SID, 'status': 'stopped',
                                               'generation': 'g', 'cliPid': 10, 'launcherPid': 11,
                                               'startedAt': '2026-10-23T11:00:00+09:00'},
                          'autonomous': {'enabled': False, 'status': 'paused_checkpoint_or_blocker',
                                        'stopRequested': False, 'checkpoint': {'disposition': 'blocked', 'evidence': ['old refusal']}},
                          'productionDispatch': {'ledger': [], 'resumeAuthorization': {
                              'enabled': True, 'identifier': 'PA_START_CURRENT_CHECKPOINT_20261019_V3',
                              'approvalKey': 'approval', 'ticket': 'D7', 'baselineHead': self.pin['baselineHead'],
                              'userMessageEvidence': proof, 'userMessageSha256': hashlib.sha256(b'actual user instruction').hexdigest()}}}}
        self.state['demoProductionRouting']['selectedApproval'] = 'approval'
        self.env_patch = patch.dict(os.environ, {key: '' for key in ('ANTHROPIC_API_KEY','ANTHROPIC_AUTH_TOKEN','CLAUDE_CODE_USE_BEDROCK','CLAUDE_CODE_USE_VERTEX','CLAUDE_CODE_USE_FOUNDRY')})
        self.env_patch.start()

    def tearDown(self):
        self.env_patch.stop()
        self.tmp.cleanup()

    def file(self, relative, content):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content, encoding='utf-8')

    def authority(self, root): return copy.deepcopy(self.state), copy.deepcopy(self.pin)

    def reader(self, root): return copy.deepcopy(self.inventory)

    def snapshot(self):
        return controls.control_snapshot(self.root, authority_reader=self.authority, process_reader=self.reader, clock=lambda: NOW)

    @contextlib.contextmanager
    def lock(self):
        self.assertFalse(self.locked)
        self.locked = True
        try: yield
        finally: self.locked = False

    def spawn(self, command, **kwargs):
        self.assertTrue(self.locked)
        self.calls.append((command, kwargs))
        return SimpleNamespace(pid=777)

    def run_pause(self, command, **kwargs):
        self.assertFalse(self.locked)  # Normal helper takes its own existing lock.
        self.calls.append((command, kwargs))
        return SimpleNamespace(returncode=0, stdout='pause_requested', stderr='')

    def action(self, action, expected=None):
        return controls.control_action(self.root, {'action': action, 'expected': expected or self.snapshot()['requestPin']},
            authority_reader=self.authority, process_reader=self.reader, clock=lambda: NOW,
            helper_loader=lambda root: SimpleNamespace(state_lock=self.lock), spawn=self.spawn, run=self.run_pause)

    def process(self, sid=SID, pid=10, created='2026-10-23T11:00:01+09:00'):
        return {'pid': pid, 'name': 'claude.exe', 'executable': 'C:/Users/test/.local/bin/claude.exe',
                'createdAt': created, 'commandLine': f'claude.exe --resume {sid} --settings "{self.root.as_posix()}/.claude/settings.local.json" --name "ProjectPA Claude Production Lead"'}

    def test_disabled_blocked_checkpoint_is_preserved_and_normal_same_sid_only(self):
        before = copy.deepcopy(self.state)
        view = self.snapshot()
        self.assertTrue(view['resume']['allowed'], view['resume']['reasons'])
        self.assertFalse(view['automaticResume']['enabled'])
        self.assertFalse(view['automaticResume']['eligible'])
        result = self.action('resume')
        self.assertEqual(result['status'], 'START_REQUESTED_UNCONFIRMED')
        command = self.calls[0][0]
        self.assertEqual(command[command.index('--resume-session')+1], SID)
        for forbidden in ('--autonomous', '--prepare-session', '--dangerously-skip-permissions'):
            self.assertNotIn(forbidden, command)
        self.assertEqual(self.state, before)
        self.assertFalse(result['actualStartConfirmed'])

    def test_running_same_owner_is_noop_and_other_idle_vscode_is_ignored(self):
        self.inventory['processes'] = [dict(self.process(OTHER,99), commandLine=f'claude --resume {OTHER} --stdio')]
        self.assertTrue(self.snapshot()['resume']['allowed'])
        self.inventory['processes'].append(self.process())
        self.assertEqual(self.action('resume')['status'], 'NOOP_ALREADY_RUNNING')
        self.assertEqual(self.calls, [])

    def test_ambiguous_inventory_old_pid_and_other_production_block(self):
        for inventory in ({'available': False, 'processes': []},
                          {'available': True, 'processes': [self.process(created='2026-10-22T11:00:00+09:00')]},
                          {'available': True, 'processes': [self.process(OTHER,99)]}):
            self.inventory = inventory
            self.assertEqual(self.action('resume')['status'], 'BLOCKED')
        self.assertEqual(self.calls, [])

    def test_stale_pin_approval_v3_and_inflight_block_without_mutation(self):
        pin = self.snapshot()['requestPin']
        self.pin['baselineHead'] = 'new-head'
        self.assertEqual(self.action('resume', pin)['reasons'], ['STALE_CONTROL_PIN'])
        self.pin['baselineHead'] = 'b'*40
        for field, value in [('enabled', False), ('identifier', 'old')]:
            auth = self.state['productionEnvironment']['productionDispatch']['resumeAuthorization']
            old = auth[field]; auth[field] = value
            self.assertEqual(self.action('resume')['status'], 'BLOCKED')
            auth[field] = old
        self.state['productionEnvironment']['productionDispatch']['ledger'] = [{'status': 'in_flight'}]
        self.assertEqual(self.action('resume')['status'], 'BLOCKED')
        self.assertEqual(self.calls, [])

    def test_explicit_user_pause_and_human_review_are_never_cleared(self):
        auto = self.state['productionEnvironment']['autonomous']
        for status in ('pause_requested', 'paused_by_user', 'ready_for_human_review'):
            auto['status'] = status
            self.assertEqual(self.action('resume')['status'], 'BLOCKED')
            self.assertEqual(auto['status'], status)
        self.assertEqual(self.calls, [])

    def test_confirmed_exhaustion_blocks_until_reset_even_when_observation_ages(self):
        auto = self.state['productionEnvironment']['autonomous']
        auto['modObservation'] = {'sessionId': SID, 'observedAt': '2026-10-23T11:59:30+09:00',
                                  'rateLimits': [{'kind':'five_hour','percentUsed':100,'resetsAt':'2026-10-23T13:00:00+09:00'}]}
        self.assertEqual(self.snapshot()['usage']['status'], 'WAITING')
        self.assertFalse(self.snapshot()['resume']['allowed'])
        auto['modObservation']['observedAt'] = '2026-10-23T10:00:00+09:00'
        self.assertEqual(self.snapshot()['usage']['status'], 'WAITING')
        self.assertFalse(self.snapshot()['resume']['allowed'])
        auto['modObservation']['rateLimits'][0]['percentUsed'] = 80
        self.assertEqual(self.snapshot()['usage']['status'], 'UNKNOWN')
        self.assertIsNone(self.snapshot()['usage']['windows'][0]['percentUsed'])
        auto['modObservation']['observedAt'] = '2026-10-23T11:59:30+09:00'
        auto['modObservation']['rateLimits'][0]['percentUsed'] = 100
        auto['modObservation']['rateLimits'][0]['resetsAt'] = '2026-10-23T11:00:00+09:00'
        self.assertEqual(self.snapshot()['usage']['status'], 'UNKNOWN')

    def test_safe_stop_calls_only_normal_pause_even_with_inflight_never_kill(self):
        self.state['productionEnvironment']['productionDispatch']['ledger'] = [{'status': 'in_flight'}]
        result = self.action('safe-stop')
        self.assertEqual(result['status'], 'SAFE_STOP_REQUESTED')
        self.assertEqual(self.calls[0][0][-4:], ['autonomy', 'pause', '--session-id', SID])
        self.assertNotIn('timeout', self.calls[0][1])
        self.assertFalse(result['actualStopConfirmed'])
        self.state['productionEnvironment']['autonomous']['stopRequested'] = True
        self.assertEqual(self.action('safe-stop')['status'], 'NOOP_SAFE_STOP_ALREADY_REQUESTED')

    def test_api_key_environment_cannot_silently_route_subscription_action(self):
        with patch.dict(os.environ, {'ANTHROPIC_API_KEY':'fixture-present-not-real'}):
            self.assertIn('SUBSCRIPTION_ONLY_AUTH_REVIEW_REQUIRED', self.snapshot()['resume']['reasons'])
            self.assertEqual(self.action('resume')['status'], 'BLOCKED')


if __name__ == '__main__': unittest.main()

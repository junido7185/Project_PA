import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('pa_console', Path(__file__).with_name('pa_console.py'))
console = importlib.util.module_from_spec(spec)
spec.loader.exec_module(console)


class ConsoleTests(unittest.TestCase):
    def test_current_adaptive_amendment_keeps_unknown_usage_without_historical_cap_stop(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.project(root)
            p=root/console.STATE;s=json.loads(p.read_text())
            s['a'].update(activeTicket='D7',status='in_progress',executionPolicy='APPROVED_GOAL_ADAPTIVE_PRODUCTION',contract='contract.md',perTicketBudget={'compileFix':3,'targetedPlay':3,'dedicatedValidators':1})
            s['a']['ticketStatus']['D7']='NOT_STARTED'
            source=Path(__file__).resolve().parents[2]/'AI_WORKFLOW/03_TASKS/CODEX_DEMO_POLISH_FINISH_PROMPT.md'
            (root/'contract.md').write_text(source.read_text('utf-8-sig').splitlines()[0]+'\n','utf-8')
            p.write_text(json.dumps(s));before=p.read_bytes()
            view=console.snapshot(root)
            self.assertNotIn('UNKNOWN_BUDGET',view['work_gate']);self.assertIn('WAIT_USER_02',view['work_gate'])
            self.assertEqual(view['ticket'],'D7');self.assertEqual(view['budget']['compile'],[None,None])
            self.assertEqual(view['historical_ticket_limits']['compileFix'],3)
            self.assertEqual(console.mod_state(root)['_console']['budgetPolicy']['mode'],'adaptive')
            self.assertEqual(p.read_bytes(),before)
    def project(self, root):
        p = root/console.STATE; p.parent.mkdir(parents=True)
        p.write_text(json.dumps({'demoProductionRouting': {'selectedApproval': 'a', 'unityEditorOwner': 'codex'},
            'a': {'activeTicket': 'D2', 'status': 'blocked_budget', 'ticketStatus': {'D2': 'INCOMPLETE_BLOCKED_BUDGET'},
                  'results': {'D2': {'compileCycles': 4, 'targetedPlayRuns': 5, 'dedicatedValidatorsAdded': 1,
                  'approvedTotalBudget': {'compileFix': 4, 'targetedPlay': 5, 'dedicatedValidators': 1}}}}}))
        h=root/console.HANDOFF;h.parent.mkdir(parents=True);h.write_text('Exact next action: wait for current task budget.\n')

    def test_render_keeps_work_block_separate_from_available_quota(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);self.project(root);before=(root/console.STATE).read_bytes()
            text=console.statusline(root, {'rate_limits': {'five_hour': {'used_percentage': 0, 'resets_at': 0}}})
            self.assertIn('WORK_BLOCKED', text);self.assertIn('5h used 0%', text)
            self.assertIn('7d used N/A', text);self.assertIn('compile 4/4', text)
            self.assertEqual(before,(root/console.STATE).read_bytes())

    def test_missing_limits_do_not_turn_into_zero(self):
        self.assertEqual(console.percent(None), 'N/A')
        self.assertEqual(console.percent(True), 'N/A')
        self.assertEqual(console.percent(300), 'N/A')

    def test_settings_summary_does_not_copy_secrets(self):
        with tempfile.TemporaryDirectory() as d:
            p=Path(d)/'settings.json';p.write_text(json.dumps({'env': {'ANTHROPIC_API_KEY':'secret-value'}, 'apiKeyHelper':'secret-command'}))
            summary=json.dumps(console.configuration_summary(p))
            self.assertNotIn('secret-value',summary);self.assertNotIn('secret-command',summary)
            self.assertIn('ANTHROPIC_API_KEY',summary)

    def test_missing_project_is_visible_and_not_an_exception_in_statusline(self):
        with tempfile.TemporaryDirectory() as d:
            self.assertIn('unavailable',console.statusline(Path(d),{}))


if __name__ == '__main__': unittest.main()

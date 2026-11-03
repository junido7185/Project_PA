"""Offline regression: task duration is unlimited; inactivity and safety remain bounded."""
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import progress_watchdog as progress
import production_adapter as guard
import test_production_adapter as fixtures


class RecoveryTests(unittest.TestCase):
    def test_real_activity_continues_past_old_total_deadline(self):
        with tempfile.TemporaryDirectory() as d:
            output = Path(d)/'child.jsonl'
            elapsed = [0]
            class Child:
                args = ['fixture']
                def communicate(self, payload, timeout):
                    elapsed[0] += 4
                    if elapsed[0] >= 20: return 'complete', ''
                    output.write_text(str(elapsed[0]))
                    raise subprocess.TimeoutExpired(self.args, timeout)
            result = progress.communicate_active(Child(), 'task', idle_seconds=5,
                paths=[output], clock=lambda:elapsed[0])
            self.assertEqual(result, ('complete',''))
            self.assertGreater(elapsed[0], 5)

    def test_periodic_snapshots_do_not_hide_a_frozen_child(self):
        elapsed = [0]; snapshots = []
        class Child:
            args = ['frozen-fixture']
            def communicate(self, payload, timeout):
                elapsed[0] += 2
                raise subprocess.TimeoutExpired(self.args, timeout)
        with self.assertRaises(subprocess.TimeoutExpired):
            progress.communicate_active(Child(), 'task', idle_seconds=5,
                clock=lambda:elapsed[0],checkpoint_seconds=2,
                checkpoint=lambda:snapshots.append(elapsed[0]))
        self.assertEqual(elapsed[0],6)
        self.assertEqual(snapshots,[2,4,6])

    def test_long_idle_setting_is_valid_but_invalid_timeout_still_rejected(self):
        progress.InactivityWatchdog(7200)
        for seconds in (0,-1,float('inf'),float('nan')):
            with self.assertRaises(ValueError):progress.InactivityWatchdog(seconds)

    def test_many_failures_need_changed_cause_and_new_evidence_not_new_approval(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s=fixtures.fixture()
            pin={'approvalKey':'a','ticket':'D2','workspaceSha256':'same'}
            entry=dict(pin,operation='compile',returncode=1)
            s['productionEnvironment']['productionDispatch']={'ledger':[entry.copy() for _ in range(20)]}
            req={'operation':'compile'}
            self.assertTrue(guard.recovery_errors(s,pin,req,root))
            proof=root/'diagnosis.md';proof.write_text('A new Console trace establishes the cause.')
            req['recovery']={'cause':'assembly order','changedApproach':'repair existing dependency', 'evidence':'diagnosis.md'}
            self.assertFalse(guard.recovery_errors(s,pin,req,root))
            entry=s['productionEnvironment']['productionDispatch']['ledger'][-1]
            entry['recoveryEvidenceSha256']=guard.digest(proof.read_bytes())
            self.assertTrue(guard.recovery_errors(s,pin,req,root))

    def test_start_has_no_45_minute_cap_and_retains_live_owner_guard(self):
        from types import SimpleNamespace
        args=SimpleNamespace(action='start',check_only=False,owner='claude-code',timeout=180)
        s=fixtures.fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
        with patch.dict(guard.os.environ,{},clear=True),patch.object(guard,'authority',return_value=(s,pin)),\
             patch.object(guard,'authorization_errors',return_value=[]),patch.object(guard,'processes',return_value={}):
            guard.enforce_entry(args)

    def test_one_proven_resume_covers_approved_followup_not_explicit_user_hold(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s=fixtures.fixture();fixtures.Contracts().adaptive(root,s)
            s['a'].update(activeTicket='D3',approvedTickets=['D2','D3'],preapprovedThrough='D3',ticketStatus={'D3':'NOT_STARTED'})
            proof=root/'Logs/AgentConsole/resume.md';proof.parent.mkdir(parents=True);proof.write_text('Actual user resume fixture')
            s['productionEnvironment']['productionDispatch']={'resumeAuthorization':{'enabled':True,
                'approvalKey':'a','ticket':'D2','baselineHead':'head','userMessageEvidence':proof.relative_to(root).as_posix(),
                'userMessageSha256':guard.digest(proof.read_bytes())}}
            pin={key:'value' for key in ('workspaceSha256','stateSha256','handoffSha256','contractSha256')}
            pin.update(approvalKey='a',ticket='D3',owner='claude-code',baselineHead='head')
            request=dict(pin,provider='claude-code',sessionId='sid')
            self.assertEqual(guard.authorization_errors(s,pin,request,root),[])
            s['a']['ticketStatus']['D3']='USER_REJECTED_HOLD'
            self.assertIn('TICKET_NOT_RUNNABLE',guard.authorization_errors(s,pin,request,root))

    def test_structured_unity_edits_reuse_writer_gate_and_protected_paths_remain(self):
        req={'operation':'unity','params':{'toolCall':{'name':'manage_gameobject','arguments':{'action':'modify','target':'owned-prop'}}}}
        argv,_,tool=guard.operation(req)
        self.assertEqual(argv,['mcp','--method','tools/call'])
        self.assertEqual(tool['name'],'manage_gameobject')
        for action,target in [('delete','owned-prop'),('modify','Prototype_FirstDay'),('modify','SmartphoneUI.cs')]:
            req['params']['toolCall']['arguments'].update(action=action,target=target)
            with self.assertRaises(guard.GuardError):guard.operation(req)


if __name__=='__main__':unittest.main()

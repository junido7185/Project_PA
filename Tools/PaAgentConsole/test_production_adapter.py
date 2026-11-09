"""Safety contracts in temporary files. No Unity, models or real approval mutation."""
import contextlib
import copy
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec=importlib.util.spec_from_file_location('adapter',Path(__file__).with_name('production_adapter.py'))
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)

def fixture():
    return {'demoProductionRouting':{'selectedApproval':'a','unityEditorOwner':'claude-code'},
      'a':{'activeTicket':'D2','status':'in_progress','policyId':'PREAPPROVED_MILESTONE_CONTINUATION',
       'approvalSource':'human','approvedTickets':['D2'],'preapprovedThrough':'D2','stopAtMilestone':'human-review',
       'pushAllowed':False,'ticketStatus':{'D2':'IN_PROGRESS'},'perTicketBudget':{'compileFix':3,'targetedPlay':3,'dedicatedValidators':1},
       'results':{'D2':{'compileCycles':0,'targetedPlayRuns':0,'dedicatedValidatorsAdded':0}}},
      'productionEnvironment':{'baseHead':'head','coordinatorLease':{'owner':'claude-code','status':'active','sessionId':'sid','cliPid':200}}}

class Contracts(unittest.TestCase):
    def adaptive(self,root,state):
        state['a'].update(executionPolicy='APPROVED_GOAL_ADAPTIVE_PRODUCTION',contract='contract.md')
        source=g.ROOT/'AI_WORKFLOW/03_TASKS/CODEX_DEMO_POLISH_FINISH_PROMPT.md'
        (root/'contract.md').write_text(source.read_text('utf-8-sig').splitlines()[0]+'\n','utf-8')
    def test_adaptive_amendment_does_not_enforce_historical_caps_or_invent_missing_totals(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s=fixture();self.adaptive(root,s);s['a']['results']={}
            before=copy.deepcopy(s)
            self.assertEqual(g.budget_policy(s,root)['mode'],'adaptive')
            self.assertFalse(g.budget_errors(s,root=root));self.assertEqual(s,before)
            s['a']['results']['D2']={'compileCycles':40,'targetedPlayRuns':50,'dedicatedValidatorsAdded':1}
            self.assertFalse(g.budget_errors(s,{'compileFix':1},root))
    def test_adaptive_flag_alone_does_not_disable_guards(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s=fixture();s['a'].update(executionPolicy='APPROVED_GOAL_ADAPTIVE_PRODUCTION',contract='missing.md')
            self.assertIn('BUDGET_POLICY_REVIEW_REQUIRED',g.budget_errors(s,root=root))
    def test_missing_boolean_negative_and_exhausted_counters_fail_closed(self):
        for value in (None,True,-1,3):
            s=fixture();s['a']['results']['D2']['compileCycles']=value
            self.assertTrue(g.budget_errors(s))
    def test_budget_ledger_never_regresses_across_provider_or_session(self):
        s=fixture();s['productionEnvironment']['productionDispatch']={'ledger':[{'approvalKey':'a','ticket':'D2','usedAfter':{'compileFix':2}}]}
        self.assertIn('CUMULATIVE_BUDGET_REGRESSION:compileFix',g.budget_errors(s,{}))
    def test_zero_cost_stop_can_follow_spent_budget_but_cannot_add_play(self):
        s=fixture();s['a']['results']['D2']['targetedPlayRuns']=3
        self.assertFalse(g.budget_errors(s,{}));self.assertTrue(g.budget_errors(s,{'targetedPlay':1}))
    def test_spent_validator_does_not_block_compile_when_no_new_validator_is_requested(self):
        s=fixture();s['a']['results']['D2']['dedicatedValidatorsAdded']=1
        self.assertFalse(g.budget_errors(s))
        self.assertTrue(g.budget_errors(s,{'dedicatedValidators':1}))
    def test_no_environment_setup_or_subscription_reset_activates_resume(self):
        s=fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code','baselineHead':'head'}
        errors=g.authorization_errors(s,pin,dict(pin,provider='claude-code',sessionId='sid'))
        self.assertIn('USER_02_PRODUCTION_RESUME_REQUIRED',errors)
    def test_raw_old_helper_cannot_dispatch_without_permit(self):
        with patch.dict(g.os.environ,{},clear=True):
            with self.assertRaises(g.GuardError):g.enforce_entry(SimpleNamespace(action='editor'))
    def test_authorized_existing_lead_start_accepts_finite_default_but_refuses_live_previous_pid(self):
        s=fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
        args=SimpleNamespace(action='start',check_only=False,owner='claude-code',timeout=45)
        with patch.dict(g.os.environ,{},clear=True),patch.object(g,'authority',return_value=(s,pin)),\
             patch.object(g,'authorization_errors',return_value=[]),patch.object(g,'processes',return_value={}):
            g.enforce_entry(args)
        with patch.dict(g.os.environ,{},clear=True),patch.object(g,'authority',return_value=(s,pin)),\
             patch.object(g,'authorization_errors',return_value=[]),patch.object(g,'processes',return_value={200:'claude.exe'}):
            with self.assertRaises(g.GuardError):g.enforce_entry(args)
    def test_autonomous_supervisor_child_is_accepted_only_for_its_own_generation(self):
        s=fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
        s['productionEnvironment']['autonomous']={'generation':'tok'}
        lease=s['productionEnvironment']['coordinatorLease'];lease.update(generation='tok',launcherPid=g.os.getpid(),cliPid=None)
        args=SimpleNamespace(action='start',check_only=False,owner='claude-code',timeout=45,
                             autonomous=True,foreground=True,lease_token='tok')
        live={g.os.getpid():'python.exe'}
        def run(arguments,processes):
            with patch.dict(g.os.environ,{},clear=True),patch.object(g,'authority',return_value=(s,pin)),\
                 patch.object(g,'authorization_errors',return_value=[]),patch.object(g,'processes',return_value=processes):
                g.enforce_entry(arguments)
        run(args,live)
        for change in ({'lease_token':'stale'},{'foreground':False}):
            with self.subTest(change=change):
                with self.assertRaises(g.GuardError):run(SimpleNamespace(**{**vars(args),**change}),live)
        lease['cliPid']=300
        with self.assertRaises(g.GuardError):run(args,{**live,300:'claude.exe'})
        lease.update(cliPid=None,launcherPid=999)
        with self.assertRaises(g.GuardError):run(args,{**live,999:'python.exe'})
    def test_nested_support_child_cannot_spawn_existing_worker(self):
        s=fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
        args=SimpleNamespace(action='worker',role='critic',timeout=8)
        with patch.dict(g.os.environ,{'PA_AGENT_CONSOLE_CHILD':'1'},clear=True),\
             patch.object(g,'authority',return_value=(s,pin)),patch.object(g,'authorization_errors',return_value=[]):
            with self.assertRaises(g.GuardError):g.enforce_entry(args)
    def test_transfer_requires_exact_dead_child_and_every_observation(self):
        s=fixture();pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
        errors=g.transfer_errors(s,pin,{'targetProvider':'codex'},{}, {200:'claude.exe'})
        self.assertIn('EXACT_PREVIOUS_CHILD_NOT_CONFIRMED_EXITED',errors)
        self.assertIn('EXACT_EDITOR_REQUIRED',errors)
        self.assertIn('USER_02_WRITER_TRANSFER_NOT_AUTHORIZED',errors)
    def test_transfer_gate_can_accept_complete_observations_without_resetting_state(self):
        s=fixture();s['productionEnvironment']['productionDispatch']={'resumeAuthorization':{'enabled':True,'writerTransferAllowed':True}}
        pin={key:key for key in ('approvalKey','ticket','baselineHead','workspaceSha256','stateSha256','handoffSha256','contractSha256')}
        pin['owner']='claude-code'
        request=dict(pin,targetProvider='codex',previousSessionId='sid')
        obs={key:False for key in ('inFlight','isCompiling','isPlaying','sceneDirty')}
        obs.update(observedAt=g.now(),projectRoot=str(g.ROOT),previousChildPid=200,editorPid=100,inputReleased=True,saveOverrideClear=True)
        before=copy.deepcopy(s)
        self.assertFalse(g.transfer_errors(s,pin,request,obs,{100:'unity.exe'}))
        self.assertEqual(before,s)
    def test_arbitrary_code_and_shell_operations_are_refused(self):
        for op in ('execute_code','shell','worker','resume-all'):
            with self.assertRaises(g.GuardError):g.operation({'operation':op})
    def dispatch_case(self, timeout=False, adaptive=False):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory);s=fixture()
            if adaptive:self.adaptive(root,s);s['a']['results']={}
            g.write(root/g.STATE,s)
            pin={'approvalKey':'a','ticket':'D2','owner':'claude-code'}
            request={'operation':'compile','provider':'claude-code','sessionId':'sid','runtimeObservation':{
                'observedAt':g.now(),'projectRoot':str(root),'isCompiling':False,'inFlight':False,'sceneDirty':False,
                'inputReleased':True,'saveOverrideClear':True,'editorPid':100}}
            def inspect(req, path):
                return g.read(root/g.STATE),pin,['mcp','--method','tools/call'],{'compileFix':1,'targetedPlay':0,'dedicatedValidators':0},None,[]
            def launch(*args,**kwargs):
                recorded=g.read(root/g.STATE)
                if adaptive:self.assertNotIn('D2',recorded['a']['results'])
                else:self.assertEqual(recorded['a']['results']['D2']['compileCycles'],1)
                self.assertEqual(recorded['productionEnvironment']['productionDispatch']['ledger'][0]['status'],'in_flight')
                if timeout:raise subprocess.TimeoutExpired('fake',180)
                return SimpleNamespace(returncode=1)
            with patch.object(g,'inspect',side_effect=inspect),patch.object(g,'processes',return_value={100:'unity.exe',200:'claude.exe'}),\
                 patch.object(g,'helper',return_value=SimpleNamespace(state_lock=contextlib.nullcontext)),patch.object(g.subprocess,'run',side_effect=launch):
                result=g.dispatch(request,True,root)
            final=g.read(root/g.STATE)
            if adaptive:
                self.assertNotIn('D2',final['a']['results'])
                ledger=final['productionEnvironment']['productionDispatch']['ledger'][0]
                self.assertEqual(ledger['costs']['compileFix'],1);self.assertIsNone(ledger['usedAfter']['compileFix'])
            else:self.assertEqual(final['a']['results']['D2']['compileCycles'],1)
            return result,final
    def test_adaptive_dispatch_records_attempt_without_fabricating_lifetime_total(self):
        self.dispatch_case(adaptive=True)
    def test_failed_dispatch_keeps_precharged_budget(self):
        result,state=self.dispatch_case()
        self.assertEqual(result['returncode'],1)
        self.assertNotIn('PASS',result['status'])
    def test_timeout_keeps_in_flight_without_retry_or_refund(self):
        result,state=self.dispatch_case(True)
        self.assertEqual(result['status'],'AMBIGUOUS_IN_FLIGHT_NO_REPLAY')
        self.assertEqual(state['productionEnvironment']['productionDispatch']['ledger'][0]['status'],'in_flight')

if __name__=='__main__':unittest.main()

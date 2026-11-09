"""No model, original state/config or production process changes."""
import importlib.util
import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

spec=importlib.util.spec_from_file_location('bridge_candidate',Path(__file__).resolve().parents[1]/'CodexAppServerBridge.py')
bridge=importlib.util.module_from_spec(spec);spec.loader.exec_module(bridge)
spec=importlib.util.spec_from_file_location('supervisor_candidate_tests',Path(__file__).resolve().parents[1]/'Invoke-ProjectPAProduction.py')
supervisor=importlib.util.module_from_spec(spec);spec.loader.exec_module(supervisor)


class BridgeObservationTests(unittest.TestCase):
    def test_path_cli_is_reused_without_discovery_or_version_process(self):
        with mock.patch.object(bridge.shutil,'which',return_value='installed-codex.exe'),mock.patch.object(bridge.subprocess,'run') as run:
            self.assertEqual(bridge.resolve_codex_executable(),'installed-codex.exe');run.assert_not_called()

    def test_missing_path_reuses_verified_installed_extension_cli(self):
        with tempfile.TemporaryDirectory() as d:
            home=Path(d);binary=home/'.vscode/extensions/openai.chatgpt-26.1007.1-win32-x64/bin/windows-x86_64/codex.exe';binary.parent.mkdir(parents=True);binary.write_bytes(b'existing')
            with mock.patch.object(bridge.shutil,'which',return_value=None),mock.patch.object(bridge.Path,'home',return_value=home),mock.patch.object(bridge.os,'name','nt'),mock.patch.object(bridge.subprocess,'run',return_value=SimpleNamespace(returncode=0,stdout='codex-cli 0.160.1\n')) as run:
                self.assertEqual(bridge.resolve_codex_executable(),str(binary))
                self.assertEqual(run.call_args.args[0],[str(binary),'--version'])

    def test_failed_version_probe_does_not_execute_appserver(self):
        with tempfile.TemporaryDirectory() as d:
            home=Path(d);binary=home/'.vscode/extensions/openai.chatgpt-26.1/bin/windows-x86_64/codex.exe';binary.parent.mkdir(parents=True);binary.write_bytes(b'file')
            with mock.patch.object(bridge.shutil,'which',return_value=None),mock.patch.object(bridge.Path,'home',return_value=home),mock.patch.object(bridge.os,'name','nt'),mock.patch.object(bridge.subprocess,'run',return_value=SimpleNamespace(returncode=0,stdout='other tool')):
                with self.assertRaises(bridge.CodexBridgeError) as caught:bridge.resolve_codex_executable()
                self.assertEqual(caught.exception.category,'cli_unavailable')

    def test_exact_sanitized_bridge_failure_and_location_preserved_without_raw_rpc(self):
        try:raise bridge.CodexBridgeError('configuration','Codex app-server RPC failed: config/read (configuration)',-32602,method='config/read')
        except bridge.CodexBridgeError as e:detail=bridge.failure_observation(e,'codex_usage_observation')
        self.assertEqual(detail['method'],'config/read');self.assertEqual(detail['code'],-32602)
        self.assertIn('config/read',detail['message']);self.assertEqual(detail['frames'][-1]['function'],'test_exact_sanitized_bridge_failure_and_location_preserved_without_raw_rpc')
        self.assertFalse(detail['claudeResumeBlocked']);self.assertTrue(detail['codexRelayAttemptSkipped'])
        unknown=bridge.failure_observation(RuntimeError('secret raw account response'),'stage')
        self.assertNotIn('secret',str(unknown))

    def test_supervisor_failure_reporting_never_reopens_the_failed_bridge(self):
        spec=importlib.util.spec_from_file_location('supervisor_candidate',Path(__file__).resolve().parents[1]/'Invoke-ProjectPAProduction.py')
        supervisor=importlib.util.module_from_spec(spec);spec.loader.exec_module(supervisor)
        with mock.patch.object(supervisor,'codex_bridge_module',side_effect=RuntimeError('unavailable')):
            try:raise bridge.CodexBridgeError('cli_unavailable','Codex executable was not found',method='executable/resolve')
            except Exception as exc:detail=supervisor.relay_failure_detail(exc,'codex_usage_observation')
        self.assertEqual(detail['message'],'Codex executable was not found')
        self.assertFalse(detail['claudeResumeBlocked']);self.assertEqual(detail['writerTransfer'],'separate_adapter_gates_not_executed')


class RecoveryTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name);self.runs=self.root/'Logs/ParallelProduction';self.folder=self.runs/'Autonomous-20261011';self.folder.mkdir(parents=True)
        self.state=self.root/'state.json'
        for name,value in [('ROOT',self.root),('RUNS',self.runs),('STATE',self.state),
                           ('_CODEX_BRIDGE_MODULE',None),('_CODEX_BRIDGE_LOAD_ERROR',None),('_CODEX_BRIDGE_LOAD_TRACEBACK',None)]:
            patch=mock.patch.object(supervisor,name,value);patch.start();self.addCleanup(patch.stop)
        patch=mock.patch.object(supervisor,'state_lock',contextlib.nullcontext);patch.start();self.addCleanup(patch.stop)

    def test_normal_start_uses_json_only_ack_without_rewriting_saved_envelope(self):
        content='다음 실제 벌목 작업';envelope='Previously saved explicit instruction body'
        message={'id':'next-job','targetSessionId':'sid','targetOwner':'claude-code',
            'sourceKind':'pa_user_input','explicitUserSubmit':True,'text':content,'envelope':envelope,
            'textSha256':supervisor.hashlib.sha256(content.encode()).hexdigest(),
            'envelopeSha256':supervisor.hashlib.sha256(envelope.encode()).hexdigest(),
            'delivery':{'status':'NOT_ATTEMPTED'},'path':'Logs/AgentConsole/messages/next-job.json'}
        path=self.root/message['path'];supervisor.write(path,message);before=path.read_bytes()
        state={'productionEnvironment':{'relaySupport':{'userInstructions':[copy.deepcopy(message)]}}}
        with mock.patch.object(supervisor,'safe_path',side_effect=lambda rel:(self.root/rel).resolve()):
            output=supervisor.pending_user_instruction_prompt(state,'sid')
        raw=output.split('containing ONLY ',1)[1].split('. Report your actual decision',1)[0]
        receipt=json.loads(raw)
        self.assertEqual(receipt,{'protocol':'pa-instruction-ack-v1','id':'next-job',
            'textSha256':message['textSha256'],'kind':'received'})
        self.assertIn('dedicated JSON-only assistant text',output)
        self.assertNotIn('[PA_RECEIVED:',output);self.assertNotIn('[PA_RESULT:',output)
        self.assertEqual(before,path.read_bytes());self.assertEqual(state['productionEnvironment']['relaySupport']['userInstructions'][0],message)

    def failure(self,n=0):
        return {'type':'CodexBridgeError','category':'cli_unavailable','message':'CLI absent',
                'stage':'codex_usage_observation','observedAt':f'2026-10-21T13:{n:04d}:00+09:00',
                'generation':'g','sessionId':'s','code':None,'method':None}

    def test_repeated_failures_are_coalesced_but_every_event_is_archived(self):
        relay={'observations':{'codex':{'rateLimits':[{'percentUsed':93}]}},'lifetimeUsage':'unaggregated'}
        original=copy.deepcopy(relay)
        for n in range(1000):supervisor.retain_bridge_failure(relay,self.failure(n),self.folder)
        self.assertEqual(len(relay['bridgeFailureHistory']),1)
        row=relay['bridgeFailureHistory'][0]
        self.assertEqual(row['count'],1000);self.assertEqual(row['firstSeen'],self.failure()['observedAt'])
        self.assertEqual(row['lastSeen'],self.failure(999)['observedAt'])
        records=[json.loads(x) for x in (self.folder/'bridge-failures.jsonl').read_text(encoding='utf-8').splitlines()]
        self.assertEqual(records,[self.failure(n) for n in range(1000)])
        self.assertEqual(relay['bridgeFailureArchive']['capturedEventCount'],1000)
        self.assertEqual(relay['observations'],original['observations']);self.assertEqual(relay['lifetimeUsage'],'unaggregated')

    def test_distinct_failures_bound_state_and_migrate_old_history_without_loss(self):
        old=[dict(self.failure(n),message=f'legacy{n}') for n in range(25)]
        relay={'bridgeFailureHistory':copy.deepcopy(old)}
        for n in range(60):supervisor.retain_bridge_failure(relay,dict(self.failure(n),message=f'new{n}'),self.folder)
        self.assertEqual(len(relay['bridgeFailureHistory']),20)
        archived=[json.loads(x) for x in (self.folder/'bridge-failures.jsonl').read_text(encoding='utf-8').splitlines()]
        self.assertEqual(archived[:25],old);self.assertEqual(len(archived),85)
        self.assertEqual(relay['bridgeFailureArchive']['capturedEventCount'],85)

    def test_archive_failure_preserves_legacy_history_without_growing_state(self):
        relay={'bridgeFailureHistory':[self.failure()]};before=copy.deepcopy(relay['bridgeFailureHistory'])
        with mock.patch.object(Path,'open',side_effect=PermissionError('fixture')):
            for n in range(50):supervisor.retain_bridge_failure(relay,self.failure(n),self.folder)
        self.assertEqual(relay['bridgeFailureHistory'],before)
        self.assertEqual(relay['bridgeFailureArchiveError']['type'],'PermissionError')

    def bridge_source(self,source):
        p=self.root/'Tools/LoopEngineering/CodexAppServerBridge.py';p.parent.mkdir(parents=True,exist_ok=True);p.write_text(source,encoding='utf-8');return p

    def test_running_process_keeps_startup_bridge_after_disk_replacement(self):
        p=self.bridge_source("VERSION='old'\n")
        pinned=supervisor.codex_bridge_module();p.write_text("VERSION='replacement'\n",encoding='utf-8')
        self.assertIs(supervisor.codex_bridge_module(),pinned);self.assertEqual(pinned.VERSION,'old')
        # A new process starts with empty caches and observes the new installed source.
        supervisor._CODEX_BRIDGE_MODULE=None
        self.assertEqual(supervisor.codex_bridge_module().VERSION,'replacement')

    def test_load_failure_is_pinned_until_new_process_and_traceback_does_not_grow(self):
        p=self.bridge_source('raise RuntimeError("fixture import fault")\n')
        sizes=[]
        for n in range(25):
            try:supervisor.codex_bridge_module()
            except RuntimeError as e:
                size=0;tb=e.__traceback__
                while tb:size+=1;tb=tb.tb_next
                sizes.append(size)
        self.assertLessEqual(max(sizes)-min(sizes),1)
        p.write_text("VERSION='fixed'\n",encoding='utf-8')
        with self.assertRaises(RuntimeError):supervisor.codex_bridge_module()

    def test_worker_is_released_only_after_its_exact_pid_is_registered(self):
        supervisor.write(self.state,{'productionEnvironment':{'workers':{'job':{'status':'assigned'}}}})
        signals=[]
        def release(value):
            self.assertEqual(supervisor.read(self.state)['productionEnvironment']['workers']['job']['supervisorPid'],4312)
            signals.append(value)
        pipe=mock.Mock();pipe.write.side_effect=release
        child=SimpleNamespace(pid=4312,stdin=pipe)
        with mock.patch.object(supervisor.subprocess,'Popen',return_value=child) as popen:
            self.assertIs(supervisor.launch_registered_supervisor('job',self.folder),child)
        self.assertEqual(signals,['registered\n']);pipe.close.assert_called_once()
        self.assertIn('--registered-start',popen.call_args.args[0])
        supervisor.registered_supervisor_release(io.StringIO(signals[0]))
        with self.assertRaises(RuntimeError):supervisor.registered_supervisor_release(io.StringIO(''))

    def test_registration_failure_closes_pipe_without_releasing_child(self):
        supervisor.write(self.state,{'productionEnvironment':{'workers':{'job':{}}}})
        child=SimpleNamespace(pid=4312,stdin=mock.Mock())
        with mock.patch.object(supervisor.subprocess,'Popen',return_value=child),mock.patch.object(supervisor,'write',side_effect=OSError('fixture state failure')):
            with self.assertRaises(OSError):supervisor.launch_registered_supervisor('job',self.folder)
        child.stdin.write.assert_not_called();child.stdin.close.assert_called_once()

    def test_headless_periodic_checkpoint_uses_the_actual_session(self):
        sid='d1ff77d7-03aa-4167-8dfe-576673db1b47'
        supervisor.write(self.state,{'productionEnvironment':{'coordinatorLease':{'owner':'claude-code','status':'stopped'},'coordinator':{}}})
        args=SimpleNamespace(owner='claude-code',check_only=False,autonomous=False,resume_session=sid,
                             intake_check=False,prepare_session=True,prompt_file=None,headless=True,effort='high',timeout=45)
        report={'configurationReady':True,'routing':{'unityEditorOwner':'claude-code','nextTicket':'D7'}}
        doctor=SimpleNamespace(configuration=lambda _:report,runtime_check=lambda:{})
        def communicate(child,prompt,**kw):
            kw['checkpoint']()
            Path(kw['paths'][0]).write_text(json.dumps({'type':'result','is_error':False,'session_id':sid,'result':'fixture'}),encoding='utf-8')
        def snapshot(provider,**kw):
            self.assertEqual(provider,'claude-code');self.assertEqual(kw['session_id'],sid)
        with mock.patch.object(supervisor,'doctor',return_value=doctor),mock.patch.object(supervisor.shutil,'which',return_value='fixture-cli'),\
             mock.patch.object(supervisor.subprocess,'Popen',return_value=SimpleNamespace(pid=4321,returncode=0)),\
             mock.patch.object(supervisor.progress,'communicate_active',side_effect=communicate),\
             mock.patch.object(supervisor,'relay_checkpoint',side_effect=snapshot) as checkpoint,\
             mock.patch.object(supervisor,'actual_effort',return_value='fixture'),contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(supervisor.lead_start(args),0)
        checkpoint.assert_called_once()

    def test_completed_worker_preserves_result_before_final_usage_rpc_or_record_failure(self):
        for failure in ('usage_rpc','usage_state_write'):
            with self.subTest(failure=failure):
                folder=self.folder/failure;folder.mkdir();(folder/'assignment.md').write_text('Fixture only.')
                job={'id':'completed-'+failure,'logDirectory':str(folder),'worktree':str(self.root/'worktree'),
                    'role':'verifier','requestedModel':'fixture','requestedEffort':'high','timeoutMinutes':1,
                    'goal':'Fixture result preservation','allowedFiles':[],'usage':{'inputTokens':321}}
                supervisor.write(self.state,{'productionEnvironment':{'relaySupport':{'observations':{
                    'codex':{'existingUsage':'preserved'}}}}})
                requests=[];records=[]
                events=iter([
                    {'method':'item/completed','params':{'item':{'type':'agentMessage','text':'Actual fixture handoff.'}}},
                    {'method':'turn/completed','params':{'turn':{'id':'turn-fixture','status':'completed'}}}])
                def request(method,params):
                    requests.append(method)
                    if method=='thread/start':return {'thread':{'id':'actual-fixture-session'},'model':'fixture'}
                    if method=='turn/start':return {'turn':{'id':'turn-fixture'}}
                    if method=='account/rateLimits/read':
                        saved=json.loads((folder/'result.json').read_text('utf-8-sig'))
                        self.assertEqual(saved['turn']['status'],'completed');self.assertFalse(saved['is_error'])
                        self.assertEqual(records[-1]['status'],'ready_for_review')
                        self.assertEqual((folder/'HANDOFF.md').read_text(),'Actual fixture handoff.')
                        if failure=='usage_rpc':
                            raise bridge.CodexBridgeError('timeout','Fixture final usage RPC timeout',method=method)
                        return {'observedAt':supervisor.now(),'ordinaryUsageAllowed':True,'rateLimits':[{'percentUsed':20}]}
                    raise AssertionError('Unexpected RPC '+method)
                server=SimpleNamespace(request=request,next_event=lambda **kw:next(events),
                    _process=SimpleNamespace(pid=4321),diagnostics={'fixture':True})
                module=SimpleNamespace(CodexAppServer=lambda _:contextlib.nullcontext(server),normalize_usage=lambda value:value)
                actual_write=supervisor.write
                def write(path,data):
                    if failure=='usage_state_write' and Path(path)==self.state:
                        raise OSError('Fixture final usage state write failed')
                    return actual_write(path,data)
                with mock.patch.object(supervisor,'codex_bridge_module',return_value=module),\
                     mock.patch.object(supervisor,'record',side_effect=lambda value:records.append(copy.deepcopy(value))),\
                     mock.patch.object(supervisor,'write',side_effect=write),\
                     mock.patch.object(supervisor,'relay_checkpoint') as checkpoint,\
                     contextlib.redirect_stdout(io.StringIO()):
                    supervisor.supervise_codex_appserver(job)
                self.assertEqual(job['status'],'ready_for_review');self.assertEqual(job['exitCode'],0)
                self.assertFalse(job['resultIsError']);self.assertEqual(job['usage'],{'inputTokens':321})
                self.assertIsNone(job['codexPid']);self.assertNotIn('relayContinuation',job)
                self.assertEqual(requests.count('account/rateLimits/read'),1)
                self.assertEqual(job['postTurnUsageObservation']['stage'],'worker_post_turn_usage_observation')
                self.assertEqual(job['postTurnUsageObservation']['nextWorkUsageAvailability'],'UNKNOWN_AFTER_OBSERVATION_FAILURE')
                self.assertEqual(supervisor.read(self.state)['productionEnvironment']['relaySupport']['observations']['codex'],
                                 {'existingUsage':'preserved'})
                self.assertTrue((folder/'post-turn-usage-observation.json').is_file())
                checkpoint.assert_called_once();self.assertTrue(checkpoint.call_args.kwargs['safe_point'])

    def test_tooling_application_admission_precedes_guard_import_and_preserves_registry(self):
        locked=[False]
        @contextlib.contextmanager
        def admission_lock():
            self.assertFalse(locked[0]);locked[0]=True
            try:yield
            finally:locked[0]=False
        guard=SimpleNamespace(enforce_entry=mock.Mock(side_effect=lambda args:self.assertFalse(locked[0])))
        loader=SimpleNamespace(exec_module=mock.Mock(side_effect=lambda module:self.assertTrue(locked[0])))
        args=SimpleNamespace(action='start',check_only=False)
        for operation,status,blocked in [('tooling_application','in_flight',True),
            ('tooling_application','complete',False),('tooling_application','rolled_back',False),('native','in_flight',False)]:
            with self.subTest(operation=operation,status=status):
                state={'productionEnvironment':{'productionDispatch':{'ledger':[{
                    'id':'tooling-application-fixture','operation':operation,'status':status}],
                    'lifetimeUsage':'unaggregated'},'autonomous':{'stopRequested':True}}}
                supervisor.write(self.state,state);before=self.state.read_bytes();loader.exec_module.reset_mock();guard.enforce_entry.reset_mock()
                with mock.patch.object(supervisor,'state_lock',admission_lock),\
                     mock.patch.object(supervisor.importlib.util,'spec_from_file_location',return_value=SimpleNamespace(loader=loader)) as spec,\
                     mock.patch.object(supervisor.importlib.util,'module_from_spec',return_value=guard):
                    if blocked:
                        with self.assertRaisesRegex(RuntimeError,'TOOLING_APPLICATION_IN_FLIGHT'):supervisor.enforce_production_entry(args)
                        spec.assert_not_called();guard.enforce_entry.assert_not_called()
                        supervisor.enforce_production_entry(SimpleNamespace(action='status'))
                    else:supervisor.enforce_production_entry(args)
                self.assertEqual(self.state.read_bytes(),before);loader.exec_module.assert_called_once()
                guard.enforce_entry.assert_called_once()
        with mock.patch.object(supervisor,'state_lock',admission_lock),\
             mock.patch.object(supervisor,'_ENTRY_SOURCE_SHA256','stale-preinstall-module'),\
             mock.patch.object(supervisor.importlib.util,'spec_from_file_location') as spec:
            with self.assertRaisesRegex(RuntimeError,'HELPER_SOURCE_CHANGED_RESTART_REQUIRED'):
                supervisor.enforce_production_entry(args)
            spec.assert_not_called()


class DispatchDiagnosisTests(unittest.TestCase):
    def run_case(self,observe=None,relay=None,record=None):
        with contextlib.redirect_stdout(io.StringIO()) as output:
            detail=supervisor.relay_maintenance_result(observe or mock.Mock(),relay or mock.Mock(return_value={}),
                record or mock.Mock(),lambda:True)
        return detail,output.getvalue()

    def test_usage_failure_is_confirmed_before_relay_not_a_quota_or_game_result(self):
        relay=mock.Mock()
        detail,_=self.run_case(observe=mock.Mock(side_effect=RuntimeError('fixture')),relay=relay)
        relay.assert_not_called();self.assertEqual(detail['stage'],'codex_usage_observation')
        self.assertEqual(detail['dispatchStatus'],'NOT_ATTEMPTED_CONFIRMED');self.assertTrue(detail['codexRelayAttemptSkipped'])
        self.assertFalse(detail['gameCompletion'])

    def test_failure_during_relay_is_unknown_even_if_side_effect_happened(self):
        sent=[]
        def send_then_fail():sent.append('job');raise OSError('fixture after side effect')
        detail,_=self.run_case(relay=send_then_fail)
        self.assertEqual(sent,['job']);self.assertEqual(detail['dispatchStatus'],'UNKNOWN')
        self.assertIsNone(detail['codexRelayAttemptSkipped']);self.assertEqual(detail['stage'],'relay_call')

    def test_dispatched_job_survives_post_dispatch_record_failure_without_retry(self):
        choice={'dispatched':True,'jobId':'actual-job','sessionId':'actual-session','supervisorPid':123}
        relay=mock.Mock(return_value=choice)
        detail,output=self.run_case(relay=relay,record=mock.Mock(side_effect=OSError('fixture write failure')))
        relay.assert_called_once();self.assertEqual(detail['dispatchStatus'],'DISPATCHED')
        self.assertFalse(detail['codexRelayAttemptSkipped']);self.assertEqual(detail['stage'],'post_dispatch_observation')
        self.assertEqual(detail['dispatchResult'],choice);self.assertIn('actual-job',output)

    def test_explicit_no_dispatch_is_skipped_and_normal_dispatch_remains_dispatched(self):
        for sent,status,skipped in [(False,'NOT_DISPATCHED_CONFIRMED',True),(True,'DISPATCHED',False)]:
            with self.subTest(sent=sent):
                record=mock.Mock();detail,_=self.run_case(relay=mock.Mock(return_value={'dispatched':sent}),record=record)
                self.assertEqual(detail['status'],'OK');self.assertEqual(detail['dispatchStatus'],status)
                self.assertIs(detail['codexRelayAttemptSkipped'],skipped);record.assert_called_once()


if __name__=='__main__':unittest.main()

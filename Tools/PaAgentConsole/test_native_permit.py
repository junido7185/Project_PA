"""Offline native boundary fixtures. Never import WinAPI, focus, capture, or send input."""
import ast
import base64
import contextlib
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

spec=importlib.util.spec_from_file_location('native_adapter_fixture',Path(__file__).with_name('production_adapter.py'))
g=importlib.util.module_from_spec(spec);spec.loader.exec_module(g)


class NativePermitTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        self.qa='Logs/VisualQA/20261021-140000-NATIVE_PRODUCTION'
        (self.root/self.qa/'save').mkdir(parents=True)
        (self.root/self.qa/'result.txt').write_text('READY native Windows input\n',encoding='utf-8')
        self.state={'productionEnvironment':{'coordinatorLease':{'owner':'claude-code','sessionId':'owner-sid','generation':None,'status':'active'},
            'autonomous':{'status':'running','stopRequested':False}}}
        self.request={'operation':'native','provider':'claude-code','sessionId':'owner-sid','generation':None,
            'params':{'pid':123,'gameView':True,'action':'key','values':['E','.12'],'qaRun':self.qa}}
        self.actual={'observedAt':g.now(),'projectRoot':str(self.root),'editorPid':123,'windowPid':123,
            'window':456,'windowTitle':'Game','processCreationFileTime':'100','editorCreationFileTime':'100',
            'isCompiling':False,'sceneDirty':False,'isPlaying':True,'inputReleased':True,'qaActive':True,
            'saveOverrideClear':False,'scenePath':'Assets/Scenes/WorldSandbox.unity','qaMode':'native',
            'qaOut':str(self.root/self.qa),'saveRoot':str(self.root/self.qa/'save'),'clientWidth':1920,'clientHeight':1080}

    def test_native_requires_real_isolated_context_and_preserves_state(self):
        before=copy.deepcopy(self.state)
        result=g.validate_native_context(self.state,self.request,self.actual,self.root)
        self.assertEqual(result['qaProofSha256'],g.digest((self.root/self.qa/'result.txt').read_bytes()))
        self.assertEqual(self.state,before)
        argv,costs,tool=g.operation(self.request)
        self.assertEqual(argv,['native','--pid','123','--game-view','--log',self.qa+'/native-input','key','E','.12'])
        self.assertEqual(sum(costs.values()),0);self.assertIsNone(tool)

    def test_unknown_input_save_dirty_and_wrong_actual_identity_fail_closed(self):
        for field,value in [('inputReleased',None),('inputReleased',False),('saveOverrideClear',None),
            ('sceneDirty',True),('sceneDirty',None),('isPlaying',False),('qaActive',False),('qaMode','candidate'),
            ('saveRoot',str(self.root/'elsewhere')),('qaOut',str(self.root/'elsewhere')),
            ('editorPid',999),('windowPid',999),('editorCreationFileTime','200'),('windowTitle','Inspector')]:
            with self.subTest(field=field,value=value):
                with self.assertRaises(g.GuardError):g.validate_native_context(self.state,self.request,{**self.actual,field:value},self.root)

    def test_owner_generation_and_user_stop_are_preserved(self):
        for change in [{'generation':'stale'},{'sessionId':'other'},{'provider':'codex'}]:
            with self.subTest(change=change):
                with self.assertRaises(g.GuardError):g.validate_native_context(self.state,{**self.request,**change},self.actual,self.root)
        req=copy.deepcopy(self.request);del req['generation']
        with self.assertRaises(g.GuardError):g.validate_native_context(self.state,req,self.actual,self.root)
        for control in [{'stopRequested':True},{'status':'pause_requested'},{'status':'paused_by_user'}]:
            state=copy.deepcopy(self.state);state['productionEnvironment']['autonomous'].update(control)
            with self.assertRaises(g.GuardError):g.validate_native_context(state,self.request,self.actual,self.root)
        (self.root/self.qa/'result.txt').write_text('READY native Windows input\nSTOP native-input session',encoding='utf-8')
        with self.assertRaises(g.GuardError):g.validate_native_context(self.state,self.request,self.actual,self.root)

    def test_candidate_shortcuts_and_outside_click_are_not_added(self):
        with self.assertRaises(g.GuardError):g.operation({'operation':'candidate'})
        for values in [['CTRL+S'],['ALT+F4'],['E','nan']]:
            req=copy.deepcopy(self.request);req['params']['values']=values
            with self.assertRaises(g.GuardError):g.operation(req)
        req=copy.deepcopy(self.request);req['params'].update(action='click',values=['1920','0'])
        with self.assertRaises(g.GuardError):g.validate_native_context(self.state,req,self.actual,self.root)

    def test_consumer_reobserves_before_consuming_and_refuses_changed_input(self):
        pin={key:key for key in ('approvalKey','ticket','owner','baselineHead','workspaceSha256','handoffSha256','contractSha256')}
        context=g.validate_native_context(self.state,self.request,self.actual,self.root)
        ledger={'id':'permit','operation':'native','status':'in_flight','adapterPid':987,'nativeRequest':self.request,'nativeContext':context}
        self.state['productionEnvironment']['productionDispatch']={'ledger':[ledger]}
        g.write(self.root/g.STATE,self.state)
        argv=g.operation(self.request)[0];args=SimpleNamespace(action='native',native_action='key',pid=123)
        permit={'id':'permit','observedAt':g.now(),'authority':pin,'nativeRequest':self.request,
            'reservedStateSha256':g.digest((self.root/g.STATE).read_bytes()),
            'argvSha256':g.digest(json.dumps(argv,ensure_ascii=False).encode())}
        path='Logs/AgentConsole/fixture/permit.json';g.write(self.root/path,permit)
        helper=SimpleNamespace(state_lock=contextlib.nullcontext,native_runtime_observation=mock.Mock(return_value={**self.actual,'inputReleased':None}))
        with mock.patch.object(g,'ROOT',self.root),mock.patch.object(g,'helper',return_value=helper),\
             mock.patch.dict(g.os.environ,{'PA_PRODUCTION_PERMIT':path}),mock.patch.object(g.sys,'argv',['helper',*argv]),\
             mock.patch.object(g,'authority',return_value=(self.state,pin)),mock.patch.object(g.os,'getppid',return_value=987),\
             mock.patch.object(g,'processes',return_value={987:'python.exe'}):
            with self.assertRaises(g.GuardError):g.enforce_entry(args)
            self.assertNotIn('consumedAt',g.read(self.root/g.STATE)['productionEnvironment']['productionDispatch']['ledger'][0])
            helper.native_runtime_observation.return_value=self.actual
            g.enforce_entry(args)
            with self.assertRaises(g.GuardError):g.enforce_entry(args)
        self.assertEqual(helper.native_runtime_observation.call_count,3)

    def test_dispatch_inflight_is_not_exempted_and_does_not_launch(self):
        self.state['a']={'results':{}}
        self.state['productionEnvironment']['coordinatorLease']['cliPid']=200
        g.write(self.root/g.STATE,self.state)
        request={**self.request,'runtimeObservation':{'observedAt':g.now(),'projectRoot':str(self.root),'inFlight':True}}
        pin={'approvalKey':'a','ticket':'D7','owner':'claude-code'}
        def inspect(*_):return self.state,pin,*g.operation(self.request),[]
        helper=SimpleNamespace(state_lock=contextlib.nullcontext,native_runtime_observation=lambda _:self.actual)
        with mock.patch.object(g,'inspect',side_effect=inspect),mock.patch.object(g,'helper',return_value=helper),\
             mock.patch.object(g,'processes',return_value={123:'unity.exe',200:'claude.exe'}),mock.patch.object(g.subprocess,'run') as launch:
            with self.assertRaises(g.GuardError):g.dispatch(request,True,self.root)
            launch.assert_not_called()

    def test_tooling_application_blocks_direct_start_and_dispatch_without_usage_mutation(self):
        pin={'approvalKey':'a','ticket':'D7','owner':'claude-code'}
        self.state['a']={'results':{'D7':{'compileCycles':17,'targetedPlayRuns':23}}}
        helper=SimpleNamespace(state_lock=contextlib.nullcontext)
        args=SimpleNamespace(action='start',check_only=False,owner='claude-code',timeout=45)
        for status in ('in_flight','complete','rolled_back'):
            with self.subTest(status=status):
                self.state['productionEnvironment']['productionDispatch']={'ledger':[{
                    'id':'tooling-application-fixture','operation':'tooling_application','status':status}],
                    'lifetimeUsage':'unaggregated'}
                before=copy.deepcopy(self.state)
                with mock.patch.object(g,'ROOT',self.root),mock.patch.object(g,'helper',return_value=helper),\
                     mock.patch.object(g,'authority',return_value=(self.state,pin)),\
                     mock.patch.object(g,'authorization_errors',return_value=[]),mock.patch.object(g,'budget_errors',return_value=[]),\
                     mock.patch.object(g,'recovery_errors',return_value=[]),mock.patch.object(g,'processes',return_value={}),\
                     mock.patch.dict(g.os.environ,{},clear=True),mock.patch.object(g.subprocess,'run') as launch:
                    errors=g.inspect(self.request,self.root)[-1]
                    if status=='in_flight':
                        self.assertIn('TOOLING_APPLICATION_IN_FLIGHT',errors)
                        with self.assertRaisesRegex(g.GuardError,'TOOLING_APPLICATION_IN_FLIGHT'):g.enforce_entry(args)
                        report=g.dispatch(self.request,True,self.root)
                        self.assertEqual(report['status'],'BLOCKED_PRESERVED')
                        self.assertIn('TOOLING_APPLICATION_IN_FLIGHT',report['errors'])
                    else:
                        self.assertFalse(errors);g.enforce_entry(args)
                    launch.assert_not_called()
                self.assertEqual(self.state,before)


class InputReleaseTests(unittest.TestCase):
    def functions(self):
        source=Path(__file__).resolve().parents[1]/'OpeningDemoFinal/windows_player.py'
        tree=ast.parse(source.read_text('utf-8-sig'))
        selected=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in ('hold_keys','click_button')]
        env={'u':SimpleNamespace(MapVirtualKeyW=lambda key,_:key),'time':SimpleNamespace(sleep=mock.Mock())}
        exec(compile(ast.Module(body=selected,type_ignores=[]),str(source),'exec'),env)
        return env

    def test_failed_keydown_and_release_still_attempt_all_owned_keyups(self):
        env=self.functions();calls=[]
        def event(scan,up=False):
            calls.append((scan,up))
            if (scan,up) in [(2,False),(2,True)]:raise OSError('fixture')
        env['key_event']=event
        with self.assertRaises(RuntimeError):env['hold_keys']([1,2],.1)
        self.assertEqual(calls,[(1,False),(2,False),(2,True),(1,True)])

    def test_failed_mouse_down_still_attempts_owned_mouse_up(self):
        env=self.functions();calls=[]
        def event(up=False):
            calls.append(up)
            if not up:raise OSError('fixture')
        env['mouse_event']=event
        with self.assertRaises(OSError):env['click_button']()
        self.assertEqual(calls,[False,True])


class RuntimeObservationTests(unittest.TestCase):
    def test_fixed_query_reads_actual_fixture_and_rejects_other_editor_without_importing_winapi(self):
        path=Path(__file__).resolve().parents[1]/'LoopEngineering/Invoke-ProjectPAProduction.py'
        spec=importlib.util.spec_from_file_location('native_runtime_fixture',path)
        h=importlib.util.module_from_spec(spec);spec.loader.exec_module(h)
        b=lambda value:base64.b64encode(value.encode()).decode()
        values=['PA_NATIVE_V1',b(str(h.ROOT/'Assets')),'123','100','1','0',
            b('Assets/Scenes/WorldSandbox.unity'),'0',b(str(h.ROOT/'Logs/qa/save')),b('native'),b('Logs/qa'),'1']
        def rpc(method,*args,**kw):
            if method=='initialize':return {'protocolVersion':'fixture'},'session'
            if method=='notifications/initialized':return {},None
            code=args[0]['arguments']['code']
            self.assertNotIn('SetString',code);self.assertNotIn('EnterPlaymode',code);self.assertNotIn('.Focus',code)
            return {'content':[{'type':'text','text':json.dumps({'success':True,'data':{'result':'\n'.join(values)}})}]},None
        window=mock.Mock(return_value={'windowPid':123,'inputReleased':True})
        loader=SimpleNamespace(loader=SimpleNamespace(exec_module=mock.Mock()))
        with mock.patch.object(h,'desktop_available',return_value=True),mock.patch.object(h,'doctor',return_value=SimpleNamespace(rpc=rpc)),\
             mock.patch.object(h.importlib.util,'spec_from_file_location',return_value=loader),\
             mock.patch.object(h.importlib.util,'module_from_spec',return_value=SimpleNamespace(native_window_observation=window)):
            actual=h.native_runtime_observation(123)
            self.assertTrue(actual['qaActive']);self.assertFalse(actual['saveOverrideClear']);self.assertEqual(actual['qaMode'],'native')
            window.assert_called_once_with(123,True)
            with self.assertRaises(RuntimeError):h.native_runtime_observation(999)
            self.assertEqual(window.call_count,1)


if __name__=='__main__':unittest.main()

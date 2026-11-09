import json
import hashlib
import datetime as dt
from pathlib import Path
import tempfile
import unittest
from production_observation import observation, open_evidence, run_events


class ObservationTests(unittest.TestCase):
    def test_actual_quota_exit_is_waiting_separate_from_old_checkpoint(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            s['productionEnvironment']['coordinatorLease']['status']='stopped'
            s['productionEnvironment']['autonomous'].update(status='paused_checkpoint_or_blocker',stopRequested=False,
                modObservation={'sessionId':'prod','observedAt':'2026-10-23T00:49:00+09:00',
                    'rateLimits':[{'kind':'five_hour','percentUsed':100,'resetsAt':'2099-01-01T00:00:00Z'}]})
            (run/'result.json').write_text(json.dumps({'session_id':'prod','api_error':'usage_limit_reached'}))
            self.assertEqual(observation(root,s,self.probe)['production']['status'],'WAITING')
            s['productionEnvironment']['autonomous']['status']='pause_requested'
            self.assertEqual(observation(root,s,self.probe)['production']['status'],'PAUSE_REQUESTED')

    def test_current_headless_activity_is_separate_from_old_autonomy_blocker(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            s['productionEnvironment']['coordinatorLease']['status']='active'
            s['productionEnvironment']['autonomous'].update(status='paused_checkpoint_or_blocker',enabled=False,
                checkpoint={'recordedAt':'2026-10-19T15:00:00+09:00'},stopRequested=False)
            events=[{'type':'system','subtype':'init','session_id':'prod'},
                {'type':'assistant','timestamp':'2026-10-20T06:01:00Z','message':{'content':[
                    {'type':'tool_use','name':'Read','id':'actual','input':{'file_path':'AGENTS.md'}}]}}]
            (run/'claude.jsonl').write_text('\n'.join(json.dumps(e) for e in events))
            alive=lambda pid,start:{'status':'ALIVE','pid':pid}
            v=observation(root,s,alive)
            self.assertEqual(v['production']['status'],'RUNNING_HEADLESS')
            self.assertEqual(v['production']['recordedStatus'],'paused_checkpoint_or_blocker')
            s['productionEnvironment']['autonomous']['status']='pause_requested'
            self.assertEqual(observation(root,s,alive)['production']['status'],'PAUSE_REQUESTED')

    def test_build_action_opens_only_existing_local_folder(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d); s,run=self.setup_project(root)
            build=root/'Builds/Windows/ExistingBuild'; build.mkdir(parents=True)
            (build/'ProjectPA.exe').write_bytes(b'existing')
            (build/'ProjectPA_Data').mkdir()
            view=observation(root,s,self.probe)
            opened=[]
            result=open_evidence(root,build.relative_to(root).as_posix(),view,opened.append)
            self.assertEqual(opened,[str(build.resolve())]);self.assertFalse(result['executed'])
            with self.assertRaises(ValueError):
                open_evidence(root,(build/'ProjectPA.exe').relative_to(root).as_posix(),view,opened.append)

    def setup_project(self, root):
        run = root/'Logs/ParallelProduction/current'; run.mkdir(parents=True)
        return {'demoProductionRouting': {'selectedApproval': 'a', 'activeTicket': 'D7', 'unityEditorOwner': 'claude-code'},
                'productionEnvironment': {'coordinator': {'runDirectory': str(run)},
                    'coordinatorLease': {'sessionId': 'prod', 'generation': 'g', 'cliPid': 42, 'startedAt': '2026-10-20T15:00:00+09:00'},
                    'autonomous': {'cycleStartedAt': '2026-10-20T15:00:00+09:00', 'status': 'waiting_usage_reset',
                        'modObservation': {'sessionId': 'prod', 'rateLimits': []},
                        'resumeReservation': {'sessionId': 'prod', 'generation': 'g', 'resetAt': '2026-10-20T20:30:00+09:00', 'resumeAt': '2026-10-20T20:31:00+09:00'}},
                    'workers': {}}}, run

    def probe(self, pid, start): return {'pid': pid, 'status': 'EXITED'}

    def test_waiting_is_not_active_child_and_missing_quota_is_not_zero(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d); s,run=self.setup_project(root); before=json.dumps(s)
            (run/'result.json').write_text(json.dumps({'session_id':'prod','result':'usage limit'}))
            v=observation(root,s,self.probe)
            self.assertEqual(v['production']['status'],'UNKNOWN');self.assertIsNone(v['production']['lastActivityAt'])
            self.assertEqual(v['usage']['rateLimits'],[]);self.assertIsNone(v['usage']['codexBalance'])
            self.assertIsNone(v['usage']['resumeAt']);self.assertEqual(json.dumps(s),before)

    def test_stale_reservation_and_foreign_usage_are_hidden(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);a=s['productionEnvironment']['autonomous']
            a['resumeReservation']['generation']='old';a['modObservation']={'sessionId':'other','rateLimits':[{'percentUsed':0}]}
            v=observation(root,s,self.probe)
            self.assertFalse(v['usage']['reservationValid']);self.assertIsNone(v['usage']['resumeAt']);self.assertEqual(v['usage']['rateLimits'],[])

    def test_old_workers_excluded_and_real_native_support_kept_without_fake_session(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            s['productionEnvironment']['workers']={'old':{'startedAt':'2026-10-12T12:00:00+09:00'},'support':{'createdAt':'2026-10-20T16:00:00+09:00','provider':'codex','executionMethod':'native_codex_current_turn','goal':'isolated candidate'}}
            v=observation(root,s,self.probe)
            self.assertEqual([w['id'] for w in v['team']],['support']);self.assertIsNone(v['team'][0]['sessionId'])

    def test_loop_joins_actual_read_edit_result_check_capture_with_citations(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            manual=root/'Logs/ParallelProduction/World-v16-edge-verifier-x/HANDOFF.md';manual.parent.mkdir();manual.write_text('milky foam')
            shader=root/'Assets/PA_StylizedWater.shader';shader.parent.mkdir();shader.write_text('bandFoam = smoothstep(0.35, 0.8, bandFoam);')
            capture=root/'Logs/after.png';capture.write_bytes(b'image')
            events=[]
            def tool(id,name,inp,minute):
                events.append({'type':'assistant','timestamp':f'2026-10-20T06:{minute}:00Z','message':{'content':[{'type':'tool_use','id':id,'name':name,'input':inp}]}})
                events.append({'type':'user','message':{'content':[{'type':'tool_result','tool_use_id':id,'content':'errors=False msgs=0'}]}})
            tool('read','Read',{'file_path':str(manual)},'01')
            tool('edit','Edit',{'file_path':str(shader),'new_string':shader.read_text()},'02')
            tool('check','mcp__unityMCP__execute_code',{'code':'ShaderHasError'},'03')
            tool('capture','mcp__unityMCP__manage_camera',{'action':'screenshot','output_folder':'Logs','screenshot_file_name':'after.png'},'04')
            (run/'claude.jsonl').write_text('\n'.join(json.dumps(e) for e in events)+'\n{partial')
            v=observation(root,s,self.probe)
            self.assertEqual(v['production']['lastActivityAt'],'2026-10-20T15:04:00+09:00')
            self.assertEqual(v['changes'][0]['outcome'],'RETURNED');self.assertTrue(v['changes'][0]['currentTextMatches'])
            self.assertEqual(v['loop']['critique']['path'],manual.relative_to(root).as_posix())
            self.assertEqual(v['loop']['checks'][0]['result'],'errors=False msgs=0')
            self.assertEqual(v['loop']['reuseAfterFix'][0]['path'],'Logs/after.png')
            self.assertEqual(v['loop']['stageCounts']['adoptions'],0);self.assertGreater(v['changes'][0]['line'],0)

    def test_open_only_existing_observed_evidence_never_shell_or_traversal(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d); (root/'e.png').write_bytes(b'png');opened=[]
            v={'artifacts':[{'path':'e.png','openable':True}]}
            self.assertEqual(open_evidence(root,'e.png',v,opened.append),{'opened':'e.png'})
            self.assertEqual(opened,[str(root/'e.png')])
            for path in ['../e.png','https://example.com/e.png','bad.exe','e.png;whoami']:
                with self.assertRaises(ValueError):open_evidence(root,path,v,opened.append)

    def test_unknown_event_shapes_no_activity_from_ui_heartbeat(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            (run/'claude.jsonl').write_text('\n'.join(json.dumps(x) for x in [None,[],{'message':'string'},{'message':{'content':['str']}},{'type':'system','subtype':'ui_invalidate','timestamp':'2026-10-20T12:00:00Z'}]))
            v=observation(root,s,self.probe);self.assertIsNone(v['production']['lastActivityAt'])

    def test_bridge_failure_and_quota_wait_remain_separate_without_inventing_error_time(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            (run/'result.json').write_text(json.dumps({'session_id':'prod','result':'usage limit'}))
            log=root/'Logs/ParallelProduction/Autonomous-20261011/supervisor.log';log.parent.mkdir();log.write_text('Relay maintenance preserved: CodexBridgeError\n')
            v=observation(root,s,self.probe)
            self.assertEqual(v['production']['status'],'UNKNOWN');self.assertEqual(v['bridge']['status'],'LEGACY_ERROR_NO_DETAIL')
            self.assertIsNone(v['bridge']['claudeResumeBlocked']);self.assertIsNone(v['bridge']['codexRelayAttemptSkipped']);self.assertIsNone(v['bridge']['observedAt'])
            self.assertIsNone(v['usage']['resumeAt'])

    def test_completed_reset_record_and_scheduler_failure_do_not_override_latest_owner_usage(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);env=s['productionEnvironment'];auto=env['autonomous']
            current=dt.datetime.now(dt.timezone.utc)
            auto['modObservation']={'sessionId':'prod','observedAt':current.isoformat(),
                'rateLimits':[{'kind':'five_hour','percentUsed':25,'resetsAt':(current+dt.timedelta(hours=5)).isoformat()}]}
            auto['resumeReservation']['waitCompletedAt']='2026-10-21T11:52:16+09:00'
            source=root/'Logs/scheduler.json';source.write_text(json.dumps({'sameSid':'prod',
                'observedAt':'2026-10-22T12:00:00+09:00','task':'\\ProjectPA\\UsageResume','state':'Ready',
                'lastRun':'2026-10-22T03:42:00+09:00','lastResult':1,'nextRun':None,
                'serverResetAt':'2026-10-22T03:40:00+09:00','actualRebootOrRestartObserved':False}))
            env['workers']['support']={'ownerReviewRequest':{'targetSessionId':'prod',
                'usageWait':{'percentUsed':100,'serverResetAt':'2026-10-22T03:40:00+09:00'},
                'scheduledTaskEvidence':'Logs/scheduler.json'}}
            before=json.dumps(s);v=observation(root,s,self.probe)
            self.assertEqual(v['usage']['rateLimits'][0]['percentUsed'],25)
            self.assertEqual(v['usage']['quotaState'],'OBSERVED_UNEXHAUSTED');self.assertFalse(v['usage']['waitingForReset'])
            self.assertEqual(v['production']['status'],'UNKNOWN');self.assertFalse(v['usage']['reservationValid'])
            self.assertIsNone(v['usage']['resumeAt']);self.assertEqual(v['usage']['reservationRecord']['waitCompletedAt'],'2026-10-21T11:52:16+09:00')
            scheduler=v['usage']['scheduledTaskObservations'][0]
            self.assertEqual(scheduler['lastResult'],1);self.assertEqual(scheduler['lastRun'],'2026-10-22T03:42:00+09:00')
            self.assertIsNone(scheduler['nextRun']);self.assertFalse(scheduler['actualRestartReported'])
            self.assertEqual(json.dumps(s),before)
            auto['modObservation']['rateLimits']=[{'percentUsed':100,'resetsAt':(current-dt.timedelta(hours=1)).isoformat()}]
            expired=observation(root,s,self.probe)['usage']
            self.assertEqual(expired['quotaState'],'RESET_ELAPSED_UNVERIFIED');self.assertFalse(expired['waitingForReset'])
            auto['modObservation']['rateLimits'][0]['resetsAt']=(current+dt.timedelta(hours=1)).isoformat()
            waiting=observation(root,s,self.probe)
            self.assertEqual(waiting['production']['status'],'WAITING');self.assertTrue(waiting['usage']['waitingForReset'])

    def test_registered_worker_video_uses_preserved_copy(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);wt=root/'Logs/worker/worktree'
            clip=wt/'Evidence/Logs/movie.mp4';clip.parent.mkdir(parents=True);clip.write_bytes(b'existing')
            s['productionEnvironment']['workers']['actual']={'startedAt':'2026-10-20T15:02:00+09:00','worktree':str(wt),'evidence':['Evidence/Logs/movie.mp4']}
            v=observation(root,s,self.probe);clips=[a for a in v['artifacts'] if a['kind']=='video']
            self.assertEqual(len(clips),1);self.assertEqual(clips[0]['path'],clip.relative_to(root).as_posix())

    def test_pause_requested_and_unknown_never_become_running_with_live_child(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root)
            (run/'result.json').write_text(json.dumps({'session_id':'prod'}))
            s['productionEnvironment']['coordinatorLease']['status']='active'
            for recorded,expected in [('pause_requested','PAUSE_REQUESTED'),('future_unknown_state','UNKNOWN')]:
                s['productionEnvironment']['autonomous']['status']=recorded
                v=observation(root,s,lambda pid,start:{'pid':pid,'status':'ALIVE'})
                self.assertEqual(v['production']['status'],expected)
                self.assertEqual(v['production']['child']['status'],'ALIVE')

    def test_child_identity_uses_latest_valid_lease_or_autonomous_cycle_start(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);env=s['productionEnvironment']
            (run/'result.json').write_text(json.dumps({'session_id':'prod'}))
            env['autonomous']['status']='running';env['coordinatorLease']['status']='active'
            old='2026-10-21T23:00:00+09:00';fresh='2026-10-22T12:33:13+09:00'
            for lease_start,cycle_start in ((fresh,old),(old,fresh)):
                with self.subTest(lease_start=lease_start,cycle_start=cycle_start):
                    env['coordinatorLease']['startedAt']=lease_start
                    env['autonomous']['cycleStartedAt']=cycle_start
                    calls=[]
                    def probe(pid,not_before):
                        calls.append((pid,not_before));return {'pid':pid,'status':'ALIVE'}
                    view=observation(root,s,probe)
                    self.assertEqual(calls[0][1],lease_start)
                    self.assertEqual(calls[1],(42,fresh))
                    self.assertEqual(view['production']['status'],'RUNNING')

    def test_manual_stages_update_from_edit_and_read_to_owner_adoption_and_different_task_report(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);path='Docs/manual.md'
            manual=root/path;manual.parent.mkdir();manual.write_text('new decision',encoding='utf-8')
            def events(sid,operations):
                rows=[{'type':'system','subtype':'init','session_id':sid}]
                for index,(name,file,minute) in enumerate(operations):
                    rows.append({'type':'assistant','timestamp':f'2026-10-20T06:{minute}:00Z','message':{'content':[{
                        'type':'tool_use','id':str(index),'name':name,'input':{'file_path':str(file),'new_string':'new decision'}}]}})
                    rows.append({'type':'user','message':{'content':[{'type':'tool_result','tool_use_id':str(index),'content':'returned'}]}})
                return '\n'.join(json.dumps(x) for x in rows)
            (run/'claude.jsonl').write_text(events('prod',[('Edit',manual,'02'),('Read',manual,'03')]))
            initial=observation(root,s,self.probe)['loop']
            self.assertEqual(initial['stageCounts']['changes'],1);self.assertEqual(initial['stageCounts']['readsAfterChange'],1)
            self.assertEqual(initial['stageCounts']['usageReports'],0)
            worker=root/'Logs/later';wt=worker/'worktree';copy=wt/path;copy.parent.mkdir(parents=True);copy.write_bytes(manual.read_bytes())
            (worker/'claude.jsonl').write_text(events('different-task',[('Read',copy,'05')]))
            (worker/'HANDOFF.md').write_text('Used Docs/manual.md for the next shoreline correction.\n',encoding='utf-8')
            s['productionEnvironment']['workers']={'support':{'componentReview':{'owner':'claude-code','sessionId':'prod',
                'reviewedAt':'2026-10-20T15:02:30+09:00','components':{'manual':{'decision':'adopted_integrated',
                    'files':{path:{'before':'old','after':hashlib.sha256(manual.read_bytes()).hexdigest()}}}}}},
                'later-task':{'sessionId':'different-task','startedAt':'2026-10-20T15:04:00+09:00','worktree':str(wt),'logDirectory':str(worker)}}
            updated=observation(root,s,self.probe)['loop'];trail=updated['manualTrail'][0]
            self.assertEqual(updated['stageCounts']['adoptions'],1);self.assertTrue(trail['adoptions'][0]['currentMatchesAdopted'])
            self.assertEqual(updated['stageCounts']['laterTaskReads'],1);self.assertTrue(trail['laterTaskReads'][0]['copyMatchesAdopted'])
            self.assertEqual(updated['stageCounts']['usageReports'],1);self.assertEqual(trail['usageReports'][0]['status'],'REPORTED_WITH_EVIDENCE')
            self.assertNotEqual(initial['coverage'],updated['coverage']);self.assertNotIn('PASS',updated['coverage'])
            (worker/'HANDOFF.md').write_text('Read only; application not claimed.',encoding='utf-8')
            self.assertEqual(observation(root,s,self.probe)['loop']['stageCounts']['usageReports'],0)

    def test_actual_owner_method_adoption_and_diff_pointer_are_observed_without_inferred_reuse(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);manual=root/'Docs/current.md';manual.parent.mkdir();manual.write_text('current')
            sha=hashlib.sha256(manual.read_bytes()).hexdigest();patch=root/'Logs/change.patch';patch.write_text('diff')
            s['productionEnvironment']['workers']['support']={'methodAdoptionReview':{
                'owner':'claude-code','sessionId':'prod','reviewedAt':'2026-10-20T15:03:00+09:00','decision':'adopted_applied_docs',
                'files':{'Docs/current.md':['old',sha]}},'toolAdoptionEvidence':{'diff':'Logs/change.patch'}}
            view=observation(root,s,self.probe);trail=view['loop']['manualTrail'][0]
            self.assertTrue(trail['adoptions'][0]['currentMatchesAdopted']);self.assertIn('methodAdoptionReview',trail['adoptions'][0]['pointer'])
            self.assertEqual(view['loop']['stageCounts']['usageReports'],0)
            opened=[];open_evidence(root,'Logs/change.patch',view,opened.append);self.assertEqual(opened,[str(patch)])
            report=root/'Logs/use.json';report.write_text(json.dumps({'session':'prod','change':'Used this manual to preserve submerged silhouettes; real diff and captures linked.'}))
            s['productionEnvironment']['coordinator'].update(owner='claude-code',sessionId='prod')
            source=run/'claude.jsonl';source.write_text('\n'.join(json.dumps(row) for row in [
                {'type':'system','subtype':'init','session_id':'prod'},
                {'type':'assistant','session_id':'prod','timestamp':'2026-10-20T06:04:00Z','message':{'content':[
                    {'type':'tool_use','id':'report-write','name':'Write','input':{'file_path':str(report),'content':report.read_text()}}]}},
                {'type':'user','message':{'content':[{'type':'tool_result','tool_use_id':'report-write','content':'Report written'}]}}
            ])+'\n')
            use={'manualPath':'Docs/current.md','manualSha256':sha,'report':'Logs/use.json','reportSha256':hashlib.sha256(report.read_bytes()).hexdigest(),
                'source':source.relative_to(root).as_posix(),'line':2,'eventSha256':hashlib.sha256(source.read_text('utf-8-sig').splitlines(keepends=True)[1].encode()).hexdigest(),
                'reportField':'change','sessionId':'prod','at':'2026-10-20T15:04:00+09:00'}
            s['productionEnvironment']['workers']['support']['toolAdoptionMethodUses']=[use]
            applied=observation(root,s,self.probe)['loop']
            self.assertEqual(applied['stageCounts']['usageReports'],1);self.assertEqual(applied['stageCounts']['laterTaskReads'],0)
            self.assertEqual(applied['manualTrail'][0]['usageReports'][0]['status'],'OWNER_REPORTED_WITH_RESULT')
            self.assertTrue(applied['manualTrail'][0]['usageReports'][0]['ownerAttributionVerified'])
            report.write_text('changed evidence')
            self.assertEqual(observation(root,s,self.probe)['loop']['stageCounts']['usageReports'],0)
            s['productionEnvironment']['workers']['support']['methodAdoptionReview']['decision']='adopted_on_review_integration_denied'
            self.assertEqual(observation(root,s,self.probe)['loop']['stageCounts']['adoptions'],0)

    def test_registered_report_with_arbitrary_or_unrelated_source_never_claims_owner_use(self):
        with tempfile.TemporaryDirectory() as d:
            root=Path(d);s,run=self.setup_project(root);manual=root/'Docs/current.md';manual.parent.mkdir();manual.write_text('current')
            sha=lambda data:hashlib.sha256(data).hexdigest()
            report=root/'Logs/use.json';source=root/'Logs/arbitrary.txt'
            review={'owner':'claude-code','sessionId':'prod','reviewedAt':'2026-10-20T15:03:00+09:00',
                'decision':'adopted_applied_docs','files':{'Docs/current.md':['old',sha(manual.read_bytes())]}}
            use={'manualPath':'Docs/current.md','manualSha256':sha(manual.read_bytes()),'report':'Logs/use.json',
                'reportField':'change','sessionId':'other','line':1,'at':'2026-10-20T15:04:00+09:00'}
            s['productionEnvironment']['workers']['support']={'methodAdoptionReview':review,'toolAdoptionMethodUses':[use]}
            s['productionEnvironment']['coordinator'].update(owner='claude-code',sessionId='prod')
            cases=[('different_sid_arbitrary_line','other',source,'not a JSON event\n',1),
                ('same_sid_arbitrary_line','prod',source,'not a JSON event\n',1)]
            for name,body in [('empty_assistant',[]),('unrelated_successful_write',[
                {'type':'tool_use','id':'other','name':'Write','input':{'file_path':str(root/'Logs/other.json'),'content':'other'}}])]:
                rows=[{'type':'system','subtype':'init','session_id':'prod'},
                    {'type':'assistant','session_id':'prod','timestamp':'2026-10-20T06:04:00Z','message':{'content':body}},
                    {'type':'user','message':{'content':[{'type':'tool_result','tool_use_id':'other','content':'written'}]}}]
                cases.append((name,'prod',run/'claude.jsonl','\n'.join(json.dumps(row) for row in rows)+'\n',2))
            for name,sid,event_source,event_text,line in cases:
                with self.subTest(case=name):
                    report.write_text(json.dumps({'session':sid,'change':'Used the manual; this is only a registered claim.'}))
                    event_source.write_text(event_text);use.update(sessionId=sid,source=event_source.relative_to(root).as_posix(),
                        line=line,reportSha256=sha(report.read_bytes()),eventSha256=sha(event_text.splitlines(keepends=True)[line-1].encode()))
                    loop=observation(root,s,self.probe)['loop'];claim=loop['manualTrail'][0]['usageReports'][0]
                    self.assertEqual(claim['status'],'REGISTERED_REPORT_UNVERIFIED_SOURCE')
                    self.assertFalse(claim['ownerAttributionVerified']);self.assertIn('출처 미확인',claim['text'])
                    self.assertEqual(loop['stageCounts']['usageReports'],0)
                    self.assertEqual(loop['stageCounts']['unverifiedUsageReports'],1)


if __name__=='__main__':unittest.main()

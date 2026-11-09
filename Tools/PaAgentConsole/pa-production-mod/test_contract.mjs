// Candidate interface tests. Uses real register.js, no model/production calls.
import {readFile} from 'node:fs/promises';
import assert from 'node:assert/strict';
const source=await readFile(new URL('./hooks/register.js',import.meta.url),'utf8');
const {register}=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64'));
const handlers=[];register((name,matcher,fn)=>handlers.push({name,matcher:typeof matcher==='function'?null:matcher,fn:typeof matcher==='function'?matcher:fn}));
const state={demoProductionRouting:{selectedApproval:'a'},a:{ticketStatus:{D2:'INCOMPLETE_BLOCKED_BUDGET'},results:{D2:{approvedTotalBudget:{compileFix:4,targetedPlay:5}}}},_console:{productionHandoffPath:'preserved.md',budgetPolicy:{mode:'adaptive'},observation:{production:{status:'WAITING',recordedStatus:'waiting_usage_reset',sessionId:'prod-session',owner:'claude-code',ticket:'D7',lastActivityAt:'2026-10-20T06:51:57Z',launcher:{pid:7,status:'ALIVE'},child:{pid:8,status:'EXITED'}},usage:{sessionId:'prod-session',rateLimits:[{kind:'five_hour',percentUsed:100}],resumeAt:'2026-10-20T11:31:00Z'},team:[{id:'actual-worker',provider:'claude-code',status:'ready_for_review'}],changes:[{path:'Assets/a.shader',currentTextMatches:true,outcome:'RETURNED'}],artifacts:[{path:'Logs/after.png',kind:'image',openable:true}],loop:{coverage:'PARTIAL',remaining:['manual reuse unverified']},bridge:{status:'LEGACY_ERROR_NO_DETAIL',message:'CodexBridgeError',claudeResumeBlocked:false,codexRelayAttemptSkipped:true}}}};
let clipboard='',opened=0,clock,fail=false,calls=[],transportCalls=0,deliveryFailure=false,gateHold=false,savedInstruction=null;
const $={process:{run:async argv=>{calls.push(argv);if(argv.includes('--production-control'))return {exitCode:0,stdout:JSON.stringify({status:'START_REQUESTED_UNCONFIRMED',actualStartConfirmed:false})};if(argv.includes('--open-evidence'))return {exitCode:0,stdout:'{}'};if(argv.includes('--instruction-submit')) { const payload=JSON.parse(argv.at(-1)); savedInstruction={id:payload.id,targetSessionId:'prod-session',path:'Logs/AgentConsole/messages/'+payload.id+'.json',envelope:'[PA instruction '+payload.id+']',saved:true,canDeliver:!savedInstruction||savedInstruction.id!==payload.id}; return {exitCode:0,stdout:JSON.stringify(savedInstruction)}; } if(argv.includes('--instruction-delivery')) { if(deliveryFailure && JSON.parse(argv.at(-1)).result.isDelivered) throw new Error('record failed after send'); return {exitCode:0,stdout:JSON.stringify({canSend:!gateHold,targetSessionId:'prod-session',targetHold:gateHold?{reason:'TARGET_CHANGED'}:null})}; } assert.ok(argv.includes('--mod-state'));if(fail)throw new Error('source unavailable');return {exitCode:0,stdout:JSON.stringify(state)};}},session:{usage:async()=>({context:{percent:12},rateLimits:[{kind:'viewer_limit',percentUsed:12}]}),id:async()=>'viewer-session',version:async()=>({version:'2.1.301'}),send:async()=>{transportCalls++;return {isDelivered:true};}},agent:{list:async()=>[{id:'real-native-id',description:'known task',status:'completed'}]},clock:{every:(ms,fn)=>{assert.equal(ms,10000);clock=fn;return {cancel(){}};}},command:{register:async cmd=>assert.equal(cmd.immediate,true)},ui:{invalidate(){},open:async()=>opened++,copy:async t=>clipboard=t,resolve:()=>Object.fromEntries(['Box','Text','Button','Input'].map(name=>[name,props=>({name,...props})]))}};
const find=(n,c)=>handlers.find(h=>h.name===n&&(!c||h.matcher?.command===c)).fn;
const pane=()=>handlers.find(h=>h.name==='ui.render'&&h.matcher?.component==='Pane').fn($,{requestId:'pa-production-state'},async()=>null);
const flat=t=>[t,...(t.children??[]).filter(c=>typeof c==='object').flatMap(flat)];
const content=t=>flat(t).flatMap(x=>x.name==='Text'?x.children:[]).join('\n');
await find('session.start')($,{},async e=>e);
assert.match((await find('command.run','pa-status')($)).text,/WAITING/);
await find('command.run','pa')($);assert.equal(opened,1);
let tree=await pane();assert.match(content(tree),/CodexBridgeError/);assert.match(content(tree),/ALIVE.*EXITED/);
for(const [tab,pattern] of [['team',/actual-worker/],['changes',/Assets\/a.shader/],['loop',/PARTIAL/],['usage',/viewer-session[\s\S]*12%[\s\S]*prod-session[\s\S]*100%/],['bridge',/LEGACY_ERROR_NO_DETAIL/],['evidence',/Logs\/after.png/]]){
 await flat(tree).find(x=>x.key==='tab-'+tab).onPress();tree=await pane();assert.match(content(tree),pattern);
}
await flat(tree).find(x=>x.key==='open-0').onPress();assert.equal(calls.at(-1).at(-1),'Logs/after.png');
await flat(tree).find(x=>x.key==='pa-copy-resume').onPress();assert.match(clipboard,/Provider switch is not a task approval/);
const before=calls.length;await clock();assert.equal(calls.length,before+1);
fail=true;await flat(tree).find(x=>x.key==='pa-refresh').onPress();tree=await pane();assert.match(content(tree),/source unavailable/);assert.match(content(tree),/Logs\/after.png/);
assert.equal(await handlers.find(h=>h.name==='ui.render'&&h.matcher?.component==='Pane').fn($,{requestId:'other'},async()=> 'pass'),'pass');
assert.doesNotMatch(source,/\$\.(model|prompt)\.|\$\.fs\.write|\$\.agent\.spawn/);
fail=false;
state._console.observation.production.status='PAUSE_REQUESTED';
state._console.observation.loop={coverage:'편집 1 / 원본 채택 1 / 후속 읽기 1 / 사용 보고 1',manualTrail:[{path:'Docs/current-decision.md',
 changes:[{currentTextMatches:true,source:'Logs/current.jsonl',line:2}],adoptions:[{owner:'claude-code',currentMatchesAdopted:true,pointer:'existing.componentReview'}],
 laterTaskReads:[{jobId:'different-task',copyMatchesAdopted:true,source:'Logs/later.jsonl',line:3}],
 usageReports:[{ownerAttributionVerified:true,text:'Used current decision',source:'Logs/later/HANDOFF.md',line:4},
 {ownerAttributionVerified:false,text:'hash-valid report only',source:'Logs/arbitrary.txt',line:1}]}]};
await flat(tree).find(x=>x.key==='pa-refresh').onPress();
await flat(tree).find(x=>x.key==='tab-loop').onPress();tree=await pane();
assert.match(content(tree),/current-decision.md/);assert.match(content(tree),/원본 채택 기록/);
assert.match(content(tree),/different-task/);assert.match(content(tree),/owner 출처가 확인된 적용 보고/);
assert.match(content(tree),/등록된 보고·owner 출처 미확인 · hash-valid report only/);
assert.doesNotMatch(content(tree),/v16|v17b|공유 매뉴얼을 개선하고/);
assert.match((await find('command.run','pa-status')($)).text,/PAUSE_REQUESTED/);
console.log('Candidate pane: real render hook, all observation views, scope separation, evidence action, polling and failure preservation passed.');

// Exercise startup boundaries against a fresh copy of the actual hook for each case.
let caseId=0;
async function startup({event={isInteractive:true,cwd:'C:\\Users\\sdjsd\\Desktop\\Unity\\Project_PA\\'},viewer='viewer-session',producer='prod-session',unavailable=false,openFails=false}={}) {
 const h=[];const mod=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64')+'#'+caseId++);
 mod.register((name,matcher,fn)=>h.push({name,fn:typeof matcher==='function'?matcher:fn}));
 const openings=[];let reads=0;
 const api={...$,process:{run:async()=>{reads++;if(unavailable)throw new Error('offline');return {exitCode:0,stdout:JSON.stringify({_console:{observation:{production:{sessionId:producer}}}})};}},session:{...$.session,id:async()=>viewer},ui:{...$.ui,open:async opts=>{openings.push(opts);if(openFails)throw new Error('unsupported surface');}}};
 const start=h.find(x=>x.name==='session.start').fn;
 await start(api,event,async e=>e);
 return {openings,repeat:()=>start(api,event,async e=>e),reads:()=>reads};
}
let s=await startup();assert.equal(s.openings.length,0,'startup must not reopen a user-dismissed pane');
await s.repeat();assert.equal(s.openings.length,0,'user close/Escape must not be undone by another start event');
for(const config of [{event:{isInteractive:false,cwd:'C:/Users/sdjsd/Desktop/Unity/Project_PA'}},{event:{isInteractive:true,cwd:'C:/other'}},{event:{}},{viewer:'prod-session'},{viewer:null},{producer:null},{unavailable:true}]) {
 s=await startup(config);assert.equal(s.openings.length,0,JSON.stringify(config));
}
s=await startup({openFails:true});await s.repeat();assert.equal(s.openings.length,0,'startup never retries a closed/unsupported UI');
console.log('Observation startup: no automatic opening; user-closed, headless, writer, unknown and unsupported panes preserved.');

// Fixed supported VS Code entry and explicit control. No automatic opening.
const above=handlers.find(h=>h.name==='ui.render'&&h.matcher?.component==='AbovePrompt');
let bar=await above.fn($,{surface:'vscode'},async()=>null);
assert.match(flat(bar).find(x=>x.key==='pa-open-production').label,/0.3.0/);
const openingsBefore=opened;await flat(bar).find(x=>x.key==='pa-open-production').onPress();
assert.equal(opened,openingsBefore+1);
state._console.controls={requestPin:{owner:'claude-code',sessionId:'prod-session'},resume:{allowed:false,reasons:['USER_STOP_OR_HUMAN_REVIEW_PRESERVED']},safeStop:{allowed:false,reasons:['UNKNOWN']}};
await find('command.run','pa-resume')($);assert.match((await find('command.run','pa-resume')($)).text,/USER_STOP/);
assert.equal(calls.filter(x=>x.includes('--production-control')).length,0);
state._console.controls.resume={allowed:true,reasons:[]};
await find('command.run','pa-resume')($);
const controlCalls=calls.filter(x=>x.includes('--production-control'));
assert.equal(controlCalls.length,1);assert.equal(JSON.parse(controlCalls[0].at(-1)).action,'resume');
assert.equal(JSON.parse(controlCalls[0].at(-1)).expected.sessionId,'prod-session');
// New explicit user-submit boundary: official transport is never a read receipt.
// A stopped owner must retain the submission without entering official transport.
await flat(tree).find(x=>x.key==='tab-instructions').onPress();tree=await pane();
const offlineBefore=transportCalls;await flat(tree).find(x=>x.key==='pa-instruction').onSubmit('다음 실제 물가 작업');
assert.equal(transportCalls,offlineBefore);tree=await pane();assert.match(content(tree),/미전달\/재개 대기/);
state._console.observation.production.child.status='ALIVE';
state._console.observation.production.identityMatches=true;

await flat(tree).find(x=>x.key==='tab-instructions').onPress(); tree=await pane();
assert.match(content(tree),/권한 승인/);
let input=flat(tree).find(x=>x.key==='pa-instruction');assert.equal(input.name,'Input');
const beforeTransport=transportCalls;await input.onSubmit('실제 다음 벌목 작업을 이어가고 미확인을 구분해줘');
assert.equal(transportCalls,beforeTransport+1);tree=await pane();assert.match(content(tree),/전달됨\(수신 큐\).*실제 읽기/);
await flat(tree).find(x=>x.key==='pa-refresh').onPress();assert.equal(transportCalls,beforeTransport+1,'refresh must not dispatch');
state._console.observation.instructions=[{id:'confirmed',status:'RESULT',text:'next real job',receipt:{confirmed:true,source:'Logs/actual.jsonl',line:3},result:{text:'실제 결과와 남은 검증'}}];
await flat(tree).find(x=>x.key==='pa-refresh').onPress();tree=await pane();assert.match(content(tree),/실제 결과와 남은 검증/);
gateHold=true; input=flat(tree).find(x=>x.key==='pa-instruction');const beforeHold=transportCalls;await input.onSubmit('owner 변경 직전 지시');assert.equal(transportCalls,beforeHold,'target-change guard must prevent official send');tree=await pane();assert.match(content(tree),/전송 보류/);gateHold=false;

deliveryFailure=true;input=flat(tree).find(x=>x.key==='pa-instruction');await input.onSubmit('기록 실패 뒤 같은 지시 재전송 금지');const dispatched=transportCalls;
await input.onSubmit('기록 실패 뒤 같은 지시 재전송 금지');assert.equal(transportCalls,dispatched,'ambiguous post-send record must retain id and never resend');
let cancelled=false;clock.cancel=()=>{cancelled=true;};
const closeHook=find('ui.close');await closeHook($,{id:'pa-production-state',origin:{kind:'person'}},async e=>e);
const beforeClosedRead=calls.length;await clock();assert.equal(calls.length,beforeClosedRead,'closed pane polling must not read');
console.log('Explicit input: save/official queue/receipt/result separation, no polling dispatch, ambiguous-send id reuse and close preservation passed.');

// Closing during an in-progress refresh invalidates the earlier explicit open.
const raceHandlers=[];
const raceMod=await import('data:text/javascript;base64,'+Buffer.from(source).toString('base64')+'#close-race');
raceMod.register((name,matcher,fn)=>raceHandlers.push({name,matcher,fn:typeof matcher==='function'?matcher:fn}));
let unblockRace, raceOpenings=0;
const delayedRead=new Promise(resolve=>{unblockRace=resolve;});
const raceApi={...$,process:{run:async()=>{await delayedRead;return {exitCode:0,stdout:JSON.stringify(state)};}},ui:{...$.ui,open:async()=>{raceOpenings++;}}};
const inProgress=raceHandlers.find(h=>h.name==='command.run'&&h.matcher?.command==='pa').fn(raceApi);
await Promise.resolve();
await raceHandlers.find(h=>h.name==='ui.close').fn(raceApi,{id:'pa-production-state'},async e=>e);
unblockRace();await inProgress;
assert.equal(raceOpenings,0,'close during refresh must cancel the prior open');
console.log('Close during refresh: earlier open invalidated; no forced reopening.');

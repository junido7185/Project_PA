import { test, expect, mock, tier } from 'claude-code/testing'
tier('builtin')
test('pa status uses returned evidence without a model', async ($, on) => {
  mock.clock(on)
  on('session.start', ($, e) => ({ cwd: e.cwd }))
  on('command.register', ($, e) => ({ value: { command: e.name } }))
  on('process.run', () => ({ value: { exitCode: 0, stdout: JSON.stringify({_console:{observation:{production:{status:'WAITING',reason:'confirmed limit',sessionId:'prod',child:{status:'EXITED'},launcher:{status:'ALIVE'}},usage:{resumeAt:'2026-10-20T11:31:00Z'},bridge:{status:'LEGACY_ERROR_NO_DETAIL',message:'CodexBridgeError'}}}}), stderr: '' } }))
  await $.session.start({surface:'terminal',isInteractive:true,cwd:'C:/Users/sdjsd/Desktop/Unity/Project_PA'})
  const result = await $.command.run({command:'pa-status',args:'',origin:{kind:'composer'}})
  expect(result.text).toContain('WAITING')
  expect(result.text).toContain('CodexBridgeError')
  expect(result.text).toContain('EXITED')
})

const ROOT = 'C:/Users/sdjsd/Desktop/Unity/Project_PA'
for (const [label, interactive, cwd, viewer, producer, expected] of [
  ['observation', true, ROOT, 'viewer', 'producer', 0],
  ['headless', false, ROOT, 'viewer', 'producer', 0],
  ['production writer', true, ROOT, 'producer', 'producer', 0],
  ['other project', true, 'C:/other', 'viewer', 'producer', 0],
  ['unknown production identity', true, ROOT, 'viewer', null, 0],
] as const) {
  test(`startup preserves closed panes: ${label}`, async ($, on) => {
    mock.clock(on)
    const openings: any[] = []
    on('session.start', ($, e) => ({ cwd: e.cwd }))
    let ids = 0, reads = 0
    on('session.id', () => { ids++; return { value: viewer } })
    on('command.register', ($, e) => ({ value: { command: e.name } }))
    on('process.run', () => { reads++; return { value: { exitCode: 0, stdout: JSON.stringify({_console:{observation:{production:{sessionId:producer}}}}), stderr: '' } } })
    on('ui.open', ($, e) => { openings.push(e); return { value: undefined } })
    await $.session.start({ surface: 'terminal', isInteractive: interactive, cwd })
    expect(reads).toBe(1)
    expect(ids).toBe(1)
    if (expected) {
      const status = await $.command.run({command:'pa-status',args:'',origin:{kind:'composer'}})
      expect(status.text).toContain('자동 열기: 관찰 화면 열기 요청')
    }
    expect(openings.length).toBe(expected)
    if (expected) {
      expect(openings[0].focus).toBe(undefined)
      expect(openings[0].closeOnEscape).toBe(true)
      // A repeat lifecycle event must not reopen a user-dismissed pane.
      await $.session.start({ surface: 'terminal', isInteractive: interactive, cwd })
      expect(openings.length).toBe(1)
    }
  })
}

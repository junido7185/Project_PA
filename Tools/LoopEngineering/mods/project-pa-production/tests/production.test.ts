import { expect, mock, test } from 'claude-code/testing'

test('usage command returns actual engine values without invoking the model or changing owner', async ($, on) => {
  on('session.id', () => ({ value: 'usage-probe' }))
  on('session.model', () => ({ value: 'claude-opus-5-5' }))
  on('session.version', () => ({ value: { version: '2.1.296' } }))
  on('session.usage', () => ({ value: { context: { percent: 23 }, rateLimits: [
    { kind: 'five_hour', percentUsed: 48, resetsAt: '2026-10-13T02:00:00Z' },
  ] } }))
  const response = await $.command.run({ command: 'pa-usage', args: '' })
  const usage = JSON.parse(response.text)
  expect(usage.source).toBe('claude_mods')
  expect(usage.sessionId).toBe('usage-probe')
  expect(usage.rateLimits[0].percentUsed).toBe(48)
  expect(usage.context.percent).toBe(23)
  expect(Number.isFinite(Date.parse(usage.observedAt))).toBe(true)
})

test('actual engine usage fields are delivered without auth or model requests', async ($, on) => {
  mock.clock(on)
  const calls: string[][] = []
  on('session.cwd', () => ({ value: 'C:\\Users\\sdjsd\\Desktop\\Unity\\Project_PA' }))
  on('session.id', () => ({ value: 'lead-session' }))
  on('session.model', () => ({ value: 'claude-opus-5-5' }))
  on('session.version', () => ({ value: { version: '2.1.296' } }))
  on('session.usage', () => ({ value: { context: { percent: 37 }, rateLimits: [
    { kind: 'seven_day', percentUsed: 100, resetsAt: '2026-10-14T03:00:00Z' },
  ] } }))
  on('process.run', ($, e) => {
    calls.push(e.argv)
    return { value: { exitCode: 0, stdout: '{"status":"running"}', stderr: '' } }
  })
  on('command.register', () => ({ value: undefined }))
  on('session.start', () => ({ cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  await $.session.start({ surface: 'terminal', isInteractive: false, cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' })
  expect(calls.length).toBe(1)
  expect(calls[0]).toContain('observe')
  const observation = JSON.parse(calls[0][calls[0].indexOf('--observation') + 1])
  expect(observation.rateLimits[0].resetsAt).toBe('2026-10-14T03:00:00Z')
  expect(observation.model).toBe('claude-opus-5-5')
})

test('worker worktree never arms the original production timer', async ($, on) => {
  let processes = 0
  mock.clock(on)
  on('session.cwd', () => ({ value: 'C:/Users/sdjsd/Desktop/Unity/Project_PA/.worktrees/critic' }))
  on('process.run', () => { processes++; return { value: { exitCode: 0, stdout: '{}', stderr: '' } } })
  on('session.start', () => ({ cwd: '/worker' }))
  await $.session.start({ surface: 'terminal', isInteractive: false, cwd: '/worker' })
  expect(processes).toBe(0)
})

test('pause uses the existing locked helper and does not send Unity input', async ($, on) => {
  let command: string[] = []
  on('process.run', ($, e) => {
    command = e.argv
    return { value: { exitCode: 0, stdout: '{"enabled":false}', stderr: '' } }
  })
  const response = await $.command.run({ command: 'pa-pause', args: '' })
  expect(command).toContain('autonomy')
  expect(command).toContain('pause')
  expect(command).not.toContain('native')
  expect(response.text).toMatch(/resume disabled/)
})

test('status is model-free and prints real helper output', async ($, on) => {
  on('process.run', () => ({ value: { exitCode: 0, stdout: 'waiting_usage_reset', stderr: '' } }))
  const response = await $.command.run({ command: 'pa-status', args: '' })
  expect(response.text).toBe('waiting_usage_reset')
})

test('timer cancels goal and delivers a safe pause once', async ($, on) => {
  const clock = mock.clock(on)
  let pauses = 0
  on('session.cwd', () => ({ value: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  on('session.id', () => ({ value: 'lead' }))
  on('session.model', () => ({ value: 'claude-opus-5-5' }))
  on('session.version', () => ({ value: { version: '2.1.296' } }))
  on('session.usage', () => ({ value: { context: {}, rateLimits: [] } }))
  on('process.run', () => ({ value: { exitCode: 0, stdout: '{"stopRequested":true}', stderr: '' } }))
  on('command.register', () => ({ value: undefined }))
  on('ui.status', () => ({ value: undefined }))
  on('command.run', ($, e) => ({ text: e.command === 'goal' ? 'cleared' : '' }))
  on('prompt.submit', ($, e) => { pauses++; return { text: e.text } })
  on('session.start', () => ({ cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  await $.session.start({ surface: 'terminal', isInteractive: false, cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' })
  await clock.advance(30000)
  await clock.advance(30000)
  expect(pauses).toBe(1)
})

test('relay checkpoint is delivered once and preserves sole Unity ownership', async ($, on) => {
  const clock = mock.clock(on)
  const prompts: string[] = []; const commands: string[] = []
  on('session.cwd', () => ({ value: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  on('session.id', () => ({ value: 'lead' }))
  on('session.model', () => ({ value: 'claude-opus-5-5' }))
  on('session.version', () => ({ value: { version: '2.1.296' } }))
  on('session.usage', () => ({ value: { context: {percent: 89}, rateLimits: [] } }))
  on('process.run', () => ({ value: { exitCode: 0, stdout: JSON.stringify({
    status: 'running', relayCheckpointRequest: {reason: 'context_pressure', requestedAt: 'one'},
  }), stderr: '' } }))
  on('command.register', () => ({ value: undefined }))
  on('ui.status', () => ({ value: undefined }))
  on('command.run', ($, e) => { commands.push(e.command); return { text: 'cleared' } })
  on('prompt.submit', ($, e) => { prompts.push(e.text); return { text: e.text } })
  on('session.start', () => ({ cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  await $.session.start({ surface: 'terminal', isInteractive: false, cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' })
  await clock.advance(30000); await clock.advance(30000)
  expect(prompts.length).toBe(1)
  expect(prompts[0]).toMatch(/Original Unity ownership remains with Claude/)
  expect(prompts[0]).toMatch(/Snapshots and quota observations are not product PASS/)
  expect(commands).toContain('goal')
})

test('optional observer failure leaves production running without delivering a false handoff', async ($, on) => {
  const clock = mock.clock(on); let prompts = 0
  on('session.cwd', () => ({ value: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  on('session.id', () => ({ value: 'lead' }))
  on('session.model', () => ({ value: 'claude-opus-5-5' }))
  on('session.version', () => ({ value: { version: '2.1.296' } }))
  on('session.usage', () => ({ value: { context: {}, rateLimits: [] } }))
  on('process.run', () => ({ value: { exitCode: 1, stdout: '', stderr: 'observer failed' } }))
  on('command.register', () => ({ value: undefined }))
  on('ui.status', () => ({ value: undefined }))
  on('prompt.submit', () => { prompts++; return { text: '' } })
  on('session.start', () => ({ cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' }))
  await $.session.start({ surface: 'terminal', isInteractive: false, cwd: 'C:/Users/sdjsd/Desktop/Unity/Project_PA' })
  await clock.advance(30000)
  expect(prompts).toBe(0)
})

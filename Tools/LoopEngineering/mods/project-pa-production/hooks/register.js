const ROOT = 'C:/Users/sdjsd/Desktop/Unity/Project_PA'
const HELPER = ROOT + '/Tools/LoopEngineering/Invoke-ProjectPAProduction.py'
const PYTHON = 'C:/msys64/mingw64/bin/python.exe'
let pauseDelivered = false
let relayDelivered = null

async function control($, operation, extra = []) {
  return $.process.run([PYTHON, '-B', '-X', 'utf8', HELPER, 'autonomy', operation, ...extra], { cwd: ROOT, timeoutMs: 20000 })
}

async function observe($) {
  const usage = await $.session.usage()
  const id = await $.session.id()
  const model = await $.session.model()
  const version = await $.session.version()
  const response = await control($, 'observe', ['--session-id', id, '--observation', JSON.stringify({
    rateLimits: usage.rateLimits || [], context: usage.context, model, version,
  })])
  if (response.exitCode !== 0) throw new Error('Usage observation failed; existing logs/state preserved: ' + response.stderr)
  return JSON.parse(response.stdout)
}

async function optionalObserve($) {
  try { return await observe($) }
  catch { $.ui.status('Project P.A. · observer unavailable; source and logs preserved'); return null }
}

export function register(on) {
  on('session.start', async ($, e, next) => {
    const cwd = (await $.session.cwd()).replaceAll('\\', '/').toLowerCase()
    if (cwd !== ROOT.toLowerCase()) return next(e)
    await optionalObserve($)
    await $.command.register({ name: 'pa-lead-status', description: 'Show the existing production lease, workers and verified usage reset' })
    await $.command.register({ name: 'pa-usage', description: 'Read current engine usage without a model request; never infer quota from a passed reset' })
    await $.command.register({ name: 'pa-pause', description: 'Preserve current work and stop automatic continuation at a safe point' })
    $.clock.every(30000, async () => {
      const state = await optionalObserve($)
      if (!state) return
      $.ui.status('Project P.A. · ' + (state.status || 'observation') + (state.nextResumeAt ? ' · reset ' + state.nextResumeAt : ''))
      if (state.stopRequested && !pauseDelivered) {
        pauseDelivered = true
        // Run outside a turn hook. Never await an idle-dependent prompt inside turn.complete.
        await $.command.run({ command: 'goal', args: 'clear' })
        await $.prompt.submit({ text: 'Project P.A. automation pause was requested. Preserve current work at a safe point, '
          + 'finish only the current atomic operation, leave concise handoff and an autonomy blocked checkpoint. '
          + 'Do not start workers or new production steps. Stop without commit/push or touching personal saves.' })
      } else if (!state.stopRequested && state.relayCheckpointRequest &&
          relayDelivered !== state.relayCheckpointRequest.requestedAt) {
        relayDelivered = state.relayCheckpointRequest.requestedAt
        await $.command.run({ command: 'goal', args: 'clear' })
        await $.prompt.submit({ text: 'Project P.A. relay requests a safe checkpoint ('
          + state.relayCheckpointRequest.reason + '). Finish only the current atomic game operation. '
          + 'Do not start additional workers. Preserve owned worktrees, source changes, validation results and remaining defects. '
          + 'Leave the concise current handoff and call the existing helper autonomy checkpoint with continue disposition '
          + '(or blocked/human-review if appropriate), exact next action and actual evidence. '
          + 'Snapshots and quota observations are not product PASS. Keep the current recorded state/lease writer; availability alone never transfers ownership. '
          + 'If context is high, the supervisor will use a fresh continuation session from this handoff. Checkpoint safely; the existing supervisor continues the same approved goal. This is not a task-duration limit.' })
      }
    })
    return next(e)
  })
  on('session.measure', async ($, e, next) => {
    if (!e.agentId) await optionalObserve($)
    return next(e)
  })
  on('turn.complete', async ($, e, next) => {
    if (!e.agentId) await optionalObserve($)
    return next(e)
  })
  on('command.run', { command: 'pa-lead-status' }, async ($) => {
    const response = await control($, 'status')
    return { text: response.stdout || response.stderr }
  })
  on('command.run', { command: 'pa-usage' }, async ($) => {
    const usage = await $.session.usage()
    return { text: JSON.stringify({
      source: 'claude_mods', sessionId: await $.session.id(), observedAt: new Date().toISOString(),
      rateLimits: usage.rateLimits || [], context: usage.context,
      model: await $.session.model(), version: await $.session.version(),
    }) }
  })
  on('command.run', { command: 'pa-pause' }, async ($) => {
    const response = await control($, 'pause')
    return { text: response.exitCode === 0 ? 'Safe pause requested; automatic quota resume disabled. Current work is preserved.' : response.stderr }
  })
}

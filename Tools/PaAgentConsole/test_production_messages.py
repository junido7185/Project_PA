import contextlib
import copy
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import production_messages as messages

OWNER = 'd1ff77d7-03aa-4167-8dfe-576673db1b47'
OBSERVER = 'd9c1ccb8-cf6e-422e-b670-8beca13cb3b8'
WHEN = '2026-10-22T12:00:00+09:00'
IDENT = '1792638000000-11111111-2222-4333-8444-555555555555'


class FixtureHelper:
    """Use the same lock/write interface, confined to a temporary project."""
    def __init__(self):
        self.locks = 0
        self.fail_state_once = False

    @contextlib.contextmanager
    def state_lock(self):
        self.locks += 1
        yield

    def read(self, path):
        return json.loads(path.read_text('utf-8'))

    def write(self, path, value):
        if self.fail_state_once and path.name == 'loop-state.json':
            self.fail_state_once = False
            raise OSError('fixture state replacement failed')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(value, ensure_ascii=False), encoding='utf-8')


class ProductionMessageTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name).resolve()
        self.helper = FixtureHelper()
        self.helper_patch = patch.object(messages, 'production_helper', return_value=self.helper)
        self.helper_patch.start()
        self.clock_patch = patch.object(messages, 'now', return_value=WHEN)
        self.clock_patch.start()
        self.run = self.root / 'Logs/ParallelProduction/selected'
        self.run.mkdir(parents=True)
        self.events = self.run / 'claude.jsonl'
        self.append({'type': 'system', 'subtype': 'init', 'session_id': OWNER})
        self.state = {'demoProductionRouting': {'unityEditorOwner': 'claude-code', 'selectedApproval': 'approved'},
                      'approved': {'lifetimeUsage': None, 'priorRefusals': ['preserve']},
                      'productionEnvironment': {
                          'coordinatorLease': {'owner': 'claude-code', 'sessionId': OWNER, 'status': 'stopped',
                                               'startedAt': '2026-10-22T11:00:00+09:00'},
                          'coordinator': {'runDirectory': str(self.run)},
                          'autonomous': {'status': 'paused_checkpoint_or_blocker', 'stopRequested': False},
                          'relaySupport': {'pendingCandidates': ['existing'], 'userInstructions': []}}}
        self.helper.write(self.root / messages.STATE, self.state)
        self.payload = {'id': IDENT, 'text': '물가 결과를 검토해주세요.\n기존 중지는 보존합니다.',
                        'sourceSessionId': OBSERVER, 'sourceVersion': '2.1.303'}

    def tearDown(self):
        self.clock_patch.stop()
        self.helper_patch.stop()
        self.tmp.cleanup()

    def append(self, event):
        with self.events.open('a', encoding='utf-8') as stream:
            stream.write(json.dumps(event, ensure_ascii=False) + '\n')

    def state_now(self):
        return self.helper.read(self.root / messages.STATE)

    def save(self):
        return messages.submit_instruction(self.root, self.payload)

    def view(self):
        return messages.instruction_observations(self.root, self.state_now())[0]

    def assistant(self, body, **extra):
        return dict({'type': 'assistant', 'session_id': OWNER,
                     'timestamp': '2026-10-22T12:01:00+09:00',
                     'message': {'content': [{'type': 'text', 'text': body}]}}, **extra)

    def ack(self, kind='received', reply=None):
        value = {'protocol': 'pa-instruction-ack-v1', 'id': IDENT,
                 'textSha256': messages.sha(self.payload['text']), 'kind': kind}
        if reply is not None:
            value['reply'] = reply
        return json.dumps(value)

    def test_save_preserves_authority_and_duplicate_id_does_not_append(self):
        saved = self.save()
        duplicate = self.save()
        self.assertTrue(saved['saved'])
        self.assertTrue(saved['canDeliver'])
        self.assertTrue(duplicate['reused'])
        self.assertEqual(saved['targetSessionId'], OWNER)
        self.assertEqual(saved['text'], self.payload['text'])
        current = self.state_now()
        self.assertEqual(len(current['productionEnvironment']['relaySupport']['userInstructions']), 1)
        restored = copy.deepcopy(current)
        restored['productionEnvironment']['relaySupport']['userInstructions'] = []
        self.assertEqual(restored, self.state)
        self.assertGreaterEqual(self.helper.locks, 2)
        self.assertIn('not new approval', saved['envelope'])
        with self.assertRaisesRegex(ValueError, 'different text'):
            messages.submit_instruction(self.root, dict(self.payload, text='different'))

    def test_traversal_bad_text_owner_and_same_sid_are_rejected(self):
        for ident in ('../escape', '123-evil;cmd', IDENT + '/a'):
            with self.assertRaises(ValueError):
                messages.submit_instruction(self.root, dict(self.payload, id=ident))
        for text in ('   ', 'x' * 16001, '\x00'):
            with self.assertRaises(ValueError):
                messages.submit_instruction(self.root, dict(self.payload, text=text))
        with self.assertRaisesRegex(ValueError, 'own production'):
            messages.submit_instruction(self.root, dict(self.payload, sourceSessionId=OWNER))
        state = self.state_now()
        state['productionEnvironment']['coordinatorLease']['owner'] = 'codex'
        self.helper.write(self.root / messages.STATE, state)
        with self.assertRaisesRegex(ValueError, 'not claude-code'):
            self.save()
        self.assertFalse((self.root / messages.MESSAGE_DIR).exists())

    def test_queued_is_not_read_or_result_and_unknown_is_never_retried(self):
        self.save()
        result = messages.record_delivery(self.root, {'id': IDENT, 'result': {'isDelivered': True}})
        self.assertEqual(result['status'], 'DELIVERED')
        self.assertTrue(result['queueOnly'])
        self.assertEqual(self.view()['status'], 'DELIVERED')
        self.assertIsNone(self.view()['receipt'])
        self.assertFalse(self.save()['canDeliver'])
        later = messages.record_delivery(self.root, {'id': IDENT, 'result': {'unknown': True, 'error': 'late observation failure'}})
        self.assertEqual(later['delivery']['status'], 'QUEUED')
        self.assertEqual(self.view()['status'], 'DELIVERED')

    def test_registered_startup_assignment_is_separate_from_receipt(self):
        saved = self.save()
        state = self.state_now()
        state['productionEnvironment']['coordinatorLease']['startedAt'] = '2026-10-22T12:01:00+09:00'
        self.helper.write(self.root / messages.STATE, state)
        assignment = self.run / 'assignment.md'
        assignment.write_text('[PA_STARTUP_INSTRUCTION:' + IDENT + ']\n' + saved['envelope'], encoding='utf-8')
        view = self.view()
        self.assertEqual(view['status'], 'STARTUP_ASSIGNED')
        self.assertTrue(view['startupAssignment']['notReceiptProof'])
        self.assertIsNone(view['receipt'])
        assignment.write_text('[PA_STARTUP_INSTRUCTION:' + IDENT + ']\nprovenance only', encoding='utf-8')
        self.assertIsNone(self.view()['startupAssignment'])

    def test_unknown_transport_and_definite_not_delivered_remain_separate(self):
        self.save()
        messages.record_delivery(self.root, {'id': IDENT, 'result': {'unknown': True, 'error': 'pipe closed after write'}})
        view = self.view()
        self.assertEqual(view['status'], 'SAVED')
        self.assertEqual(view['delivery']['status'], 'UNKNOWN')
        self.assertFalse(self.save()['canDeliver'])
        other = dict(self.payload, id=IDENT + '-b')
        messages.submit_instruction(self.root, other)
        messages.record_delivery(self.root, {'id': other['id'], 'result': {'isDelivered': False, 'reason': 'session gone'}})
        found = next(v for v in messages.instruction_observations(self.root, self.state_now()) if v['id'] == other['id'])
        self.assertEqual(found['delivery']['status'], 'NOT_DELIVERED')
        self.assertEqual(found['delivery']['reason'], 'session gone')

    def test_saved_send_proof_survives_following_state_write_failure(self):
        self.save()
        self.helper.fail_state_once = True
        with self.assertRaises(OSError):
            messages.record_delivery(self.root, {'id': IDENT, 'result': {'isDelivered': True}})
        self.assertEqual(self.state_now()['productionEnvironment']['relaySupport']['userInstructions'][0]['delivery']['status'], 'NOT_ATTEMPTED')
        self.assertEqual(self.save()['delivery']['status'], 'QUEUED')
        self.assertFalse(self.save()['canDeliver'])
        self.assertEqual(self.view()['status'], 'DELIVERED')

    def test_pre_send_unknown_is_reconciled_without_ever_permitting_a_second_send(self):
        self.save()
        messages.record_delivery(self.root, {'id': IDENT, 'result': {'unknown': True, 'error': 'send has not returned'}})
        self.assertFalse(self.save()['canDeliver'])
        self.helper.fail_state_once = True
        with self.assertRaises(OSError):
            messages.record_delivery(self.root, {'id': IDENT, 'result': {'isDelivered': True}})
        self.assertEqual(self.save()['delivery']['status'], 'QUEUED')
        self.assertFalse(self.save()['canDeliver'])
        proof = json.loads((self.root / messages.MESSAGE_DIR / (IDENT + '.json')).read_text('utf-8'))
        self.assertEqual(proof['deliveryHistory'][0]['status'], 'UNKNOWN')
        self.assertEqual(proof['delivery']['status'], 'QUEUED')

    def test_successful_read_is_receipt_only_and_observation_does_not_write(self):
        saved = self.save()
        self.append({'type': 'assistant', 'message': {'content': [{'type': 'tool_use', 'id': 'read-1', 'name': 'Read',
                     'input': {'file_path': str(self.root / saved['path'])}}]}})
        self.append({'type': 'user', 'message': {'content': [{'type': 'tool_result', 'tool_use_id': 'read-1', 'content': (self.root / saved['path']).read_text('utf-8')}]}})
        before = {str(p): p.read_bytes() for p in (self.root / messages.STATE, self.root / saved['path'])}
        view = self.view()
        self.assertEqual(view['status'], 'BODY_READ')
        self.assertIsNone(view['receipt'])
        self.assertEqual(view['bodyRead']['kind'], 'full_body_returned')
        self.assertEqual(view['bodyRead']['timeBasis'], 'append_boundary')
        self.assertIsNone(view['result'])
        self.assertEqual(before, {str(p): p.read_bytes() for p in (self.root / messages.STATE, self.root / saved['path'])})

    def test_prompt_tool_result_failed_read_old_and_foreign_markers_are_not_receipts(self):
        saved = self.save()
        received = '[PA_RECEIVED:' + IDENT + ']'
        self.append({'type': 'user', 'message': {'content': [{'type': 'text', 'text': received}]}})
        self.append(self.assistant(received, timestamp='2026-10-22T11:59:00+09:00'))
        self.append(self.assistant(received, session_id=OBSERVER))
        self.append({'type': 'assistant', 'message': {'content': [{'type': 'tool_use', 'id': 'read', 'name': 'Read', 'input': {'file_path': saved['path']}}]}})
        self.append({'type': 'user', 'message': {'content': [{'type': 'tool_result', 'tool_use_id': 'read', 'is_error': True, 'content': received}]}})
        self.assertEqual(self.view()['status'], 'SAVED')
        self.assertIsNone(self.view()['receipt'])

    def test_marker_requires_owner_answer_and_result_is_not_game_acceptance(self):
        self.save()
        self.append(self.assistant(self.ack()))
        self.assertEqual(self.view()['status'], 'RECEIVED')
        self.append(self.assistant(self.ack('result')))
        self.assertIsNone(self.view()['result'])
        self.append(self.assistant(self.ack('result', 'native 보류: 현재 권한 검토가 필요합니다.')))
        view = self.view()
        self.assertEqual(view['status'], 'RESULT')
        self.assertTrue(view['result']['notGameAcceptance'])
        self.assertIn('보류', view['result']['text'])

    def test_tampered_text_and_other_selected_run_do_not_receive(self):
        saved = self.save()
        self.events.write_text(json.dumps({'type': 'system', 'subtype': 'init', 'session_id': OBSERVER}) + '\n')
        self.append(self.assistant('[PA_RECEIVED:' + IDENT + ']'))
        self.assertIsNone(self.view()['receipt'])
        path = self.root / saved['path']
        data = json.loads(path.read_text('utf-8'))
        data['text'] = 'tampered'
        self.helper.write(path, data)
        self.assertIn('hash mismatch', self.view()['evidenceError'])
        with self.assertRaisesRegex(ValueError, 'hash mismatch'):
            self.save()

    def test_normal_later_run_can_prove_receipt_without_stream_timestamps(self):
        self.save()
        later = self.root / 'Logs/ParallelProduction/later'
        later.mkdir()
        state = self.state_now()
        state['productionEnvironment']['coordinator']['runDirectory'] = str(later)
        state['productionEnvironment']['coordinatorLease']['startedAt'] = '2026-10-22T13:00:00+09:00'
        self.helper.write(self.root / messages.STATE, state)
        (later / 'claude.jsonl').write_text('\n'.join(json.dumps(e) for e in [
            {'type': 'system', 'subtype': 'init', 'session_id': OWNER},
            {'type': 'assistant', 'message': {'content': [{'type': 'text', 'text': self.ack()}]}}
        ]) + '\n')
        self.assertEqual(self.view()['receipt']['timeBasis'], 'run_started_after_submission')

    def test_existing_id_and_send_entry_hold_changed_owner_or_sid(self):
        saved = self.save()
        original = (self.root / saved['path']).read_bytes()
        for owner, sid in [('codex', OWNER), ('claude-code', OBSERVER)]:
            state = self.state_now()
            state['demoProductionRouting']['unityEditorOwner'] = owner
            state['productionEnvironment']['coordinatorLease'].update(owner=owner, sessionId=sid)
            self.helper.write(self.root / messages.STATE, state)
            reused = self.save()
            self.assertFalse(reused['canDeliver'])
            self.assertEqual(reused['targetSessionId'], OWNER)
            self.assertEqual(reused['targetHold']['reason'], 'TARGET_CHANGED')
            gate = messages.record_delivery(self.root, {'id': IDENT, 'beginSend': True,
                'result': {'unknown': True, 'error': 'enter transport'}})
            self.assertFalse(gate['canSend'])
            self.assertEqual(original, (self.root / saved['path']).read_bytes())

    def test_send_entry_checks_current_target_once_and_unknown_never_reenters(self):
        self.save()
        payload = {'id': IDENT, 'beginSend': True, 'result': {'unknown': True}}
        self.assertTrue(messages.record_delivery(self.root, payload)['canSend'])
        self.assertFalse(messages.record_delivery(self.root, payload)['canSend'])

    def test_fenced_quoted_examples_and_partial_read_do_not_claim_receipt(self):
        saved = self.save()
        for body in ('```\n[PA_RECEIVED:' + IDENT + ']\n[PA_RESULT:' + IDENT + ']\n```\n실제 처리 결과가 아니다',
                     '```json\n' + self.ack('result', 'example') + '\n```',
                     '> ' + self.ack(), 'Example: ' + self.ack()):
            self.append(self.assistant(body))
        self.append({'type': 'assistant', 'message': {'content': [{'type': 'tool_use', 'id': 'one-line', 'name': 'Read',
            'input': {'file_path': saved['path'], 'offset': 1, 'limit': 1}}]}})
        self.append({'type': 'user', 'message': {'content': [{'type': 'tool_result', 'tool_use_id': 'one-line', 'content': '1→{'}]}})
        view = self.view()
        self.assertEqual(view['status'], 'FILE_LOOKUP')
        self.assertIsNone(view['bodyRead'])
        self.assertIsNone(view['receipt'])
        self.assertIsNone(view['result'])

    def test_mod_state_exposes_only_latest_twenty_and_cli_modes_return_json(self):
        for number in range(22):
            messages.submit_instruction(self.root, dict(self.payload, id=IDENT + '-' + str(number)))
        self.assertEqual(len(messages.instruction_observations(self.root, self.state_now())), 20)
        import pa_console
        handoff = self.root / pa_console.HANDOFF
        handoff.parent.mkdir(parents=True)
        handoff.write_text('current handoff')
        with patch.object(pa_console, 'budget_policy', return_value={}), patch.object(pa_console, 'observation', return_value={}):
            view = pa_console.mod_state(self.root)
        self.assertEqual(len(view['_console']['observation']['instructions']), 20)
        output = io.StringIO()
        with patch('sys.argv', ['pa_console.py', '--root', str(self.root), '--instruction-submit', json.dumps(self.payload)]), contextlib.redirect_stdout(output):
            pa_console.main()
        self.assertTrue(json.loads(output.getvalue())['saved'])


if __name__ == '__main__':
    unittest.main()

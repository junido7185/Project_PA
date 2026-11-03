"""Check that public editor commands queue their requested action without touching Unity."""
import contextlib
import importlib.util
import io
from pathlib import Path
import sys
import unittest
from unittest import mock

spec = importlib.util.spec_from_file_location(
    'pa_editor_entry', Path(__file__).resolve().parents[1] / 'Invoke-ProjectPAProduction.py')
production = importlib.util.module_from_spec(spec)
spec.loader.exec_module(production)


class EditorEntryTests(unittest.TestCase):
    def test_each_editor_action_queues_an_editor_request(self):
        for action in ('connect', 'build', 'performance', 'capture'):
            with self.subTest(action=action), mock.patch.object(sys, 'argv',
                    ['production', 'editor', '--action', action]), \
                    mock.patch.object(production, 'git', return_value=str(production.ROOT)), \
                    mock.patch.object(production, 'enforce_production_entry') as guard, \
                    mock.patch.object(production, 'write') as queued, \
                    contextlib.redirect_stdout(io.StringIO()) as output:
                self.assertEqual(production.main(), 0)
                guard.assert_called_once()
                queued.assert_called_once()
                path, request = queued.call_args.args
                self.assertEqual(path, production.ROOT / 'Logs/ParallelProduction/editor-request.json')
                self.assertEqual(request['action'], action)
                self.assertTrue(request['id'])
                self.assertIn('Editor command queued:', output.getvalue())

    def test_unpermitted_editor_never_reaches_queue(self):
        with mock.patch.dict(production.os.environ,{},clear=True),mock.patch.object(sys,'argv',
                ['production','editor','--action','build']),mock.patch.object(production,'git',return_value=str(production.ROOT)),mock.patch.object(production,'write') as queued:
            with self.assertRaisesRegex(ValueError,'PRODUCTION_GUARD'):
                production.main()
            queued.assert_not_called()


if __name__ == '__main__':
    unittest.main()

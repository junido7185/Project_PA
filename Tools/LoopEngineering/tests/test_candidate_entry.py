"""Isolated candidate boundaries and unknown-quota read-only attempts; no Unity/process launch."""
import importlib.util
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest import mock

spec = importlib.util.spec_from_file_location('candidate_entry', Path(__file__).resolve().parents[1] / 'Invoke-ProjectPAProduction.py')
p = importlib.util.module_from_spec(spec)
spec.loader.exec_module(p)


class CandidateBoundaryTests(unittest.TestCase):
    def test_dry_run_never_launches_or_writes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory); runs = root / 'Logs/ParallelProduction'
            build_id = '2ad83fac-93f6-4120-a138-47bedf62638a'
            exe = runs / 'Candidates' / build_id / 'ProjectPA.exe'; exe.parent.mkdir(parents=True); exe.write_bytes(b'fixture')
            save = runs / 'Standalone/save'; save.mkdir(parents=True); output = runs / 'Standalone/results'
            report = runs / ('editor-result-' + build_id + '.json')
            report.write_text(json.dumps(dict(action='build', success=True, path=str(exe))))
            args = SimpleNamespace(id=build_id,save_root=str(save),output=str(output),label='first',dry_run=True)
            with mock.patch.object(p,'ROOT',root), mock.patch.object(p,'RUNS',runs), mock.patch.object(p.subprocess,'Popen') as launch:
                p.candidate_launch(args); launch.assert_not_called(); self.assertFalse(output.exists())
                args.save_root=str(root / 'personal-save'); Path(args.save_root).mkdir()
                with self.assertRaises(RuntimeError): p.candidate_launch(args)
                args.save_root=str(save); output.mkdir(); (output/'frames-first.csv').write_text('preserved')
                with self.assertRaises(RuntimeError): p.candidate_launch(args)
                launch.assert_not_called()


class UsageAttemptTests(unittest.TestCase):
    def state(self):
        return dict(productionEnvironment=dict(autonomous=dict(enabled=True,stopRequested=False),workers={},
            relaySupport=dict(autoHandoff=dict(enabled=True),teams={'claude-code':dict(capability='verified')},observations={})))

    def args(self,role='critic',files=None):
        return SimpleNamespace(provider='auto',role=role,files=files or [])

    def test_unknown_quota_permits_one_read_only_attempt_not_a_builder(self):
        state=self.state(); self.assertEqual(p.worker_provider(self.args(),state),('claude-code',True))
        for args in [self.args('builder'),self.args(files=['Assets/Scripts/a.cs'])]:
            with self.assertRaises(RuntimeError):p.worker_provider(args,state)
        state['productionEnvironment']['workers']['probe']=dict(availabilityProbe=True,status='running')
        with self.assertRaises(RuntimeError):p.worker_provider(self.args(),state)

    def test_pause_final_review_and_fresh_exhaustion_still_block(self):
        for change in [dict(stopRequested=True),dict(checkpoint=dict(disposition='human-review'))]:
            state=self.state();state['productionEnvironment']['autonomous'].update(change)
            with self.assertRaises(RuntimeError):p.worker_provider(self.args(),state)
        state=self.state();state['productionEnvironment']['relaySupport']['observations']['claude-code']=dict(
            source='claude_mods',observedAt=p.now(),rateLimits=[dict(percentUsed=100)])
        with self.assertRaises(RuntimeError):p.worker_provider(self.args(),state)


class ResearchStagingTests(unittest.TestCase):
    def test_new_markdown_is_available_with_relative_links_and_no_binary_copy(self):
        with tempfile.TemporaryDirectory() as directory:
            root=Path(directory); research=root/'Docs/01_GAME_DESIGN/ReferenceResearch'
            research.mkdir(parents=True); (research/'README.md').write_text('[motion](motion/notes.md)')
            (research/'motion').mkdir(); (research/'motion/notes.md').write_text('Research hypothesis, not a requirement')
            (research/'movie.mp4').write_bytes(b'large fixture media excluded')
            with mock.patch.object(p,'ROOT',root):
                wt=root/'worker'; files=p.stage_research_reference(wt,'motion')
                index=wt/'Evidence/Docs/01_GAME_DESIGN/ReferenceResearch/README.md'
                self.assertEqual(index.read_text(),'[motion](motion/notes.md)')
                self.assertTrue((index.parent/'motion/notes.md').is_file())
                self.assertFalse((index.parent/'movie.mp4').exists())
                (research/'new-npc.md').write_text('Added later')
                newer=p.stage_research_reference(root/'next-worker','world')
                self.assertIn('Evidence/Docs/01_GAME_DESIGN/ReferenceResearch/new-npc.md',newer)
                self.assertEqual(p.stage_research_reference(root/'tooling-worker','none'),[])


if __name__=='__main__':unittest.main()

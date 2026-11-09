Bandai Namco Research Motion Dataset (BVH, 30 fps)
Source: https://github.com/BandaiNamcoResearchInc/Bandai-Namco-Research-Motiondataset
Copyright (c) 2022 Bandai Namco Research Inc.
License: CC BY-NC 4.0 (LICENSE-dataset-1.txt, LICENSE-dataset-2.txt) - attribution required, NON-COMMERCIAL use only.

Citation:
  Makito Kobayashi, Chen-Chieh Liao, Keito Inoue, Sentaro Yojima, Masafumi Takahashi.
  "Motion Capture Dataset for Practical Use of AI-based Motion Editing and Stylization", arXiv:2306.08861, 2023.

Use in Project P.A. (2026-11-01):
  Motion/*.bvh are unmodified copies of selected locomotion takes (walk/run/dash, childish/happy/youthful/active styles)
  plus dataset-1_bow_childish_001 (standing calibration pose only).
  Assets/Editor/PA_MocapLocomotion.cs retargets them to the C-01 humanoid and bakes looping clips into
  Assets/Art/Animation/PlayerLocomotionMocap/ (derived work under the same license terms).
  Before any commercial release these clips must be replaced with commercially licensed or original motion.

Update 2026-11-02:
  Product clips now use the actor's "normal" style: dataset-2_walk_normal_001 (walk), dataset-2_run_normal_001 (run),
  dataset-2_run_normal_002 (sprint), dataset-1_bow_normal_001 (idle standing frames). Also added, unmodified:
  dataset-1_walk/run/dash_normal_* (unused, kept for review). Same CC BY-NC 4.0 terms and attribution apply.

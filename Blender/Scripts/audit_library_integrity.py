"""Read-only check of original BLEND animation sets against library rig references."""
import json
from pathlib import Path
import bpy

ROOT = Path(__file__).resolve().parents[2]
manifest = json.loads((ROOT / "Blender/Library/library-manifest.json").read_text(encoding="utf-8"))
rows = []
for asset in manifest["assets"]:
    source = ROOT / asset["librarySource"]
    if source.suffix != ".blend":
        continue
    with bpy.data.libraries.load(str(source), link=False) as (available, unused):
        expected = list(available.actions)
    collection = bpy.data.collections[asset["id"]]
    referenced = set()
    for obj in collection.all_objects:
        animation = obj.animation_data
        if not animation:
            continue
        if animation.action:
            referenced.add(animation.action.name)
        for track in animation.nla_tracks:
            for strip in track.strips:
                if strip.action:
                    referenced.add(strip.action.name)
    base_names = {name.rsplit(".", 1)[0] if name.rsplit(".",1)[-1].isdigit() else name for name in referenced}
    rows.append({"id":asset["id"],"sourceActions":sorted(expected),"libraryReferencedActions":sorted(referenced),
                 "allSourceActionsReferenced":set(expected).issubset(base_names)})
report = {"status":"PASS" if all(r["allSourceActionsReferenced"] for r in rows) else "INCOMPLETE_ANIMATION_SET",
          "assets":rows,"allLibraryActions":sorted(a.name for a in bpy.data.actions)}
(ROOT / "Docs/AssetProvenance/library-animation-audit.json").write_text(json.dumps(report,indent=2)+"\n",encoding="utf-8")
print(json.dumps(report))

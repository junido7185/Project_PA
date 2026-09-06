"""Verify ART-000 originals, outputs and metadata; never changes source assets."""
import ast
import collections
import io
import json
from pathlib import Path
import re
import stat
import struct
import subprocess
import zipfile
import asset_intake as intake

ROOT, DOCS = intake.ROOT, intake.DOCS
checks = []

def require(condition, message):
    if not condition:
        raise AssertionError(message)
    checks.append(message)


inventory = json.loads((DOCS / "asset-inventory.json").read_text(encoding="utf-8"))
selection = json.loads((DOCS / "selected-assets.json").read_text(encoding="utf-8"))["assets"]
require(len(inventory["packs"]) == 6, "6 requested packs present")
require(len(inventory["models"]) == 550, "550 logical models inventoried")
require(len({a["id"] for a in selection}) == len(selection) == 33, "33 unique selected asset IDs")
for pack in inventory["packs"]:
    require(intake.sha(pack["masterPath"]) == pack["archiveSHA256"], "Downloads master unchanged: " + pack["packName"])
    root = ROOT / pack["extractedPath"]
    require(intake.sha(root.parent/pack["downloadedArchiveName"]) == pack["archiveSHA256"], "Staged archive matches: " + pack["packName"])
    require(root.is_dir(), "Extracted staging exists: " + pack["packName"])
    for license in pack["licenseFiles"]:
        require((ROOT/license).is_file(), "License preserved: " + license)
    require(pack["licenseFiles"] or (pack["packName"] == "CubeWorldKit" and "no license file" in pack["licenseEvidence"]), "License evidence is explicit: " + pack["packName"])
    for duplicate in pack["duplicateDownloads"]:
        require(intake.sha(Path(pack["masterPath"]).parent/duplicate["name"]) == duplicate["sha256"], "Duplicate master unchanged: " + duplicate["name"])
for file in inventory["files"]:
    require(intake.sha(ROOT/file["path"]) == file["sha256"], "Source content hash: " + file["path"])
for asset in selection:
    require(intake.sha(ROOT/asset["blenderSource"]) == intake.sha(ROOT/asset["librarySource"]) == asset["blenderSourceSha256"], "Library copy source hash: " + asset["id"])
    if asset["importToUnity"]:
        require(intake.sha(ROOT/asset["unityPath"]) == asset["sourceSha256"], "Unity FBX source hash: " + asset["id"])
        require((ROOT/asset["wrapperPath"]).is_file(), "Wrapper exists: " + asset["id"])
    else:
        require(not (ROOT/asset["unityPath"]).exists(), "Reference-only model not copied to Unity: " + asset["id"])

reports = {}
for filename in ["unity-validation.json", "blender-validation.json", "library-reopen-validation.json", "library-animation-audit.json"]:
    reports[filename] = json.loads((DOCS/filename).read_text(encoding="utf-8-sig"))
    require(reports[filename]["status"] == "PASS", "Evidence PASS: " + filename)
require(len(reports["unity-validation.json"]["assets"]) == 28, "Unity validator covered 28 selected candidates")
require(len(reports["library-reopen-validation.json"]["assets"]) == 33, "Library reopen/render covered 33 candidates")
require(intake.sha(ROOT/"Blender/Library/ProjectPA_AssetLibrary.blend") == reports["library-reopen-validation.json"]["librarySha256"], "Reopen evidence matches final library bytes")
for a in reports["library-reopen-validation.json"]["assets"]:
    png = (ROOT/a["preview"]).read_bytes()
    require(png[:8] == b"\x89PNG\r\n\x1a\n" and struct.unpack(">II", png[16:24]) == (512,512), "Reference PNG 512x512: " + a["id"])
for i in range(1,4):
    png=(DOCS/f"Previews/contact-sheet-{i}.png").read_bytes()
    require(struct.unpack(">II",png[16:24])==(3072,1024), f"Contact sheet {i} 3072x1024")

new_scopes = [ROOT/"Assets/Art/External",ROOT/"Assets/Art/ProjectPA/Materials",ROOT/"Assets/Art/ProjectPA/Prefabs",ROOT/"Assets/Art/ProjectPA/Derived"]
paths = [p for root in new_scopes for p in [root,*root.rglob("*")] if p.suffix != ".meta"]
paths += [ROOT/"Assets/Editor/PA_Art000IntakeValidator.cs"]
for path in paths:
    require(Path(str(path)+".meta").is_file(), "Unity-generated meta present: " + intake.rel(path))
guids = collections.defaultdict(list)
for meta in (ROOT/"Assets").rglob("*.meta"):
    match = re.search(r"^guid: ([a-f0-9]{32})$",meta.read_text(encoding="utf-8-sig"),re.M)
    if match:
        guids[match[1]].append(intake.rel(meta))
duplicates = {guid: paths for guid,paths in guids.items() if len(paths)>1}
require(not duplicates, "No duplicate GUID in current Assets tree")
require(not list((ROOT/"Assets/Art/External").rglob("*.zip")), "No ZIP copied inside Unity Assets")
tracked = subprocess.run(["git","ls-files","-z","ExternalAssetSources","Blender/Source","Blender/Library/Sources"],cwd=ROOT,capture_output=True,check=True).stdout
require(not tracked, "Master/expanded source paths are not tracked")
ignored = subprocess.run(["git","check-ignore","ExternalAssetSources/Tools/Blender/blender-4.5.13-windows-x64.zip"],cwd=ROOT,capture_output=True)
require(ignored.returncode==0,"Portable Blender archive ignored by Git")

# Malicious archive names are rejected in memory: no deletion or filesystem fixtures.
for name,link in [("../escape.fbx",False),("/absolute.obj",False),("C:/outside.fbx",False),("safe/file:stream",False),("CON.txt",False),("trailing. /file",False),("linked.obj",True)]:
    stream=io.BytesIO()
    with zipfile.ZipFile(stream,"w") as archive:
        info=zipfile.ZipInfo(name)
        if link:
            info.external_attr=(stat.S_IFLNK|0o777)<<16
        archive.writestr(info,b"test")
    stream.seek(0)
    rejected=False
    with zipfile.ZipFile(stream) as archive:
        try: intake.checked_members(archive)
        except ValueError: rejected=True
    require(rejected,"Unsafe archive member rejected: "+name)
for script in list((ROOT/"Tools/Art").glob("*.py"))+list((ROOT/"Blender/Scripts").glob("*.py")):
    ast.parse(script.read_text(encoding="utf-8"))
    require(True,"Python syntax: "+intake.rel(script))

required_docs=["EXTERNAL_ASSET_REGISTRY.md","MODEL_INVENTORY.md","SELECTED_ASSET_SPECIFICATIONS.md","STYLE_MEASUREMENTS.md","PREVIEW_REVIEW.md","DEMO_ASSET_REQUIREMENTS_AND_PRODUCTION_PLAN.md"]
for doc in required_docs:
    require((DOCS/doc).is_file() and (DOCS/doc).stat().st_size>100,"Required document: "+doc)
require((ROOT/"PROJECT_PA_ART_STYLE_GRAMMAR.md").is_file(),"Project PA style grammar present")
baseline=json.loads((ROOT/"Logs/ART000/pre-unity-protected.json").read_text())
changed=[p for p,h in baseline.items() if not (ROOT/p).is_file() or intake.sha(ROOT/p)!=h]
# Concurrent CONTENT work must not be erased; report any subsequent difference.
intake.write_json(DOCS/"final-integrity-report.json",{"ticket":"ART-000","status":"PASS","checksPassed":len(checks),
    "archivePacks":6,"logicalModels":550,"libraryAssets":33,"unityAssets":28,"sourceFilesChecked":len(inventory["files"]),
    "assetsGuidCount":len(guids),"duplicateGuids":duplicates,"protectedFilesChangedSincePreUnity":changed,
    "scope":"source integrity, generated artifact coverage, archive path rejection, metadata and report evidence; not gameplay regression or final art approval",
    "checkSummary":checks[:25]+checks[-25:]})
print(json.dumps({"status":"PASS","checks":len(checks),"guids":len(guids),"protectedFilesChanged":changed}))

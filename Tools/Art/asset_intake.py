"""ART-000: auditable, copy-only CC0 intake. Python standard library only.

Run audit first, inspect inventory, then run --import-selected. Never executes
archive contents. Existing files must match byte-for-byte before they are reused.
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import math
from pathlib import Path, PurePosixPath
import re
import shutil
import stat
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DOCS = ROOT / "Docs/AssetProvenance"
DOWNLOADS = Path("C:/Users/sdjsd/Downloads")
PACKS = [
    ("Kenney", "MiniMarket", "kenney_mini-market*.zip", "https://kenney.nl/assets/mini-market", "PA_REF_KENNEY_MARKET"),
    ("Kenney", "FoodKit", "kenney_food-kit*.zip", "https://kenney.nl/assets/food-kit", "PA_REF_KENNEY_FOOD"),
    ("Kenney", "MiniArcade", "kenney_mini-arcade*.zip", "https://kenney.nl/assets/mini-arcade", "PA_REF_KENNEY_ARCADE"),
    ("Quaternius", "CuteFish", "Cute Fish Pack*.zip", "https://quaternius.com/packs/cutefish.html", "PA_REF_Q_FISH"),
    ("Quaternius", "UltimateNature", "Ultimate Nature Pack*.zip", "https://quaternius.com/packs/ultimatenature.html", "PA_REF_Q_NATURE"),
    ("Quaternius", "CubeWorldKit", "Cube World*.zip", "https://quaternius.com/packs/cubeworldkit.html", "PA_REF_Q_CUBE"),
]
# Explicit shortlist: these are visual candidates, not new items or gameplay.
# name: (classification, purpose, existing gameplay owner, proposed display height m)
SELECTED = {
    "MiniMarket": {
        "cash-register": ("USE_WITH_NORMALIZATION", "checkout indicator", "Shop", 0.55),
        "display-bread": ("DERIVATIVE_SOURCE", "empty sales tray/frame", "ShopSlot", 1.0),
        "display-fruit": ("DERIVATIVE_SOURCE", "empty produce display", "ShopSlot", 1.0),
        "shelf-end": ("DERIVATIVE_SOURCE", "stock shelving frame", "StorageBox", 1.6),
        "shopping-basket": ("USE_WITH_NORMALIZATION", "producer stock basket", "ProducerNpcController", 0.35),
    },
    "FoodKit": {
        "carrot": ("USE_WITH_NORMALIZATION", "Carrot item visual", "Item_Carrot", 0.25),
        "loaf": ("USE_WITH_NORMALIZATION", "BreadLoaf item visual", "Item_BreadLoaf", 0.20),
        "fish": ("DERIVATIVE_SOURCE", "GrilledFish composition", "Item_10_GrilledFish", 0.15),
        "mushroom": ("REFERENCE_ONLY", "future forage vocabulary; no new item", "future content approval", 0.25),
        "bag": ("DERIVATIVE_SOURCE", "seed bag; no stock painted onto prop", "Item_15_Seed", 0.30),
        "barrel": ("DERIVATIVE_SOURCE", "producer container", "ProducerNpcController", 0.75),
        "cutting-board": ("USE_WITH_NORMALIZATION", "kitchen preparation surface", "Workbench", 0.035),
        "pot": ("USE_WITH_NORMALIZATION", "kitchen processing prop", "Workbench", 0.30),
        "plate": ("USE_WITH_NORMALIZATION", "processed food presentation", "ItemInstance", 0.035),
    },
    "MiniArcade": {
        "arcade-machine": ("REFERENCE_ONLY", "later Culture/Luxury growth reference", "VillageCultureVisualController", 1.65),
        "prizes": ("REFERENCE_ONLY", "later culture reward shapes", "VillageCultureVisualController", 0.5),
    },
    "CuteFish": {
        "Tuna": ("USE_WITH_NORMALIZATION", "generic existing Fish item; species not new gameplay", "Item_Fish", 0.35),
        "Koi": ("REFERENCE_ONLY", "pond motion reference", "FishingSpot", 0.30),
        "FishingRod_Lvl1": ("USE_WITH_NORMALIZATION", "fishing activity prop", "FishingSpot", 1.5),
        "Dock_Long_NoRope": ("DERIVATIVE_SOURCE", "shore interaction platform", "FishingSpot", 0.4),
    },
    "UltimateNature": {
        "CommonTree_1": ("USE_WITH_NORMALIZATION", "forest resource silhouette", "WorldGrid resource projection", 4.0),
        "BirchTree_1": ("USE_WITH_NORMALIZATION", "forest variation", "WorldGrid resource projection", 4.0),
        "Rock_1": ("DERIVATIVE_SOURCE", "mine ore vein base", "MiningSpot", 1.0),
        "Wheat": ("USE_WITH_NORMALIZATION", "existing farm/producer Wheat", "Farmland / ProducerNpcController", 0.7),
        "WoodLog": ("USE_WITH_NORMALIZATION", "Wood resource", "Item_Wood", 0.4),
        "Grass_Short": ("USE_WITH_NORMALIZATION", "walkable meadow dressing", "WorldGrid visual layer", 0.25),
        "Flowers": ("USE_WITH_NORMALIZATION", "existing village response", "VillageChangeSignalController", 0.4),
        "Bush_1": ("USE_WITH_NORMALIZATION", "forage landmark", "Gatherable", 1.0),
    },
    "CubeWorldKit": {
        "Chest_Closed": ("DERIVATIVE_SOURCE", "PA storage silhouette", "StorageBox", 0.7),
        "Chest_Open": ("DERIVATIVE_SOURCE", "PA producer stock lid parts", "ProducerNpcController", 0.7),
        "Pickaxe_Stone": ("DERIVATIVE_SOURCE", "mining tool parts", "MiningSpot", 0.8),
        "Axe_Stone": ("DERIVATIVE_SOURCE", "woodworking tool parts", "Workbench", 0.7),
        "Cart": ("REFERENCE_ONLY", "later mine area transport dressing", "ProducerNpcController", 0.8),
    },
}


def sha(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def rel(path):
    return Path(path).relative_to(ROOT).as_posix()


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def copy_new(source, destination):
    destination = Path(destination)
    if not destination.resolve().is_relative_to(ROOT):
        raise ValueError(f"Write outside Project_PA: {destination}")
    if destination.exists():
        if sha(source) != sha(destination):
            raise ValueError(f"Refusing overwrite: {destination}")
        return
    destination.parent.mkdir(parents=True, exist_ok=True)
    # Exclusive creation protects originals even if another process appears.
    with Path(source).open("rb") as src, destination.open("xb") as dst:
        shutil.copyfileobj(src, dst)
    if sha(source) != sha(destination):
        raise ValueError(f"Copy verification failed: {destination}")


def checked_members(archive):
    members, seen, total = [], set(), 0
    for entry in archive.infolist():
        name = entry.filename.replace("\\", "/")
        parts = PurePosixPath(name)
        invalid = (parts.is_absolute() or not parts.parts or
                   any(p in ("..", ".") or ":" in p or p.endswith((".", " ")) or
                       re.match(r"^(con|prn|aux|nul|com[1-9]|lpt[1-9])(\.|$)", p, re.I)
                       for p in parts.parts))
        if invalid or stat.S_ISLNK(entry.external_attr >> 16) or entry.flag_bits & 1:
            raise ValueError(f"Unsafe archive entry: {name}")
        if name.casefold() in seen:
            raise ValueError(f"Duplicate archive path: {name}")
        seen.add(name.casefold())
        total += entry.file_size
        if total > 2_000_000_000 or entry.file_size > 500_000_000:
            raise ValueError("Archive exceeds intake size budget")
        members.append(entry)
    return members


def extract_checked(archive_path, destination):
    with zipfile.ZipFile(archive_path) as archive:
        entries = checked_members(archive)
        bad = archive.testzip()
        if bad:
            raise ValueError(f"ZIP CRC failed: {bad}")
        for entry in entries:
            target = destination / entry.filename.replace("\\", "/")
            if not target.resolve().is_relative_to(destination.resolve()):
                raise ValueError("Extraction escaped staging")
            if entry.is_dir():
                target.mkdir(parents=True, exist_ok=True)
                continue
            payload = archive.read(entry)
            if target.exists():
                if target.read_bytes() != payload:
                    raise ValueError(f"Staged source differs: {target}")
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with target.open("xb") as stream:
                    stream.write(payload)


def mesh_metrics(path):
    vertices, faces, materials, normals, uvs = [], [], set(), 0, 0
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        fields = line.split()
        if not fields:
            continue
        if fields[0] == "v":
            vertices.append(tuple(float(v) for v in fields[1:4]))
        elif fields[0] == "f":
            indices = [int(f.split("/")[0]) for f in fields[1:]]
            faces.append([i-1 if i > 0 else len(vertices)+i for i in indices])
        elif fields[0] == "usemtl":
            materials.add(" ".join(fields[1:]))
        elif fields[0] == "vn":
            normals += 1
        elif fields[0] == "vt":
            uvs += 1
    if not vertices or not faces or not all(math.isfinite(v) for p in vertices for v in p):
        raise ValueError(f"Invalid OBJ: {path}")
    lo = [min(v[a] for v in vertices) for a in range(3)]
    hi = [max(v[a] for v in vertices) for a in range(3)]
    area = 0.0
    for face in faces:
        origin = vertices[face[0]]
        for i in range(1, len(face)-1):
            b = [v-o for v, o in zip(vertices[face[i]], origin)]
            c = [v-o for v, o in zip(vertices[face[i+1]], origin)]
            cross = [b[1]*c[2]-b[2]*c[1], b[2]*c[0]-b[0]*c[2], b[0]*c[1]-b[1]*c[0]]
            area += math.sqrt(sum(v*v for v in cross))/2
    triangles = sum(len(f)-2 for f in faces)
    return {"vertices": len(vertices), "polygons": len(faces), "triangles": triangles,
            "boundsMinSource": lo, "boundsMaxSource": hi,
            "dimensionsSource": [h-l for h, l in zip(hi, lo)],
            "surfaceAreaSource": area, "trianglesPerSourceArea": triangles/area if area else None,
            "materialNames": sorted(materials), "normalRecords": normals, "uvRecords": uvs,
            "coordinateCaveat": "OBJ coordinates; final metres/pivot/forward require Unity and visual check"}


def gltf_metrics(path):
    if path.suffix.lower() == ".glb":
        payload = path.read_bytes()
        length, kind = struct.unpack_from("<II", payload, 12)
        if payload[:4] != b"glTF" or kind != 0x4E4F534A:
            raise ValueError(f"Invalid GLB: {path}")
        data = json.loads(payload[20:20+length])
    else:
        data = json.loads(path.read_text(encoding="utf-8-sig"))
    return {"animations": [a.get("name", "unnamed") for a in data.get("animations", [])],
            "materials": [{"name": m.get("name"), "pbr": m.get("pbrMetallicRoughness", {}),
                           "doubleSided": m.get("doubleSided", False)} for m in data.get("materials", [])],
            "images": len(data.get("images", [])), "meshCount": len(data.get("meshes", []))}


def classify(pack, path):
    if path.stem in SELECTED[pack]:
        return SELECTED[pack][path.stem]
    lowered = path.as_posix().lower()
    if any(s in lowered for s in ("character", "/animals/", "/enemies/", "sword", "gambling")):
        return "NOT_NEEDED", "preserve PA cast; no combat/casino scope", "none", 0
    if pack == "CubeWorldKit" and ("/blocks/" in lowered or "/pixel blocks/" in lowered):
        return "REFERENCE_ONLY", "shape study only; never replaces WorldGrid terrain", "none", 0
    if pack == "MiniArcade":
        return "REFERENCE_ONLY", "later culture reference; not demo dependency", "none", 0
    return "NOT_NEEDED", "outside bounded demo shortlist; retain source for later review", "none", 0


def audit():
    packs, models, files, assets = [], [], [], []
    for creator, pack, pattern, url, collection in PACKS:
        candidates = sorted(DOWNLOADS.glob(pattern), key=lambda p: p.stat().st_mtime, reverse=True)
        if not candidates:
            raise FileNotFoundError(f"Missing downloaded pack: {pack}")
        master = candidates[0]
        stage = ROOT / "ExternalAssetSources" / creator / pack
        archive = stage / master.name
        copy_new(master, archive)
        extracted = stage / "Extracted"
        extract_checked(archive, extracted)
        all_files = sorted(p for p in extracted.rglob("*") if p.is_file())
        license_files = [p for p in all_files if "license" in p.name.lower()]
        licenses = []
        for path in license_files:
            contents = path.read_text(encoding="utf-8-sig")
            if "CC0" not in contents:
                raise ValueError(f"License needs review for {pack}: {path}")
            target = DOCS / "Licenses" / (pack + "_" + path.name)
            copy_new(path, target)
            licenses.append(rel(target))
        pack_models = []
        for path in all_files:
            item = {"pack": pack, "path": rel(path), "bytes": path.stat().st_size,
                    "sha256": sha(path), "extension": path.suffix.lower()}
            files.append(item)
            if path.suffix.lower() != ".obj":
                continue
            classification, purpose, owner, target_height = classify(pack, path)
            # Path, not stem, is identity: Cube World has two Block_Blank models.
            source_key = path.relative_to(extracted).with_suffix("").as_posix()
            record = {"pack": pack, "name": path.stem, "sourceKey": source_key,
                      "classification": classification, "purpose": purpose,
                      "obj": rel(path), "metrics": mesh_metrics(path)}
            models.append(record)
            pack_models.append(record)
            if path.stem not in SELECTED[pack]:
                continue
            siblings = [p for p in all_files if p.stem == path.stem]
            blender_source = next((p for p in siblings if p.suffix.lower() == (".glb" if creator == "Kenney" else ".blend")), None)
            fbx = next((p for p in siblings if p.suffix.lower() == ".fbx"), None)
            gltf = next((p for p in siblings if p.suffix.lower() in (".glb", ".gltf")), None)
            if blender_source is None or fbx is None:
                raise ValueError(f"Selected source format missing: {pack}/{path.stem}")
            if gltf:
                record["gltf"] = gltf_metrics(gltf)
            asset_id = "PA_REF_" + re.sub(r"[^A-Z0-9]+", "_", f"{pack}_{path.stem}".upper())
            existing = ROOT / "Assets/Art/Ultimate Nature Pack - Jun 2019/FBX" / fbx.name
            # Existing Nature format layout is inspected by exact content match.
            existing_matches = [p for p in (ROOT / "Assets/Art/Ultimate Nature Pack - Jun 2019").rglob(fbx.name) if sha(p) == sha(fbx)] if pack == "UltimateNature" else []
            unity = existing_matches[0] if existing_matches else ROOT / "Assets/Art/External" / creator / pack / "Models" / fbx.name
            library = ROOT / "Blender/Library/Sources" / collection / blender_source.name
            assets.append({"id": asset_id, "pack": pack, "name": path.stem, "collection": collection,
                           "classification": classification, "purpose": purpose, "gameplayOwner": owner,
                           "blenderSource": rel(blender_source), "librarySource": rel(library),
                           "sourceFbx": rel(fbx), "sourceSha256": sha(fbx),
                           "unityPath": rel(unity), "reuseExisting": bool(existing_matches),
                           "importToUnity": classification != "REFERENCE_ONLY", "targetHeight": target_height,
                           "wrapperPath": "Assets/Art/ProjectPA/Prefabs/Intake/" + asset_id + ".prefab",
                           "derivative": False, "interactionForward": "-Z target; visual front unverified",
                           "footprint": "derive from normalized XZ extents / 2m cell", "clearance": "1m approach; validate in gameplay integration",
                           "collider": "none on visual; existing owner supplies collider", "navmesh": "visual only; owner controls obstacle",
                           "mobility": "dynamic item or owner-controlled placeable", "materialFamily": "source look; ART-001 PA palette pending"})
        packs.append({"packName": pack, "creator": creator, "sourceUrl": url,
                      "downloadedArchiveName": master.name, "masterPath": str(master), "archiveSHA256": sha(master),
                      "archiveBytes": master.stat().st_size, "extractedPath": rel(extracted),
                      "duplicateDownloads": [{"name": p.name, "sha256": sha(p)} for p in candidates[1:]],
                      "licenseFiles": licenses, "licenseDesignation": "CC0-1.0",
                      "licenseEvidence": "archive and official pack page" if licenses else "official pack page; archive contains no license file",
                      "originalFormats": dict(collections.Counter(p.suffix.lower() for p in all_files)),
                      "logicalModelCount": len(pack_models), "selectedBlenderFormat": "GLB" if creator == "Kenney" else "BLEND",
                      "selectedUnityFormat": "FBX", "auditDate": "2026-09-06", "importDate": None,
                      "detectedVersionDate": {"MiniMarket":"1.0 / 2024-10-25", "FoodKit":"2.0 / 2024-06-26", "MiniArcade":"1.2 / 2024-07-22", "CuteFish":"February 2020 (folder/page)", "UltimateNature":"June 2019 (folder/page)", "CubeWorldKit":"August 2023 (folder/page)"}[pack],
                      "classificationCounts": dict(collections.Counter(m["classification"] for m in pack_models)),
                      "derivativeCreated": False})
    inventory = {"ticket": "ART-000", "packs": packs, "models": models, "files": files}
    write_json(DOCS / "asset-inventory.json", inventory)
    write_json(DOCS / "selected-assets.json", {"ticket": "ART-000", "assets": assets})
    print(json.dumps({"audit": "PASS", "packs": len(packs), "models": len(models), "files": len(files),
                      "librarySelection": len(assets), "unitySelection": sum(a["importToUnity"] for a in assets)}))


def import_selected():
    selection = json.loads((DOCS / "selected-assets.json").read_text(encoding="utf-8"))
    inventory = json.loads((DOCS / "asset-inventory.json").read_text(encoding="utf-8"))
    for pack in inventory["packs"]:
        if sha(Path(pack["masterPath"])) != pack["archiveSHA256"]:
            raise ValueError("Master changed after audit")
    for asset in selection["assets"]:
        copy_new(ROOT / asset["blenderSource"], ROOT / asset["librarySource"])
        # GLB may still reference external images; retain its relative dependency path.
        library_textures = (ROOT / asset["blenderSource"]).parent / "Textures"
        if library_textures.exists():
            for texture in library_textures.iterdir():
                if texture.is_file():
                    copy_new(texture, (ROOT / asset["librarySource"]).parent / "Textures" / texture.name)
        if not asset["importToUnity"]:
            continue
        source = ROOT / asset["sourceFbx"]
        if sha(source) != asset["sourceSha256"]:
            raise ValueError("Selected source changed after audit")
        copy_new(source, ROOT / asset["unityPath"])
        if asset["reuseExisting"]:
            continue
        # Kenney FBX declares a local Textures/colormap.png; Cube atlas is shared per pack.
        texture_dir = source.parent / "Textures"
        if texture_dir.exists():
            for texture in texture_dir.iterdir():
                if texture.is_file():
                    copy_new(texture, (ROOT / asset["unityPath"]).parent / "Textures" / texture.name)
        if asset["pack"] == "CubeWorldKit":
            pack_root = next(p for p in inventory["packs"] if p["packName"] == "CubeWorldKit")
            atlas = next((ROOT / pack_root["extractedPath"]).rglob("Atlas.png"))
            copy_new(atlas, (ROOT / asset["unityPath"]).parent / "Textures/Atlas.png")
    for pack in inventory["packs"]:
        pack["importDate"] = "2026-09-06"
    write_json(DOCS / "asset-inventory.json", inventory)
    write_json(ROOT / "Blender/Library/library-manifest.json", selection)
    for folder in ["Source", "Generated", "Export"]:
        (ROOT / "Blender" / folder).mkdir(parents=True, exist_ok=True)
    for folder in ["Shop", "Production", "Nature", "Fishing", "Food", "Culture", "Buildings", "Props"]:
        (ROOT / "Assets/Art/ProjectPA/Derived" / folder).mkdir(parents=True, exist_ok=True)
    print(json.dumps({"import": "COPIED_PENDING_UNITY_VALIDATION", "newFbx": sum(a["importToUnity"] and not a["reuseExisting"] for a in selection["assets"]),
                      "reusedFbx": sum(a["importToUnity"] and a["reuseExisting"] for a in selection["assets"])}))


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--import-selected", action="store_true")
    args = parser.parse_args()
    import_selected() if args.import_selected else audit()

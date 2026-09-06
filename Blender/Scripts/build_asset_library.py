"""Build the ART-000 reference library with Blender --background --factory-startup
--disable-autoexec --python Blender/Scripts/build_asset_library.py.
Only copied source files are read. Original meshes are never saved over.
"""
import json
import colorsys
import hashlib
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
manifest = json.loads((ROOT / "Blender/Library/library-manifest.json").read_text(encoding="utf-8"))
output = ROOT / "Blender/Library/ProjectPA_AssetLibrary.blend"
if output.exists():
    raise RuntimeError("Library already exists; choose a new versioned output, never overwrite a source.")

for obj in list(bpy.context.scene.objects):
    for default_collection in list(obj.users_collection):
        default_collection.objects.unlink(obj)

collections = {}
report = []
for index, asset in enumerate(manifest["assets"]):
    name = asset["collection"]
    if name not in collections:
        collections[name] = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(collections[name])
    collection = bpy.data.collections.new(asset["id"])
    collections[name].children.link(collection)
    source = ROOT / asset["librarySource"]
    source_actions = []
    if source.suffix.lower() == ".blend":
        with bpy.data.libraries.load(str(source), link=False) as (available, loaded):
            loaded.objects = available.objects
            loaded.actions = available.actions
        source_actions = [action for action in loaded.actions if action]
        objects = [obj for obj in loaded.objects if obj is not None]
        for obj in objects:
            collection.objects.link(obj)
        if source_actions:
            rigs = [obj for obj in objects if obj.type == "ARMATURE"]
            if len(rigs) != 1:
                raise RuntimeError("Source action ownership needs review: " + asset["id"])
            animation = rigs[0].animation_data_create()
            linked = {strip.action for track in animation.nla_tracks for strip in track.strips if strip.action}
            if animation.action:
                linked.add(animation.action)
            for action in source_actions:
                action.use_fake_user = True
                if action in linked:
                    continue
                track = animation.nla_tracks.new()
                track.name = "PA_SOURCE_" + action.name
                track.mute = True
                track.strips.new(action.name, int(action.frame_range[0]), action)
    else:
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=str(source))
        objects = list(set(bpy.data.objects) - before)
        for obj in objects:
            for previous in list(obj.users_collection):
                previous.objects.unlink(obj)
            collection.objects.link(obj)
    meshes = [obj for obj in objects if obj.type == "MESH"]
    if not meshes:
        raise RuntimeError("No meshes: " + asset["id"])
    # Asset Browser collection retains rig, materials, hierarchy and animations.
    collection.asset_mark()
    collection.asset_data.description = asset["purpose"] + "; " + asset["classification"]
    collection["pa_asset_id"] = asset["id"]
    collection["pa_provenance"] = asset["blenderSource"]
    collection["pa_source_sha256"] = hashlib.sha256(source.read_bytes()).hexdigest()
    collection["pa_unity_source_sha256"] = asset["sourceSha256"]
    collection["pa_gameplay_owner"] = asset["gameplayOwner"]
    collection["pa_source_actions"] = [action.name for action in source_actions]
    bounds = [obj.matrix_world @ Vector(corner) for obj in meshes for corner in obj.bound_box]
    # Resolve each loaded image inside this source pack, never across pack names.
    pack_root = ROOT / asset["blenderSource"].split("/Extracted/")[0] / "Extracted"
    for material in {m for o in meshes for m in o.data.materials if m}:
        for node in material.node_tree.nodes if material.use_nodes else []:
            if node.type != "TEX_IMAGE" or not node.image:
                continue
            image = node.image
            if image.source != "FILE" or image.packed_file:
                continue
            filename = Path(image.filepath.replace("\\", "/")).name
            if not image.has_data:
                candidates = list(pack_root.rglob(filename))
                if not candidates or len({hashlib.sha256(p.read_bytes()).hexdigest() for p in candidates}) != 1:
                    raise RuntimeError("Missing or ambiguous texture in " + asset["pack"] + ": " + filename)
                image.filepath = str(candidates[0])
                image.reload()
            image.pack()
    material_report = []
    for material in {m for o in meshes for m in o.data.materials if m}:
        principled = next((n for n in material.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None) if material.use_nodes else None
        rgba = list(principled.inputs["Base Color"].default_value) if principled else list(material.diffuse_color)
        material_report.append({"name": material.name, "baseColorLinear": rgba,
            "saturationLinear": colorsys.rgb_to_hsv(*rgba[:3])[1],
            "roughness": float(principled.inputs["Roughness"].default_value) if principled else float(material.roughness),
            "metallic": float(principled.inputs["Metallic"].default_value) if principled else float(material.metallic),
            "textures": [n.image.name for n in material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image] if material.use_nodes else []})
    report.append({"id": asset["id"], "meshes": len(meshes),
                   "vertices": sum(len(o.data.vertices) for o in meshes),
                   "polygons": sum(len(o.data.polygons) for o in meshes),
                   "dimensions": [max(v[a] for v in bounds)-min(v[a] for v in bounds) for a in range(3)],
                   "actions": sorted(action.name for action in source_actions) if source_actions else sorted({o.animation_data.action.name for o in objects if o.animation_data and o.animation_data.action}),
                   "materials": material_report,
                   "smoothPolygons": sum(p.use_smooth for o in meshes for p in o.data.polygons),
                   "bevelModifiers": sum(m.type == "BEVEL" for o in meshes for m in o.modifiers),
                   "meshSurfaceAreaLocal": sum(p.area for o in meshes for p in o.data.polygons),
                   "measurementCaveat": "Modifiers only measure live bevels; applied bevel geometry is assessed visually. Source units precede Unity wrapper normalization."})
    collection.hide_viewport = True
    collection.hide_render = True

# Repair external texture links using only audited project staging; pack into output.
for image in bpy.data.images:
    if image.packed_file or image.source != "FILE":
        continue
    if not image.has_data:
        filename = Path(bpy.path.abspath(image.filepath)).name
        candidates = [p for p in (ROOT / "ExternalAssetSources").rglob(filename) if "/Tools/" not in p.as_posix()]
        if not candidates:
            raise RuntimeError("Missing source texture: " + image.filepath)
        if len({hashlib.sha256(p.read_bytes()).hexdigest() for p in candidates}) > 1:
            raise RuntimeError("Ambiguous source texture: " + filename)
        image.filepath = str(candidates[0])
        image.reload()
    image.pack()

derived = bpy.data.collections.new("PA_DERIVED_RESERVED")
bpy.context.scene.collection.children.link(derived)
derived["status"] = "ART-000 production plan only; no derivative models created"
bpy.ops.wm.save_as_mainfile(filepath=str(output))
(ROOT / "Docs/AssetProvenance/blender-validation.json").write_text(
    json.dumps({"status": "PASS", "blenderVersion": bpy.app.version_string, "assets": report}, indent=2) + "\n", encoding="utf-8")
print("ART000_BLENDER_LIBRARY_PASS " + str(len(report)))

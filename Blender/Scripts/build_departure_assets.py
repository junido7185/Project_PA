"""VS-PRESENT-001 P0 production. Read audited sources; never overwrite ART-000.

Run portable Blender --background --factory-startup --disable-autoexec
--python-exit-code 1 --python Blender/Scripts/build_departure_assets.py.
"""
import colorsys
import hashlib
import json
import math
import shutil
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
LIBRARY = ROOT / "Blender/Library/ProjectPA_AssetLibrary.blend"
REVISION = sys.argv[sys.argv.index("--revision")+1] if "--revision" in sys.argv else "01"
assert REVISION.isdigit() and len(REVISION) == 2
GENERATED = ROOT / "Blender/Generated/VS_PRESENT_001"
EXPORT = ROOT / "Blender/Export/VS_PRESENT_001" / ("r" + REVISION)
UNITY = ROOT / "Assets/Art/ProjectPA/Derived/Departure"
EVIDENCE = ROOT / "Docs/AssetProvenance/VS_PRESENT_001" / ("r" + REVISION)
for directory in (GENERATED, EXPORT, UNITY, EVIDENCE):
    directory.mkdir(parents=True, exist_ok=True)
OUTPUT = GENERATED / ("PA_DepartureTrainingKit_r" + REVISION + ".blend")
if OUTPUT.exists():
    raise RuntimeError("Generated kit already exists; preserve prior output and choose a versioned output.")

inventory = json.loads((ROOT / "Docs/AssetProvenance/asset-inventory.json").read_text(encoding="utf-8"))
audited = {entry["path"]: entry["sha256"] for entry in inventory["files"]}
library_hash = hashlib.sha256(LIBRARY.read_bytes()).hexdigest()
prior = json.loads((ROOT / "Docs/AssetProvenance/library-reopen-validation.json").read_text(encoding="utf-8"))
assert library_hash == prior["librarySha256"], "ART-000 library changed since validated checkpoint"

def source(path):
    assert path in audited, "Source absent from ART-000 file inventory: " + path
    digest = hashlib.sha256((ROOT / path).read_bytes()).hexdigest()
    assert digest == audited[path], "Audited source content changed: " + path
    return {"path": path, "sha256": digest}

APPLE = source("ExternalAssetSources/Kenney/FoodKit/Extracted/Models/GLB format/apple.glb")
BOAT = source("ExternalAssetSources/Quaternius/CuteFish/Extracted/Cute Fish Pack - Feb 2020/Blends/Boat.blend")
SHELF = source("ExternalAssetSources/Kenney/MiniMarket/Extracted/Models/GLB format/display-bread.glb")
for obj in list(bpy.context.scene.objects):
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"
scene.unit_settings.scale_length = 1.0
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.69, 0.80, 0.85, 1)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.55

PALETTE = {
    "PA_Wood": "A07850", "PA_WoodLight": "D4B896", "PA_Leaf": "8DB87A",
    "PA_LeafDark": "547957", "PA_Terracotta": "D4714A", "PA_Coral": "F08070",
    "PA_Gold": "F5D76E", "PA_Teal": "5BAFC0", "PA_Cream": "F5EAD5",
    "PA_Ink": "344B4E", "PA_Apple": "D6604F"
}
materials = {}
def linear(value):
    return value / 12.92 if value < .04045 else ((value + .055) / 1.055) ** 2.4
for name, hexcolor in PALETTE.items():
    rgba = tuple(linear(int(hexcolor[i:i+2], 16) / 255) for i in (0, 2, 4)) + (1,)
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = rgba
    mat.use_nodes = True
    node = mat.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = rgba
    node.inputs["Roughness"].default_value = .8
    node.inputs["Metallic"].default_value = 0
    materials[name] = mat

current = None
assets = []
def begin(name):
    global current
    current = bpy.data.collections.new(name)
    current["pa_ticket"] = "VS-PRESENT-001"
    scene.collection.children.link(current)
    return current

def own(obj, name, material):
    obj.name = name
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    current.objects.link(obj)
    obj.data.materials.clear()
    obj.data.materials.append(materials[material])
    return obj

def cube(name, center, size, material, bevel=.025):
    bpy.ops.mesh.primitive_cube_add(size=1, location=center)
    obj = own(bpy.context.object, name, material)
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        modifier = obj.modifiers.new("PA soft working edge", "BEVEL")
        modifier.width = min(bevel, min(size) * .10)
        modifier.segments = 2
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj

def sphere(name, center, radius, material):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=radius, location=center)
    return own(bpy.context.object, name, material)

def cylinder(name, center, radius, depth, material, face=False):
    bpy.ops.mesh.primitive_cylinder_add(vertices=12, radius=radius, depth=depth, location=center,
                                      rotation=(math.pi/2, 0, 0) if face else (0, 0, 0))
    return own(bpy.context.object, name, material)

def icon(name, coords, y, material):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata([(x, y, z) for x, z in coords], [], [tuple(range(len(coords)))])
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    current.objects.link(obj)
    mesh.materials.append(materials[material])
    solidify = obj.modifiers.new("Pictogram relief", "SOLIDIFY")
    solidify.thickness = .022
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=solidify.name)
    obj.select_set(False)
    return obj

def lettering(body, center, size, material):
    curve = bpy.data.curves.new("PA lettering " + body, "FONT")
    curve.body = body
    curve.align_x = "CENTER"
    curve.align_y = "CENTER"
    curve.size = size
    curve.extrude = .004
    obj = bpy.data.objects.new("Label_" + body, curve)
    current.objects.link(obj)
    obj.location = center
    obj.rotation_euler = (math.pi/2, 0, 0)
    obj.data.materials.append(materials[material])
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.convert(target="MESH")
    return bpy.context.object

def bounds(objects):
    bpy.context.view_layer.update()
    vertices = [o.matrix_world @ Vector(v) for o in objects if o.type == "MESH" for v in o.bound_box]
    return Vector([min(v[i] for v in vertices) for i in range(3)]), Vector([max(v[i] for v in vertices) for i in range(3)])

def join(name, objects=None):
    objects = list(objects or current.objects)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.hide_set(False)
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    return obj

def normalize(objects, axis, target):
    lo, hi = bounds(objects)
    scale = target / (hi[axis] - lo[axis])
    offset = Vector(((lo.x+hi.x)/2, (lo.y+hi.y)/2, lo.z))
    for obj in objects:
        obj.data.transform(obj.matrix_world)
        obj.matrix_world = Matrix.Identity(4)
        for vertex in obj.data.vertices:
            vertex.co = (vertex.co - offset) * scale

def remap_mesh(obj, role):
    old = list(obj.data.materials)
    mapping = []
    for poly in obj.data.polygons:
        mat = old[poly.material_index] if old else None
        color = tuple(mat.diffuse_color[:3]) if mat else (.3, .3, .3)
        if mat and mat.use_nodes:
            shader = next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
            if shader:
                color = tuple(shader.inputs["Base Color"].default_value[:3])
            tex = next((n.image for n in mat.node_tree.nodes if n.type == "TEX_IMAGE" and n.image), None)
            if tex and obj.data.uv_layers.active:
                uv = obj.data.uv_layers.active.data[poly.loop_indices[0]].uv
                x, y = int((uv.x % 1)*tex.size[0]), int((uv.y % 1)*tex.size[1])
                pos = (y*tex.size[0]+x)*4
                color = tuple(tex.pixels[pos:pos+3])
        hue, saturation, value = colorsys.rgb_to_hsv(*color)
        if role == "fruit":
            name = "PA_LeafDark" if .17 < hue < .5 else "PA_Wood" if hue > .055 and hue < .17 else "PA_Apple"
        elif role == "shelf":
            name = "PA_Ink" if value < .05 else "PA_Teal" if hue > .4 else "PA_Wood"
        else:
            name = "PA_Ink" if value < .08 else "PA_WoodLight" if saturation < .2 else "PA_Wood"
        mapping.append(name)
    obj.data.materials.clear()
    names = list(dict.fromkeys(mapping))
    for name in names:
        obj.data.materials.append(materials[name])
    for poly, name in zip(obj.data.polygons, mapping):
        poly.material_index = names.index(name)

def register(name, sources, purpose, footprint, clearance, details, children=None):
    objects = list(current.objects)
    lo, hi = bounds(objects)
    assert abs(lo.z) < .006 and abs((lo.x+hi.x)/2) < .006 and abs((lo.y+hi.y)/2) < .006, (name, tuple(lo), tuple(hi))
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    path = EXPORT / (name + ".fbx")
    assert not path.exists(), "Refuse overwrite of versioned export: " + name
    if (UNITY / path.name).exists():
        previous_report = ROOT / "Docs/AssetProvenance/VS_PRESENT_001/derived-assets.json"
        previous_assets = json.loads(previous_report.read_text(encoding="utf-8"))["assets"]
        expected = next(entry["sha256"] for entry in previous_assets if entry["id"] == name)
        assert hashlib.sha256((UNITY/path.name).read_bytes()).hexdigest() == expected, "Unity FBX was independently changed: " + name
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True, object_types={"MESH"},
                             axis_forward="-Z", axis_up="Y", global_scale=1,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             bake_space_transform=True, add_leaf_bones=False, bake_anim=False,
                             mesh_smooth_type="FACE", use_custom_props=True)
    shutil.copy2(path, UNITY / path.name)
    dimensions = hi-lo
    assets.append({"id": name, "purpose": purpose, "sources": sources, "derivativeWork": details,
                   "fbxPath": (UNITY/path.name).relative_to(ROOT).as_posix(),
                   "sha256": hashlib.sha256(path.read_bytes()).hexdigest(),
                   "blenderDimensionsXYZ": list(dimensions), "unityDimensionsXYZ": [dimensions.x, dimensions.z, dimensions.y],
                   "pivot": "ground center (0,0,0)", "authoringForward": "Blender -Y", "unityForward": "-Z",
                   "footprintMetersXZ": footprint, "clearanceMeters": clearance,
                   "collider": "none in model; gameplay owner creates primitive colliders", "navMesh": "existing scene owner",
                   "materials": sorted({m.name for o in objects for m in o.data.materials}),
                   "triangles": sum(len(p.vertices)-2 for o in objects for p in o.data.polygons),
                   "independentChildren": children or [], "collection": current.name})
    current.hide_render = True
    print("VS_PRESENT_ASSET_EXPORTED", name, tuple(dimensions), flush=True)

# Append only the empty audited frame from the packed ART-000 library.
begin("PA_DepartureTrainingShelf")
with bpy.data.libraries.load(str(LIBRARY), link=False) as (available, loaded):
    loaded.objects = ["display-bread"]
frame = loaded.objects[0]
assert frame and frame.type == "MESH"
current.objects.link(frame)
frame.data = frame.data.copy()
for vertex in frame.data.vertices:
    vertex.co.x *= 2.57
    vertex.co.y *= 2.0
    vertex.co.z = vertex.co.z * .55 + .94
remap_mesh(frame, "shelf")
cube("WorkingTop_1m", (0, 0, .95), (1.6, 1.0, .1), "PA_WoodLight")
for x in (-.65, .65):
    for y in (-.44, .44):
        cube("CounterLeg", (x, y, .47), (.16, .16, .94), "PA_Wood")
cube("FrontCrossBrace", (0, -.44, .3), (1.46, .12, .14), "PA_Teal")
cube("BlankPriceHolder", (0, -.605, .89), (.52, .09, .26), "PA_Gold")
cube("BlankPriceFace", (0, -.655, .89), (.43, .015, .17), "PA_Cream", .007)
cube("RearBalanceRail", (0, .635, .91), (1.46, .05, .1), "PA_Wood")
obj = join("PA_DepartureTrainingShelf")
lo, hi = bounds([obj])
obj.data.transform(Matrix.Translation(Vector((-(lo.x+hi.x)/2, -(lo.y+hi.y)/2, -lo.z))))
register("PA_DepartureTrainingShelf", [SHELF, {"library": LIBRARY.relative_to(ROOT).as_posix(), "sha256": library_hash,
         "mesh": "display-bread", "excludedOriginalObjects": ["bread", "bread.001"]}],
         "ShopSlot visual only; empty counter and blank price holder", [1.8, 1.35], 1.2,
         "Only frame retained; rescaled tray, four legs, braces, 1m worktop and removable blank price face authored. No stock mesh.")

# Three independent board meshes in one FBX. Unity instantiates exactly one child.
begin("PA_DepartureTrainingBoards")
board_names = []
for index, role in enumerate(("Move", "Harvest", "Trade"), 1):
    before = set(current.objects)
    cube("BoardFoot", (0, 0, .06), (1.7, .60, .12), "PA_Wood")
    for x in (-.62, .62):
        cube("BoardPost", (x, .04, 1.00), (.12, .16, 2.0), "PA_Wood")
    cube("BoardFrame", (0, 0, 1.83), (1.78, .16, 1.46), "PA_Teal")
    cube("BoardFace", (0, -.087, 1.83), (1.63, .025, 1.29), "PA_Cream")
    cube("BoardHeader", (0, -.115, 2.36), (1.64, .035, .25), "PA_Ink")
    lettering("P.A.  /  0" + str(index), (0, -.146, 2.365), .16, "PA_Gold")
    if role == "Move":
        for x in (-.48, 0, .48):
            icon("ForwardChevron", [(x-.16, 1.6), (x+.04, 1.6), (x+.22, 1.88),
                                     (x+.04, 2.16), (x-.16, 2.16), (x+.02, 1.88)], -.15, "PA_Terracotta")
        lettering("W A S D", (0, -.15, 1.38), .17, "PA_Ink")
    elif role == "Harvest":
        cube("TreeTrunkSymbol", (-.12, -.14, 1.72), (.15, .05, .62), "PA_Wood")
        for layer, (x, z, radius) in enumerate(((-.36, 1.90, .23), (.12, 1.87, .25), (-.12, 1.99, .24))):
            cylinder("CanopySymbol", (x, -.16-layer*.028, z), radius, .04, "PA_LeafDark", True)
        for x, z in ((-.38, 1.89), (-.07, 2.03), (.21, 1.83)):
            cylinder("FruitSymbol", (x, -.26, z), .085, .045, "PA_Apple", True)
        cube("InteractionKey", (0, -.15, 1.39), (1.04, .055, .21), "PA_Ink")
        lettering("SPACE", (0, -.19, 1.39), .14, "PA_Cream")
    else:
        cube("ShelfSymbol", (-.30, -.15, 1.61), (.65, .045, .12), "PA_Wood")
        for x in (-.5, -.12):
            # Keep the legs behind the shelf face; coplanar joins render black.
            cube("ShelfLegSymbol", (x, -.12, 1.45), (.07, .045, .35), "PA_Wood")
        for x in (-.49, -.29, -.09):
            cylinder("StockSymbol", (x, -.16, 1.82), .095, .035, "PA_Apple", True)
        cylinder("PriceCoinSymbol", (.40, -.17, 1.96), .25, .04, "PA_Gold", True)
        lettering("G", (.40, -.20, 1.96), .27, "PA_Ink")
        icon("PricePointer", [(.13, 1.62), (.52, 1.62), (.52, 1.48), (.7, 1.72), (.52, 1.96), (.52, 1.82), (.13, 1.82)], -.14, "PA_Teal")
        lettering("PRICE", (.41, -.15, 1.34), .12, "PA_Ink")
    board = join("Board_" + role, list(set(current.objects)-before))
    board_names.append(board.name)
register("PA_DepartureTrainingBoards", [{"creator": "Project P.A. original Blender geometry", "externalDesignCopied": False}],
         "Action pictograms for three Training Bays; choose one named child", [1.8, .6], .8,
         "Original shared timber training board; relief direction, tree/fruit, shelf/coin pictograms; text converted to mesh.", board_names)

begin("PA_DepartureCheckpoint")
for x in (-1.2, 1.2):
    cube("CheckpointFoot", (x, 0, .075), (.42, .55, .15), "PA_Ink")
    cube("CheckpointPost", (x, 0, .65), (.19, .22, 1.3), "PA_Teal")
    cylinder("CheckpointLight", (x, 0, 1.38), .14, .22, "PA_Gold")
    cube("CheckpointCap", (x, 0, 1.53), (.3, .3, .09), "PA_Ink")
cube("CheckpointThreshold", (0, 0, .025), (2.0, .42, .05), "PA_Gold")
join("PA_DepartureCheckpoint")
register("PA_DepartureCheckpoint", [{"creator": "Project P.A. original Blender geometry"}],
         "Movement destination light gate, clear 2m crossing", [2.82, .55], 1.0,
         "Two short checkpoint lights plus threshold. No invisible wall or collision in FBX.")

begin("PA_DepartureFruit")
before = set(bpy.data.objects)
bpy.ops.import_scene.gltf(filepath=str(ROOT / APPLE["path"]))
apple_objects = [o for o in set(bpy.data.objects)-before if o.type == "MESH"]
for obj in apple_objects:
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    current.objects.link(obj)
normalize(apple_objects, 2, .28)
for obj in apple_objects:
    remap_mesh(obj, "fruit")
join("PA_DepartureFruit")
register("PA_DepartureFruit", [APPLE], "TEMP vertical slice fruit ItemData visual", [.3, .3], 0,
         "Audited Kenney apple newly selected, normalized to .28m and remapped to PA apple/leaf/wood material family.")

begin("PA_DepartureBoat")
with bpy.data.libraries.load(str(ROOT / BOAT["path"]), link=False) as (available, loaded):
    loaded.objects = available.objects
boat_objects = [o for o in loaded.objects if o and o.type == "MESH"]
for obj in boat_objects:
    current.objects.link(obj)
normalize(boat_objects, 1, 5.8)
for obj in boat_objects:
    remap_mesh(obj, "boat")
# Company cargo skiff: boarding plank and company pennant, avoiding a new vehicle system.
cube("CargoBoardingDeck", (0, 0, .69), (1.5, 2.3, .12), "PA_WoodLight")
cylinder("PennantMast", (0, 1.25, 1.46), .045, 1.54, "PA_Ink")
icon("DeparturePennant", [(0, 2.19), (.61, 2.19), (.48, 1.91), (0, 1.91)], 1.25, "PA_Terracotta")
lettering("P.A.", (.25, 1.215, 2.05), .15, "PA_Cream")
obj = join("PA_DepartureBoat")
lo, hi = bounds([obj])
obj.data.transform(Matrix.Translation(Vector((-(lo.x+hi.x)/2, -(lo.y+hi.y)/2, -lo.z))))
register("PA_DepartureBoat", [BOAT], "P0 harbor dressing; no vehicle gameplay", [3.1, 5.8], 1.0,
         "Audited Quaternius skiff normalized and PA palette mapped; new central boarding deck and company pennant. No movement authority.")

# Save editable mesh kit with source provenance, then render only generated assets.
for asset in assets:
    collection = bpy.data.collections[asset["collection"]]
    collection["pa_provenance"] = json.dumps(asset["sources"], ensure_ascii=False)
    collection.asset_mark()
    collection.asset_data.description = asset["purpose"]
bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT))
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 20
scene.cycles.use_denoising = True
scene.render.resolution_x = 960
scene.render.resolution_y = 960
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"
begin("PA_ReferenceOnly")
cube("ReferenceGround", (0, 0, -.07), (200, 200, .1), "PA_Cream", 0)
camera_data = bpy.data.cameras.new("PA_ReferenceCamera")
camera = bpy.data.objects.new("PA_ReferenceCamera", camera_data)
current.objects.link(camera)
camera_data.type = "ORTHO"
scene.camera = camera
light_data = bpy.data.lights.new("PA_ReferenceKey", "AREA")
light = bpy.data.objects.new("PA_ReferenceKey", light_data)
current.objects.link(light)
light.location = (-3, -4, 8)
light.rotation_euler = (-light.location).to_track_quat("-Z", "Y").to_euler()
light_data.energy = 1100
light_data.size = 5
for asset in assets:
    collection = bpy.data.collections[asset["collection"]]
    collection.hide_render = False
    preview_objects = list(collection.objects)
    if asset["independentChildren"]:
        for index, obj in enumerate(sorted(preview_objects, key=lambda o: asset["independentChildren"].index(o.name))):
            obj.location.x = (index-1)*2.1
        scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    else:
        scene.render.resolution_x, scene.render.resolution_y = 960, 960
    lo, hi = bounds(preview_objects)
    target = (lo+hi)/2
    if asset["independentChildren"]:
        camera.location = target + Vector((.12, -8, 1.2))
        camera_data.ortho_scale = 7.2
    else:
        camera.location = target + Vector((5, -8, 5.2))
        camera_data.ortho_scale = max(hi-lo)*1.43
    camera.rotation_euler = (target-camera.location).to_track_quat("-Z", "Y").to_euler()
    scene.render.filepath = str(EVIDENCE / (asset["id"] + ".png"))
    bpy.ops.render.render(write_still=True)
    asset["referenceRender"] = Path(scene.render.filepath).relative_to(ROOT).as_posix()
    for obj in preview_objects:
        obj.location = (0, 0, 0)
    collection.hide_render = True
    print("VS_PRESENT_REFERENCE_RENDER_PASS", asset["id"], flush=True)
assert hashlib.sha256(LIBRARY.read_bytes()).hexdigest() == library_hash
for entry in (APPLE, BOAT, SHELF):
    assert hashlib.sha256((ROOT/entry["path"]).read_bytes()).hexdigest() == entry["sha256"]
report = {"ticket": "VS-PRESENT-001", "phase": "P0", "status": "BLENDER_EXPORT_AND_RENDER_PASS",
          "revision": REVISION,
          "blenderVersion": bpy.app.version_string, "librarySha256BeforeAndAfter": library_hash,
          "styleGrammar": "PROJECT_PA_ART_STYLE_GRAMMAR.md", "roughness": .8, "metallic": 0,
          "paletteSrgb": PALETTE, "sourcesPreserved": True, "assets": assets,
          "unityValidation": "Pending editor importer, wrapper bounds/reference and actual P0 gameplay validation"}
(EVIDENCE / "derived-assets.json").write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
(EVIDENCE.parent / "derived-assets.json").write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
print("VS_PRESENT_BLENDER_PRODUCTION_PASS assets=" + str(len(assets)), flush=True)

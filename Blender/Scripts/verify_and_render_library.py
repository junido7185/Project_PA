"""Open the generated library, verify it, and render source contact sheets.
No .blend is saved by this script. Runs in Blender's background process.
"""
import colorsys
import hashlib
import json
import math
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "Docs/AssetProvenance/Previews"
OUTPUT.mkdir(parents=True, exist_ok=True)
manifest = json.loads((ROOT / "Blender/Library/library-manifest.json").read_text(encoding="utf-8"))
scene = bpy.context.scene
scene.render.engine = "CYCLES"
scene.cycles.device = "CPU"
scene.cycles.samples = 16
scene.cycles.use_denoising = True
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"
scene.render.film_transparent = False
scene.world.use_nodes = True
scene.world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.65, 0.72, 0.8, 1)
scene.world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.5
scene.view_settings.view_transform = "Standard"
scene.view_settings.look = "None"

def fresh_material(name, rgba):
    material = bpy.data.materials.new(name)
    material.diffuse_color = rgba
    material.use_nodes = True
    node = material.node_tree.nodes.get("Principled BSDF")
    node.inputs["Base Color"].default_value = rgba
    node.inputs["Roughness"].default_value = 0.8
    return material

bpy.ops.mesh.primitive_plane_add(size=200, location=(0, 0, -0.012))
ground = bpy.context.object
ground.data.materials.append(fresh_material("PA_Audit_Ground", (0.84, 0.80, 0.71, 1)))
camera_data = bpy.data.cameras.new("PA_Audit_Camera")
camera = bpy.data.objects.new("PA_Audit_Camera", camera_data)
scene.collection.objects.link(camera)
camera.location = (4, -6, 4.5)
camera.rotation_euler = (Vector((0, 0, 0.85))-camera.location).to_track_quat("-Z", "Y").to_euler()
camera_data.type = "ORTHO"
camera_data.ortho_scale = 3.25
scene.camera = camera
light_data = bpy.data.lights.new("PA_Audit_Key", "AREA")
light = bpy.data.objects.new("PA_Audit_Key", light_data)
scene.collection.objects.link(light)
light.location = (-3, -4, 7)
light.rotation_euler = (-light.location).to_track_quat("-Z", "Y").to_euler()
light_data.energy = 500
light_data.size = 5

results = []
image_metrics = {}
for asset in manifest["assets"]:
    collection = bpy.data.collections.get(asset["id"])
    assert collection and collection.asset_data, "Missing asset collection: " + asset["id"]
    assert collection["pa_source_sha256"] == hashlib.sha256((ROOT / asset["librarySource"]).read_bytes()).hexdigest()
    assert collection["pa_gameplay_owner"] == asset["gameplayOwner"]
    for parent in (bpy.data.collections.get(asset["collection"]), collection):
        parent.hide_viewport = False
        parent.hide_render = False
    objects = list(collection.all_objects)
    meshes = [o for o in objects if o.type == "MESH"]
    assert meshes
    for obj in objects:
        obj.hide_render = obj.type in ("LIGHT", "CAMERA")
    root = bpy.data.objects.new("PA_Audit_Pose_" + asset["id"], None)
    scene.collection.objects.link(root)
    for obj in objects:
        if obj.parent not in objects:
            matrix = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = matrix
    bpy.context.view_layer.update()
    bounds = [o.matrix_world @ Vector(corner) for o in meshes for corner in o.bound_box]
    lo = Vector([min(v[a] for v in bounds) for a in range(3)])
    hi = Vector([max(v[a] for v in bounds) for a in range(3)])
    scale = 2 / max(hi-lo)
    root.scale *= scale
    root.location = -Vector(((hi.x+lo.x)/2, (hi.y+lo.y)/2, lo.z))*scale
    bpy.context.view_layer.update()
    # Source-space UV swatches, not whole-atlas unused colors.
    swatches = set()
    for obj in meshes:
        uv_layer = obj.data.uv_layers.active
        for poly in obj.data.polygons:
            material = obj.data.materials[poly.material_index] if len(obj.data.materials) else None
            if not material:
                raise RuntimeError("Missing material: " + asset["id"])
            node = material.node_tree.nodes.get("Principled BSDF") if material.use_nodes else None
            rgba = list(node.inputs["Base Color"].default_value) if node else list(material.diffuse_color)
            textures = [n.image for n in material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image] if material.use_nodes else []
            if textures:
                image = textures[0]
                assert image.packed_file, "Unpacked image: " + image.name
                if image.name not in image_metrics:
                    # Reopened packed images allocate their pixel buffer lazily.
                    pixels = list(image.pixels[:])
                    assert image.has_data and min(image.size) > 0 and pixels, "Unreadable packed image: " + image.name
                    image_metrics[image.name] = (pixels, tuple(image.size))
                pixels, size = image_metrics[image.name]
                if uv_layer:
                    for loop in poly.loop_indices:
                        uv = uv_layer.data[loop].uv
                        x, y = int((uv.x % 1)*size[0]), int((uv.y % 1)*size[1])
                        i = (y*size[0]+x)*4
                        swatches.add(tuple(round(v, 4) for v in pixels[i:i+3]))
            else:
                swatches.add(tuple(round(v, 4) for v in rgba[:3]))
    output = OUTPUT / (asset["id"] + ".png")
    scene.render.filepath = str(output)
    bpy.ops.render.render(write_still=True)
    results.append({"id": asset["id"], "preview": output.relative_to(ROOT).as_posix(),
                    "sampledColorCount": len(swatches),
                    "meanSampledSaturation": sum(colorsys.rgb_to_hsv(*c)[1] for c in swatches)/len(swatches) if swatches else 0,
                    "colorMeasurement": "unique source material colors or UV vertex texture samples; excludes lighting, not screen pixels",
                    "sourceForward": "source orientation retained; contact-sheet camera looks from Blender -Y/+X/+Z",
                    "previewNormalization": "longest source extent = 2m for comparison; Unity uses per-item target height"})
    collection.hide_render = True
    collection.hide_viewport = True

# Compose 6-column sheets with original renders; no source mesh/material mutation is saved.
for page in range(math.ceil(len(results)/12)):
    subset = results[page*12:(page+1)*12]
    width, height = 6*512, 2*512
    pixels = [0.92, 0.9, 0.86, 1.0]*(width*height)
    for index, result in enumerate(subset):
        image = bpy.data.images.load(str(ROOT / result["preview"]), check_existing=False)
        src = list(image.pixels[:])
        x, y = (index % 6)*512, (1-index//6)*512
        for row in range(512):
            start = ((y+row)*width+x)*4
            pixels[start:start+512*4] = src[row*512*4:(row+1)*512*4]
    sheet = bpy.data.images.new("ART000_Contact_"+str(page+1), width=width, height=height)
    sheet.pixels[:] = pixels
    sheet.filepath_raw = str(OUTPUT / ("contact-sheet-"+str(page+1)+".png"))
    sheet.file_format = "PNG"
    sheet.save()

(ROOT / "Docs/AssetProvenance/library-reopen-validation.json").write_text(json.dumps({
    "status": "PASS", "assets": results, "packedImageCount": len(image_metrics),
    "librarySha256": hashlib.sha256((ROOT / "Blender/Library/ProjectPA_AssetLibrary.blend").read_bytes()).hexdigest(),
    "renderEngine": "Cycles CPU, 16 samples", "renderScope": "source look and shape review; final Unity day/night shadows remain ART-001"}, indent=2)+"\n", encoding="utf-8")
print("ART000_LIBRARY_REOPEN_RENDER_PASS " + str(len(results)))

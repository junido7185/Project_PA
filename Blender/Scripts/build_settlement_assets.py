"""P3 two-asset kit. ART-000 library is loaded read-only; no P0 script execution."""
import bpy, json, hashlib, math, sys
from pathlib import Path
from mathutils import Vector, Matrix

ROOT = Path(__file__).resolve().parents[2]
LIB = ROOT / 'Blender/Library/ProjectPA_AssetLibrary.blend'
OUT = ROOT / 'Blender/Generated/VS_PRESENT_001_P3'
ART = ROOT / 'Assets/Art/ProjectPA/Derived/Settlement'
DOC = ROOT / 'Docs/AssetProvenance/VS_PRESENT_001/P3'
for p in (OUT, ART, DOC): p.mkdir(parents=True, exist_ok=True)
revision = sys.argv[sys.argv.index('--revision')+1] if '--revision' in sys.argv else '01'
blend = OUT / ('PA_FirstSettlement_r'+revision+'.blend')
assert not blend.exists(), 'Preserve earlier kit; choose a new revision'
library_hash = hashlib.sha256(LIB.read_bytes()).hexdigest()
assert library_hash == json.loads((ROOT/'Docs/AssetProvenance/library-reopen-validation.json').read_text(encoding='utf-8'))['librarySha256']
with bpy.data.libraries.load(str(LIB), link=False) as (available, loaded):
    # Real library mesh reuse in both hero structures, not merely a file existence check.
    names = [n for n in available.collections if 'CHEST_CLOSED' in n]
    assert names, 'Audited chest collection missing'
    loaded.collections = [names[0]]
chest = loaded.collections[0]
for o in list(bpy.context.scene.objects):
    for c in list(o.users_collection): c.objects.unlink(o)
scene=bpy.context.scene
scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
palette={'Wood':'A07850','WoodLight':'D4B896','Terracotta':'D4714A','Teal':'5BAFC0','Cream':'F5EAD5','Ink':'344B4E','Gold':'F5D76E'}
materials={}
for key,color in palette.items():
    def linear(v): return v/12.92 if v<.04045 else ((v+.055)/1.055)**2.4
    rgba=tuple(linear(int(color[i:i+2],16)/255) for i in (0,2,4))+(1,)
    m=bpy.data.materials.new('PA_'+key);m.diffuse_color=rgba;m.use_nodes=True
    node=m.node_tree.nodes.get('Principled BSDF');node.inputs['Base Color'].default_value=rgba
    node.inputs['Roughness'].default_value=.8;node.inputs['Metallic'].default_value=0
    materials[key]=m

def own(obj,name,mat):
    obj.name=name
    for c in list(obj.users_collection):c.objects.unlink(obj)
    current.objects.link(obj);obj.data.materials.clear();obj.data.materials.append(materials[mat]);return obj
def box(name,loc,scale,mat):
    bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=own(bpy.context.object,name,mat);o.scale=scale
    bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
    bevel=o.modifiers.new('Soft working edges','BEVEL');bevel.width=min(.045,min(scale)*.12);bevel.segments=2
    bpy.ops.object.modifier_apply(modifier=bevel.name);return o
def mesh(name,vertices,faces,mat):
    me=bpy.data.meshes.new(name);me.from_pydata(vertices,[],faces);me.update()
    o=bpy.data.objects.new(name,me);current.objects.link(o);me.materials.append(materials[mat]);return o
def sign(text,loc,size):
    bpy.ops.object.text_add(location=loc,rotation=(math.pi/2,0,0));o=bpy.context.object
    o.data.body=text;o.data.align_x='CENTER';o.data.size=size;o.data.extrude=.004
    bpy.ops.object.convert(target='MESH');return own(bpy.context.object,'PA_LogisticsMark','Cream')
def bounds(objects):
    points=[o.matrix_world@Vector(v) for o in objects if o.type=='MESH' for v in o.bound_box]
    return Vector(tuple(min(v[i] for v in points) for i in range(3))),Vector(tuple(max(v[i] for v in points) for i in range(3)))
def storage(loc):
    originals=[o for o in chest.all_objects if o.type=='MESH']
    lo,hi=bounds(originals);factor=.58/(hi.z-lo.z);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    for src in originals:
        o=src.copy();o.data=src.data.copy();o.matrix_world=Matrix.Identity(4);current.objects.link(o)
        for v in o.data.vertices:v.co=(src.matrix_world@v.co-center)*factor+Vector(loc)
        o.data.materials.clear();o.data.materials.append(materials['WoodLight'])
        for poly in o.data.polygons:poly.material_index=0
        o.name='LibraryStorage_'+src.name

assets=[]
for kind in ('PA_SettlementHub_Tier0','PA_StarterShelter'):
    current=bpy.data.collections.new(kind);scene.collection.children.link(current)
    if kind.endswith('Tier0'):
        box('Foundation',(0,0,.10),(3.6,3.3,.2),'WoodLight')
        for x in (-1.6,1.6):
            for y in (-1.4,1.4):box('AssemblyPost',(x,y,1.28),(.18,.18,2.36),'Wood')
        box('RearStorageRail',(0,1.42,.8),(3.25,.12,1.15),'Wood')
        box('OpenCounter',(-.65,-.65,.93),(1.45,.68,.16),'WoodLight')
        for x in (-1.22,-.08):box('CounterLeg',(x,-.65,.54),(.15,.48,.8),'Wood')
        box('ExpansionRail',(1.6,0,.45),(.15,2.9,.2),'Ink')
        # Broad asymmetric single-pitch canopy and exposed extensions.
        roof=box('Canopy',(0,0,2.54),(3.8,3.55,.18),'Teal');roof.rotation_euler.x=.10
        box('FrontLogisticsFascia',(0,-1.78,2.37),(3.8,.12,.35),'Terracotta')
        sign('P.A.  /  BASE 00',(0,-1.855,2.26),.26)
        storage((.83,.95,.2));storage((-.15,.95,.2))
        box('CargoBoard',(.75,.35,1.15),(.8,.12,.62),'Cream')
    else:
        box('Pallet',(0,0,.09),(3.25,3.15,.18),'WoodLight')
        # Shared A-frame shelter: open front; visibly different from the hub canopy.
        for side in (-1,1):
            mesh('Canvas',[(0,-1.43,2.35),(side*1.5,-1.43,.24),(side*1.5,1.43,.24),(0,1.43,2.35)],[(0,1,2,3)],'Cream')
            box('CanvasHem',(side*1.51,0,.26),(.12,3,.14),'Terracotta')
        mesh('BackCanvas',[(-1.5,1.42,.24),(1.5,1.42,.24),(0,1.42,2.35)],[(0,1,2)],'Terracotta')
        box('Ridge',(0,0,2.37),(.13,3.2,.13),'Wood')
        for x in (-1.5,1.5):box('TentPeg',(x,-1.5,.28),(.12,.12,.5),'Wood')
        box('Bedroll',(-.45,.25,.29),(.8,1.55,.22),'Teal')
        storage((.66,.6,.18))
        box('ProfessionBadge',(.8,-1.47,.8),(.63,.10,.32),'Terracotta')
        sign('P.A.',(.8,-1.53,.73),.20)
    bpy.context.view_layer.update()
    objects=list(current.objects)
    lo,hi=bounds(objects)
    assert lo.z>-.01 and lo.z<.03,(kind,lo)
    # Export an origin-rooted single mesh; source chest and ART library remain intact.
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
    o.name=kind
    for poly in o.data.polygons:poly.use_smooth=False
    path=ART/(kind+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
        apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')
    current.asset_mark();current.asset_data.description='P3 original modular structure with audited ART-000 storage prop'
    current['pa_library_source']=names[0]
    assets.append({'id':kind,'classification':'NEW hero structure + DERIVE audited library storage','sourceCollection':names[0],
        'dimensionsBlender':list(hi-lo),'footprintCells':[2,2],'footprintMeters':[4,4],'pivot':'ground center',
        'forward':'Blender -Y; Unity wrapper verifies -Z','interactionFace':'front opening -Z','navClearanceMeters':2,
        'collider':'Unity root box inside 4m footprint; separate external entrance cell','palette':palette,
        'roughness':.8,'metallic':0,'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),
        'fbx':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    current.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.resolution_x=1200;scene.render.resolution_y=900;scene.render.resolution_percentage=100
scene.world.color=(.65,.73,.78);scene.view_settings.view_transform='Standard'
current=bpy.data.collections.new('ReferenceOnly');scene.collection.children.link(current)
box('Ground',(0,0,-.12),(200,200,.2),'WoodLight')
bpy.ops.object.camera_add(location=(6,-9,6));cam=bpy.context.object;scene.camera=cam
cam.rotation_euler=(Vector((0,0,1))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6.9
bpy.ops.object.light_add(type='AREA',location=(-3,-4,7));light=bpy.context.object;light.data.energy=1500;light.data.shape='DISK';light.data.size=5
light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
for a in assets:
    collection=bpy.data.collections[a['id']];collection.hide_render=False
    scene.render.filepath=str(DOC/(a['id']+'_r'+revision+'.png'));bpy.ops.render.render(write_still=True)
    a['referenceRender']=str(Path(scene.render.filepath).relative_to(ROOT));collection.hide_render=True
assert hashlib.sha256(LIB.read_bytes()).hexdigest()==library_hash
(DOC/'derived-assets.json').write_text(json.dumps({'ticket':'VS-PRESENT-001-P3','librarySha256':library_hash,'libraryPreserved':True,
    'styleGrammar':'PROJECT_PA_ART_STYLE_GRAMMAR.md','sourceProvenance':'Docs/AssetProvenance/selected-assets.json / CubeWorldKit CC0',
    'blend':str(blend.relative_to(ROOT)),'assets':assets},indent=2)+'\n',encoding='utf-8')
print('P3_BLENDER_EXPORT_RENDER_PASS',flush=True)

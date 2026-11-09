"""P4 three starter worksite kits. ART-000 library is loaded read-only; no P0 script execution."""
import bpy, json, hashlib, math, sys
from pathlib import Path
from mathutils import Vector, Matrix


if '--manifest-only' in sys.argv:
    root=Path(__file__).resolve().parents[2]
    library=root/'Blender/Library/ProjectPA_AssetLibrary.blend'
    blend=root/'Blender/Generated/VS_PRESENT_001_P4/PA_StarterWorksites_r01.blend'
    bpy.ops.wm.open_mainfile(filepath=str(blend))
    entries=[]
    for role in ('Wood','Ore','Wheat'):
        name='PA_StarterWorksite_'+role
        mesh=bpy.data.objects[name]
        points=[mesh.matrix_world@Vector(v) for v in mesh.bound_box]
        dimensions=[max(v[i] for v in points)-min(v[i] for v in points) for i in range(3)]
        fbx=root/('Assets/Art/ProjectPA/Derived/Worksites/'+name+'.fbx')
        entries.append({'id':name,'role':role,'classification':'DERIVE ART-000 utility kit','dimensions':dimensions,'pivot':'ground center','forward':'Unity wrapper -Z','footprint':'1 cell / 2x2m','clearance':'front entrance cell 2m','collider':'low platform / trigger face; existing placement occupancy','materials':'P3 PA palette / roughness0.8 metallic0','fbx':str(fbx.relative_to(root)),'sha256':hashlib.sha256(fbx.read_bytes()).hexdigest(),'referenceRender':'Docs/AssetProvenance/VS_PRESENT_001/P4/'+name+'_r01.png'})
    payload={'ticket':'VS-PRESENT-001-P4','librarySha256':hashlib.sha256(library.read_bytes()).hexdigest(),'libraryPreserved':True,'styleGrammar':'PROJECT_PA_ART_STYLE_GRAMMAR.md','sourceProvenance':'ART-000 selected-assets.json / Quaternius CubeWorldKit + UltimateNature CC0','sourceCollections':[c.name for c in bpy.data.collections if c.name.startswith('PA_REF_')],'blend':str(blend.relative_to(root)),'assets':entries}
    assert payload['librarySha256']==json.loads((root/'Docs/AssetProvenance/library-reopen-validation.json').read_text())['librarySha256']
    (root/'Docs/AssetProvenance/VS_PRESENT_001/P4/derived-assets.json').write_text(json.dumps(payload,indent=2)+'\n',encoding='utf-8')
    print('P4_MANIFEST_RECOVERY_PASS; existing FBX/render/blend preserved',flush=True)
    sys.exit(0)

ROOT = Path(__file__).resolve().parents[2]
LIB = ROOT / 'Blender/Library/ProjectPA_AssetLibrary.blend'
OUT = ROOT / 'Blender/Generated/VS_PRESENT_001_P4'
ART = ROOT / 'Assets/Art/ProjectPA/Derived/Worksites'
DOC = ROOT / 'Docs/AssetProvenance/VS_PRESENT_001/P4'
for p in (OUT, ART, DOC): p.mkdir(parents=True, exist_ok=True)
revision = sys.argv[sys.argv.index('--revision')+1] if '--revision' in sys.argv else '01'
blend = OUT / ('PA_StarterWorksites_r'+revision+'.blend')
assert not blend.exists(), 'Preserve earlier kit; choose a new revision'
library_hash = hashlib.sha256(LIB.read_bytes()).hexdigest()
assert library_hash == json.loads((ROOT/'Docs/AssetProvenance/library-reopen-validation.json').read_text(encoding='utf-8'))['librarySha256']
with bpy.data.libraries.load(str(LIB), link=False) as (available, loaded):
    # Real library mesh reuse in both hero structures, not merely a file existence check.
    names = [n for n in available.collections if any(k in n for k in ('CHEST_CLOSED','WOODLOG','ROCK_1','AXE_STONE','PICKAXE_STONE'))]
    assert names, 'Audited chest collection missing'
    loaded.collections = names
sources={c.name:c for c in loaded.collections}
chest = next(c for n,c in sources.items() if 'CHEST_CLOSED' in n)
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


def library_prop(key,loc,height,mat):
    collection=next(c for n,c in sources.items() if key in n)
    originals=[o for o in collection.all_objects if o.type=='MESH'];lo,hi=bounds(originals)
    factor=height/max(hi.z-lo.z,.01);center=Vector(((lo.x+hi.x)/2,(lo.y+hi.y)/2,lo.z))
    for src in originals:
        o=src.copy();o.data=src.data.copy();o.matrix_world=Matrix.Identity(4);current.objects.link(o)
        for v in o.data.vertices:v.co=(src.matrix_world@v.co-center)*factor+Vector(loc)
        o.data.materials.clear();o.data.materials.append(materials[mat])
        for p in o.data.polygons:p.material_index=0
        o.name='Library_'+key

assets=[]
for role in ('Wood','Ore','Wheat'):
    kind='PA_StarterWorksite_'+role
    current=bpy.data.collections.new(kind);scene.collection.children.link(current)
    box('WorkingBorder',(0,0,.06),(1.76,1.76,.12),'WoodLight')
    box('PracticeGround',(0,0,.13),(1.56,1.56,.06),'Wood' if role=='Wheat' else 'Cream')
    for x in (-.75,.75):box('CornerPost',(x,.73,.31),(.14,.14,.5),'Terracotta')
    box('RoleBoard',(0,.74,.76),(1.20,.12,.35),'Teal' if role=='Wheat' else 'Terracotta')
    sign('P.A. / '+role.upper(),(0,.665,.72),.15)
    if role=='Wood':
        for x in (-.42,0,.42):library_prop('WOODLOG',(x,.12,.18),.50,'Wood')
        library_prop('AXE_STONE',(-.50,-.49,.18),.7,'Ink')
    elif role=='Ore':
        library_prop('ROCK_1',(-.23,.1,.18),.72,'Ink')
        library_prop('PICKAXE_STONE',(.46,-.42,.18),.72,'Wood')
        box('SortingTray',(.47,.37,.24),(.50,.50,.16),'Teal')
    else:
        for x in (-.5,0,.5):box('Furrow',(x,-.04,.19),(.23,1.0,.055),'WoodLight')
        library_prop('CHEST_CLOSED',(.52,.46,.19),.34,'WoodLight')
        box('SeedPacket',(-.5,.42,.25),(.2,.32,.10),'Gold')
    bpy.context.view_layer.update();objects=list(current.objects);lo,hi=bounds(objects)
    assert abs(lo.z)<.01 and hi.x-lo.x<=1.9 and hi.y-lo.y<=1.9,(kind,lo,hi)
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object
    scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR');o.name=kind
    path=ART/(kind+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,add_leaf_bones=False,bake_anim=False)
    assets.append({'id':kind,'role':role,'classification':'DERIVE ART-000 utility kit','sourceCollections':list(sources),'dimensions':list(hi-lo),'pivot':'ground center','forward':'Unity wrapper -Z','footprint':'1 cell / 2x2m','clearance':'front entrance cell 2m','collider':'low platform; existing placement owns occupancy','palette':palette,'roughness':.8,'metallic':0,'fbx':str(path.relative_to(ROOT)),'sha256':hashlib.sha256(path.read_bytes()).hexdigest()})
    current.hide_render=True
bpy.ops.wm.save_as_mainfile(filepath=str(blend))
scene.render.engine='CYCLES';scene.cycles.samples=16;scene.cycles.use_denoising=True
scene.render.resolution_x=1000;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.65,.73,.78);scene.view_settings.view_transform='Standard'
current=bpy.data.collections.new('ReferenceOnly');scene.collection.children.link(current)
box('Ground',(0,0,-.12),(200,200,.2),'WoodLight')
bpy.ops.object.camera_add(location=(3,-5,4));cam=bpy.context.object;scene.camera=cam
cam.rotation_euler=(Vector((0,0,.4))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=3.2
bpy.ops.object.light_add(type='AREA',location=(-3,-4,7));light=bpy.context.object;light.data.energy=1400;light.data.size=5
light.rotation_euler=(-light.location).to_track_quat('-Z','Y').to_euler()
for a in assets:
    collection=bpy.data.collections[a['id']];collection.hide_render=False
    scene.render.filepath=str(DOC/(a['id']+'_r'+revision+'.png'));bpy.ops.render.render(write_still=True)
    a['referenceRender']=str(Path(scene.render.filepath).relative_to(ROOT));collection.hide_render=True
assert hashlib.sha256(LIB.read_bytes()).hexdigest()==library_hash
(DOC/'derived-assets.json').write_text(json.dumps({'ticket':'VS-PRESENT-001-P4','librarySha256':library_hash,'libraryPreserved':True,'styleGrammar':'PROJECT_PA_ART_STYLE_GRAMMAR.md','sourceProvenance':'ART-000 selected-assets.json / Quaternius CubeWorldKit + UltimateNature CC0','blend':str(blend.relative_to(ROOT)),'assets':assets},indent=2)+'\n',encoding='utf-8')
print('P4_BLENDER_EXPORT_RENDER_PASS',flush=True)

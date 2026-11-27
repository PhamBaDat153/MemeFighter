"""Convert a ZBrush FBX export into a game-ready fighter model.
python3 convert.py <Name> <input.fbx> <outdir>
 - applies transforms, scales with one shared factor (keeps relative sizes between characters)
 - decimates heavy subtools (budget ~25k tris)
 - bakes textures + polypaint into one 2048 atlas on a new UV set
 - exports <Name>.fbx (+ <Name>_Albedo.png) facing -Y (Unity +Z), feet at origin
 - renders <Name>_Portrait.png (transparent) and <Name>_Preview.png
"""
import bpy, sys, os, math
from mathutils import Vector

NAME, SRC, OUT = sys.argv[-3], sys.argv[-2], sys.argv[-1]
os.makedirs(OUT, exist_ok=True)
SCALE = 25.0            # ZBrush units -> meters (~1.15-1.4 m tall characters)
BIG_CAP, PART_CAP = 16000, 7000
ATLAS = 2048

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
sc = bpy.context.scene
meshes = [o for o in sc.objects if o.type == 'MESH']

def activate(o):
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

# ---------------------------------------------------------------- transforms
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
for o in meshes:
    for v in o.data.vertices:
        mn = Vector(map(min, mn, v.co)); mx = Vector(map(max, mx, v.co))
center = (mn + mx) / 2
for o in meshes:
    for v in o.data.vertices:
        v.co = Vector(((v.co.x - center.x) * SCALE, (v.co.y - center.y) * SCALE, (v.co.z - mn.z) * SCALE))
    o.data.update()
height = (mx.z - mn.z) * SCALE
print("HEIGHT", round(height, 3))

# ---------------------------------------------------------------- clean normals (imported custom normals break after decimation)
for o in meshes:
    activate(o)
    if o.data.has_custom_normals:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    bpy.ops.object.shade_smooth()

# ---------------------------------------------------------------- decimate
counts = {o.name: sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes}
biggest = max(counts, key=counts.get)
for o in meshes:
    t = counts[o.name]
    cap = BIG_CAP if o.name == biggest else PART_CAP
    if t > cap:
        activate(o)
        d = o.modifiers.new("Dec", 'DECIMATE'); d.ratio = cap / t
        bpy.ops.object.modifier_apply(modifier=d.name)
    bpy.ops.object.select_all(action='DESELECT')

# ---------------------------------------------------------------- materials read from the original UVs / polypaint
for o in meshes:
    me = o.data
    if me.uv_layers:
        me.uv_layers[0].name = "Orig"
    else:
        me.uv_layers.new(name="Orig")
    mat = me.materials[0] if me.materials else None
    img_node = None
    if mat and mat.use_nodes:
        img_node = next((n for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image), None)
    if img_node is not None and len(me.uv_layers) and me.uv_layers["Orig"].data and any(True for _ in me.uv_layers["Orig"].data):
        uvn = mat.node_tree.nodes.new("ShaderNodeUVMap"); uvn.uv_map = "Orig"
        mat.node_tree.links.new(uvn.outputs["UV"], img_node.inputs["Vector"])
    else:
        m = bpy.data.materials.new(o.name + "_vc"); m.use_nodes = True
        bsdf = m.node_tree.nodes["Principled BSDF"]
        if me.color_attributes:
            vc = m.node_tree.nodes.new("ShaderNodeVertexColor"); vc.layer_name = me.color_attributes[0].name
            m.node_tree.links.new(vc.outputs["Color"], bsdf.inputs["Base Color"])
        me.materials.clear(); me.materials.append(m)
    for m in me.materials:
        if m and m.use_nodes and "Principled BSDF" in m.node_tree.nodes:
            m.node_tree.nodes["Principled BSDF"].inputs["Metallic"].default_value = 0.0

# ---------------------------------------------------------------- join + atlas UVs
bpy.ops.object.select_all(action='DESELECT')
for o in meshes: o.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
obj = bpy.context.active_object
obj.name = NAME
obj.data.name = NAME + "Mesh"
me = obj.data
print("TRIS", sum(len(p.vertices) - 2 for p in me.polygons), "mats", len(me.materials))

atlas_uv = me.uv_layers.new(name="Atlas")
me.uv_layers.active = atlas_uv
activate(obj)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.003)
bpy.ops.object.mode_set(mode='OBJECT')

img = bpy.data.images.new(NAME + "_Albedo", ATLAS, ATLAS)
for m in me.materials:
    n = m.node_tree.nodes.new("ShaderNodeTexImage"); n.image = img; n.name = "BakeTarget"
    m.node_tree.nodes.active = n
sc.render.engine = 'CYCLES'; sc.cycles.device = 'CPU'; sc.cycles.samples = 1
sc.render.bake.use_pass_direct = False; sc.render.bake.use_pass_indirect = False; sc.render.bake.use_pass_color = True
sc.render.bake.margin = 6
bpy.ops.object.bake(type='DIFFUSE')
img.filepath_raw = os.path.join(OUT, NAME + "_Albedo.png"); img.file_format = 'PNG'; img.save()

# single final material on the atlas
final = bpy.data.materials.new(NAME); final.use_nodes = True
tex = final.node_tree.nodes.new("ShaderNodeTexImage"); tex.image = img
bsdf = final.node_tree.nodes["Principled BSDF"]
final.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.7
bsdf.inputs["Sheen Weight"].default_value = 0.1
me.uv_layers.remove(me.uv_layers["Orig"])
me.materials.clear(); me.materials.append(final)
for p in me.polygons: p.material_index = 0
for ca in list(me.color_attributes): me.color_attributes.remove(ca)
activate(obj); bpy.ops.object.shade_smooth()

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, NAME + ".blend"))
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT, NAME + ".fbx"), use_selection=True, object_types={'MESH'},
                         apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                         mesh_smooth_type='FACE', path_mode='COPY', embed_textures=False)

# ---------------------------------------------------------------- renders
sc.cycles.samples = 48; sc.cycles.use_denoising = True
sc.view_settings.view_transform = 'Standard'
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs["Color"].default_value = (0.12, 0.11, 0.2, 1)
w.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.6
def light(n, loc, e, size, col=(1, 1, 1), aim=0.7):
    d = bpy.data.lights.new(n, 'AREA'); d.energy = e; d.size = size; d.color = col
    o = bpy.data.objects.new(n, d); sc.collection.objects.link(o); o.location = loc
    o.rotation_euler = (Vector((0, 0, aim)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()
light("Key", (-1.5, -2.4, 2.5), 170, 2); light("Fill", (2.1, -1.8, 1.3), 60, 2.5, (0.85, 0.9, 1)); light("Rim", (0.8, 2.3, 2.3), 170, 1.5, (1, 0.85, 0.6))
cd = bpy.data.cameras.new("C"); cam = bpy.data.objects.new("C", cd); sc.collection.objects.link(cam); sc.camera = cam

def shoot(path, target, dist, ang, lens, res, transparent):
    cd.lens = lens
    sc.render.resolution_x = sc.render.resolution_y = res
    sc.render.film_transparent = transparent
    a = math.radians(ang)
    cam.location = target + Vector((-math.sin(a) * dist, -math.cos(a) * dist, 0.1))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)

PH = float(os.environ.get("PORTRAIT_H", "0.7")); PD = float(os.environ.get("PORTRAIT_D", "2.4"))
shoot(os.path.join(OUT, NAME + "_Portrait.png"), Vector((0, 0, height * PH)), PD, -22, 85, 512, True)
shoot(os.path.join(OUT, NAME + "_Preview.png"), Vector((0, 0, height * 0.5)), 4.6, -30, 50, 480, False)
print("DONE", NAME)

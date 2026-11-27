"""Rig, bake and export the corgi.  python3 rig_bake_export.py <outdir>"""
import bpy, sys, os, math
from mathutils import Vector

OUT = sys.argv[-1]
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "corgi_parts.blend"))
scene = bpy.context.scene

def ctx_obj(o):
    bpy.ops.object.mode_set(mode='OBJECT') if bpy.context.object and bpy.context.object.mode != 'OBJECT' else None
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o

# ---------------------------------------------------------------- armature (Unity Humanoid names)
from mathutils import Euler
H = 0.14   # must match build_corgi.py
BONES = [
    # name, head, tail, parent, connect     (A-pose, matches build_corgi.py v4)
    ("Hips", (0, 0, 0.2), (0, 0, 0.38), None, False),
    ("Spine", (0, 0, 0.38), (0, 0, 0.55), "Hips", True),
    ("Chest", (0, 0, 0.55), (0, 0, 0.72), "Spine", True),
    ("Neck", (0, 0, 0.72), (0, 0, 0.82), "Chest", True),
    ("Head", (0, 0, 0.82), (0, 0, 1.26), "Neck", True),
    ("Tail", (0, 0.22, 0.26), (0, 0.4, 0.28), "Hips", False),
]
EAR_TILT = {"Left": (0.05, 0.38, 0.2), "Right": (-0.1, -0.52, -0.28)}
for side, s in (("Left", 1), ("Right", -1)):
    ear_base = Vector((0.16 * s, 0.02, 1.0 + H))
    ear_tip = ear_base + Euler(EAR_TILT[side]).to_matrix() @ Vector((0, 0, 0.36))
    BONES += [
        (f"{side}Shoulder", (0.06 * s, 0, 0.74), (0.21 * s, 0, 0.77), "Chest", False),
        (f"{side}UpperArm", (0.21 * s, 0, 0.77), (0.34 * s, -0.016, 0.62), f"{side}Shoulder", True),
        (f"{side}LowerArm", (0.34 * s, -0.016, 0.62), (0.47 * s, -0.033, 0.48), f"{side}UpperArm", True),
        (f"{side}Hand", (0.47 * s, -0.033, 0.48), (0.56 * s, -0.045, 0.35), f"{side}LowerArm", True),
        (f"{side}UpperLeg", (0.15 * s, 0, 0.2), (0.15 * s, -0.005, 0.12), "Hips", False),
        (f"{side}LowerLeg", (0.15 * s, -0.005, 0.12), (0.15 * s, 0, 0.06), f"{side}UpperLeg", True),
        (f"{side}Foot", (0.15 * s, 0, 0.06), (0.15 * s, -0.12, 0.03), f"{side}LowerLeg", True),
        (f"{side}Toes", (0.15 * s, -0.12, 0.03), (0.15 * s, -0.2, 0.03), f"{side}Foot", True),
        (f"{side}Ear", tuple(ear_base), tuple(ear_tip), "Head", False),
    ]

arm_data = bpy.data.armatures.new("CorgiRig")
rig = bpy.data.objects.new("CorgiRig", arm_data)
scene.collection.objects.link(rig)
ctx_obj(rig)
bpy.ops.object.mode_set(mode='EDIT')
eb = arm_data.edit_bones
for name, h, t, parent, connect in BONES:
    b = eb.new(name)
    b.head = h; b.tail = t
    b.roll = 0
for name, h, t, parent, connect in BONES:
    if parent:
        eb[name].parent = eb[parent]
        eb[name].use_connect = connect
bpy.ops.object.mode_set(mode='OBJECT')

# ---------------------------------------------------------------- weights
body = bpy.data.objects["Body"]
ctx_obj(body)
rig.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.object.parent_set(type='ARMATURE_AUTO')
# ears/tail bones should not pull on the body
for g in ("LeftEar", "RightEar"):
    if g in body.vertex_groups:
        body.vertex_groups.remove(body.vertex_groups[g])
print("body groups:", len(body.vertex_groups))

# face + ear parts: rigid to Head / ears, then join into one mesh
others = [o for o in scene.objects if o.type == 'MESH' and o.name != "Body"]
for o in others:                     # small parts don't need many polygons
    if len(o.data.polygons) > 200:
        d = o.modifiers.new("Dec", 'DECIMATE'); d.ratio = 0.4
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=d.name)
for o in others:
    if o.name.startswith("Ear"):
        bone = "LeftEar" if o.name.endswith("L") else "RightEar"
    elif o.name in ("Collar", "Tag", "TagRing"):
        bone = "Chest"
    else:
        bone = "Head"
    vg = o.vertex_groups.new(name=bone)
    vg.add(list(range(len(o.data.vertices))), 1.0, 'REPLACE')

bpy.ops.object.select_all(action='DESELECT')
for o in others: o.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = body
bpy.ops.object.join()
corgi = bpy.context.active_object
corgi.name = "Corgi"
corgi.data.name = "CorgiMesh"
# make sure the armature modifier is there (join keeps body's)
if not any(m.type == 'ARMATURE' for m in corgi.modifiers):
    m = corgi.modifiers.new("Armature", 'ARMATURE'); m.object = rig
corgi.parent = rig
print("tris:", sum(len(p.vertices) - 2 for p in corgi.data.polygons))

# ---------------------------------------------------------------- UVs + bake vertex colors to a texture
ctx_obj(corgi)
bpy.ops.object.mode_set(mode='EDIT')
bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.004)
bpy.ops.object.mode_set(mode='OBJECT')

img = bpy.data.images.new("Corgi_Albedo", 1024, 1024)
img.colorspace_settings.name = 'sRGB'
for mat in corgi.data.materials:
    nt = mat.node_tree
    node = nt.nodes.new("ShaderNodeTexImage"); node.image = img; node.name = "BakeTarget"
    nt.nodes.active = node

scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 1
scene.render.bake.use_pass_direct = False
scene.render.bake.use_pass_indirect = False
scene.render.bake.use_pass_color = True
scene.render.bake.margin = 8
bpy.ops.object.bake(type='DIFFUSE')
img.filepath_raw = os.path.join(OUT, "Corgi_Albedo.png")
img.file_format = 'PNG'
img.save()

# swap vertex-color input for the baked texture
for mat in corgi.data.materials:
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    tex = nt.nodes["BakeTarget"]
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    for n in [n for n in nt.nodes if n.type == 'VERTEX_COLOR']:
        nt.nodes.remove(n)
corgi.data.color_attributes.remove(corgi.data.color_attributes["Col"])

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "BufferingCorgi.blend"))

# ---------------------------------------------------------------- FBX for Unity
bpy.ops.object.select_all(action='DESELECT')
rig.select_set(True); corgi.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=os.path.join(OUT, "BufferingCorgi.fbx"),
    use_selection=True, object_types={'ARMATURE', 'MESH'},
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
    bake_space_transform=True, add_leaf_bones=False, bake_anim=False,
    mesh_smooth_type='FACE', path_mode='COPY', embed_textures=True,
    armature_nodetype='NULL', use_armature_deform_only=True,
)
print("exported")

# ---------------------------------------------------------------- posed copy for preview (idle: arms down)
pb = rig.pose.bones
from mathutils import Quaternion
ctx_obj(rig)
bpy.ops.object.mode_set(mode='POSE')
def rot_world(name, axis, deg):
    b = pb[name]; b.rotation_mode = 'QUATERNION'
    la = (b.bone.matrix_local.to_3x3().inverted() @ Vector(axis)).normalized()
    b.rotation_quaternion = Quaternion(la, math.radians(deg))
rot_world("Head", (0, 1, 0), 9)
rot_world("RightEar", (0, 1, 0), 18)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "corgi_posed_preview.blend"), copy=True)
print("done")

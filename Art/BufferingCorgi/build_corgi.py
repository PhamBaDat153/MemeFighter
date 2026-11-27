"""Builds 'Buffering Corgi' v5 - Party-Animals-style plush corgi with meme derp face.
Run: python3 build_corgi.py <outdir>
Blender coords: Z up, character faces -Y, character's left = +X. ~1.2 m tall. Arms in A-pose.
"""
import bpy, math, sys, os, random
from mathutils import Vector, Quaternion, Euler

OUT = sys.argv[-1] if len(sys.argv) > 1 else "out"
os.makedirs(OUT, exist_ok=True)
random.seed(7)

def srgb(h):
    h = h.lstrip('#')
    c = [int(h[i:i+2], 16) / 255 for i in (0, 2, 4)]
    lin = [x / 12.92 if x <= 0.04045 else ((x + 0.055) / 1.055) ** 2.4 for x in c]
    return (*lin, 1.0)

ORANGE = srgb("#F3A12A")      # golden orange
ORANGE_DK = srgb("#E27E16")   # deeper orange on the back / lower body
CREAM = srgb("#F6EEDA")
NOSE = srgb("#151212")
PINK = srgb("#F7D9C8")
TONGUE = srgb("#E8697A")
EYE_W = srgb("#FFFFFF")
PUPIL = srgb("#1A100A")
BROW = srgb("#8A4A12")
COLLAR = srgb("#C8202A")
GOLD = srgb("#D9A441")

def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def mix(c1, c2, t):
    return tuple(c1[i] * (1 - t) + c2[i] * t for i in range(4))

bpy.ops.wm.read_factory_settings(use_empty=True)

def apply_mods(o):
    bpy.context.view_layer.objects.active = o
    for m in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)

# ================================================================ body (union of soft shapes)
blob = []

def ell(co, r, sx, sy, sz, rot=None, store=blob):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=32, ring_count=16, radius=1, location=co)
    o = bpy.context.active_object
    o.scale = (r * sx, r * sy, r * sz)
    if rot is not None:
        o.rotation_mode = 'QUATERNION'; o.rotation_quaternion = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    store.append(o)
    return o

def capsule(a, b, r, store=blob):
    a, b = Vector(a), Vector(b)
    half = (b - a).length / 2
    q = Vector((0, 0, 1)).rotation_difference((b - a).normalized())
    return ell((a + b) / 2, 1.0, r, r, half + r * 0.7, rot=q, store=store)

def clump(base, direction, length, radius):
    d = Vector(direction).normalized()
    ell(Vector(base) + d * (length * 0.5), 1.0, radius, radius * 0.8, length * 0.6,
        rot=Vector((0, 0, 1)).rotation_difference(d))

H = 0.14   # extra body height: everything above the torso moves up by this

# tall plush bean: widest near the bottom, narrower at the shoulders
ell((0, 0.0, 0.40), 0.335, 1.0, 0.9, 1.15)
ell((0, 0.0, 0.66), 0.27, 1.0, 0.92, 0.85)
ell((0, -0.07, 0.36), 0.315, 1.0, 1.0, 1.02)         # big round belly
ell((0, -0.12, 0.33), 0.22, 1.0, 0.9, 1.0)           # belly pushed forward
# head: big round cranium, full rounded cheeks, short broad snout blended into the face
ell((0, -0.01, 0.89 + H), 0.30, 1.1, 0.95, 0.88)     # cranium
ell((0.14, -0.09, 0.80 + H), 0.16, 1.05, 0.95, 0.85)  # cheeks
ell((-0.14, -0.09, 0.80 + H), 0.16, 1.05, 0.95, 0.85)
ell((0, -0.2, 0.80 + H), 0.14, 1.05, 1.0, 0.74)      # snout
for s in (1, -1):
    ell((0.047 * s, -0.295, 0.772 + H), 0.056, 1.0, 0.8, 0.75)      # muzzle pads (dog "w" mouth)
    ell((0.26 * s, -0.03, 0.81 + H), 0.075, 1.0, 0.9, 0.9)          # scalloped fluffy cheeks
    ell((0.285 * s, 0.0, 0.755 + H), 0.068, 1.0, 0.9, 0.9)
    ell((0.26 * s, 0.04, 0.705 + H), 0.06, 1.0, 0.9, 0.9)
ell((0, -0.05, 1.08 + H), 0.17, 1.1, 0.9, 0.6)                       # forehead dome
# thick one-piece arms growing out of the shoulders, rounded ends (no separate paw)
for s in (1, -1):
    capsule((0.21 * s, 0.0, 0.77), (0.52 * s, -0.04, 0.43), 0.135)
# tiny feet poking out at the bottom
for s in (1, -1):
    capsule((0.15 * s, 0, 0.13), (0.15 * s, 0, 0.07), 0.1)
    ell((0.15 * s, -0.08, 0.05), 0.115, 0.95, 1.25, 0.5)
# little fluffy tail
ell((0, 0.28, 0.26), 0.09, 0.9, 1.0, 0.9)
for k in range(5):
    a = k * 2 * math.pi / 5
    clump((0, 0.3, 0.27), (math.cos(a) * 0.6, 1, math.sin(a) * 0.6 + 0.1), 0.1, 0.045)

bpy.ops.object.select_all(action='DESELECT')
for o in blob: o.select_set(True)
bpy.context.view_layer.objects.active = blob[0]
bpy.ops.object.join()
body = bpy.context.active_object
body.name = "Body"
rm = body.modifiers.new("Remesh", 'REMESH'); rm.mode = 'VOXEL'; rm.voxel_size = 0.01
sm = body.modifiers.new("Smooth", 'SMOOTH'); sm.factor = 0.75; sm.iterations = 10
dec = body.modifiers.new("Decimate", 'DECIMATE'); dec.ratio = 0.07
apply_mods(body)
bpy.ops.object.shade_smooth()
print("body faces", len(body.data.polygons))

# ================================================================ separate parts
parts = {}

def register(o, name, subsurf=0):
    o.name = name
    if subsurf:
        m = o.modifiers.new("Sub", 'SUBSURF'); m.levels = subsurf
        apply_mods(o)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.shade_smooth()
    parts[name] = o
    return o

def sphere(name, loc, scale, rot=None):
    tmp = []
    return register(ell(loc, 1.0, *scale, rot=rot, store=tmp), name)

def cone(base, direction, length, radius, verts=10):
    d = Vector(direction).normalized()
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=0.0, depth=length)
    o = bpy.context.active_object
    o.rotation_mode = 'QUATERNION'
    o.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(d)
    o.location = Vector(base) + d * (length / 2)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return o

def tuft(store, base, direction, length, radius, count=3, spread=0.35):
    d = Vector(direction).normalized()
    side = d.cross(Vector((0, 0, 1)))
    if side.length < 0.1: side = d.cross(Vector((1, 0, 0)))
    side.normalize()
    for i in range(count):
        t = (i - (count - 1) / 2) / max(1, (count - 1) / 2)
        dd = (d + side * t * spread + Vector([random.uniform(-.08, .08) for _ in range(3)])).normalized()
        ll = length * (1.0 - 0.25 * abs(t)) * random.uniform(0.9, 1.1)
        store.append(cone(Vector(base) + side * t * radius * 1.2, dd, ll, radius))

def join_named(objs, name, subsurf=1):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    return register(bpy.context.active_object, name, subsurf)

def on_ellipsoid(center, radii, u):
    u = Vector(u).normalized()
    return Vector(center) + Vector((radii[0] * u.x, radii[1] * u.y, radii[2] * u.z)), u

# ---------------------------------------------------------------- derp eyes looking in opposite directions
EYES = {
    "L": ((0.13, -0.238, 0.905 + H), (0.082, 0.05, 0.087), (0.65, -1.0, 0.55)),     # looks out + up
    "R": ((-0.125, -0.238, 0.9 + H), (0.09, 0.053, 0.095), (-0.6, -1.0, -0.5)),  # looks out + down
}
for nm, (c, r, look) in EYES.items():
    sphere(f"Eye{nm}", c, r)
    p, n = on_ellipsoid(c, r, look)
    sphere(f"Pupil{nm}", p - n * 0.008, (0.046, 0.02, 0.048), rot=Vector((0, -1, 0)).rotation_difference(n))
    sh, n2 = on_ellipsoid(c, r, Vector(look) + Vector((0.25, -0.2, 0.35)))
    sphere(f"Shine{nm}", sh, (0.012, 0.005, 0.012), rot=Vector((0, -1, 0)).rotation_difference(n2))

# eyelids: orange fur shells over the top of each eye, cut at a tilted lid line
#   (name, lid opening above center as a fraction of eye height, tilt in degrees; inner corners raised = worried)
LIDS = {"L": (0.55, -14), "R": (0.15, 18)}
for nm, (open_h, tilt) in LIDS.items():
    c, r, look = EYES[nm]
    tmp = []
    lid = ell(c, 1.0, r[0] * 1.13, r[1] * 1.2, r[2] * 1.13, store=tmp)
    bpy.ops.mesh.primitive_cube_add(size=1, location=(c[0], c[1], c[2] + r[2] * open_h - 0.5))
    cut = bpy.context.active_object
    cut.rotation_euler = (0, math.radians(tilt), 0)
    bm = lid.modifiers.new("Lid", 'BOOLEAN'); bm.operation = 'DIFFERENCE'; bm.object = cut; bm.solver = 'EXACT'
    apply_mods(lid)
    bpy.data.objects.remove(cut)
    lid.data.materials.clear()
    register(lid, f"Lid{nm}")

# BIG glossy nose, blep tongue, mouth
nose = sphere("Nose", (0, -0.35, 0.85 + H), (0.1, 0.062, 0.068))
for v in nose.data.vertices:            # mesh coords are local to the nose center
    zn = max(-1.0, min(1.0, v.co.z / 0.068))
    v.co.x *= 0.8 + 0.3 * zn                                  # wider at the top
    v.co.y += 0.012 * (1 - zn * zn)                           # slightly flatter front
sphere("NoseShine", (0.03, -0.405, 0.875 + H), (0.024, 0.008, 0.011))
sphere("Philtrum", (0, -0.335, 0.79 + H), (0.006, 0.006, 0.026))
sphere("MouthL", (0.046, -0.315, 0.738 + H), (0.04, 0.011, 0.009), rot=Quaternion((0, 1, 0), -0.4))
sphere("MouthR", (-0.046, -0.315, 0.738 + H), (0.04, 0.011, 0.009), rot=Quaternion((0, 1, 0), 0.4))

# ---------------------------------------------------------------- big rounded ears with fluffy cream insides
def ear(name, base, tilt, scale):
    bpy.ops.mesh.primitive_cone_add(vertices=20, radius1=1, radius2=0.28, depth=2)
    o = bpy.context.active_object
    o.scale = scale
    bpy.ops.object.transform_apply(scale=True)
    o.location = (0, 0, scale[2]); bpy.ops.object.transform_apply(location=True)
    o.rotation_euler = tilt; o.location = base
    bpy.ops.object.transform_apply(location=True, rotation=True)
    return register(o, name, 2)

EARS = {"L": ((0.16, 0.02, 1.0 + H), (0.05, 0.38, 0.2)), "R": ((-0.16, 0.02, 1.0 + H), (-0.1, -0.52, -0.28))}
for nm, (base, tilt) in EARS.items():
    ear(f"Ear{nm}", base, tilt, (0.17, 0.065, 0.2))
    ear(f"EarInner{nm}", Vector(base) + Vector((0, -0.035, 0.02)), tilt, (0.11, 0.035, 0.15))
    fl = []
    up = Euler(tilt).to_matrix() @ Vector((0, 0, 1))
    tuft(fl, Vector(base) + Vector((0, -0.06, 0.03)), up + Vector((0, -0.6, 0)), 0.13, 0.026, count=3, spread=0.45)
    join_named(fl, f"EarFluff{nm}")

# messy cowlick
fur = []
tuft(fur, (0.02, -0.03, 1.1 + H), (0.15, 0.2, 1), 0.15, 0.04, count=5, spread=0.8)
join_named(fur, "FurTufts")

# ---------------------------------------------------------------- red collar + gold tag
bpy.ops.mesh.primitive_torus_add(major_radius=0.245, minor_radius=0.032, major_segments=48, minor_segments=12,
                                 location=(0, -0.01, 0.78), rotation=(math.radians(-6), 0, 0))
col = bpy.context.active_object
col.scale = (1.0, 0.92, 0.75)
bpy.ops.object.transform_apply(scale=True)
register(col, "Collar")
bpy.ops.mesh.primitive_cylinder_add(vertices=24, radius=0.05, depth=0.018, location=(0, -0.265, 0.715),
                                    rotation=(math.radians(80), 0, 0))
register(bpy.context.active_object, "Tag")
bpy.ops.mesh.primitive_torus_add(major_radius=0.018, minor_radius=0.006, location=(0, -0.257, 0.768),
                                 rotation=(0, math.radians(90), 0))
register(bpy.context.active_object, "TagRing")

# ================================================================ colors
def paint(obj, fn):
    me = obj.data
    attr = me.color_attributes.new("Col", 'FLOAT_COLOR', 'POINT')
    mw = obj.matrix_world
    for v in me.vertices:
        attr.data[v.index].color = fn(mw @ v.co)

def body_color(p):
    x, y, z = p
    ax = abs(x)
    c = mix(ORANGE, ORANGE_DK, 0.45 * smoothstep(0.4, 0.05, z) + 0.3 * smoothstep(0.0, 0.3, y))
    hz = z - H                       # head rules in head space
    head = hz > 0.70
    front = smoothstep(-0.04, -0.16, y)
    blaze_w = 0.024 + 0.06 * smoothstep(1.05, 0.88, hz)
    blaze = smoothstep(blaze_w + 0.02, blaze_w, ax) * front if head else 0
    muzzle = smoothstep(0.88, 0.83, hz) * smoothstep(-0.12, -0.19, y) * smoothstep(0.22, 0.15, ax) if head else 0
    chin = smoothstep(0.78, 0.72, hz) * front if head else 0
    cheek = smoothstep(0.83, 0.75, hz) * smoothstep(0.17, 0.25, ax) * smoothstep(0.06, -0.08, y) if head else 0
    bx, bz = ax / 0.22, (z - 0.42) / 0.38
    belly = smoothstep(1.08, 0.92, math.sqrt(bx * bx + bz * bz)) * smoothstep(0.02, -0.12, y) if not head else 0
    paws = smoothstep(0.43, 0.48, ax) * smoothstep(0.5, 0.42, z) * (1 if z > 0.12 else 0)
    tail = smoothstep(0.3, 0.36, y) * smoothstep(0.42, 0.32, z)
    w = max(belly, muzzle, blaze, chin, cheek, paws, tail)
    return mix(c, CREAM, w)

paint(body, body_color)
flat = {
    "Nose": NOSE, "NoseShine": EYE_W, "MouthL": NOSE, "MouthR": NOSE, "Philtrum": NOSE,
    "EyeL": EYE_W, "EyeR": EYE_W, "PupilL": PUPIL, "PupilR": PUPIL,
    "ShineL": EYE_W, "ShineR": EYE_W, "LidL": ORANGE_DK, "LidR": ORANGE_DK,
    "EarInnerL": PINK, "EarInnerR": PINK, "EarFluffL": CREAM, "EarFluffR": CREAM,
    "Collar": COLLAR, "Tag": GOLD, "TagRing": GOLD,
}
for name, o in parts.items():
    if name in flat:
        paint(o, lambda p, c=flat[name]: c)
    elif name.startswith("Ear"):
        paint(o, lambda p: ORANGE)
    else:
        paint(o, body_color)

# ================================================================ materials (vertex color x fur streak noise)
def make_mat(name, rough, sheen, fur_noise, metal=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    ca = nt.nodes.new("ShaderNodeVertexColor"); ca.layer_name = "Col"
    if fur_noise:
        tc = nt.nodes.new("ShaderNodeTexCoord")
        mp = nt.nodes.new("ShaderNodeMapping"); mp.inputs["Scale"].default_value = (1.0, 1.0, 0.22)
        nz = nt.nodes.new("ShaderNodeTexNoise"); nz.inputs["Scale"].default_value = 90.0; nz.inputs["Detail"].default_value = 6.0
        mr = nt.nodes.new("ShaderNodeMapRange")
        mr.inputs["From Min"].default_value = 0.35; mr.inputs["From Max"].default_value = 0.65
        mr.inputs["To Min"].default_value = 0.82; mr.inputs["To Max"].default_value = 1.05
        mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = 'RGBA'; mul.blend_type = 'MULTIPLY'
        mul.inputs["Factor"].default_value = 1.0
        nt.links.new(tc.outputs["Object"], mp.inputs["Vector"])
        nt.links.new(mp.outputs["Vector"], nz.inputs["Vector"])
        nt.links.new(nz.outputs["Fac"], mr.inputs["Value"])
        nt.links.new(ca.outputs["Color"], mul.inputs["A"])
        nt.links.new(mr.outputs["Result"], mul.inputs["B"])
        nt.links.new(mul.outputs["Result"], bsdf.inputs["Base Color"])
    else:
        nt.links.new(ca.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Sheen Weight"].default_value = sheen
    bsdf.inputs["Sheen Roughness"].default_value = 0.5
    bsdf.inputs["Metallic"].default_value = metal
    return m

fur_mat = make_mat("CorgiFur", 0.85, 0.8, True)
gloss = make_mat("CorgiGloss", 0.12, 0.0, False)
accessory = make_mat("CorgiCollar", 0.35, 0.0, False)
glossy = {"Nose", "NoseShine", "EyeL", "EyeR", "PupilL", "PupilR", "ShineL", "ShineR"}
acc = {"Collar", "Tag", "TagRing"}
body.data.materials.append(fur_mat)
for name, o in parts.items():
    o.data.materials.append(gloss if name in glossy else accessory if name in acc else fur_mat)

bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT, "corgi_parts.blend"))
print("saved parts", len(parts))

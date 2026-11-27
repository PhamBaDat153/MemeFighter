"""Render preview shots of a .blend: python3 render.py <blend> <outprefix> [views]"""
import bpy, sys, math, os
from mathutils import Vector

blend, prefix = sys.argv[1], sys.argv[2]
views = sys.argv[3].split(",") if len(sys.argv) > 3 else ["front", "threequarter", "side", "back"]
bpy.ops.wm.open_mainfile(filepath=blend)
scene = bpy.context.scene
scene.render.engine = 'CYCLES'
scene.cycles.device = 'CPU'
scene.cycles.samples = 48
scene.cycles.use_denoising = True
scene.render.resolution_x = 720
scene.render.resolution_y = 720
scene.view_settings.view_transform = 'Standard'

world = bpy.data.worlds.new("W") if not scene.world else scene.world
scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.12, 0.11, 0.2, 1)
world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.35

def light(name, kind, loc, energy, size, color=(1, 1, 1)):
    d = bpy.data.lights.new(name, kind); d.energy = energy; d.color = color
    if kind == 'AREA': d.size = size
    o = bpy.data.objects.new(name, d); scene.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = (Vector((0, 0, 0.7)) - Vector(loc)).to_track_quat('-Z', 'Y').to_euler()

light("Key", 'AREA', (-1.6, -2.4, 2.4), 160, 2.0)
light("Fill", 'AREA', (2.2, -1.8, 1.2), 50, 2.5, (0.85, 0.9, 1.0))
light("Rim", 'AREA', (0.8, 2.4, 2.2), 170, 1.5, (1.0, 0.85, 0.6))

# ground disc
bpy.ops.mesh.primitive_circle_add(vertices=64, radius=1.4, fill_type='NGON', location=(0, 0, 0))
g = bpy.context.active_object
gm = bpy.data.materials.new("Ground"); gm.use_nodes = True
gm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.2, 0.17, 0.38, 1)
g.data.materials.append(gm)

cam_d = bpy.data.cameras.new("Cam"); cam_d.lens = 60
cam = bpy.data.objects.new("Cam", cam_d); scene.collection.objects.link(cam)
scene.camera = cam
target = Vector((0, 0, 0.75))
dist = 4.4
angles = {"front": 0, "threequarter": -35, "side": -90, "back": 180, "gameview": -70}
for v in views:
    a = math.radians(angles[v])
    cam.location = target + Vector((math.sin(a) * dist * -1, -math.cos(a) * dist, 0.25))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    scene.render.filepath = f"{prefix}_{v}.png"
    bpy.ops.render.render(write_still=True)
    print("wrote", scene.render.filepath)

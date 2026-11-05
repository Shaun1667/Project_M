# 정면 정사영 렌더 (blender_rig_from_joints.py의 관절 찍기용). 실행: blender --background --python Tools/blender_front_render.py -- <FBX 폴더> <출력 폴더>  (names 목록을 고쳐서)
import bpy, json, math, os, sys
from mathutils import Vector
src, out = sys.argv[sys.argv.index("--")+1:][:2]
names = ["Boss_IceWitch","Enemy_IceGolem","Enemy_MagmaGolem","Boss_LavaLord","Enemy_ClockworkSoldier","Enemy_TimeAfterimage","Enemy_TempleGuardian","Boss_TimeGuardian"]
info = {}
for n in names:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(src, n + ".fbx"))
    mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
    bpy.context.view_layer.update()
    pts = [mesh.matrix_world @ v.co for v in mesh.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    c = (mn + mx) / 2
    size = max(mx.x - mn.x, mx.z - mn.z) * 1.05
    sc = bpy.context.scene
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.display.shading.color_type = 'TEXTURE'
    sc.display.shading.light = 'FLAT'
    sc.render.resolution_x = sc.render.resolution_y = 800
    cd = bpy.data.cameras.new("C"); cd.type = 'ORTHO'; cd.ortho_scale = size
    cam = bpy.data.objects.new("C", cd); sc.collection.objects.link(cam)
    # 앞쪽(-Y)에서 +Y를 봄
    cam.location = (c.x, mn.y - 3, c.z); cam.rotation_euler = (math.radians(90), 0, 0)
    sc.camera = cam
    sc.render.filepath = os.path.join(out, n + ".png")
    bpy.ops.render.render(write_still=True)
    info[n] = {"cx": c.x, "cz": c.z, "size": size, "min": list(mn), "max": list(mx)}
    # 뒤에서 본 것도 한 장 (얼굴 방향 확인용은 생략)
json.dump(info, open(os.path.join(out, "info.json"), "w"), indent=1)
print("RESULT ok")

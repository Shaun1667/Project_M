# 슬라임 소녀 어깨 고치기 (2026-11-05, 사용자 요청 — Mixamo 자동 리깅의 어깨가 팔 가운데쯤이라 위팔이 4 cm였음)
# 위팔 시작을 몸 옆(0.094 m) · 팔꿈치를 위팔 = 아래팔 길이(0.233 m)로 옮기고, 팔 둘레 0.055 m 안 정점의 팔 무게를 x 위치로 다시 나눔.
# 팔 밖 정점(옆 머리카락)의 팔 · 아래팔 · 손 무게는 Spine2로 (팔을 따라 늘어나지 않게). Mixamo 다른 무게는 그대로.
# 실행: blender --background --python Tools/blender_slime_shoulder.py  (SRC = Mixamo 원본 리깅 FBX — git 9fc22c7의 Boss_SlimeGirl_Rigged.fbx, OUT = 고친 FBX)
import bpy, json, math
from mathutils import Vector
from mathutils.geometry import intersect_point_line
SRC = r"Assets/_Project/Art/Varco3D/Boss_SlimeGirl_Rigged_mixamo.fbx"
OUT = r"Assets/_Project/Art/Varco3D/Boss_SlimeGirl_Rigged.fbx"
SH, EL, WR = 0.094, 0.233, 0.341   # 새 어깨 · 팔꿈치, 손목 안쪽 (몸 중심에서 m)
BL, R = 0.03, 0.055                # 관절 섞는 폭, 팔 둘레
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=SRC)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
toA = arm.matrix_world.inverted() @ mesh.matrix_world
out = {}
lines = {}
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='EDIT')
eb = arm.data.edit_bones
for side in ("Left", "Right"):
    up, fo, ha, cl = (eb[f"mixamorig:{side}{n}"] for n in ("Arm","ForeArm","Hand","Shoulder"))
    up.head.x = math.copysign(SH, up.head.x); cl.tail = up.head.copy()
    fo.head.x = math.copysign(EL, fo.head.x); up.tail = fo.head.copy(); fo.tail = ha.head.copy()
    lines[side] = (up.head.copy(), ha.head.copy())
bpy.ops.object.mode_set(mode='OBJECT')
vg = {g.name: g for g in mesh.vertex_groups}
spine = vg["mixamorig:Spine2"]
stat = {"arm": 0, "hair_moved": 0}
for side, s in (("Left", 1), ("Right", -1)):
    cl_n, up_n, fo_n = (f"mixamorig:{side}{n}" for n in ("Shoulder", "Arm", "ForeArm"))
    hand_pre = f"mixamorig:{side}Hand"
    a0, a1 = lines[side]
    for v in mesh.data.vertices:
        p = toA @ v.co
        x = p.x * s
        if x < SH - BL or x > WR: continue
        ws = {mesh.vertex_groups[g.group].name: g.weight for g in v.groups}
        moving = {n: w for n, w in ws.items() if n in (up_n, fo_n) or n.startswith(hand_pre)}
        chain = sum(moving.values()) + ws.get(cl_n, 0)
        if chain <= 0: continue
        q, _ = intersect_point_line(p, a0, a1)
        if (p - q).length > R:
            # 팔 밖 (옆 머리카락 등): 팔 · 아래팔 · 손 무게를 가슴으로 — 팔을 따라 늘어나지 않게
            if not moving: continue
            for n, w in moving.items(): vg[n].remove([v.index])
            spine.add([v.index], ws.get("mixamorig:Spine2", 0) + sum(moving.values()), 'REPLACE')
            stat["hair_moved"] += 1
            continue
        if x < SH + BL: t = (x - (SH - BL)) / (2*BL); new = {cl_n: 1-t, up_n: t}
        elif x < EL - BL: new = {up_n: 1}
        elif x < EL + BL: t = (x - (EL - BL)) / (2*BL); new = {up_n: 1-t, fo_n: t}
        else: new = {fo_n: 1}
        for n in list(ws):
            if n == cl_n or n in moving: vg[n].remove([v.index])
        for n, w in new.items():
            if w > 0: vg[n].add([v.index], w * chain, 'REPLACE')
        stat["arm"] += 1
out.update(stat)
bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=OUT, use_selection=True, add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=True, object_types={'ARMATURE','MESH'})
print("RESULT " + json.dumps(out))

# Mixamo가 리깅하지 못한 사람형(2026-11-05: 얼음 마녀 · 골렘 2 · 용암 군주 · 시계병 · 잔상 · 수호상 · 시간의 수호자)을 Blender에서 리깅한다.
# 1) Tools/blender_front_render.py로 정면 렌더(800px, info.json = 화면 → 월드) 2) 그림에서 관절 픽셀을 찍어 Tools/blender_rig_joints.json에 (왼쪽 = 그림 오른쪽, 오른쪽은 대칭)
# 3) 실행: blender --background --python Tools/blender_rig_from_joints.py -- <원본 FBX 폴더> <출력 폴더> Tools/blender_rig_joints.json <info.json> [모델 이름...]
# 뼈 이름은 Mixamo(손가락 없음 25뼈) — Unity Humanoid가 그대로 잡고, 무게는 뼈 선분까지 거리(가까운 3뼈, 1/d^4).
# 정면 렌더에서 찍은 관절(픽셀, 왼쪽 = 그림 오른쪽)로 Mixamo 이름 뼈대(25뼈)를 세우고, 뼈 선분까지 거리로 무게(가까운 3뼈, 1/d^4)를 칠해 FBX로 내보낸다.
import bpy, json, os, sys
from mathutils import Vector
from mathutils.geometry import intersect_point_line
args = sys.argv[sys.argv.index("--")+1:]
src, outdir, jfile, ifile = args[:4]
only = args[4:] if len(args) > 4 else None
J = json.load(open(jfile)); INFO = json.load(open(ifile))
res = {}
for name, pix in J.items():
    if only and name not in only: continue
    inf = INFO[name]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(src, name + ".fbx"))
    mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
    bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); bpy.context.view_layer.objects.active = mesh
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    if mesh.parent: mesh.parent = None
    verts = [v.co.copy() for v in mesh.data.vertices]
    size, cx, cz = inf["size"], inf["cx"], inf["cz"]
    def w(p, side=1):
        x = cx + (p[0] - 400) / 800 * size * side if side == 1 else cx - (p[0] - 400) / 800 * size
        z = cz + (400 - p[1]) / 800 * size
        r = 0.035 * size
        ys = [v.y for v in verts if abs(v.x - x) < r and abs(v.z - z) < r]
        y = (min(ys) + max(ys)) / 2 if ys else 0.0
        return Vector((x, y, z))
    P = {k: w(v) for k, v in pix.items()}
    R = {k: w(v, -1) for k, v in pix.items() if k in ("Clav","Shoulder","Elbow","Wrist","HandEnd","UpLeg","Knee","Ankle","Toe")}
    for d in (P, R):  # 발끝은 발목보다 앞(-Y)
        d["ToeTip"] = Vector((d["Toe"].x, d["Ankle"].y - 0.12 * size, d["Toe"].z))
        d["Toe"] = Vector((d["Toe"].x, d["Ankle"].y - 0.07 * size, d["Toe"].z))
    adata = bpy.data.armatures.new("Armature"); arm = bpy.data.objects.new("Armature", adata)
    bpy.context.scene.collection.objects.link(arm)
    bpy.context.view_layer.objects.active = arm; bpy.ops.object.mode_set(mode='EDIT')
    eb = adata.edit_bones
    def bone(n, h, t, parent=None):
        b = eb.new("mixamorig:" + n); b.head = h; b.tail = t if (t - h).length > 1e-4 else h + Vector((0, 0, 0.02))
        if parent: b.parent = eb["mixamorig:" + parent]
        return b
    bone("Hips", P["Hips"], P["Spine"])
    bone("Spine", P["Spine"], P["Spine1"], "Hips"); bone("Spine1", P["Spine1"], P["Spine2"], "Spine"); bone("Spine2", P["Spine2"], P["Neck"], "Spine1")
    bone("Neck", P["Neck"], P["Head"], "Spine2"); bone("Head", P["Head"], P["HeadTop"], "Neck")
    bone("HeadTop_End", P["HeadTop"], P["HeadTop"] + Vector((0, 0, 0.03 * size)), "Head")
    for side, d in (("Left", P), ("Right", R)):
        bone(side + "Shoulder", d["Clav"], d["Shoulder"], "Spine2"); bone(side + "Arm", d["Shoulder"], d["Elbow"], side + "Shoulder")
        bone(side + "ForeArm", d["Elbow"], d["Wrist"], side + "Arm"); bone(side + "Hand", d["Wrist"], d["HandEnd"], side + "ForeArm")
        bone(side + "UpLeg", d["UpLeg"], d["Knee"], "Hips"); bone(side + "Leg", d["Knee"], d["Ankle"], side + "UpLeg")
        bone(side + "Foot", d["Ankle"], d["Toe"], side + "Leg"); bone(side + "ToeBase", d["Toe"], d["ToeTip"], side + "Foot")
        bone(side + "Toe_End", d["ToeTip"], d["ToeTip"] + Vector((0, -0.02 * size, 0)), side + "ToeBase")
    segs = [(b.name, b.head.copy(), b.tail.copy()) for b in eb if not b.name.endswith("_End")]
    bpy.ops.object.mode_set(mode='OBJECT')
    # 무게: 뼈 선분까지 거리 → 가까운 3개, 1/d^4
    for g in list(mesh.vertex_groups): mesh.vertex_groups.remove(g)
    groups = {n: mesh.vertex_groups.new(name=n) for n, _, _ in segs}
    for v in mesh.data.vertices:
        ds = []
        for n, h, t in segs:
            q, f = intersect_point_line(v.co, h, t); f = max(0.0, min(1.0, f))
            ds.append(((v.co - (h + (t - h) * f)).length, n))
        ds.sort(); top = ds[:3]
        ws = [(1.0 / (d + 0.01 * size) ** 4, n) for d, n in top]; s = sum(x for x, _ in ws)
        for x, n in ws:
            if x / s > 0.02: groups[n].add([v.index], x / s, 'REPLACE')
    mesh.parent = arm
    mod = mesh.modifiers.new("Armature", 'ARMATURE'); mod.object = arm
    bpy.ops.object.select_all(action='DESELECT'); mesh.select_set(True); arm.select_set(True); bpy.context.view_layer.objects.active = arm
    out = os.path.join(outdir, name + "_Rigged.fbx")
    bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=True)
    res[name] = {"bones": len(segs), "verts": len(verts)}
print("RESULT " + json.dumps(res))

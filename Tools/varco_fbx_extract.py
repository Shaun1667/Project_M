"""Varco FBX 받기 + 묻힌 텍스처 꺼내기 (3-39). FBX에 PNG가 2장(base_color · normal) 묻혀 있고 이름이 모든 모델에서 같으므로
<모델>_base_color.png · <모델>_normal.png로 꺼내 Textures/에 둔다 (VarcoModelSetup.EnsureVarcoMaterial이 읽는 이름).
노멀 맵은 평균 색이 푸른 쪽(파랑 > 빨강 · 초록)으로 가린다.
사용: python varco_fbx_extract.py <urls.json: {모델 이름: fbx url}> <Varco3D 폴더>"""
import json, os, struct, sys, urllib.request
from io import BytesIO
from PIL import Image


def pngs(data):
    out, i = [], 0
    while True:
        i = data.find(b'\x89PNG\r\n\x1a\n', i)
        if i < 0: return out
        j = i + 8
        while j + 8 <= len(data):  # 청크를 따라가 IEND 끝까지
            length, kind = struct.unpack('>I4s', data[j:j + 8])
            j += 12 + length
            if kind == b'IEND': break
        out.append(data[i:j])
        i = j


def is_normal(png):
    im = Image.open(BytesIO(png)).convert('RGB').resize((32, 32))
    r = g = b = 0
    for pr, pg, pb in im.getdata(): r += pr; g += pg; b += pb
    return b > r * 1.2 and b > g * 1.2


urls = json.load(open(sys.argv[1], encoding='utf-8'))
root = sys.argv[2]
os.makedirs(os.path.join(root, 'Textures'), exist_ok=True)
for model, url in urls.items():
    fbx = os.path.join(root, model + '.fbx')
    if not os.path.exists(fbx): urllib.request.urlretrieve(url, fbx)
    data = open(fbx, 'rb').read()
    images = pngs(data)
    if len(images) < 2:
        print(model, '텍스처', len(images), '장 — 확인 필요'); continue
    normal = next((p for p in images if is_normal(p)), images[1])
    color = next((p for p in images if p is not normal), images[0])
    open(os.path.join(root, 'Textures', model + '_base_color.png'), 'wb').write(color)
    open(os.path.join(root, 'Textures', model + '_normal.png'), 'wb').write(normal)
    print(model, 'fbx', len(data) // 1024, 'KB, 텍스처', len(images))

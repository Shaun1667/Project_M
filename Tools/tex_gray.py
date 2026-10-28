"""던전 타일 텍스처를 회색 돌 빛깔로 (2026-10-28, 사용자 "너무 황토색 → 회색 빛깔").
원본(Docs/Varco3D_src)에서 매번 다시 만들므로 여러 번 돌려도 결과가 같다.
채도를 낮추고(남길 채도 비율) 살짝 차가운 색을 곱한다. 밝기(명도)는 그대로.
사용: python tex_gray.py [남길 채도 = 0.25] [차가운 정도 = 0.04]"""
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SRC = ROOT / 'Docs' / 'Varco3D_src'
DST = ROOT / 'Assets' / '_Project' / 'Art' / 'Varco3D' / 'Textures'
NAMES = ['Floor', 'Wall', 'Pillar', 'StairsDown', 'StairsUp']

keep = float(sys.argv[1]) if len(sys.argv) > 1 else 0.25
cool = float(sys.argv[2]) if len(sys.argv) > 2 else 0.04
tint = (1 - cool, 1.0, 1 + cool)  # 빨강 조금 빼고 파랑 조금 더함

for n in NAMES:
    name = f'Tile_{n}_base_color.png'
    im = Image.open(SRC / name)
    alpha = im.getchannel('A') if im.mode == 'RGBA' else None
    rgb = im.convert('RGB')
    gray = rgb.convert('L').convert('RGB')
    out = Image.blend(gray, rgb, keep)
    r, g, b = out.split()
    out = Image.merge('RGB', [c.point(lambda v, k=k: min(255, round(v * k))) for c, k in zip((r, g, b), tint)])
    if alpha is not None:
        out.putalpha(alpha)
    out.save(DST / name)
    print(name, 'keep', keep, 'cool', cool)

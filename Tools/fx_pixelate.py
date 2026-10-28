"""3D 이펙트 렌더(검정 배경 f_00~15.png) → 도트 프레임 <이름>_0~15.png.
줄이기(평균) → 밝기로 투명(가산 혼합이라 검정 = 투명) → 전체 프레임 공통 팔레트로 색 수 제한(디더 없음).
3D 레퍼런스를 Varco로 다시 그리기 전 밑그림 · 비교용 (Docs/PixelTest/fx_src/README.md "3D 레퍼런스").
사용: python fx_pixelate.py <렌더 폴더> <이름> <출력 폴더> [한 변 px = 64] [색 수 = 16] [마지막 몇 장 사라지게 = 0]"""
import sys
from PIL import Image

src, name, outdir = sys.argv[1], sys.argv[2], sys.argv[3]
size = int(sys.argv[4]) if len(sys.argv) > 4 else 64
colors = int(sys.argv[5]) if len(sys.argv) > 5 else 16
fade = int(sys.argv[6]) if len(sys.argv) > 6 else 0
DARK = 28

frames = [Image.open(f'{src}/f_{i:02}.png').convert('RGB').resize((size, size), Image.Resampling.BOX) for i in range(16)]
# 마지막 fade장은 밝기를 점점 줄여 사라지게 (3D 효과가 시간에 잘려 끝나던 것)
for j in range(fade):
    i = 16 - fade + j
    k = 1 - (j + 1) / (fade + 1)
    frames[i] = frames[i].point(lambda v: int(v * k))
# 공통 팔레트: 전 프레임을 붙인 그림에서 고름
strip = Image.new('RGB', (size * 16, size))
for i, f in enumerate(frames): strip.paste(f, (i * size, 0))
pal = strip.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
for i, f in enumerate(frames):
    q = f.quantize(palette=pal, dither=Image.Dither.NONE).convert('RGB')
    out = Image.new('RGBA', (size, size))
    po, pq = out.load(), q.load()
    for y in range(size):
        for x in range(size):
            c = pq[x, y]
            po[x, y] = (0, 0, 0, 0) if max(c) < DARK else (*c, 255)
    out.save(f'{outdir}/{name}_{i}.png')
print(f'{name}: 16 x {size}px, {colors} colors -> {outdir}')

"""이펙트 4장(Art/Pixel/Fx/<이름>_0~3) → 16프레임 시트용 참고 그림 (1024², 검정 배경, 4 × 4칸).
기존 4장을 키프레임으로 0 · 5 · 10 · 15번 칸(대각선 — 같은 간격)에 정수 배로 키워 놓고 나머지 칸은 비운다.
Varco EditImage가 빈 칸에 사이 프레임을 그리게 한다 (Docs/PixelTest/fx_src/README.md "16프레임").
사용: python fx_keys16.py <Fx 폴더> <이름> <출력.png> [시트 크기 = 1024 — 256이면 키우지 않음, Varco 올리기용으로 파일이 작음]"""
import sys
from PIL import Image

GRID = 4
KEY_CELLS = [0, 5, 10, 15]

fxdir, name, out = sys.argv[1], sys.argv[2], sys.argv[3]
SIZE = int(sys.argv[4]) if len(sys.argv) > 4 else 1024
frames = [Image.open(f'{fxdir}/{name}_{i}.png').convert('RGBA') for i in range(4)]
cell = SIZE // GRID
w = max(f.width for f in frames); h = max(f.height for f in frames)
k = max(1, (cell - SIZE // 64) // max(w, h))  # 칸 안에 여백, 정수 배 (픽셀 격자 유지)
sheet = Image.new('RGBA', (SIZE, SIZE), (0, 0, 0, 255))
for f, idx in zip(frames, KEY_CELLS):
    big = f.resize((f.width * k, f.height * k), Image.Resampling.NEAREST)
    cx = (idx % GRID) * cell + (cell - big.width) // 2
    cy = (idx // GRID) * cell + (cell - big.height) // 2
    sheet.alpha_composite(big, (cx, cy))
sheet.convert('RGB').save(out)
print(f'{name}: frame {w}x{h} x{k} -> {out}')

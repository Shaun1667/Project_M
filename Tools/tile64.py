"""격자 맞춤 AI 타일 → 정확히 64×64 타일. 1024px 그림을 64×64 칸(16px)으로 나눠 칸 가운데 영역의 가장 흔한 색을 집는다 (평균 금지 — 중간색 없음).
타일은 칸 수가 정확히 64여야 하므로 칸 크기를 찾지 않고 그림 크기 ÷ 64로 고정한다.
사용: python tile64.py <in.png> <out.png> [T=64]"""
import sys
from collections import Counter
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
T = int(sys.argv[3]) if len(sys.argv) > 3 else 64
im = Image.open(src).convert('RGB')
W, H = im.size
cw, ch = W / T, H / T
px = im.load()
out = Image.new('RGBA', (T, T))
op = out.load()
for r in range(T):
    for c in range(T):
        x0 = int(c * cw + cw * 0.3); x1 = max(x0 + 1, int(c * cw + cw * 0.7))
        y0 = int(r * ch + ch * 0.3); y1 = max(y0 + 1, int(r * ch + ch * 0.7))
        col = Counter(px[x, y] for y in range(y0, y1) for x in range(x0, x1)).most_common(1)[0][0]
        op[c, r] = col + (255,)
out.save(dst)
print('ok', dst)

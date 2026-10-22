"""격자 맞춤 AI 픽셀 그림 → 진짜 64px 스프라이트. 칸 크기 · 시작 위치를 찾아(칸 안 색 차이가 가장 작은 값) 칸마다 가운데 영역의 가장 흔한 색을 집는다.
평균을 내지 않으므로 중간색이 생기지 않는다. 사용: python grid64.py <in.png> <out64.png> [T=64]"""
import sys
from collections import Counter
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
T = int(sys.argv[3]) if len(sys.argv) > 3 else 64
im = Image.open(src).convert('RGB')
W, H = im.size
small = im.resize((W // 2, H // 2), Image.Resampling.NEAREST)  # 점수 계산만 빠르게
px = small.load()

def score(p, ox, oy):
    # 칸 경계 안쪽에서 이웃 픽셀 색 차이 합 (작을수록 칸 안이 한 색)
    s = 0; n = 0
    for y in range(oy, small.height - 1, 3):
        for x in range(ox, small.width - 1, 3):
            if (x - ox) % p == p - 1 or (y - oy) % p == p - 1: continue
            a = px[x, y]; b = px[x + 1, y]; c = px[x, y + 1]
            s += sum(abs(a[i] - b[i]) for i in range(3)) + sum(abs(a[i] - c[i]) for i in range(3)); n += 1
    return s / max(n, 1)

best = None
for p2 in [x / 2 for x in range(12, 21)]:  # 원본에서 칸 12~20px (절반 크기에서 6~10)
    for ox in range(int(p2)):
        for oy in range(0, int(p2), 2):
            sc = score(int(round(p2)), ox, oy)
            if best is None or sc < best[0]: best = (sc, p2 * 2, ox * 2, oy * 2)
_, cell, ox, oy = best
full = im.load()
cols = int((W - ox) // cell); rows = int((H - oy) // cell)
out = Image.new('RGBA', (cols, rows))
op = out.load()
for r in range(rows):
    for c in range(cols):
        x0 = int(ox + c * cell + cell * 0.3); x1 = int(ox + c * cell + cell * 0.7)
        y0 = int(oy + r * cell + cell * 0.3); y1 = int(oy + r * cell + cell * 0.7)
        cnt = Counter(full[x, y] for y in range(y0, max(y0 + 1, y1)) for x in range(x0, max(x0 + 1, x1)))
        col = cnt.most_common(1)[0][0]
        rr, gg, bb = col
        green = gg > 110 and gg - max(rr, bb) > 45  # 배경 초록 + 가장자리에 섞인 초록 (캐릭터에 진초록이 생기면 기준을 다시 볼 것)
        if green: op[c, r] = (0, 0, 0, 0)
        elif gg - max(rr, bb) > 15 and max(rr, bb) < 30: op[c, r] = (18, 16, 22, 255)  # 외곽선에 배경 초록이 섞인 칸 → 외곽선 색
        else: op[c, r] = (rr, gg, bb, 255)
box = out.getbbox()
spr = out.crop(box)
if spr.height > T - 1 or spr.width > T:  # 칸이 64보다 많이 잡힌 경우만 가장 가까운 칸으로 줄임
    s = (T - 2) / max(spr.size)
    spr = spr.resize((max(1, round(spr.width * s)), max(1, round(spr.height * s))), Image.Resampling.NEAREST)
canvas = Image.new('RGBA', (T, T))
canvas.paste(spr, ((T - spr.width) // 2, T - spr.height - 1), spr)
canvas.save(dst)
print(f'cell {cell:.1f}px offset ({ox},{oy}) grid {cols}x{rows} sprite {spr.size}')

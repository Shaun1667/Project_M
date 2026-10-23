"""AI 그림 한 장(초록 배경) → 정해진 크기 캔버스의 픽셀 스프라이트 (2026-10-23 적 · 보스).
키를 HEIGHT px로(가로가 넘치면 가로에) 맞춰 줄이고(칸 가운데 색 — 평균 안 함), 가로 가운데 · 발끝 = 아래에서 2줄 위. 초록은 투명, 초록 낀 외곽선은 외곽선 색.
사용: python sprite_fit.py <in.png> <out.png> <canvas px> <height px> [green|magenta]
(초록 몸 — 초록 슬라임 등 — 은 마젠타 #FF00FF 배경으로 뽑고 magenta를 준다)"""
import sys
from collections import Counter
from PIL import Image


BG = 'green'


def is_green(c):
    """배경색인지 (이름은 처음 쓰던 초록 그대로 — BG가 magenta면 마젠타)."""
    r, g, b = c[:3]
    if BG == 'magenta':
        return r > 150 and b > 150 and r - g > 80 and b - g > 80
    return g > 110 and g - max(r, b) > 45


def bbox(img):
    w, h = img.size
    px = img.load()
    xs, ys = [], []
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            if not is_green(px[x, y]): xs.append(x); ys.append(y)
    return min(xs), min(ys), max(xs), max(ys)


def fit(src, canvas, height):
    img = Image.open(src).convert('RGB')
    w, h = img.size
    bb = bbox(img)
    s = max((bb[3] - bb[1]) / height, (bb[2] - bb[0]) / (canvas - 4))  # 키를 맞추되 가로가 캔버스를 넘으면 가로에 맞춤 (꼬리 잘림 방지)
    cx = (bb[0] + bb[2]) / 2
    base = canvas - 2
    px = img.load()
    out = Image.new('RGBA', (canvas, canvas), (0, 0, 0, 0))
    op = out.load()
    for oy in range(canvas):
        for ox in range(canvas):
            sx = cx + (ox + 0.5 - canvas / 2) * s
            sy = bb[3] + (oy + 0.5 - (base + 1)) * s
            x0, x1 = int(sx - s * 0.2), int(sx + s * 0.2) + 1
            y0, y1 = int(sy - s * 0.2), int(sy + s * 0.2) + 1
            if x1 <= 0 or y1 <= 0 or x0 >= w or y0 >= h: continue
            pts = [px[x, y] for y in range(max(0, y0), min(h, y1)) for x in range(max(0, x0), min(w, x1))]
            c = Counter(pts).most_common(1)[0][0]
            r, g, b = c
            if is_green(c): continue
            if BG == 'green' and g - max(r, b) > 15 and max(r, b) < 30: c = (18, 16, 22)  # 초록 낀 외곽선
            if BG == 'magenta' and min(r, b) - g > 15 and g < 30: c = (18, 16, 22)  # 마젠타 낀 외곽선
            op[ox, oy] = tuple(c) + (255,)
    return out


if __name__ == '__main__':
    src, dst, canvas, height = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4])
    if len(sys.argv) > 5: BG = sys.argv[5]
    fit(src, canvas, height).save(dst)
    print('ok', dst)

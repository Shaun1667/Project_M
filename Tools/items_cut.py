"""아이템 아이콘 시트(AI, 단색 배경 · 칸 격자) → 아이템 스프라이트 (2026-10-26).
시트 하나는 같은 배율: 가장 큰 아이템의 긴 변 → MAX px. 칸마다 그림 가운데를 64px 캔버스 가운데에. 색은 칸 가운데 색(평균 안 함).
주문서 12룬: 빈 주문서 그림 위에 룬 획을 그린다(AI가 룬 글자를 정확히 못 그림 — 3D 때와 같은 결정, Docs/소모품_에셋.md 3-1).
사용: python items_cut.py <out_dir> <sheet.png> <cols> <rows> <green|magenta> <name,name,...>   (이름 '-'은 건너뜀)
      python items_cut.py <out_dir> --runes <blank_scroll.png>   (scroll_<rune>.png 12장)"""
import os, sys
from collections import Counter
from PIL import Image

T, MAX = 64, 40
BG = 'green'


def is_bg(c):
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
            if not is_bg(px[x, y]): xs.append(x); ys.append(y)
    return (min(xs), min(ys), max(xs), max(ys)) if xs else None


def sample(cell, bb, s):
    px = cell.load()
    w, h = cell.size
    cx, cy = (bb[0] + bb[2]) / 2, (bb[1] + bb[3]) / 2
    out = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    op = out.load()
    for oy in range(T):
        for ox in range(T):
            sx = cx + (ox + 0.5 - T / 2) * s
            sy = cy + (oy + 0.5 - T / 2) * s
            x0, x1 = int(sx - s * 0.2), int(sx + s * 0.2) + 1
            y0, y1 = int(sy - s * 0.2), int(sy + s * 0.2) + 1
            if x1 <= 0 or y1 <= 0 or x0 >= w or y0 >= h: continue
            pts = [px[x, y] for y in range(max(0, y0), min(h, y1)) for x in range(max(0, x0), min(w, x1))]
            c = Counter(pts).most_common(1)[0][0]
            r, g, b = c[:3]
            if is_bg(c): continue
            if BG == 'green' and g - max(r, b) > 15 and max(r, b) < 30: c = (18, 16, 22)  # 배경 낀 외곽선
            if BG == 'magenta' and min(r, b) - g > 15 and g < 30: c = (18, 16, 22)
            op[ox, oy] = tuple(c[:3]) + (255,)
    return out


def cut(out_dir, sheet_path, cols, rows, names):
    sheet = Image.open(sheet_path).convert('RGB')
    w, h = sheet.size
    m = 6  # 칸 경계 안쪽
    cells = []
    for i, name in enumerate(names):
        c, r = i % cols, i // cols
        cell = sheet.crop((c * w // cols + m, r * h // rows + m, (c + 1) * w // cols - m, (r + 1) * h // rows - m))
        cells.append((name, cell, bbox(cell)))
    s = max(max(bb[2] - bb[0], bb[3] - bb[1]) for _, _, bb in cells if bb) / MAX
    for name, cell, bb in cells:
        if name == '-' or not bb: continue
        sample(cell, bb, s).save(os.path.join(out_dir, name + '.png'))
        print('ok', name)


# 룬 획 (7 × 11 칸, 왼쪽 위 0,0) — 앵글로색슨 룬, SPD 주문서 12룬
RUNES = {
    'tyr': [((3, 0), (3, 10)), ((3, 0), (0, 3)), ((3, 0), (6, 3))],
    'feoh': [((1, 0), (1, 10)), ((1, 2), (5, 0)), ((1, 5), (5, 3))],
    'ur': [((1, 10), (1, 0)), ((1, 0), (5, 4)), ((5, 4), (5, 10))],
    'thorn': [((1, 0), (1, 10)), ((1, 3), (4, 5)), ((4, 5), (1, 7))],
    'rad': [((1, 0), (1, 10)), ((1, 0), (4, 2)), ((4, 2), (1, 5)), ((1, 5), (5, 10))],
    'gyfu': [((0, 1), (6, 9)), ((6, 1), (0, 9))],
    'wynn': [((1, 0), (1, 10)), ((1, 0), (4, 2)), ((4, 2), (1, 4))],
    'haegl': [((1, 0), (1, 10)), ((5, 0), (5, 10)), ((1, 3), (5, 5)), ((1, 5), (5, 7))],
    'is': [((3, 0), (3, 10))],
    'sigel': [((4, 0), (1, 4)), ((1, 4), (5, 6)), ((5, 6), (2, 10))],
    'beorc': [((1, 0), (1, 10)), ((1, 0), (4, 2)), ((4, 2), (1, 5)), ((1, 5), (4, 8)), ((4, 8), (1, 10))],
    'oethel': [((3, 0), (6, 3)), ((6, 3), (3, 6)), ((3, 6), (0, 3)), ((0, 3), (3, 0)), ((2, 5), (0, 10)), ((4, 5), (6, 10))],
}
INK = (92, 40, 24, 255)


def line(px, a, b, ox, oy):
    (x0, y0), (x1, y1) = a, b
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        px[ox + x0, oy + y0] = INK
        if x0 == x1 and y0 == y1: break
        e2 = 2 * err
        if e2 >= dy: err += dy; x0 += sx
        if e2 <= dx: err += dx; y0 += sy


def runes(out_dir, blank_path):
    blank = Image.open(blank_path).convert('RGBA')
    bb = blank.getbbox()
    ox = (bb[0] + bb[2]) // 2 - 3
    oy = (bb[1] + bb[3]) // 2 - 5
    for name, strokes in RUNES.items():
        im = blank.copy()
        px = im.load()
        for a, b in strokes: line(px, a, b, ox, oy)
        im.save(os.path.join(out_dir, f'scroll_{name}.png'))
    print('ok runes', len(RUNES))


if __name__ == '__main__':
    out = sys.argv[1]
    os.makedirs(out, exist_ok=True)
    if sys.argv[2] == '--runes':
        runes(out, sys.argv[3])
    else:
        BG = sys.argv[5]
        cut(out, sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[6].split(','))

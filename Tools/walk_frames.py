"""걷기 시트(1024, 2×2칸) → 64×64 프레임 4장. 시트 하나는 같은 배율로 줄인다(프레임마다 크기가 달라지지 않게):
가장 큰 프레임 키 → HEIGHT px, 칸 가운데 x → 32, 발끝 → BASE 줄. 색은 칸 가운데 영역에서 가장 흔한 색(평균 안 함), 초록 배경은 투명.
사용: python walk_frames.py <dir> NAME [NAME ...]  (<dir>/NAME.png → <dir>/NAME_f0~3.png, 미리보기 walk_frames_x4.png · walk_all.gif)
원본 시트(2026-10-22 금발 마법사): Docs/PixelTest/walk_src — 앞 = front, 뒤 = back, 옆(3/4, 오른쪽) = side"""
import os, sys
from collections import Counter
from PIL import Image
B = os.path.dirname(os.path.abspath(__file__))
D = sys.argv[1]
FLOOR = r'D:\Unity2022\Magic\Assets\_Project\Art\Pixel\Tiles\floor_05.png'
T, Z, BASE, HEIGHT = 64, 4, 62, 61


def is_green(c):
    r, g, b = c[:3]
    return g > 110 and g - max(r, b) > 45


def bbox(img):
    w, h = img.size
    px = img.load()
    xs, ys = [], []
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            if not is_green(px[x, y]): xs.append(x); ys.append(y)
    return min(xs), min(ys), max(xs), max(ys)


def sample(cell, bb, s):
    px = cell.load()
    w, h = cell.size
    cx = w / 2
    out = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    op = out.load()
    for oy in range(T):
        for ox in range(T):
            sx = cx + (ox + 0.5 - T / 2) * s
            sy = bb[3] + (oy + 0.5 - (BASE + 1)) * s
            x0, x1 = int(sx - s * 0.2), int(sx + s * 0.2) + 1
            y0, y1 = int(sy - s * 0.2), int(sy + s * 0.2) + 1
            if x1 <= 0 or y1 <= 0 or x0 >= w or y0 >= h: continue
            pts = [px[x, y] for y in range(max(0, y0), min(h, y1)) for x in range(max(0, x0), min(w, x1))]
            c = Counter(pts).most_common(1)[0][0]
            r, g, b = c[:3]
            if is_green(c): continue
            if g - max(r, b) > 15 and max(r, b) < 30: c = (18, 16, 22)  # 초록 낀 외곽선
            op[ox, oy] = tuple(c[:3]) + (255,)
    return out


def frames(name):
    sheet = Image.open(os.path.join(D, name + '.png')).convert('RGB')
    w, h = sheet.size
    m = 16  # 칸 경계선(AI가 가끔 그림)을 빼려고 안쪽으로 자름
    cells = [sheet.crop(((i % 2) * w // 2 + m, (i // 2) * h // 2 + m, (i % 2 + 1) * w // 2 - m, (i // 2 + 1) * h // 2 - m)) for i in range(4)]
    boxes = [bbox(c) for c in cells]
    s = max(b[3] - b[1] for b in boxes) / HEIGHT
    out = []
    for i, (c, bb) in enumerate(zip(cells, boxes)):
        f = sample(c, bb, s)
        f.save(os.path.join(D, f'{name}_f{i}.png'))
        out.append(f)
    return out


if __name__ == '__main__':
    floor = Image.open(FLOOR).convert('RGBA')
    rows = []
    for name in sys.argv[2:]:
        fs = frames(name)
        rows.append(fs)
        gif = []
        for f in fs:
            bg = floor.copy(); bg.alpha_composite(f)
            gif.append(bg.resize((T * Z, T * Z), Image.Resampling.NEAREST))
        gif[0].save(os.path.join(D, name + '_walk.gif'), save_all=True, append_images=gif[1:], duration=150, loop=0)
    sheet = Image.new('RGBA', (4 * T, len(rows) * T), (40, 40, 48, 255))
    for r, fs in enumerate(rows):
        for c, f in enumerate(fs):
            bg = floor.copy(); bg.alpha_composite(f)
            sheet.paste(bg, (c * T, r * T))
    sheet.resize((sheet.width * Z, sheet.height * Z), Image.Resampling.NEAREST).save(os.path.join(D, 'walk_frames_x4.png'))
    # 후보 모두를 한 GIF로 (3열: 앞 · 뒤 · 옆, 줄 = 후보 번호)
    cols = 3; n = len(rows); rws = (n + cols - 1) // cols
    anim = []
    for k in range(4):
        fr = Image.new('RGBA', (cols * T, rws * T), (40, 40, 48, 255))
        for i, fs in enumerate(rows):
            bg = floor.copy(); bg.alpha_composite(fs[k])
            fr.paste(bg, ((i % cols) * T, (i // cols) * T))
        anim.append(fr.resize((fr.width * Z, fr.height * Z), Image.Resampling.NEAREST))
    anim[0].save(os.path.join(D, 'walk_all.gif'), save_all=True, append_images=anim[1:], duration=160, loop=0)
    print('ok')

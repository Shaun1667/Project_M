"""얇은 벽(SPD식) 스프라이트 만들기 (2026-10-22) — PixelWallTile(RuleTile) 규칙과 짝. 64px 칸, 테두리 두께 16px(돌 블록 16×16) — 모든 벽이 같은 두께 · 같은 블록
(사용자 피드백 2026-10-22 "두꺼운 것으로 맞춰" — 잠시 8px였음).
벽 앞면(아래 칸이 바닥): wall_front_KEW_X.png — 한 칸 안에 윗면 40줄 + 벽돌 24줄, 모든 앞면 같은 높이(벽돌 그림 256px 위 24줄을 x % 4로 자름).
  K = c(위 칸도 바닥 — 한 칸 기둥 · 얇은 가로벽, 고리를 닫음) · n(위 칸은 바닥 아님), E · W = 오른쪽 · 왼쪽 칸 f(바닥) · w(벽) · o(빈 칸) — front() 참고.
벽 윗면(그 밖): wall_top_MMM.png — 보이는 쪽 변 · 모서리에만 테두리, 나머지는 투명(배경 = 어둠).
  비트 1 위 · 2 오른쪽 · 4 왼쪽 변(보이는 칸 = 바닥 또는 벽 앞면), 8 오른쪽위 · 16 왼쪽위 모서리(보이는 칸), 32 오른쪽아래 · 64 왼쪽아래 모서리(바닥).
  모서리는 붙은 변이 닫혔을 때만 — 오른쪽위: 위 · 오른쪽 닫힘, 왼쪽위: 위 · 왼쪽, 오른쪽아래: 오른쪽, 왼쪽아래: 왼쪽 (PixelWallTile과 같은 규칙).
사용: python tiles_wall.py <wallfront256.png> <out_dir>"""
import os, sys
from PIL import Image

T, B = 64, 16  # 칸 · 테두리 블록(두께) — 모든 벽 같은 두께
BASE = [(176, 183, 180), (166, 173, 170), (184, 191, 188)]
HI, SH, MORTAR = (218, 225, 222), (126, 136, 133), (64, 72, 70)


def block(px, x0, y0, size=B, h=None):
    """돌 블록 하나(size × h, 기본 정사각) — 칸 안 위치로 색을 고름(모든 칸이 같은 무늬라 이웃 칸과 이어짐)."""
    h = h or size
    base = BASE[(x0 // size + 2 * (y0 // size)) % len(BASE)]
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + size):
            dx, dy = x - x0, y - y0
            if dx == size - 1 or dy == h - 1: c = MORTAR
            elif dx == 0 or dy == 0: c = HI
            elif dx == size - 2 or dy == h - 2: c = SH
            else: c = base
            px[x, y] = c + (255,)


def strips(px, top=False, right=False, left=False, bottom=False):
    for i in range(0, T, B):
        if top: block(px, i, 0)
        if bottom: block(px, i, T - B)
        if right: block(px, T - B, i)
        if left: block(px, 0, i)


def bricks_and_base(im, bricks, x, top):
    """벽돌을 top 줄부터 아래 끝까지 + 밑동 그림자."""
    im.paste(bricks.crop((x * T, 0, (x + 1) * T, T - top)), (0, top))
    px = im.load()
    for xx in range(T):  # 밑동 그림자 — 바닥과 닿는 곳
        for yy, k in ((T - 1, 0.55), (T - 2, 0.8)):
            r, g, b, a = px[xx, yy]
            px[xx, yy] = (int(r * k), int(g * k), int(b * k), a)
    return px


def edges(px, right, left, top):
    """옆이 바닥(벽 끝): 테두리 블록 대신 얇은 밝은 모서리 2줄 — 블록을 세우면 상자처럼 보임 (2026-10-22 사용자 피드백)."""
    for yy in range(top, T):
        if left:
            px[0, yy] = HI + (255,); px[1, yy] = SH + (255,)
        if right:
            px[T - 1, yy] = HI + (255,); px[T - 2, yy] = SH + (255,)


BAND = 8                 # 윗면 가운데 띠 높이
RIM_Y = B + BAND         # 앞면 위 테두리가 시작하는 줄 (24)
BRICK_Y = RIM_Y + B      # 벽돌이 시작하는 줄 (40) — 모든 앞면 같은 높이 (2026-10-22 사용자 "벽 높이를 다 맞춰")


def front(bricks, kind, e, w, x):
    """벽 앞면(아래 칸이 바닥) — 한 칸 안에 위 40줄 = 윗면, 아래 24줄 = 벽돌. 모든 앞면이 같은 구조라 벽돌 높이가 같다.
    kind: 'c' = 위 칸도 바닥(한 칸 기둥 · 얇은 가로벽 — 윗줄 블록으로 고리를 닫음) · 'n' = 위 칸은 바닥 아님(윗면이 위 칸으로 이어짐).
    e · w = 오른쪽 · 왼쪽 칸: f(바닥 — 윗면 옆을 블록으로 닫고 벽돌 끝 모서리) · w(벽 — 열어서 옆 벽과 이음) · o(빈 칸 — 닫기만).
    윗면 = 윗줄 16(c만) · 가운데 띠 8 · 앞면 테두리 16, 테두리 두께는 다른 벽과 같은 16px, 안쪽은 투명(어둠)."""
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = bricks_and_base(im, bricks, x, BRICK_Y)
    for i in range(0, T, B):
        block(px, i, RIM_Y)
        if kind == 'c': block(px, i, 0)
    for side, x0 in ((e, T - B), (w, 0)):
        if side == 'w': continue
        if kind == 'n': block(px, x0, 0)
        block(px, x0, B, B, BAND)
    edges(px, e == 'f', w == 'f', BRICK_Y)
    return im


def valid(m):
    n, e, w = m & 1, m & 2, m & 4
    if m & 8 and (n or e): return False
    if m & 16 and (n or w): return False
    if m & 32 and e: return False
    if m & 64 and w: return False
    return m > 0


def top(m):
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = im.load()
    strips(px, top=bool(m & 1), right=bool(m & 2), left=bool(m & 4))
    if m & 8: block(px, T - B, 0)
    if m & 16: block(px, 0, 0)
    if m & 32: block(px, T - B, T - B)
    if m & 64: block(px, 0, T - B)
    return im


if __name__ == '__main__':
    src, out = sys.argv[1:3]
    os.makedirs(out, exist_ok=True)
    for f in os.listdir(out):  # 이름 규칙이 바뀌어도 옛 그림이 남지 않게
        if f.startswith('wall_') and f.endswith('.png'): os.remove(os.path.join(out, f))
    bricks = Image.open(src).convert('RGBA')
    for x in range(4):
        for kind in 'cn':
            for e in 'fwo':
                for w in 'fwo':
                    front(bricks, kind, e, w, x).save(os.path.join(out, f'wall_front_{kind}{e}{w}_{x}.png'))
    n = 0
    for m in range(128):
        if valid(m):
            top(m).save(os.path.join(out, f'wall_top_{m:03d}.png'))
            n += 1
    print('ok tops', n)

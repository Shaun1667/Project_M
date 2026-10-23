"""얇은 벽(SPD식) 스프라이트 만들기 (2026-10-22) — PixelWallTile(RuleTile) 규칙과 짝. 64px 칸, 테두리 두께 16px(돌 블록 16×16) — 모든 벽이 같은 두께 · 같은 블록
(사용자 피드백 2026-10-22 "두꺼운 것으로 맞춰" — 잠시 8px였음).
벽 앞면(아래 칸이 바닥): wall_front_nS_X.png — 위 16줄 돌 테두리 + 아래 48줄 벽돌(벽돌 그림 256px 위 48줄을 x % 4로 자름) — 모든 앞면이 같은 높이,
  S 비트 1 = 오른쪽 칸이 바닥 · 2 = 왼쪽 칸이 바닥이면 그쪽 끝에 얇은 밝은 모서리(2px, 테두리 블록은 안 세움).
홀로 선 벽(위 칸도 바닥 — 한 칸 기둥 · 얇은 가로벽)의 솟은 윗면: wall_cap_M.png — 벽 위 칸에 덮개로(아래 절반 고리, 가운데 어둠), M 비트 1 오른쪽 · 2 왼쪽 닫힘.
벽 윗면(그 밖): wall_top_MMM.png — 보이는 쪽 변 · 모서리에만 테두리, 나머지는 투명(배경 = 어둠).
  비트 1 위 · 2 오른쪽 · 4 왼쪽 변(보이는 칸 = 바닥 또는 벽 앞면), 8 오른쪽위 · 16 왼쪽위 모서리(보이는 칸), 32 오른쪽아래 · 64 왼쪽아래 모서리(바닥).
  모서리는 붙은 변이 닫혔을 때만 — 오른쪽위: 위 · 오른쪽 닫힘, 왼쪽위: 위 · 왼쪽, 오른쪽아래: 오른쪽, 왼쪽아래: 왼쪽 (PixelWallTile과 같은 규칙).
사용: python tiles_wall.py <wallfront256.png> <out_dir>"""
import os, sys
from PIL import Image

T, B = 64, 16  # 칸 · 테두리 블록(두께) — 모든 벽 같은 두께
BASE = [(176, 183, 180), (166, 173, 170), (184, 191, 188)]
HI, SH, MORTAR = (218, 225, 222), (126, 136, 133), (64, 72, 70)


def block(px, x0, y0, size=B):
    """돌 블록 하나 — 칸 안 위치로 색을 고름(모든 칸이 같은 무늬라 이웃 칸과 이어짐)."""
    base = BASE[(x0 // size + 2 * (y0 // size)) % len(BASE)]
    for y in range(y0, y0 + size):
        for x in range(x0, x0 + size):
            dx, dy = x - x0, y - y0
            if dx == size - 1 or dy == size - 1: c = MORTAR
            elif dx == 0 or dy == 0: c = HI
            elif dx == size - 2 or dy == size - 2: c = SH
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


def front(bricks, sides, x):
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = bricks_and_base(im, bricks, x, B)
    strips(px, top=True)
    edges(px, sides & 1, sides & 2, 0)
    return im


def cap_top(mask):
    """홀로 선 벽(위 · 아래 칸이 모두 바닥)의 솟은 윗면 — 벽 바로 위 칸(바닥)에 덮개로 그림(PixelMapPresenter의 PixelWallCaps).
    아래 절반만: 윗줄 블록 + 양옆 블록(닫힌 쪽만) + 가운데 어둠. 벽 칸의 앞면 위 테두리가 고리의 아랫줄이 된다 → 앞면 높이는 다른 벽과 같음.
    mask 비트 1 = 오른쪽 닫힘(오른쪽 칸이 벽 아님) · 2 = 왼쪽 닫힘."""
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = im.load()
    half = T // 2
    for yy in range(half + B, T):
        for xx in range(T):
            px[xx, yy] = (0, 0, 0, 255)
    for i in range(0, T, B):
        block(px, i, half)
    for yy in range(half + B, T, B):
        if mask & 1: block(px, T - B, yy)
        if mask & 2: block(px, 0, yy)
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
        for s in range(4):
            front(bricks, s, x).save(os.path.join(out, f'wall_front_n{s}_{x}.png'))
    for m in range(4):
        cap_top(m).save(os.path.join(out, f'wall_cap_{m}.png'))
    n = 0
    for m in range(128):
        if valid(m):
            top(m).save(os.path.join(out, f'wall_top_{m:03d}.png'))
            n += 1
    print('ok tops', n)

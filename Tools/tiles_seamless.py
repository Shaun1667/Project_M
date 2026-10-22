"""이어지는 바닥 · 벽 타일 만들기 (2026-10-22). 256×256 픽셀 그림(이어지는 텍스처 — AI 1024px를 tile64.py <in> <out> 256로 줄인 것)을
4×4칸(64px)으로 잘라 Art/Pixel/Tiles에 쓴다. 이름 번호는 아래 줄부터: floor_NN · walltop_NN (NN = (y % 4) * 4 + x % 4), 벽 앞면은 위 64줄 → wallfront_N.
벽 윗면 테두리 rim_MMM도 만든다: 비트 1 위 · 2 오른쪽 · 4 아래 · 8 왼쪽 변, 16 오른쪽위 · 32 오른쪽아래 · 64 왼쪽아래 · 128 왼쪽위 안쪽 모서리
(모서리는 양옆 변이 닫혔을 때만 — PixelMapPresenter.RimAt과 같은 규칙, 쓰는 47조합만 파일).
사용: python tiles_seamless.py <floor256.png> <walltop256.png> <wallfront256.png> <out_dir>"""
import os, sys
from PIL import Image

T, N = 64, 4
# 테두리: 바깥(열린 쪽)부터 안쪽으로 — 흰 2줄, 밝은 회색 1줄, 그림자 1줄
RIM = [(234, 240, 236, 255), (220, 228, 224, 255), (184, 194, 190, 255), (120, 128, 126, 255)]


def cut(src, out_dir, prefix):
    im = Image.open(src).convert('RGBA')
    assert im.size == (T * N, T * N), im.size
    for ry in range(N):
        for cx in range(N):
            tile = im.crop((cx * T, (N - 1 - ry) * T, (cx + 1) * T, (N - ry) * T))
            tile.save(os.path.join(out_dir, f'{prefix}_{ry * N + cx:02d}.png'))


def front(src, out_dir):
    im = Image.open(src).convert('RGBA')
    for cx in range(N):
        tile = im.crop((cx * T, 0, (cx + 1) * T, T))
        px = tile.load()
        for x in range(T):  # 밑동 그림자 2줄 — 바닥과 닿는 곳
            for y, k in ((T - 1, 0.55), (T - 2, 0.8)):
                r, g, b, a = px[x, y]
                px[x, y] = (int(r * k), int(g * k), int(b * k), a)
        tile.save(os.path.join(out_dir, f'wallfront_{cx}.png'))


def rim(mask):
    im = Image.new('RGBA', (T, T), (0, 0, 0, 0))
    px = im.load()
    w = len(RIM)

    def put(x, y, d):  # d = 열린 쪽에서 떨어진 거리 — 가까운 테두리가 이김
        if 0 <= d < w:
            cur = px[x, y]
            if cur[3] == 0 or RIM.index(cur) > d:
                px[x, y] = RIM[d]

    for y in range(T):
        for x in range(T):
            if mask & 1: put(x, y, y)
            if mask & 2: put(x, y, T - 1 - x)
            if mask & 4: put(x, y, T - 1 - y)
            if mask & 8: put(x, y, x)
            if mask & 16: put(x, y, max(T - 1 - x, y))
            if mask & 32: put(x, y, max(T - 1 - x, T - 1 - y))
            if mask & 64: put(x, y, max(x, T - 1 - y))
            if mask & 128: put(x, y, max(x, y))
    return im


def valid(mask):
    e, c = mask & 15, mask >> 4
    need = {1: 1 | 2, 2: 4 | 2, 4: 4 | 8, 8: 1 | 8}  # 모서리 비트 → 닫혀 있어야 할 변
    return mask > 0 and all(not (c & k) or not (e & v) for k, v in need.items())


if __name__ == '__main__':
    floor, top, wallfront, out = sys.argv[1:5]
    cut(floor, out, 'floor')
    cut(top, out, 'walltop')
    front(wallfront, out)
    count = 0
    for m in range(256):
        if valid(m):
            rim(m).save(os.path.join(out, f'rim_{m:03d}.png'))
            count += 1
    print('ok rims', count)

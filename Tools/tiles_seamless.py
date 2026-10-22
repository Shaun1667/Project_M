"""이어지는 바닥 타일 만들기 (2026-10-22). 256×256 픽셀 그림(이어지는 텍스처 — AI 1024px를 tile64.py <in> <out> 256로 줄인 것)을
4×4칸(64px)으로 잘라 Art/Pixel/Tiles에 floor_NN으로 쓴다 (NN = (y % 4) * 4 + x % 4, 아래 줄부터). 벽은 tiles_wall.py(얇은 벽 RuleTile).
사용: python tiles_seamless.py <floor256.png> <out_dir>"""
import os, sys
from PIL import Image

T, N = 64, 4


def cut(src, out_dir, prefix):
    im = Image.open(src).convert('RGBA')
    assert im.size == (T * N, T * N), im.size
    for ry in range(N):
        for cx in range(N):
            tile = im.crop((cx * T, (N - 1 - ry) * T, (cx + 1) * T, (N - ry) * T))
            tile.save(os.path.join(out_dir, f'{prefix}_{ry * N + cx:02d}.png'))


if __name__ == '__main__':
    floor, out = sys.argv[1:3]
    cut(floor, out, 'floor')
    print('ok')

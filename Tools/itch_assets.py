"""itch.io 페이지 그림 만들기 (2026-10-26): 게임 픽셀 그림(바닥 · 벽 · 캐릭터 · 적 · 아이템)으로 배너 · 커버를 조립한다.
제목 글자 = Galmuri11(SIL OFL)을 픽셀 크기 그대로 그린 뒤 정수 배로 키움(번짐 없음). 결과는 Docs/itch/.
사용: python itch_assets.py"""
import os
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(ROOT, 'Assets', '_Project', 'Art', 'Pixel')
FONT = os.path.join(ROOT, 'Assets', '_Project', 'Art', 'Fonts', 'Galmuri11.ttf')
OUT = os.path.join(ROOT, 'Docs', 'itch')
T = 64

L = lambda *p: Image.open(os.path.join(ART, *p)).convert('RGBA')
GOLD, GOLD_DARK, INK = (255, 206, 92), (196, 120, 34), (14, 12, 18)


def room(cols, rows, walls_top=1):
    """바닥 + 위쪽 벽 앞면 한 줄 + 위는 어둠 (게임과 같은 그림)."""
    img = Image.new('RGBA', (cols * T, rows * T), (0, 0, 0, 255))
    for y in range(rows):
        for x in range(cols):
            if y < walls_top - 1: continue
            if y == walls_top - 1:
                img.alpha_composite(L('Tiles', 'Wall', f'wall_front_n0_{x % 4}.png'), (x * T, y * T))
            else:
                gy = rows - 1 - y
                img.alpha_composite(L('Tiles', f'floor_{(gy % 4) * 4 + x % 4:02d}.png'), (x * T, y * T))
    return img


def put_unit(img, sprite, cx, cy, flip=False):
    """칸 (cx, cy — 위에서부터)에 유닛: 발 = 칸 아래 + 26px (게임과 같은 배치)."""
    if flip: sprite = sprite.transpose(Image.FLIP_LEFT_RIGHT)
    img.alpha_composite(sprite, (cx * T, cy * T - 26))


def put_item(img, sprite, cx, cy):
    img.alpha_composite(sprite, (cx * T, cy * T))


def vignette(img, strength=0.75):
    """가장자리를 어둡게 (안개처럼) — 칸 단위가 아니라 부드럽게."""
    w, h = img.size
    mask = Image.new('L', (w, h), 0)
    px = mask.load()
    for y in range(h):
        for x in range(w):
            dx = abs(x - w / 2) / (w / 2)
            dy = abs(y - h / 2) / (h / 2)
            d = max(0.0, (max(dx * 0.9, dy) - 0.45) / 0.55)
            px[x, y] = int(255 * min(1.0, d) * strength)
    dark = Image.new('RGBA', (w, h), (0, 0, 0, 255))
    img.paste(dark, (0, 0), mask)
    return img


def pixel_text(s, size, scale, fill, outline=INK, shadow=True, gradient=True):
    """Galmuri11을 size px(안티앨리어스 없이)로 그린 뒤 scale배 — 테두리 1px · 그림자 포함."""
    font = ImageFont.truetype(FONT, size)
    bbox = font.getbbox(s)
    w, h = bbox[2] - bbox[0] + 4, bbox[3] - bbox[1] + 5
    mask = Image.new('1', (w, h), 0)
    ImageDraw.Draw(mask).text((2 - bbox[0], 2 - bbox[1]), s, font=font, fill=1)
    out = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    m = mask.load(); o = out.load()
    for y in range(h):
        for x in range(w):
            if m[x, y]: continue
            near = any(0 <= x + dx < w and 0 <= y + dy < h and m[x + dx, y + dy] for dx in (-1, 0, 1) for dy in (-1, 0, 1))
            if near: o[x, y] = outline + (255,)
    if shadow:
        for y in range(h - 1, 0, -1):
            for x in range(w - 1, 0, -1):
                if o[x, y][3] == 0 and not m[x, y] and (m[x - 1, y - 1] or o[x - 1, y - 1][3]): o[x, y] = (0, 0, 0, 150)
    for y in range(h):  # 글자: 위 절반 밝은 금색, 아래 어두운 금색
        for x in range(w):
            if m[x, y]: o[x, y] = (fill if not gradient or y < h * 0.55 else GOLD_DARK) + (255,)
    return out.resize((w * scale, h * scale), Image.Resampling.NEAREST)


def banner():
    """960 × 400. 2배 크기 장면(칸 128px) — 위 줄 벽 앞면 위에 제목, 아래 바닥에 주인공 · 적."""
    base = room(8, 4, walls_top=1)
    put_unit(base, L('Characters', 'hero_blond_walk_side_1.png'), 1, 2)
    put_item(base, L('Items', 'spellbook_fire.png'), 2, 2)
    put_unit(base, L('Enemies', 'enemy_slime.png'), 4, 2, flip=True)
    put_unit(base, L('Enemies', 'enemy_mouse.png'), 5, 2, flip=True)
    put_unit(base, L('Enemies', 'enemy_rabbit.png'), 6, 2, flip=True)
    put_unit(base, L('Enemies', 'boss_slime_girl.png'), 7, 2, flip=True)
    big = base.resize((base.width * 2, base.height * 2), Image.Resampling.NEAREST)
    img = big.crop((64, 0, 1024, 400))
    vignette(img, 0.55)
    title = pixel_text('NaruDungeon', 11, 6, GOLD)
    img.alpha_composite(title, ((960 - title.width) // 2, 16))
    sub = pixel_text('속성 반응 마법 로그라이크', 11, 3, (236, 238, 246), gradient=False)
    img.alpha_composite(sub, ((960 - sub.width) // 2, 16 + title.height + 2))
    img.convert('RGB').save(os.path.join(OUT, 'banner_960x400.png'))


def cover():
    """630 × 500 (itch 목록 썸네일). 2배 크기 장면 + 제목."""
    base = room(5, 4, walls_top=1)
    put_unit(base, L('Characters', 'hero_blond_walk_front_1.png'), 1, 3)
    put_item(base, L('Items', 'spellbook_fire.png'), 2, 2)
    put_unit(base, L('Enemies', 'enemy_slime.png'), 3, 3, flip=True)
    big = base.resize((base.width * 2, base.height * 2), Image.Resampling.NEAREST)  # 640 × 512
    img = big.crop((5, 6, 635, 506))
    vignette(img, 0.5)
    title = pixel_text('NaruDungeon', 11, 5, GOLD)
    img.alpha_composite(title, ((630 - title.width) // 2, 22))
    sub = pixel_text('속성 반응 마법 로그라이크', 11, 2, (236, 238, 246), gradient=False)
    img.alpha_composite(sub, ((630 - sub.width) // 2, 22 + title.height + 2))
    img.convert('RGB').save(os.path.join(OUT, 'cover_630x500.png'))


def background():
    """페이지 바탕 (어두운 바닥 무늬, 반복용 256px)."""
    tile = Image.new('RGBA', (256, 256))
    for y in range(4):
        for x in range(4):
            tile.alpha_composite(L('Tiles', f'floor_{(3 - y) * 4 + x:02d}.png'), (x * T, y * T))
    dark = Image.new('RGBA', tile.size, (8, 9, 14, 205))
    tile.alpha_composite(dark)
    tile.convert('RGB').save(os.path.join(OUT, 'background_tile_256.png'))


if __name__ == '__main__':
    os.makedirs(OUT, exist_ok=True)
    banner(); cover(); background()
    print('ok', OUT)

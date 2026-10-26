"""AI 이펙트 시트(검정 배경, 4줄 × 4프레임) → 프레임 PNG. 픽셀 간격 · 시작 위치를 찾아(grid64.py와 같은 점수) 칸마다 가운데 영역의
가장 흔한 색을 집고, 어두운 칸(배경 검정)은 투명으로. 출력 = 원래 픽셀 1개 = 1px (보통 한 칸 32px 안팎).
사용: python fx_cut.py <sheet.png> <시트 ID(fx01~fx10)> <출력 폴더> [미리보기.png]"""
import sys
from collections import Counter
from PIL import Image

ROWS = {
    'fx01': ['projectile_fire', 'projectile_ice', 'projectile_poison', 'projectile_water'],
    'fx02': ['impact_fire', 'impact_ice', 'impact_lightning', 'impact_poison'],
    'fx03': ['impact_water', 'cast', 'steam', 'item_sparkle'],
    'fx04': ['burst_fire', 'burst_ice', 'burst_poison', 'burst_water'],
    'fx05': ['burst_lightning', 'toxic_burst', 'gas_explosion', 'big_explosion'],
    'fx06': ['lava_eruption', 'contaminated_wave', 'frost_wave', 'freeze_circle'],
    'fx07': ['toxic_ignite', 'shatter', 'thaw', 'shock'],
    'fx08': ['status_burn', 'status_poison', 'status_paralyze', 'status_wet'],
    'fx09': ['status_frozen', 'status_slow', 'contaminate', 'line_lightning'],
    'fx10': ['terrain_fire', 'terrain_poison_cloud', 'terrain_water', 'terrain_contaminated_water'],
}
DARK = 28  # 가장 밝은 채널이 이보다 작으면 배경(투명)

src, sheet, outdir = sys.argv[1], sys.argv[2], sys.argv[3]
preview = sys.argv[4] if len(sys.argv) > 4 else None
im = Image.open(src).convert('RGB')
W, H = im.size
px = im.load()


def score(p, ox, oy):
    s = n = 0
    for y in range(oy, H - 1, 5):
        for x in range(ox, W - 1, 5):
            if (x - ox) % p == p - 1 or (y - oy) % p == p - 1: continue
            a = px[x, y]
            if max(a) < DARK: continue  # 배경은 점수에서 뺌 (어디서 잘라도 같은 색)
            b = px[x + 1, y]; c = px[x, y + 1]
            s += sum(abs(a[i] - b[i]) for i in range(3)) + sum(abs(a[i] - c[i]) for i in range(3)); n += 1
    return s / max(n, 1)


best = None
for p in range(6, 13):  # 1024 시트에서 원래 픽셀 6~12px
    for ox in range(p):
        for oy in range(p):
            sc = score(p, ox, oy)
            if best is None or sc < best[0]: best = (sc, p, ox, oy)
_, cell, ox, oy = best
cols = (W - ox) // cell; rows = (H - oy) // cell
grid = Image.new('RGBA', (cols, rows))
gp = grid.load()
for r in range(rows):
    for c in range(cols):
        x0 = ox + c * cell + cell // 4; y0 = oy + r * cell + cell // 4
        cnt = Counter(px[x, y] for y in range(y0, y0 + max(1, cell // 2)) for x in range(x0, x0 + max(1, cell // 2)))
        col = cnt.most_common(1)[0][0]
        gp[c, r] = (0, 0, 0, 0) if max(col) < DARK else (*col, 255)

fw, fh = cols // 4, rows // 4  # 시트를 4 × 4로 똑같이 나눔
names = ROWS[sheet]
frames = []
for r in range(4):
    for c in range(4):
        f = grid.crop((c * fw, r * fh, (c + 1) * fw, (r + 1) * fh))
        f.save(f'{outdir}/{names[r]}_{c}.png')
        frames.append(f)
if preview:
    k = 6
    sheet_img = Image.new('RGBA', (fw * 4 * k + 30, fh * 4 * k + 30), (40, 40, 48, 255))
    for i, f in enumerate(frames):
        sheet_img.alpha_composite(f.resize((fw * k, fh * k), Image.Resampling.NEAREST), (6 + (i % 4) * (fw * k + 6), 6 + (i // 4) * (fh * k + 6)))
    sheet_img.save(preview)
print(f'{sheet}: pixel {cell}px offset ({ox},{oy}) grid {cols}x{rows} frame {fw}x{fh}')

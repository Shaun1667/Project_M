# 이펙트 픽셀 시트 (P-3 ⓓ, 2026-10-26)

Varco(GPT Image 2.5)로 시트 10장 = 이펙트 40종. 시트 하나 = **4줄 × 4칸**, 한 줄 = 이펙트 하나의 4프레임(왼쪽 → 오른쪽).
배경은 **순수 검정** — 게임에서는 가산 혼합(검정 = 투명)으로 그리므로 키 아웃 없이 밝기를 알파로 쓴다.
자르기 · 축소는 `Tools/fx_cut.py`(시트 → `Art/Pixel/Fx/<키>_<0~3>.png`, 한 칸 효과 64px · 3×3 효과 192px).

## 공통 프롬프트 (앞에 붙임)

> Pixel art visual effect sprite sheet for a top-down 16-bit dungeon roguelike (Shattered Pixel Dungeon style). Crisp hard-edged pixels, chunky pixel clusters, limited bright palette, no anti-aliasing, no blur, no gradients. Pure solid black background (#000000) everywhere. The image is an exact grid of 4 rows and 4 columns of equal square cells with generous black space between them; no grid lines, no borders, no text, no labels, no numbers. Each row is ONE effect animated over 4 frames from left to right; the effect is centered in every cell and stays the same size class across its row.

## 시트

| 시트 | 1줄 | 2줄 | 3줄 | 4줄 | 게임 키 |
|---|---|---|---|---|---|
| fx01 투사체 (루프) | 불덩이(꼬리 불꽃, 오른쪽으로 날아감) | 얼음 조각(하늘색 수정 화살) | 독 방울(보라 · 초록 물방울) | 물 구슬(파랑 물방울, 물보라 꼬리) | projectile.Fire/Ice/Poison/Water |
| fx02 맞음 1 (한 번) | 작은 불 폭발 | 얼음 파편 튐 | 전기 불꽃 튐(노랑 · 흰색) | 독 튐(보라 · 초록) | impact.Fire/Ice/Lightning/Poison |
| fx03 맞음 2 · 기타 | 물 튐 | 시전 빛(흰 마법 반짝임 → 퍼짐) | 수증기 연기 한 줌(흰 회색) | 아이템 반짝임(작은 십자 별) | impact.Water · cast.* · reaction.evaporate* · item.* |
| fx04 범위 3×3 (1) | 불 폭발 원 | 얼음 폭발(서리 고리 · 수정) | 독 폭발(독구름 퍼짐) | 물 폭발(물결 고리) | burst.Fire/Ice/Poison/Water |
| fx05 범위 3×3 (2) | 번개 폭발(가지 번개 사방) | 유독성 폭발(보라 · 초록 폭발) | 가스 폭발(주황 · 초록 불꽃 폭발) | 큰 폭발(불 + 번개, 흰 섬광) | burst.Lightning · reaction.toxic_burst · reaction.gas_explosion · fusion.fusion_fire_lightning |
| fx06 융합 · 반응 | 용암 분출(바닥에서 솟는 용암) | 오염 물결(탁한 초록 물결) | 빙결 파도(얼음 결정 물결) | 빙결 고리(바닥에 얼음 마법진) | fusion.fusion_fire_water/water_poison/ice_water · reaction.freeze* |
| fx07 반응 | 독화(초록 · 보라 불꽃) | 파쇄(얼음 조각 사방으로) | 해동(녹는 물방울) | 감전(파란 · 흰 전기 호) | reaction.toxic_ignite* · shatter · thaw* · shock* |
| fx08 상태 (루프, 유닛 위) | 화상(작은 불꽃 2~3개) | 중독(초록 거품 올라감) | 마비(노란 전기 불꽃) | 젖음(파란 물방울 떨어짐) | status.Burn/Poison/Paralyze/Wet |
| fx09 상태 · 선 | 빙결(유닛을 감싸는 얼음 덩어리 테두리) | 둔화(하늘색 서리 소용돌이) | 오염(탁한 초록 튐) | 번개 선(가로 번개 줄기, 칸 끝에서 끝까지) | status.Frozen/Slow · reaction.contaminate · line.Lightning |
| fx10 지형 (루프, 바닥 위에서 내려다봄) | 화염 지대(바닥 불길) | 독구름(초록 안개) | 물웅덩이 물결 | 오염된 물(탁한 초록 거품) | terrain.FireZone/PoisonCloud · (물 · 오염된 물 오버레이) |

## 결과 (2026-10-26)

- 고른 시트: fx01 · 02 · 04 · 05 · 06 · 07 · 08 · 09 · 10 = `_a`, fx03 = `_b` (Varco 플로우 f679a6c8, y 62600~68000 노드). 나머지 후보도 이 폴더에 둠.
- 자른 프레임 160장: `Assets/_Project/Art/Pixel/Fx/<이름>_<0~3>.png` (원래 픽셀 1개 = 1px, 한 프레임 28~42px — 시트마다 픽셀 크기가 달라 게임에서 크기를 맞춰야 함).
- 미리보기: `fxNN_cut.png`(시트별), `Docs/PixelTest/67_fx_all.gif`(40종 애니메이션).
- 게임 연결: 019aa5b (`FxSetup.PixelTable` · `FxPresenter` 2D) — TASKS "P-3 ⓓ".

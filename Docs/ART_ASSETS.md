# ART_ASSETS.md — 그래픽 에셋 목록 (3D · Varco 기준)

> 작성 2026-10-18 · 3D 기준으로 다시 씀 (사용자 요청: 픽셀 아트 대신 Varco 3D 모델로 먼저 진행).
> 지금 게임(3-4b까지)에서 화면에 그려지는 모든 요소를 코드 기준으로 모았다.
> 데이터는 비주얼을 **ID로만** 가리킨다 (CLAUDE.md 아트 규칙) — 아래 "ID" 열이 코드 · 데이터 이름이고, 모델 · 이펙트는 이 ID에 연결만 하면 된다. 규칙 코드(Simulation)는 바뀌지 않는다.
> 우선순위: **1차** = 플레이 가독성에 꼭 필요 / **2차** = 손맛 · 연출 / **3차** = 다듬기.
> **지금 남은 그래픽은 바로 아래 "★ 남은 그래픽"이 기준이다** (2026-11-05 코드 점검). 1~11번은 3-4b 때 만든 처음 목록이라 지금과 다른 줄이 있다.

---

## ★ 남은 그래픽 — 그레이박스 · 그래픽 없음 (2026-11-05 점검, 3-17까지)

점검 방법: Setup 스크립트(`MainSceneSetup` · `VarcoModelSetup` · `FxSetup`)와 `ItemVisualCatalog.asset` · 상태/지형 enum을 대조 (게임 화면으로는 확인 안 함).
지금 모델이 있는 것: 루멘 · 시아 · 쥐 · 토끼 · 슬라임 · 슬라임 소녀 · 바닥 · 벽 · 기둥 · 계단 2 · 무기 6 · 물약 12색 · 주문서 12룬. 이펙트는 기존 7속성 투사체 · 맞음, 상태 6, 지형 4, 반응 3-1~3-3b 전부.

순서 기준: 1차 = 매 판 자주 보이고 **읽어야 하는 것**(적 구분 · 보스 패턴 · 위험 칸) / 2차 = 자주 보이지만 글자 · 색으로도 알 수 있는 것 / 3차 = 드물거나 다듬기.

### 1차 — 적 · 보스 · 위험 표시

→ **2026-11-05 모델 연결됨: 1 · 3 ~ 8번** (Varco 3D + 사람형은 Mixamo 리깅, 상인은 강아지 수인으로 다시 디자인) — 12-5번 표.
→ **2026-11-05 이펙트 연결됨: 9 ~ 11번** (G-12, Setup/FX) — 9 `burst.Earth` 모래빛 연기 폭발(+ `burst.Wind`) · 10 함정 문양 7종(둥근 테 + 기호: 불꽃 · 거품 · 동심원 · 물결 · 번개 · 소용돌이 · 느낌표, 생성 타일 `trap_*`, TrapPresenter `_typeTiles`) · 11 예고 칸 = 바닥 무늬 `fx_warning`(테두리 + 사선 줄무늬) + 칸마다 붉은 마법진 `terrain.Warning`. 남은 1차: 2(25층 보스, 보류).

| # | ID | 무엇 | 지금 | 만들 것 | 연결 위치 |
|---|---|---|---|---|---|
| 1 | `boss_pharaoh` | 파라오 술사 (10층 보스) — 황금 가면 · 지팡이, 2칸 키 | 금색 큰 캡슐 | Varco 모델 (보스 1.8배) | `VarcoModelSetup` 모델 표 + `catalog.SetPrefab` |
| 2 | `boss` | 25층 임시 보스 | 슬라임 소녀 모델 같이 씀 | 3~5구역 보스가 정해지면 (지금은 보류 가능) | 〃 |
| 3 | `enemy_goblin_shaman` | 고블린 주술사 — 작은 초록 고블린, 지팡이 · 깃털 | 초록 캡슐 | Varco 모델 | 〃 |
| 4 | `enemy_scorpion` | 전갈 — 큰 집게 · 꼬리 독침 | 붉은 갈색 캡슐 | Varco 모델 | 〃 |
| 5 | `enemy_jackal` | 자칼 — 마른 개과, 황토색 | 황토 캡슐 | Varco 모델 | 〃 |
| 6 | `enemy_mummy` | 미라 — 붕대 사람형, 느린 걸음 | 베이지 캡슐 | Varco 모델 | 〃 |
| 7 | `enemy_desert_shaman` | 사막 주술사 — 두건 · 금 장신구 · 모래 지팡이 | 금색 캡슐 | Varco 모델 | 〃 |
| 8 | `shopkeeper` | 상인 (3층 상점, 중립) — 적과 헷갈리지 않는 모습 | 색 캡슐 | Varco 모델 | 〃 |
| 9 | `burst.Earth` | 대지 3×3 폭발 — 파라오 모래 폭풍 | **없음** (폭발 이펙트 안 나옴) | 모래 폭발 파티클 | `FxSetup.Table` 한 줄 |
| 10 | 함정 7종 (드러난 것) | 화염 · 독가스 · 마비 가스 · 물 · 번개 · 순간이동 · 경보 | 바닥 Tilemap 색 칸 | 종류별 바닥 무늬(데칼) 7 — 색만으로는 종류 구분이 어려움 | `TrapPresenter` |
| 11 | 지형 `Warning` | 보스 힘 모으기 예고 칸 | 붉은 색 덮개 | 깜빡이는 바닥 경고 무늬 (가독성) | `FxSetup` `terrain.Warning` |

### 2차 — 바닥 아이템 · 맵 칸 · 새 상태

→ **2026-11-05 바닥 아이템 연결됨: 12 ~ 18번** (25종 = 모델 15 + 마법서 1 × 5색 + 씨앗 1 × 5색) — 12-6번 표.
→ **2026-11-05 문 · 풀 · 솥 연결됨: 20 ~ 22번** (여닫이 양문 · 짧은 · 높은 풀 · 연금 솥) — 12-7번.
→ **2026-11-05 식물 5종 연결됨: 19번** — 12-8번.
→ **2026-11-05 이펙트 연결됨: 23 · 24번** (G-13, Setup/FX) — 상태 6: 신속 금빛 오라 · 투명 옅은 아지랑이 · 부양 발밑 하늘색 마법진 · 정신 시야 머리 위 보라 반짝임 · 잠 머리 위 느린 푸른 반짝임 · 공포 어두운 보라 오라 / 지형 2: 빙판 바닥 서리 반짝임(무늬 `fx_ice`와 함께) · 마비 가스 노란 땅 연기. 투명의 몸 반투명 재질은 코드 변경이 필요해 아직 안 함. **2차 모두 연결됨.**

| # | ID | 무엇 | 지금 | 만들 것 |
|---|---|---|---|---|
| 12 | `armor_cloth` · `armor_leather` · `armor_mail` · `armor_scale` · `armor_plate` | 갑옷 5 (바닥) | 그레이박스 상자 | 소품 모델 5 (티어별 재질 차이). 입은 모습은 3차 |
| 13 | `spellbook_fire/ice/lightning/poison/water` | 마법서 5 | 그레이박스 상자 | 책 모델 1 + 속성 색 5 (물약처럼 색만 바꾸는 방식) |
| 14 | `food_ration` · `food_bread` · `food_mystery_meat` · `food_cooked_meat` · `food_frozen_meat` | 음식 5 | 그레이박스 상자 | 소품 모델 5 (고기 3은 한 모델 + 색 3도 가능) |
| 15 | `gold` · `key_iron` | 금화 무더기 · 쇠 열쇠 | 그레이박스 상자 | 소품 모델 2 |
| 16 | `dart` · `javelin` | 던지는 무기 2 | 그레이박스 상자 | 소품 모델 2 (날아가는 모습 = 투사체에 같이 씀) |
| 16-1 | `knuckle` (3-22a) | 너클 — 손등 쇠 덮개 · 주먹 끼우개 | 그레이박스 상자 · 손에 든 모습 없음 | 소품 모델 1 (바닥) + 손에 끼운 모습(시아 양손 — `VarcoModelSetup.WeaponSpecs`) |
| 17 | `seed_firebloom/icecap/sorrowmoss/sungrass/fadeleaf` | 씨앗 5 | 그레이박스 상자 | 씨앗 모델 1 + 색 5 |
| 18 | `fusion_device` | 마법 융합기 | 그레이박스 상자 | 소품 모델 1 |
| 19 | 식물 5 (심은 것) | 화염꽃 · 얼음버섯 · 독이끼 · 햇볕풀 · 사라짐잎 | 바닥 색 칸 (함정과 같음) | 식물 모델 5 |
| 20 | `TileType.Door` · `OpenDoor` · `LockedDoor` | 문 · 열린 문 · 잠긴 문 | 상자 기둥 + 문짝 (잠긴 문은 색만 다름) | 문 모듈 모델 2 (잠긴 문 = 자물쇠 붙은 변형) |
| 21 | `TileType.Grass` · `TallGrass` | 짧은 풀 · 높은 풀 | 바닥 + 가는 상자 풀잎 | 풀 모듈 2 (높은 풀은 시야를 가리므로 키가 커야 함) |
| 22 | `TileType.AlchemyPot` | 연금 솥 (연구실) | 보라 원기둥 | 솥 모델 1 |
| 23 | 상태 `Haste` · `Invisible` · `Levitating` · `MindVision` · `Asleep` · `Terrified` | 신속 · 투명 · 부양 · 정신 시야 · 잠 · 공포 | **없음** (색 틴트만) | 유닛 위 루프 이펙트 6 (`status.<이름>`). 투명은 반투명 재질이 나을 수 있음 |
| 24 | 지형 `IcePatch` · `ParalyticGas` | 빙판 · 마비 가스 | 색 덮개만 | 지형 루프 이펙트 2 (`terrain.<이름>`) |

### 3차 — 연출 · 구역 분위기 · UI

→ **2026-11-05 이펙트 연결됨: 26 · 27번** (G-14, Setup/FX + `FxPresenter` 이벤트 연결) — 26 점화 순간 `reaction.grass_ignite` · `tallgrass_ignite` · `door_ignite`(팩 Ground AOE explosion을 1칸 크기로) / 27 `use.drink`(발밑에서 솟는 빛) · `use.read`(발밑 금빛 마법진) · `use.shatter`(던진 물약이 떨어진 칸에서 깨짐) · `trap.triggered`(함정 · 식물 발동 칸 불꽃 튐) · `boss.charge`(붉은 오라) · `boss.teleport`(떠난 칸 · 도착한 칸) · `boss.summon`(보스 발밑 붉은 문). 남은 3차: 29(UI).
→ **2026-11-05 연결됨: 25 · 28번** — 25 2구역 사막 타일 5종 + 구역별 타일 세트(G-15, 12-9번) · 28 갑옷 부품 5종을 루멘 · 시아 가슴 뼈에(G-16, 12-10번).
→ **2026-11-05 29번 일부: 아이템 아이콘 60종** (G-17, 메뉴 Setup/Item Icons) — 바닥 모델을 게임 카메라 각도로 찍어 투명 PNG(`Art/Icons3D`, 128 px) → `ItemVisualCatalog.Icon3D`, 3D 그래픽이면 인벤토리 · 빠른 칸이 먼저 씀(픽셀 그림은 2D용으로 남김). 남은 29: 상태 아이콘 · 막대 · 틀 · 로그 바탕 · 결과 화면 · 너클 아이콘(모델 없음).
→ **2026-11-05 29번 나머지 연결됨** (G-18, 메뉴 Setup/UI Art — 12-11번): 상태 아이콘 12 · 주문(속성) 아이콘 8 + 융합 · 창 틀 · 칸 틀 4상태 · 막대 틀 · 로그 바탕 · 층 표시판 · 승리 / 패배 장식 · 머리 위 피해 · 회복 숫자. **★ 남은 그래픽 1 ~ 3차 모두 연결됨** (남음: 25층 보스 2번 보류 · 너클 모델 · 마우스 커서).

| # | ID | 무엇 | 지금 | 만들 것 |
|---|---|---|---|---|
| 25 | 2구역 타일 세트 | 사막 피라미드 바닥 · 사암 벽 · 기둥 · 계단 | 1구역 타일 + 모래빛 해 색 | 타일 모듈 5 + 구역별 `TileVisualSet` 고르기 (코드 필요) |
| 26 | `reaction.grass_ignite` · `reaction.door_ignite` | 풀 · 문 점화 | 없음 (화염 지대 이펙트로는 보임) | 점화 순간 효과 1 (두 키 공용) |
| 27 | 아이템 사용 · 함정 · 보스 행동 | 물약 깨짐 · 주문서 읽기 · 함정 발동 · 순간이동 · 소환 · 힘 모으기 | 전용 이펙트 키 없음 (코드에서 키를 조합하는 곳은 확인 못 함) | 이펙트 6 + 해당 이벤트 연결 (코드 필요) |
| 28 | 갑옷 입은 모습 | 플레이어 모델에 갑옷 | 안 보임 (장착 표시 코드 없음 — 확인 필요) | 갑옷 메시 또는 재질 바꾸기 |
| 29 | UI (G-4, 9번) | 상태 아이콘 · 막대 · 빠른 칸 · 인벤토리 · 로그 바탕 · 결과 화면 | 기본 uGUI · 색 칸 + 글자 | 9번 목록 + 새 상태 6 · 아이템 아이콘 (모델 렌더로 만들지 결정 필요) |

합계: 1차 모델 7(+보스 1 보류) · 이펙트 2 · 바닥 무늬 7 / 2차 소품 모델 약 20 · 칸 모듈 6 · 이펙트 8 / 3차 타일 세트 5 · 이펙트 7 · UI.

---

## 0. 먼저 정할 것 — 3D를 어떻게 화면에 올릴지 (기술 스택 영향)

CLAUDE.md 기술 스택은 지금 **"2D 스프라이트 + 고각도 아트"** · **"URP 2D Renderer"** 가 둘 다 *확정 2026-10-14, 변경 금지*다. 3D 모델을 쓰는 방법은 둘이다.

| | A. 실시간 3D (모델을 그대로 씬에) | B. 3D 모델 → 2.5D 스프라이트로 미리 렌더 |
|---|---|---|
| 화면 | 진짜 3D, 카메라 각도 · 조명 · 그림자 자유 | 고각도에서 찍은 그림 — 겉보기는 3D, 실제는 2D |
| 기술 스택 | **변경 필요**: URP 2D Renderer → URP Universal(3D) Renderer, "2D 스프라이트" → "3D 모델" | **변경 없음** (2D 그대로) |
| 표현 레이어 수정 | 큼: Tilemap → 칸마다 3D 모듈 배치, 유닛 SpriteRenderer → 모델 + Animator, 안개 · 지형 덮개 · 조준을 3D(데칼 · 셰이더)로, 카메라 기울임, 화면 좌표 → 칸 변환을 레이 대신 평면 계산으로 | 작음: 스프라이트 교체 + 방향별 그림 고르기 |
| 애니메이션 | 리깅 + 애니메이션 클립 (Animator) | 방향 × 동작마다 렌더한 프레임 (용량 큼) |
| 이펙트 | 3D 파티클 · VFX | 2D 파티클 · 시트 |
| 규칙 코드 (Simulation) | **변경 없음** (규칙 1 — 표현만 바뀜) | 변경 없음 |
| 나중에 픽셀 전환 | 표현 레이어를 2D로 다시 만들어야 함 | 렌더 설정만 바꿔 다시 찍기 |

**결정 (2026-10-18, 사용자): A (실시간 3D)** — CLAUDE.md 기술 스택 · 아트 규칙을 바꾸고 TASKS G-1에 기록했다. 0-1 공통 규격은 추천값으로 시작한다 (G-1 그레이박스에서 카메라 각도 · 벽 높이는 인스펙터 값이라 바로 바꿀 수 있음).
아래 목록은 A 기준이다 (B여도 필요한 모델 목록은 같고, 납품물이 "모델 + 애니메이션" 대신 "렌더한 스프라이트"가 된다).

### 0-1. 3D 공통 규격 (A 기준, 정할 것)

| # | 항목 | 추천 | 이유 |
|---|---|---|---|
| S1 | 단위 | **1칸 = 1 유닛 = 1 m** | 지금 Grid 셀 1 × 1과 같음 — 규칙 · 좌표 변환 그대로 |
| S2 | 카메라 | **원근 또는 정사영, 아래로 50~60° 기울임**, 세로 약 12칸 보이게 | 지금 화면 범위(orthographic size 6) 유지, 고각도 2.5D 느낌 |
| S3 | 벽 높이 | **1.0~1.2 m** (낮게) — 또는 2 m + 카메라 쪽 벽 반투명 | 높으면 고각도에서 뒤 칸 · 유닛을 가림 |
| S4 | 유닛 키 | 플레이어 · 적 **0.8~1.0 m**, 보스 **1.6~2.0 m** (판정은 1칸 그대로) | 칸 안에 들어오는 비율 · 보스는 한눈에 크게 |
| S5 | 기준점(피벗) | 모델 **발밑 중앙 = 원점**, 앞 = +Z | 칸 중앙에 바로 놓기 |
| S6 | 폴리곤 (삼각형) | 타일 모듈 ≤ 500 · 소품 ≤ 1,500 · 플레이어 · 적 3,000~6,000 · 보스 ≤ 12,000 | 50 × 50 맵 + 화면에 수십 개 — 모바일까지 생각하면 여유 |
| S7 | 텍스처 | 모델당 1장 (베이스컬러 + 필요하면 노멀), 유닛 1024², 타일 · 소품 512² 아틀라스 | 재질 수를 줄여 배칭 |
| S8 | 화풍 | **스타일라이즈드(굵은 형태 · 단순한 색)**, 속성 색이 잘 보이게 채도 낮은 환경 + 밝은 이펙트 | 반응 · 상태 가독성이 핵심 (아래 0-2) |
| S9 | 파일 형식 | **FBX** (Unity 기본 지원) — Varco가 GLB만 내면 glTF 가져오기 패키지 또는 변환 필요 | Varco 출력 형식 · 리깅 지원 여부는 **확인 필요** |
| S10 | 리깅 · 애니메이션 | 플레이어 · 인간형 적 = 휴머노이드 리그(애니메이션 공유), 보스 · 비인간형 = 제네릭 | Varco가 리깅 · 애니메이션까지 하는지 **확인 필요** — 안 되면 별도 도구로 리깅 · 클립 제작 |
| S11 | 방향 | 8방향 회전 (모델이라 추가 비용 없음) | 지금 이동이 8방향 |

### 0-2. 속성 · 상태 색 기준 (지금 그레이박스 색 → 3D 이펙트 · 재질도 같은 계열)

| 대상 | 지금 색 (RGB 0~1) | 쓰는 곳 |
|---|---|---|
| 불 · 화상 · 화염 지대 | 주황 (1, 0.5, 0.1) | 불꽃 · 화상 효과 · 지대 |
| 얼음 · 둔화 · 빙판 | 하늘색 (0.35, 0.85, 1) · 빙판 (0.75, 0.92, 1) | |
| 빙결 | 흰빛 하늘색 (0.85, 0.97, 1) | 얼음 껍질 |
| 번개 · 마비 | 노랑 (1, 0.95, 0.25) | |
| 독 · 중독 · 독구름 | 보라 (0.7, 0.3, 1) | |
| 물 · 젖음 | 파랑 (0.15~0.25, 0.45, 1) | |
| 오염된 물 | 탁한 초록 (0.4, 0.6, 0.15) | |
| 대지 · 바람 | 미정 (Phase 7) | 팔레트 자리만 |

---

## 1. 맵 — 던전 모듈 키트 (지금 Tilemap `Grid/Terrain` → 칸마다 3D 모듈)

맵은 50 × 50 칸, 칸마다 `TileType` 하나. 모듈을 칸 단위(1 × 1 m)로 만들어 조립한다.

| ID (`TileType`) | 이름 | 필요 모델 | 변형 · 조각 | 우선 |
|---|---|---|---|---|
| `Floor` | 바닥 | 바닥 판 1 × 1 m | 3~4종 무작위 (금 · 이끼 · 돌 판) | 1차 |
| `Wall` | 벽 | **모듈 세트**: 일자 · 바깥 모서리 · 안쪽 모서리 · 끝 · T자 · 십자 · 기둥(사방 막힘) | 이웃 칸 모양으로 고르는 규칙(표현 레이어 코드) | 1차 |
| `StairsDown` | 내려가는 계단 | 바닥 아래로 꺼지는 계단 (구멍 + 계단) | — | 1차 |
| `StairsUp` | 올라가는 계단 (3-4a) | 바닥 위로 올라가는 계단 — 내려가는 것과 한눈에 구분 | — | 1차 |
| `Empty` | 빈칸 | 없음 (맵 밖) | — | — |
| (보스 방) | 5층 보스 방 21 × 15 (3-4b) | 보스 방 바닥 · **기둥 4개** · 벽 장식 | 1~2종 | 1차 (기둥) · 2차 (장식) |
| (소품) | 분위기용 | 횃불(벽걸이) · 돌무더기 · 뼈 · 통 · 거미줄 | 5~8종, 판정 없음(걸을 수 있는 칸엔 낮은 것만) | 2차 |
| (테마) | 5층 단위 던전 테마 (GDD 런 구조) | 위 키트 한 벌 = 테마 1개 | 1~5층 테마부터 | 1차는 1테마 |

---

## 2. 지형 효과 (지금 Tilemap `Grid/TerrainFx` 색 덮개) — `TerrainEffectType`

한 칸에 여러 효과가 겹칠 수 있다 → 3D에서도 **겹쳐도 각각 보이게** (바닥 데칼 + 파티클 조합).

| ID | 이름 | 3D 표현 | 우선 |
|---|---|---|---|
| `FireZone` | 화염 지대 | 그을린 바닥 데칼 + 불꽃 파티클 (칸 단위, 루프) | 1차 |
| `PoisonCloud` | 독구름 | 낮게 깔린 보라 연기 파티클 (반투명, 유닛 발목~무릎 높이) | 1차 |
| `Water` | 물 타일 | 얕은 물 평면 (물결 셰이더) — 이웃 물 칸과 이어 보이게 | 1차 |
| `ContaminatedWater` | 오염된 물 | 탁한 초록 물 평면 + 거품 파티클 | 1차 |
| `IcePatch` | 빙판 (3-3a) | 얼음 판 메시 (반투명 · 반사) | 1차 |

---

## 3. 시야 · 안개 (지금 Tilemap `Grid/Fog`)

모델 없음 — 셰이더 · 덮개로 처리 (표현 레이어 작업).

| 항목 | 3D 표현 | 우선 |
|---|---|---|
| 안 본 칸 | 완전 검정 (칸 위 검은 덮개 또는 셰이더) | 1차 |
| 본 적 있는 칸 | 어둡게 + 채도 낮춤, 그 칸의 적은 안 보임 (지금 규칙 그대로) | 1차 |
| 시야 경계 | 부드러운 가장자리 | 3차 |

---

## 4. 캐릭터 (`EntityVisualCatalog`, VisualId)

| VisualId | 이름 | 수치 | 모델 | 애니메이션 (1차 / 2차) | 우선 |
|---|---|---|---|---|---|
| `player` | 플레이어 (마법사) | HP 20 · 근접 3 | 인간형 마법사, 지팡이 | **대기 · 걷기 · 주문 시전 · 피격 · 사망** / 근접 공격 · 융합 시전 · 계단 이용 | 1차 |
| `enemy_melee` | 근접 적 | HP 8 · 피해 2 | 작은 근접형 몬스터 (예: 고블린 · 해골) | **대기 · 걷기 · 공격 · 피격 · 사망** | 1차 |
| `boss` | 임시 보스 (3-4b) | HP 60 · 피해 5 · 시야 10 | 큰 근접형 (판정은 1칸, 모델은 2배 크기) | **대기 · 걷기 · 공격 · 피격 · 사망** / 등장 · 포효 | 1차 |

공통: 발밑 그림자(원형 블롭 또는 실시간 그림자) · 선택 표시 링(조준 대상) — 1차.
적이 늘어날 때(Phase 6)를 생각해 근접 적은 휴머노이드 리그로 만들면 애니메이션을 돌려쓰기 좋다.

### 4-1. 캐릭터 위 상태 효과 (지금은 몸 색 틴트 1개)

3D에서는 **여러 상태를 동시에** 보여 줄 수 있다 (지금 틴트 우선순위: 빙결 > 마비 > 화상 > 중독 > 둔화 > 젖음).

| 상태 (`StatusType`) | 3D 표현 | 우선 |
|---|---|---|
| 빙결 `Frozen` | **얼음 껍질 메시**가 몸을 감쌈 + 애니메이션 정지 | 1차 |
| 마비 `Paralyze` | 몸 주위 전기 스파크 파티클 + 떨림 | 1차 |
| 화상 `Burn` | 몸에 작은 불꽃 파티클 | 1차 |
| 중독 `Poison` | 보라 거품 · 방울 파티클 + 몸 재질에 보라 기운 | 1차 |
| 둔화 `Slow` | 발밑 서리 파티클 + 애니메이션 느리게 | 2차 |
| 젖음 `Wet` | 물방울 떨어짐 + 재질 젖은 광택 | 2차 |

### 4-1. 구역 적 · 보스 모델 (3-17, 2026-11-04 — 지금은 그레이박스 색, Varco 생성 대기) — 우선순위는 맨 위 "★ 남은 그래픽" 1차

| VisualId | 무엇 | 지금 | 비고 |
|---|---|---|---|
| `enemy_rat` · `enemy_bunny` · `enemy_slime` | 쥐 · 토끼 · 슬라임 (숲 속 동굴) | Varco 모델 연결됨 | |
| `enemy_goblin_shaman` | 고블린 주술사 — 작은 초록 고블린, 지팡이 · 깃털 | 초록 캡슐 | 1차 |
| `enemy_scorpion` | 사막 전갈 — 큰 집게 · 꼬리 독침 | 붉은 갈색 | 1차 |
| `enemy_jackal` | 자칼 — 마른 개과, 황토색 | 황토 | 1차 |
| `enemy_mummy` | 미라 — 붕대 감은 사람형, 느린 걸음 | 베이지 | 1차 |
| `enemy_desert_shaman` | 사막 주술사 — 두건 · 금 장신구 · 모래 지팡이 | 금색 | 1차 |
| `boss_pharaoh` | 파라오 술사 (10층 보스) — 황금 가면 · 지팡이, 2칸 키 | 금색 큰 캡슐 | 1차 (3-17e) |
| 사막 바닥 · 벽 · 기둥 | 모래 바닥 · 사암 벽 · 피라미드 벽돌 | 해 색만 모래빛 | 2구역 타일 세트 |
| `enemy_frost_bat` (3-18) | 서리 박쥐 — 작은 푸른 박쥐, 날개에 서리 | Varco 모델 연결됨 (2026-11-05) | 얼음 동굴 |
| `enemy_snow_wolf` | 눈늑대 — 흰 털 늑대 | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `enemy_ice_golem` | 얼음 골렘 — 얼음 덩어리 몸, 느리고 큼 (1.2 m) | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `enemy_frost_shaman` | 서리 주술사 — 털가죽 두건 · 얼음 지팡이 | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `boss_ice_witch` | 얼음 마녀 (15층 보스) — 얼음 왕관 · 긴 망토, 2칸 키 | Varco 모델 연결됨 (2026-11-05) | 〃 (보스 1.8배) |
| 얼음 동굴 바닥 · 벽 · 기둥 | 얼어붙은 돌 바닥 · 얼음 결정 벽 | 해 색만 하늘색 | 3구역 타일 세트 |
| `enemy_fire_salamander` (3-19) | 화염 도롱뇽 — 낮고 긴 붉은 도마뱀, 등에 불꽃 | Varco 모델 연결됨 (2026-11-05) | 용암 동굴 |
| `enemy_flame_imp` | 불꽃 임프 — 작은 뿔 달린 불 정령 | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `enemy_magma_golem` | 마그마 골렘 — 검은 바위 몸 + 갈라진 틈의 용암 (1.2 m) | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `enemy_fire_shaman` | 화염 주술사 — 붉은 로브 · 불 지팡이 | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `boss_lava_lord` | 용암 군주 (20층 보스) — 용암이 흐르는 거대한 바위 거인, 2칸 키 | Varco 모델 연결됨 (2026-11-05) | 〃 (보스 1.8배) |
| `enemy_clockwork_soldier` (3-20) | 녹슨 시계병 — 태엽 · 톱니가 드러난 녹슨 갑옷 병사 | Varco 모델 연결됨 (2026-11-05) | 시간 신전 폐허 |
| `enemy_time_afterimage` | 시간 잔상 — 반투명 보라 인영, 뒤로 잔상이 끌림 | Varco 모델 연결됨 (2026-11-05) | 〃 (반투명 재질 후보) |
| `enemy_temple_guardian` | 신전 수호상 — 금 간 돌 석상, 느리고 큼 (1.2 m) | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `enemy_time_priest` | 시간 사제 — 보라 로브 · 모래시계 지팡이 | Varco 모델 연결됨 (2026-11-05) | 〃 |
| `boss_time_guardian` | 시간의 수호자 (25층 최종 보스) — 금빛 시계 고리를 두른 거인, 2칸 키 | Varco 모델 연결됨 (2026-11-05) | 〃 (보스 1.8배) |
| 시간 신전 바닥 · 벽 · 기둥 | 금 간 대리석 바닥 · 무너진 신전 벽 · 부러진 기둥 | 해 색만 보랏빛 | 5구역 타일 세트 |
| 용암 동굴 바닥 · 벽 · 기둥 · 용암 웅덩이 | 검은 현무암 바닥 · 붉게 갈라진 벽 · 용암 표면 | 해 색만 붉음, 웅덩이는 화염 지대 불씨 효과 | 4구역 타일 세트 + 용암 바닥 무늬(지금은 그을음 `fx_scorch`) |

---

## 5. 주문 (`SpellCatalog`) — 투사체 · 맞는 이펙트

| ID | 이름 | 모양 | 3D 이펙트 | 우선 |
|---|---|---|---|---|
| `fire_basic` | 불꽃 화살 | Bolt (첫 유닛까지) | 불덩이 투사체 + 맞는 칸 작은 폭발 | 1차 |
| `ice_basic` | 얼음 화살 | Bolt | 얼음 창 투사체 (작은 메시) + 얼음 파편 | 1차 |
| `lightning_basic` | 번개 줄기 | Beam (직선 관통, 벽에서 멈춤) | 지나간 칸 전부를 잇는 번개 선 (짧게 번쩍) | 1차 |
| `poison_basic` | 독 폭발 | Burst 3 × 3 | 보라 구체 투사체 + 3 × 3 터짐 | 1차 |
| `water_basic` | 물보라 | Burst 3 × 3 | 물 구체 투사체 + 3 × 3 물 튀김 | 1차 |

공통: 시전 시 지팡이 끝 빛 (속성 색) — 1차.

### 5-1. 융합 주문 (`FusionCatalog`) — 이펙트만

| ID | 이름 | 모양 | 3D 이펙트 | 우선 |
|---|---|---|---|---|
| `fusion_fire_water` | 용암 분출 | Burst 3 × 3 | 바닥에서 용암이 솟음 | 2차 |
| `fusion_water_poison` | 오염 물결 | Burst 3 × 3 | 탁한 물결이 덮침 | 2차 |
| `fusion_fire_lightning` | 폭발 마법 | Beam | 불 + 번개가 꼬인 광선 | 2차 |
| `fusion_ice_water` | 빙결 파도 | Burst 3 × 3 | 파도가 덮치며 얼어붙음 | 2차 |

공통: 융합 시전 순간 두 속성 색이 합쳐지는 발밑 마법진 — 2차.

---

## 6. 반응 이펙트 (`ReactionCatalog`, 로그 "독화! 적 #3")

같은 이름은 이펙트 하나로 묶을 수 있다.

| 이펙트 | 반응 ID | 3D 표현 | 우선 |
|---|---|---|---|
| 독화 | `toxic_ignite` · `toxic_ignite_cloud` | 대상에 보라 불꽃 솟음 | 1차 |
| 유독성 폭발 | `toxic_burst` | 대상 중심 3 × 3 보라 폭발 | 1차 |
| 가스 폭발 | `gas_explosion` | 구름 칸 중심 3 × 3 폭발 + 연기 사라짐 | 1차 |
| 증발 | `evaporate` 외 3줄 | 하얀 증기 기둥 | 1차 |
| 빙결 · 결빙 | `freeze` · `freeze_water` · `freeze_contaminated` | 서리가 퍼지며 얼어붙음 | 1차 |
| 파쇄 | `shatter` | 얼음 껍질이 깨져 파편 튐 | 1차 |
| 감전 | `shock` | 대상 스파크 + **유닛에서 유닛으로 잇는 사슬 번개** (인원 제한 없이 이어짐) | 1차 |
| 수면 감전 | `shock_water` · `shock_contaminated` | 물 표면을 따라 흐르는 전기 | 1차 |
| 소화 | `extinguish` | 불이 꺼지며 증기 | 2차 |
| 오염 | `contaminate` | 물이 탁하게 번짐 | 2차 |
| 해동 | `thaw` · `thaw_ice` | 얼음이 녹아 물방울 | 2차 |

---

## 7. 조준 미리보기 (지금 Tilemap `Grid/Aim` 색 칸)

모델 없음 — 바닥 위 데칼 · 평면 (항상 맨 위에 보이게).

| 항목 | 지금 색 | 3D 표현 | 우선 |
|---|---|---|---|
| 경로 칸 | 노랑 35% | 바닥 점선 · 화살 데칼 | 1차 |
| 맞는 칸 | 주황 55% | 칸 테두리 강조 데칼 | 1차 |
| 조준 커서 | 흰색 60% | 칸 모서리 괄호 데칼 (살짝 위아래로 움직임) | 1차 |
| 막힘 · 못 씀 | 빨강 50% | 빨간 X 데칼 | 1차 |
| 반응 · 감전 예상 범위 | 없음 | Phase 5 조합 가시성 UI | 3차 |

---

## 8. 아이템 (`ItemCatalog`)

| ID | 이름 | 필요 에셋 | 우선 |
|---|---|---|---|
| `fusion_device` | 마법 융합기 | 3D 모델 (바닥에 떨어진 모습 · 나중에 획득용) + UI 아이콘 (모델을 렌더해서) | 1차 (아이콘) · 2차 (모델) |
| (장비) | 결과 화면 "장비" 자리 | Phase 6 | — |
| (무기 6종) | 소검 · 손도끼 · 창 · 육척봉 · 더크 · 손낫 | 3D 모델 6 + 아이콘 6 (+ 공격 클립) — 목록 `Docs/무기_에셋.md`. ~~아직 연결 안 함~~ → **연결됨** (바닥 · 손에 든 모습, `VarcoModelSetup.WeaponSpecs`) | 1차 |
| (물약 12 · 주문서 12) | 힘 물약 · 강화 주문서 등 (3-7b) | ~~3D 아직~~ → **연결됨** (3-9a, 물약 모델 1 + 12색 · 빈 주문서 + 룬 12, `ConsumableArtSetup`). 목록 `Docs/소모품_에셋.md` | 1차 |
| (그 밖 아이템 20종) | 갑옷 · 마법서 · 음식 · 금화 · 열쇠 · 다트 · 투창 · 씨앗 · 융합기 | 맨 위 "★ 남은 그래픽" 2차 12~18번 | 2차 |

---

## 9. UI (uGUI 2D 그대로, 기준 1920 × 1080)

UI는 3D로 바꾸지 않는다. 아이콘은 **3D 모델 · 이펙트를 렌더해서** 만들면 화풍이 맞는다.

| 화면 요소 | 지금 | 필요 에셋 | 우선 |
|---|---|---|---|
| 상태 아이콘 6종 | 색 칸 + "중독 5" | 아이콘 6 (화상 · 둔화 · 중독 · 마비 · 젖음 · 빙결) + 틀 | 1차 |
| 주문 아이콘 5종 | 글자 | 아이콘 5 (빠른 칸 · 인벤토리) | 1차 |
| 아이템 아이콘 | 글자 | 마법 융합기 1 | 1차 |
| HP · MP 막대 | 단색 360 × 30 | 막대 틀 · 채움 (9-slice) | 1차 |
| 빠른 칸 6개 | 사각형 + 글자 | 칸 틀 4상태 (보통 · 선택 · 비어 있음 · 마나 부족) | 1차 |
| 전투 로그 8줄 | 글자만 (밝은 바닥에서 덜 읽힘) | 반투명 바탕 판 | 1차 |
| 인벤토리 창 | 어두운 판 + 칸 20 + 메뉴 | 창 틀 · 칸 틀 (보통 · 선택 · 융합 첫째 · 비활성) · 메뉴 버튼 | 1차 |
| 결과 화면 | 덮개 + 상자 | 상자 틀 · 승리 / 패배 장식 | 2차 |
| 층 표시 | 없음 | "2층" 표시판 | 2차 |
| 피해 숫자 | 없음 (로그만) | 머리 위로 떠오르는 숫자 (월드 공간 TMP) | 2차 |
| 마우스 커서 | 기본 | 커서 1~2장 | 3차 |

폰트: Galmuri11은 픽셀 폰트라 3D 화풍과 안 맞을 수 있다 → 한글 지원 일반 폰트로 바꿀지 결정 (배포 라이선스 확인) — 2차.

---

## 10. 에셋이 아닌 표현 레이어 작업 (A 선택 시, 코드 쪽)

규칙(Simulation)은 그대로이고 `Runtime/Presentation`만 바뀐다. 그래픽 작업 일정에 같이 넣어야 한다.

| 작업 | 지금 | 3D로 |
|---|---|---|
| 렌더 파이프라인 | URP 2D Renderer | URP Universal Renderer (+ 조명 · 그림자 설정) |
| 카메라 | 정사영 2D, 플레이어 칸으로 순간 이동 | 기울인 3D 카메라, 부드럽게 따라감 |
| 맵 그리기 | `TilemapPresenter` (Tilemap) | 칸마다 모듈 배치 (벽 이웃 모양 고르기 포함), 층이 바뀌면 다시 생성 |
| 유닛 | `EntityPresenter` (SpriteRenderer + 틴트) | 모델 프리팹 + Animator, 이동 보간 · 방향 회전 · 공격 · 피격 · 사망 재생 |
| 지형 효과 | `TerrainOverlayPresenter` (색 칸) | 칸마다 데칼 · 파티클 켜고 끄기 |
| 안개 | `FogPresenter` (검은 타일) | 덮개 · 셰이더 |
| 조준 | `AimPreviewPresenter` (색 칸) | 데칼 |
| 클릭 → 칸 | 화면 좌표 → Grid 변환 | 카메라 레이 → 바닥 평면(y = 0) 교점 → 칸 (물리 · 충돌체 안 씀 — 규칙 2) |
| 이펙트 | 없음 | 주문 · 반응 이벤트 구독 → VFX 재생 (로직은 즉시, 연출이 따라감 — 실시간 모드에선 1초 안) |
| 데이터 | `EntityVisualCatalog` (VisualId → 스프라이트 · 색) | VisualId → 프리팹 (ID 구조 그대로) |

---

## 11. 합계 (1차)

| 분류 | 개수 |
|---|---|
| 던전 모듈 (1테마) | 바닥 3~4 · 벽 모듈 7 · 계단 2 · 보스 방 기둥 1 |
| 지형 효과 | 5 (데칼 · 파티클 · 물 셰이더) |
| 캐릭터 모델 | 3 (플레이어 · 근접 적 · 보스) + 각 애니메이션 5개 = 클립 15 |
| 캐릭터 상태 효과 | 4 (얼음 껍질 메시 · 스파크 · 불꽃 · 독) |
| 주문 이펙트 | 5 (투사체 · 맞는 효과) + 시전 빛 1 |
| 반응 이펙트 | 8 |
| 조준 데칼 | 4 |
| UI 아이콘 | 12 (상태 6 · 주문 5 · 아이템 1) |
| UI 틀 | 막대 · 빠른 칸 · 인벤토리 창/칸 · 로그 바탕 · 아이콘 틀 |

2차: 소품 5~8 · 보스 방 장식 · 융합 이펙트 4 + 마법진 · 반응 3 · 둔화 · 젖음 효과 · 추가 애니메이션 · 융합기 모델 · 결과 화면 장식 · 층 표시 · 피해 숫자 · 폰트.
3차: 시야 가장자리 · 커서 · 조합 예상 범위 표시.

---

## 12. 연결된 Varco 모델 (G-2a, 2026-10-18)

원본 FBX · 텍스처 · 재질은 `Assets/_Project/Art/Varco3D/`(블렌더 원본은 Unity가 무시하는 `Source~/`), 표시용 프리팹은 `Assets/_Project/Prefabs/Varco/`.
프리팹 생성 · 연결은 메뉴 **ProjectM/Setup/Varco Models**(멱등) — 아래 크기 · 방향 보정은 그 스크립트(`VarcoModelSetup`)의 표와 같은 값이다.
Varco는 모든 모델을 **가장 긴 쪽 1 m**로 보내므로 크기는 프리팹에서 맞춘다. 모든 모델의 앞 = +Z (방향 보정 0°). 블렌더에서 나온 FBX는 조명 · 카메라 가져오기를 끈다 (SheepMage에 1000 W 점광원이 섞여 있었음).

| 연결 | 모델 | 프리팹 | 배율 | 결과 크기 (가로 × 높이 × 깊이 m) | 삼각형 | 뼈대 |
|---|---|---|---|---|---|---|
| `player` | SheepMage + Prop_MageStaff(오른손 뼈, 0.85 m, 길이 40% 지점을 쥠) | Unit_Player | 1 | 0.77 × 0.99 × 0.26 | 6,000 + 1,586 | 휴머노이드 이름 21개 (Generic) |
| `enemy_melee` 변형 1 | Enemy_Rabbit | Unit_Rabbit | 0.8 | 0.40 × 0.62 × 0.80 | 5,000 | 28개 (Generic) |
| `enemy_melee` 변형 2 | Enemy_Mouse | Unit_Mouse | 0.8 | 0.54 × 0.56 × 0.80 | 5,000 | 없음 |
| `enemy_melee` 변형 3 | Enemy_Slime | Unit_Slime | 0.8 | 0.80 × 0.54 × 0.80 | 3,000 | 없음 |
| `boss` | Boss_SlimeGirl | Unit_Boss | 0.6 (2026-11-05 사용자 — 1/3) | 약 0.64 m | 12,000 | **Mixamo 자동 리깅 `Boss_SlimeGirl_Rigged.fbx`** (2026-11-05 — T자세 고정 해결, Humanoid · 플레이어와 같은 Idle · Walk, 배율 1.8 — 어깨는 Blender로 고침: 위팔 시작 0.094 m · 팔꿈치 0.233 m, 옆 머리카락의 팔 무게는 가슴으로 (`Tools/blender_slime_shoulder.py`)) |
| `Floor` (90° 단위로 위치에 따라 돌림) | Tile_Floor | Tile_Floor | 1 (윗면 = y 0으로 내림) | 1 × 0.06 × 0.98 | 1,000 | — |
| `Wall` (이어진 벽) | Tile_Wall | Tile_Wall | 1 | 1 × 1 × 1 | 1,132 | — |
| 외톨이 벽 (상하좌우에 벽 없음) | Tile_Pillar + 바닥 | Tile_Pillar | 1 | 0.58 × 1 × 0.58 | 1,716 | — |
| `StairsDown` | Tile_StairsDown | Tile_StairsDown | 1 | 1 × 0.39 × 1 | 1,874 | — |
| `StairsUp` | Tile_StairsUp | Tile_StairsUp | 1 | 0.92 × 0.57 × 0.98 | 1,870 | — |

- 근접 적은 엔티티 ID로 셋 중 하나를 고정해 보여 준다 (결정 2026-10-18 — 능력은 같고 표시만 다름).
- 프리팹 모델은 텍스처 그대로 그린다 (카탈로그 색은 그레이박스 캡슐에만). 상태 틴트는 그대로 곱해진다.
- 타일 모듈 삼각형이 규격(≤ 500)보다 2~4배 많다 — 바닥 칸이 1,000개를 넘는 층에서 성능 확인 필요 (재질은 GPU 인스턴싱 켬).
- 애니메이션 클립 없음 → G-2b에서 코드 움직임 (결정 2026-10-18).

### Varco로 만들 것 / 다른 방법이 필요한 것

| Varco (3D 모델) | 확인 필요 (Varco 지원 여부) | Unity 쪽 (파티클 · 셰이더 · 데칼) |
|---|---|---|
| 캐릭터 3 · 던전 모듈 · 기둥 · 계단 · 소품 · 얼음 껍질 · 얼음 창 · 융합기 | 리깅 · 애니메이션 · FBX 출력 · 폴리곤 수 조절 · 모듈 치수(1 m) 맞추기 | 지형 효과 · 상태 · 주문 · 반응 이펙트 대부분 · 안개 · 조준 · 물 표면 |

---

### 12-3. 주인공 루멘 · 시아 (G-7, 2026-11-02 · 11-03 — 캐릭터 선택에 연결)

| 모델 | 구성 | 크기 (가로 × 높이 × 깊이 m) | 삼각형 | 재질 · 텍스처 | 뼈대 |
|---|---|---|---|---|---|
| Hero_Blond | 두상(Varco, 눈 파임을 평평하게 채움) + `Hero_Torso2`(옷 · 바지 · 신발) + `Hero_Coat`(맞춤 셰이프 키 확정) 한 메시 | 1.59 × 1.75 × 0.58 (T자세) | 100,686 (두상 4만 · 옷 3만 · 코트 3.07만) | `Hero_Blond_Head/Body/Coat` 각 `_base_color` · `_normal` (2048) | 없음 (리깅 전 원본) |
| Sia | 두상(머리카락 포함) + 내의 몸통 + 상의(재킷) · 하의(반바지 · 띠) 한 메시 | 1.65 m 높이 (T자세) | 115,000 | `Sia_Head/Body/Top/Bottom` 각 `_base_color` · `_normal` (상의 · 하의 색 4096, 나머지 2048) | 없음 (리깅 전 원본) |
| Hero_Blond_Rigged · Sia_Rigged | 위 두 모델을 Mixamo 자동 리깅 (턱 · 손목 · 팔꿈치 · 무릎 · 사타구니 표시) | 같음 | 같음 | 같은 재질 이름 → 같은 텍스처 | Mixamo 뼈대, **Humanoid** (Varco Models 메뉴) |

- 원점 = 발밑 가운데, 앞 = +Z. 블렌더 작업 파일은 저장소 밖 scratchpad의 `Hero_Blond_v2.blend` (481 MB라 `Source~/`에 넣지 않음).
- 연결 (2026-11-03): `hero_blond` → `Unit_Lumen`(Hero_Blond_Rigged), `hero_black` → `Unit_Sia`(Sia_Rigged), 배율 둘 다 0.57 (루멘 약 1.0 m · 시아 약 0.94 m, 양 마법사 0.99 m). 양 마법사와 같은 Idle · Walk 컨트롤러 · 무기 6종을 Humanoid 오른손 뼈(`mixamorig:RightHand`)에 붙인다. `player`(캐릭터를 고르지 않은 경우)는 그대로 양 마법사. → 변경 (2026-11-05, 사용자): 루멘 · 시아는 **맨손으로 시작** — 지팡이를 붙이지 않음(무기를 들면 그 무기만 손에). 양 마법사(`player`)는 지팡이 그대로.
- 캐릭터 선택 카드 그림 = 7등신 일러스트 `Art/Pixel/Characters/<ID>_illust.png` (시아 = Varco 일러스트 후보 3번).

### 12-5. 구역 적 · 보스 · 상인 (2026-11-05 — ★ 1차)

`VarcoModelSetup.ZoneUnits` 표와 같은 값. 원본 `Art/Varco3D/<모델>.fbx`, 사람형은 Mixamo 자동 리깅 `<모델>_Rigged.fbx`(Humanoid, 플레이어와 같은 Idle · Walk 컨트롤러), 텍스처 `Textures/<모델>_base_color · _normal`(FBX에 묻힌 그림을 꺼냄), 재질 `Materials/<모델>_Mat`(Setup이 만들고 FBX 묻힌 재질 `material_0`을 바꿔 연결).

| VisualId | 모델 | 프리팹 | 배율 | 게임 안 키 | 폴리곤 | 리깅 | 손에 든 것 |
|---|---|---|---|---|---|---|---|
| `enemy_goblin_shaman` | Enemy_GoblinShaman | Unit_GoblinShaman | 0.8 | 약 0.85 m | 6,000 | Mixamo (손가락 없음 25뼈) | `Prop_Staff_Goblin` 0.85 m |
| `enemy_scorpion` | Enemy_Scorpion | Unit_Scorpion | 0.8 | 길이 0.8 m | 5,000 | 없음 (코드 움직임) | — |
| `enemy_jackal` | Enemy_Jackal | Unit_Jackal | 0.8 | 길이 0.8 m | 5,000 | 없음 | — |
| `enemy_mummy` | Enemy_Mummy | Unit_Mummy | 1.0 | 약 1.1 m | 6,000 | Mixamo | — |
| `enemy_desert_shaman` | Enemy_DesertShaman | Unit_DesertShaman | 0.88 | 약 1.05 m | 6,000 | Mixamo | `Prop_Staff_Sand` 1.15 m |
| `boss_pharaoh` | Boss_Pharaoh | Unit_Pharaoh | 1.7 | 약 1.85 m | 12,000 | Mixamo | `Prop_Staff_Pharaoh` 1.8 m |
| `shopkeeper` | NPC_Shopkeeper | Unit_Shopkeeper | 0.9 | 약 0.95 m | 6,000 | Mixamo | — |
| `enemy_frost_bat` | Enemy_FrostBat | Unit_FrostBat | 0.6 | 날개 폭 0.6 m | 5,000 | 없음 (코드 움직임) | — |
| `enemy_snow_wolf` | Enemy_SnowWolf | Unit_SnowWolf | 0.85 | 길이 0.85 m | 5,000 | 없음 | — |
| `enemy_ice_golem` | Enemy_IceGolem | Unit_IceGolem | 1.2 | 약 1.2 m | 6,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_frost_shaman` | Enemy_FrostShaman | Unit_FrostShaman | 0.9 | 약 1.0 m | 6,000 | Mixamo | — |
| `boss_ice_witch` | Boss_IceWitch | Unit_IceWitch | 1.8 | 약 1.8 m | 12,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_fire_salamander` | Enemy_FireSalamander | Unit_FireSalamander | 0.9 | 길이 0.9 m | 5,000 | 없음 | — |
| `enemy_flame_imp` | Enemy_FlameImp | Unit_FlameImp | 0.65 | 약 0.7 m | 6,000 | Mixamo | — |
| `enemy_magma_golem` | Enemy_MagmaGolem | Unit_MagmaGolem | 1.25 | 약 1.2 m | 6,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_fire_shaman` | Enemy_FireShaman | Unit_FireShaman | 0.9 | 약 1.0 m | 6,000 | Mixamo | — |
| `boss_lava_lord` | Boss_LavaLord | Unit_LavaLord | 1.9 | 약 1.9 m | 12,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_clockwork_soldier` | Enemy_ClockworkSoldier | Unit_ClockworkSoldier | 0.95 | 약 0.95 m | 6,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_time_afterimage` | Enemy_TimeAfterimage | Unit_TimeAfterimage | 1.0 | 약 1.0 m | 6,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_temple_guardian` | Enemy_TempleGuardian | Unit_TempleGuardian | 1.3 | 약 1.3 m | 6,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |
| `enemy_time_priest` | Enemy_TimePriest | Unit_TimePriest | 1.0 | 약 1.1 m | 6,000 | Mixamo | — |
| `boss_time_guardian` | Boss_TimeGuardian | Unit_TimeGuardian | 1.9 | 약 1.9 m | 12,000 | Blender (`Tools/blender_rig_from_joints.py`) | — |

- **결정 (2026-11-05, 사용자)**: 사람형 리깅은 **Mixamo**(사람형만 — 네발 · 전갈은 리깅 없이 코드 움직임). 무기 · 지팡이 등 **든 물건은 몸과 따로 만들어 Unity에서 손 뼈에 붙인다**(`AttachStaff` — 높이 1 m 소품, 원점 = 아래 끝, 손이 길이 40% 지점). 그래서 고블린 · 사막 주술사 · 파라오는 지팡이 없는 몸으로 다시 생성.
- **상점 주인 다시 디자인 (2026-11-05, 사용자)**: 통통한 안경 쓴 여성 강아지 수인(체형은 처음 안의 절반 정도로 · 더 귀엽게). 원본 2D 일러스트 `Art/Pixel/Characters/shopkeeper_illust.png`(루멘 · 시아 화풍, 빛 효과 없음) → 3D용 A자세 그림 → 3D.
- 전갈 · 자칼 FBX는 원점을 발밑으로(`pivotToBottom`) 받는다 — 처음 받은 것은 원점이 가운데라 바닥에 반쯤 묻혔다.
- Mixamo 자동 리깅 실패 (`Unknown error while generating motion`)는 턱 마커가 가슴에 놓였을 때 — 턱을 입 바로 아래에 두고, 몸이 대칭선 밖으로 치우치면 Use Symmetry를 끄고 양쪽을 따로 놓으면 됐다.
- Varco 플로우 `MoaDungeon`(f679a6c8) y 105600 ~ 117000에 프롬프트 → 이미지 → 3D 노드. 포인트 약 5,900.
- 3~5구역 15종 (2026-11-05, 사용자 요청): Varco 플로우 `MoaDungeon` y 159600 ~ 168400 (그림 2장씩 1,500 + 3D 3,000 포인트). 텍스처는 FBX에 묻힌 `base_color` · `normal`을 Blender로 꺼내 `Textures/<모델>_…`로 (Unity `ExtractTextures`는 확인 창이 떠서 MCP에서 막힘). Mixamo가 실패한 사람형 8종은 Blender로 리깅 — 정면 렌더(`Tools/blender_front_render.py`)에서 관절 픽셀을 찍어(`Tools/blender_rig_joints.json`) Mixamo 이름 25뼈를 세우고 뼈 선분까지 거리로 무게(가까운 3뼈, 1/d^4)를 칠함(`Tools/blender_rig_from_joints.py`) → Unity Humanoid · Idle · Walk 그대로.
- Mixamo 자동 리깅 (2026-11-05): **팔꿈치 표시는 실제 팔꿈치에** — 어깨에 두면 `Unknown error while generating motion`으로 실패했다(얼음 마녀 · 화염 주술사에서 확인, 화염 주술사는 팔꿈치로 옮기자 성공). 가는 로브형(주술사 · 사제 · 임프)은 성공, 큰 치마(마녀) · 갑옷 덩어리(골렘 · 시계병) · 덩굴 가장자리(잔상)는 손가락 없는 뼈대로도 실패.
- 확인 못 한 것: 게임 화면(고각도 카메라)에서의 크기 · 걷기 모습, 고블린 Idle이 비틀어져 보이는지.

### 12-6. 바닥 아이템 (2026-11-05 — ★ 2차 12 ~ 18)

메뉴 **ProjectM/Setup/Consumables**(`ConsumableArtSetup.BuildFloorItems`, 물약 · 주문서 다음)가 `Prefabs/Varco/Item_*`을 만들고 `ItemVisualCatalog`에 넣는다(Upright — 원점 = 바닥 중앙). 모델 `Art/Varco3D/Item_*.fbx`(Varco, 폴리곤 2,000 · 텍스처 1024, 원점 = 발밑으로 받음), 텍스처 · 재질 `Item_*_base_color · _normal · _Mat`.

| 아이템 ID | 모델 | 바닥 크기 (긴 쪽) | 눕힘 |
|---|---|---|---|
| `armor_cloth` · `armor_leather` · `armor_mail` · `armor_scale` · `armor_plate` | Item_ArmorCloth · Leather · Mail · Scale · Plate | 0.5 m (판금 0.55) | X −90° + Y 180° (Varco가 세워 만듦 — 목이 화면 위로) |
| `food_ration` · `food_bread` | Item_FoodRation · Item_FoodBread | 0.3 · 0.25 m | — |
| `food_mystery_meat` · `food_cooked_meat` · `food_frozen_meat` | Item_MeatMystery · Cooked · Frozen | 0.32 m | 같음 |
| `gold` · `key_iron` · `dart` · `javelin` · `fusion_device` | Item_Gold · KeyIron · Dart · Javelin · FusionDevice | 0.3 · 0.3 · 0.3 · 0.7 · 0.4 m | — |
| `spellbook_fire/ice/lightning/poison/water` | Item_Spellbook (흰 표지) × 5색 | 0.35 m | — |
| `seed_firebloom/icecap/sorrowmoss/sungrass/fadeleaf` | Item_Seed (흰 씨앗) × 5색 | 0.22 m | — |

- **결정 (2026-11-05, 사용자)**: 마법서 · 씨앗은 **모델 하나 + 색만 바꿈**(물약처럼, `TintWhite` — 밝고 채도 낮은 픽셀만 칠함). 색: 마법서 불 빨강 · 얼음 하늘 · 번개 노랑 · 독 보라 · 물 파랑, 씨앗 화염꽃 주황 · 얼음버섯 하늘 · 독이끼 초록 · 햇볕풀 노랑 · 사라짐잎 연보라 ✎ Claude.
- **얼린 고기 (2026-11-05, 사용자)**: 얼음덩이로 감싸지 말고 **고기 자체가 아주 연한 파란색** — 정체불명 고기 그림을 색만 바꿔(EditImage) 같은 모양.
- 천 갑옷은 다시 생성 (2026-11-05, 사용자): 처음 것은 눕히면 천 뭉치처럼 보여서, 가죽 갑옷 그림을 구도 참고로 넣어 펼친 누빔 조끼(끈 · 밧줄 띠)로 — 다른 갑옷과 같이 눕혀 읽힘.
- 확인 못 한 것: 게임 화면(고각도 카메라 · 떠서 도는 연출)에서의 크기.
- Varco 플로우 `MoaDungeon` y 118000 ~ 128000. 포인트 이미지 1,800 + 3D 3,400.

### 12-7. 문 · 풀 · 연금 솥 (2026-11-05 — ★ 2차 20 ~ 22)

메뉴 **ProjectM/Setup/Varco Models**가 칸 모듈을 만든다 (`VarcoModelSetup` — 모델이 하나라도 없으면 예전 그레이박스). 모델 `Art/Varco3D/Prop_DoorLeaf · Prop_DoorLeafLocked · Prop_GrassShort · Prop_GrassTall · Prop_Cauldron`.

| 칸 | 프리팹 | 구성 |
|---|---|---|
| 닫힌 문 `Door` | Tile_Door | 바닥 + 문틀(벽 모델 `Tile_Wall`로 짠 기둥 둘 · 상인방, 높이 1 m · 두께 0.3) + 문짝 2 (구멍 0.64 m를 반씩, 오른쪽은 좌우 반전) — 가운데서 만남 |
| 열린 문 `OpenDoor` | Tile_OpenDoor | 같음 + 양 경첩 축이 90° — 문짝이 +Z쪽으로 양옆 기둥에 붙음 |
| 잠긴 문 `LockedDoor` | Tile_LockedDoor | 닫힌 문과 같고 문짝 = 자물쇠 문짝 |
| 짧은 풀 `Grass` | Tile_Grass | 바닥 + 풀 덤불(0.98 × 0.98, 키 0.12 m) |
| 높은 풀 `TallGrass` | Tile_TallGrass | 바닥 + 풀 덤불(키 0.55 m — 0.15 m보다 높아 시야를 가림) |
| 연금 솥 `AlchemyPot` | Tile_AlchemyPot | 바닥 + 솥(너비 0.7 m) |

- **결정 (2026-11-05, 사용자)**: 문은 SPD처럼 **양쪽으로 여는 여닫이 양문**(미닫이 아님). 칸 종류가 바뀔 때 프리팹이 바뀌어 열고 닫는 애니메이션은 없음.
- Varco 문틀 모델은 정면 그림에서 깊이를 잘못 추정해 **십자 아치**(가로 = 깊이 1 m)로 나와 버리고, 벽 모델로 짰다 — 옆 벽과 돌 질감이 이어진다.
- 확인 못 한 것: 게임 화면에서 문 방향(TilemapPresenter가 옆 벽을 보고 돌림) · 열린 문짝이 방 안쪽 · 바깥쪽 어느 쪽으로 보이는지, 풀이 바닥 무늬 회전과 어울리는지.
- Varco 플로우 `MoaDungeon` y 129000 ~ 132000. 포인트 이미지 600 + 3D 1,200 (그중 문틀 200은 쓰지 않음).

### 12-8. 식물 5종 (2026-11-05 — ★ 2차 19)

씨앗을 심어 생긴 식물(3-16b, `TrapType` Firebloom ~ Fadeleaf)을 `TrapPresenter`가 색 마름모 대신 3D로 칸 위에 세운다 — 모델 표는 `ItemVisualCatalog`의 키 `plant_<식물>`(아이템이 아니지만 같은 카탈로그, 메뉴 Setup/Consumables `FloorItemModels`가 채움), 씬 연결은 Setup/Main Scene(`_plantModels`). 발동 · 숨김이면 지우고, 층이 바뀌면 모두 지운다. 함정은 색 칸 그대로.

| 키 | 모델 | 크기 (가장 긴 쪽) |
|---|---|---|
| `plant_firebloom` 화염꽃 | Prop_PlantFirebloom | 0.5 m |
| `plant_icecap` 얼음버섯 | Prop_PlantIcecap | 0.45 m |
| `plant_sorrowmoss` 독이끼 | Prop_PlantSorrowmoss | 0.5 m |
| `plant_sungrass` 햇볕풀 | Prop_PlantSungrass | 0.5 m |
| `plant_fadeleaf` 사라짐잎 | Prop_PlantFadeleaf | 0.5 m |

- 얼음버섯은 처음 3D에서 버섯 갓이 사라져 얼음 기둥처럼 나와 **다시 생성** (2026-11-05, 사용자): 투명 · 결정 표현을 빼고 갓이 큰 불투명 버섯 한 송이로 — 3D가 형태를 유지했다. 투명 · 결정 같은 표현은 Varco 3D가 형태를 잃기 쉽다.
- 확인 못 한 것: 게임 화면 · 안개 밖(안 보이는 칸)에서도 식물이 보이는지(안개 Tilemap은 바닥만 덮는다).
- Varco 플로우 `MoaDungeon` y 133000 ~ 135400. 포인트 이미지 500 + 3D 1,000.

### 12-9. 2구역 사막 피라미드 타일 (2026-11-05 — ★ 3차 25)

결정 (2026-11-05, 사용자): Varco 새 모델 5종 + 구역별 타일 세트. 기본 세트(`TileVisualSet.asset`)의 구역 목록 `_zoneSets`에 층 범위 6 ~ 10 → `TileVisualSet_Desert.asset`(기본 세트를 복사해 아래 타입만 바꿈 — 메뉴 Setup/Varco Models `BuildDesertTiles`). `TilemapPresenter`가 `Bind(bus, 층)` · `FloorChangedEvent`로 세트를 고르고, 구역이 바뀌는 층에서만 지금 맵을 다시 세운다 (표시만 — 규칙 · 시뮬레이션은 모름).

| 타입 | 모델 | 크기 | 후보 |
|---|---|---|---|
| `Floor` (90° 단위로 돌림) | Tile_Desert_Floor | 1 (윗면 = y 0으로 내림) | 1-2 — 큰 사암 판 + 상형문자 줄 하나 |
| `Wall` | Tile_Desert_Wall | 1 | 2-1 — 정육면체에 가까운 사암 벽돌 + 상형문자 띠 |
| 외톨이 벽 (기둥) | Tile_Desert_Pillar + 사막 바닥 | 1 | 3-2 — 연꽃 기둥머리 · 넓은 받침 |
| `StairsDown` | Tile_Desert_StairsDown | 1 | 4-2 — 사암 테두리 · 풍뎅이 조각 |
| `StairsUp` | Tile_Desert_StairsUp | 1 | 5-2 — 위에 빛 · 양옆 조각 벽 |
| 풀 · 문 · 솥 | `Tile_Desert_Grass/TallGrass/Door/OpenDoor/LockedDoor/AlchemyPot` | — | 1구역과 같은 모델을 사막 바닥 위에 다시 짬, 문틀 = 사막 벽 모델 |

- 해 색(3-17c `ZoneThemePresenter`)은 그대로 함께 쓴다. 이어하기도 저장된 층의 구역 세트 · 해 색으로 시작(`GameRoot` → `Bind(…, World.Floor)`).
- 3 ~ 5구역은 G-19 (12-12번) — 같은 방식, Setup 표 `ZoneTiles` 한 줄씩.
- Varco 플로우 `MoaDungeon` y 136600 ~ 139000. 포인트 이미지 500 + 3D 1,000.

### 12-10. 입은 갑옷 부품 (2026-11-05 — ★ 3차 28)

결정 (2026-11-05, 사용자): 갑옷 부품을 따로 만들어 뼈에 붙임 (무기 · 지팡이와 같은 방식). 몸통만 덮는 껍데기(소매 · 팔 없음) 5종 — 메뉴 Setup/Varco Models `AttachArmors`가 루멘 · 시아 프리팹의 가슴 뼈(UpperChest → Chest → Spine, Mixamo `Spine2`)에 `Armor_<아이템 ID>`로 꺼 둔 채 붙이고, `EntityPresenter`가 `ArmorEquippedEvent`로 입은 것만 켠다 (판 시작 천 갑옷 포함, 맨몸 = 모두 끔).

| 아이템 | 모델 | 후보 |
|---|---|---|
| `armor_cloth` | Prop_Armor_Cloth | 1-1 누빔 베스트 · 끈 허리띠 |
| `armor_leather` | Prop_Armor_Leather | 2-1 가죽 흉갑 · 버클 둘 |
| `armor_mail` | Prop_Armor_Mail | 3-2 사슬 셔츠 · 가죽 깃 |
| `armor_scale` | Prop_Armor_Scale | 4-1 청동 비늘 조끼 |
| `armor_plate` | Prop_Armor_Plate | 5-2 강철 흉갑 · 금테 · 어깨 덮개 |

- 크기: 너비 = 어깨 관절(위팔 뼈) 사이 × 1.45 × 캐릭터 배율(루멘 1.6 — 긴 코트 위 · 시아 1.3, 정지 렌더로 맞춤), 두께 × 1.15, 윗면 = 목 뼈 높이. 후보는 두 캐릭터가 같이 쓰므로 체형이 덜 드러나는 쪽(사용자 선택).
- 확인 못 한 것: 걷기 애니메이션 중 몸 · 팔이 부품을 뚫는지, 이어하기 뒤 갑옷이 보이는지(무기와 같이 장착 이벤트가 다시 오지 않음 — 기존 무기도 같음).
- Varco 플로우 `MoaDungeon` y 140200 ~ 142600. 포인트 이미지 500 + 3D 1,000.

### 12-11. UI 그림 (2026-11-05 — ★ 3차 29 · 9번)

결정 (2026-11-05, 사용자): 틀 = Varco 이미지 UI 키트, 아이콘 = Varco 이미지, 범위 = 9번 1차 + 2차 (커서 빼고). 시트 7장 × 후보 2 → 상태 2 · 주문 2 · 창 틀 1 · 칸 틀 1 · 막대 2 · 표시판 1 · 장식 2 (사용자 선택). 흰 배경을 테두리에서 채워 지우고 덩어리마다 잘라 `Art/UI/*.png` 30장(상태 `status_<종류>` 12 · 주문 `spell_<속성>` 8 + `spell_Fusion` · `ui_panel` · `ui_slot_normal/selected/empty/disabled` · `ui_bar` · `ui_plaque` · `ui_victory/defeat`).

| 화면 | 쓰는 그림 | 코드 |
|---|---|---|
| HP · MP · 경험치 막대 | `ui_bar` 틀(9-slice, 테두리 40 → 9 px) + 채움은 틀 안쪽(7 · 6 px 들임) | `HudPresenter.ApplySkin` · `SetBar` |
| 상태 아이콘 | `status_<종류>` 46 px + 오른쪽 아래 남은 틱 | `DrawStatusIcons` |
| 빠른 칸 | 칸 틀 4상태 (보통 · 선택 · 빈 칸 · 마나 부족) + 주문 = 속성 아이콘 | `DrawQuickSlots` · `SetSlotIcon` |
| 전투 로그 | 창 틀 반투명(82 %) 판을 로그 뒤에 | `ApplySkin` |
| 층 표시판 | `ui_plaque` 화면 위 가운데 + "N층" (`HudText.FloorPlaque`) | `HudModel.Floor` |
| 인벤토리 창 | 창 틀 + 칸 틀 (선택 · 융합 첫째 = 선택 틀 파랗게 · 비활성 · 빈 칸) · 메뉴 버튼 | `InventoryPresenter.Paint` |
| 결과 화면 | 상자 = 창 틀, 제목 위 승리 / 패배 장식 | `ResultPresenter.ApplySkin` |
| 피해 숫자 | 머리 위 월드 TMP(Galmuri11), 0.8초 동안 떠오르며 흐려짐, 회복은 초록 "+n" | `EntityPresenter.SpawnNumber` (입력 대기에 넣지 않음) |

- 표는 `Data/Visuals/UiSkin.asset`(비면 예전 색 칸 그대로). 틀 두께(화면 px)는 그 에셋의 `PanelBorder` · `SlotBorder` · `BarBorder` · `PlaqueBorder`.
- 확인 못 한 것: 실제 HUD 배치(목업 렌더로 틀 · 아이콘만 확인), 인벤토리 주문 칸의 속성 아이콘(인벤토리는 주문 표를 몰라 아직 글자만).
- Varco 플로우 `MoaDungeon` y 143800 ~ 147400. 포인트 이미지 700.

### 12-12. 3 ~ 5구역 타일 (2026-11-05 — 사용자 “11 ~ 25층 테마 Varco3D로 생성 및 적용”)

2구역(12-9)과 같은 방식 — Setup/Varco Models 표 `ZoneTiles`(이름 · 첫 층 · 끝 층)마다 `Tile_<이름>_Floor/Wall/Pillar/StairsDown/StairsUp` → `TileVisualSet_<이름>.asset`, 기본 세트 구역 목록에 건다. 풀 · 문 · 솥은 구역 바닥 위에 다시 짜고 문틀 = 구역 벽. 구역 벽은 바닥 면을 칸 1 × 1 m에 꽉 채운다(`FillCellFootprint` — 용암 벽처럼 가장자리가 들쭉날쭉하면 틈이 남아서, 2구역 벽도 함께).

| 구역 (층) | 바닥 | 벽 | 기둥 | 내려가는 계단 | 올라가는 계단 |
|---|---|---|---|---|---|
| 3 얼음 동굴 (11 ~ 15) `Ice` | 1 눈 · 얼어붙은 돌 | 2 눈 덮인 바위 + 고드름 | 2 서리 기둥 | 2 | 1 |
| 4 용암 동굴 (16 ~ 20) `Lava` | 1 현무암 + 용암 금 | 1 들쭉날쭉한 화산암 | 2 육각 현무암 기둥 | 1 | 2 |
| 5 시간 신전 폐허 (21 ~ 25) `Time` | 2 대리석 + 청동 시계 무늬 | 1 대리석 + 톱니 띠 | 1 시계 고리 기둥 | 2 | 1 |

- 후보 번호 = 사용자 선택(추천대로). 투명 얼음은 Varco 3D가 형태를 잃기 쉬워 “불투명 눈 · 서리”로 요청했다 (12-8 얼음버섯 교훈).
- 26층 시간의 제단은 기본 세트 그대로.
- Varco 플로우 `MoaDungeon` y 148600 ~ 157000. 포인트 이미지 1,500 + 3D 3,000.
- G-20 (2026-11-05, 사용자 “내려가는 계단이 튀어나와 있어, 구역마다 튀어나오거나 빈틈 있는 것 수정”): 모든 구역(1구역 포함)의 바닥 판 · 계단 · 벽을 칸 1 × 1 m에 꽉 맞춤(`FitCell`) — 바닥 판 · 내려가는 계단은 윗면 = y 0(`FillCellSunk`, 내려가는 계단은 바닥 아래로 꺼짐 — 1구역 원본은 0.39 m 솟아 있었음), 벽 · 올라가는 계단은 아랫면 = 0(`FillCellFootprint`). 바닥 판 원본 0.98 m → 1 m (칸 사이 틈 없앰).

### 12-2. 무기 (3-7a, 2026-10-19)

| 아이템 | 모델 | 프리팹 (바닥) | 길이 (m) | 쥐는 곳 | 손 기울기 |
|---|---|---|---|---|---|
| `shortsword` | Prop_Weapon_Shortsword | Weapon_Shortsword | 0.45 | 0.12 | 앞 70° |
| `hand_axe` | Prop_Weapon_HandAxe | Weapon_HandAxe | 0.35 | 0.25 | 앞 70° |
| `spear` | Prop_Weapon_Spear | Weapon_Spear | 1.10 | 0.30 | 앞 70° |
| `quarterstaff` | Prop_Weapon_Quarterstaff | Weapon_Quarterstaff | 1.00 | 0.30 | 앞 70° |
| `dirk` | Prop_Weapon_Dirk | Weapon_Dirk | 0.25 | 0.15 | 앞 70° |
| `sickle` | Prop_Weapon_Sickle | Weapon_Sickle | 0.30 | 0.15 | 앞 70° |

값은 `VarcoModelSetup.WeaponSpecs`와 같다. 모두 칼처럼 앞으로 눕혀 든다 (사용자 요청 2026-10-19 — 창 · 육척봉도 세우지 않음). 바닥 모습 = `ItemVisualCatalog`(아이템 ID → 프리팹), 손 = 플레이어 프리팹 오른손 아래 `Weapon_<아이템 ID>`(꺼 둠 — 들면 `EntityPresenter.ShowHeld`가 켜고 `Staff`를 숨김). 반짝임 = FxCatalog `item.<아이템 ID>`(은빛).

### 12-1. 애니메이션 (G-2c, 2026-10-19)

| 클립 | 파일 | 적용 | 설정 |
|---|---|---|---|
| Idle | `Assets/_Project/Animation/Idle.fbx` (Mixamo, 8.3초) | 플레이어 기본 | Humanoid 리타깃 · 반복 · 루트 회전 · 높이 굽기 · 앞뒤 이동은 루트로(버림) |
| Walk | `Assets/_Project/Animation/Walk.fbx` (Mixamo, 0.97초) | 플레이어 이동 중 (`Moving`) | 〃 · 상태 속도 1.5 |

컨트롤러 `Assets/_Project/Animation/PlayerAnimator.controller` (메뉴 ProjectM/Setup/Varco Models가 만들고 플레이어 프리팹 모델에 붙임). 양 마법사 · 클립 모두 Humanoid(이 모델로 아바타).
적 · 보스는 아직 클립 없음(코드 움직임). 다음 후보: 공격 · 시전 · 피격 · 사망 클립(같은 이벤트 지점에 트리거로).

## 13. 연결된 이펙트 (G-3, 2026-10-19)

에셋: 사용자가 가져온 **Hovl Studio — Magic effects pack**(H) · **Eric VFX Studio — Free Game VFX**(E), `Assets/_Project/FX` (Shuriken 프리팹, 데모 씬은 지움).
메뉴 `ProjectM/Setup/FX`(`FxSetup`, 멱등)가 재질을 URP로 바꾸고(소프트 파티클 끔) 아래 표로 `Data/Visuals/FxCatalog.asset`을 채운다 — **이 표 = `FxSetup.Table`**. 새 효과는 표에 한 줄 추가 후 메뉴 실행.
배율 = 팩 원본 대비 (원본이 3~12 m). 한 번 재생은 복제본의 반복을 끄고 길이(초) 뒤 지움. 높이 > 0 = 칸 바닥 기준. 색 = 시작 색에 곱(0-2 색 기준), "원래" = 팩 색 그대로.

| 키 | 효과 (팩/프리팹) | 배율 | 색 | 속도 · 길이 |
|---|---|---|---|---|
| terrain.FireZone | H Sparks flashing yellow (불씨 루프) | 1.4 | 불 | 루프 |
| terrain.PoisonCloud | H Smoke ground (낮은 연기 루프) | 0.16 | 독 | 0.8 · 루프 |
| terrain.ContaminatedWater | H Sparks flashing green (거품) | 0.45 | 오염 | 루프 |
| terrain.Water · IcePatch | 파티클 없음 — 바닥 무늬(물결 · 얼음 금) | | | |
| status.Frozen | H Crystal effect blue (얼음 결정) | 0.55 | 원래 | 루프, 0.45 m |
| status.Paralyze | H Lightning aura | 0.32 | 번개 | 루프 |
| status.Burn | H Sparks flashing red | 0.5 | 불 | 루프 |
| status.Poison | H Star aura | 0.3 | 독 | 루프 |
| status.Slow · Wet | H Sparks white · Sparks blue | 0.45 · 0.4 | 얼음 · 물 | 루프 |
| cast.(속성) | H Holy hit (지팡이 끝 번쩍) | 0.14 | 속성 색 | 1.5 · 0.6초 |
| projectile.Fire | E FX_Fireball | 0.09 | 원래 | 날아가는 동안 |
| projectile.Ice · Poison · Water | H Sparks white · pink · blue (흔적) | 1.2 | 속성 색 | 날아가는 동안 |
| impact.Fire · Ice · Lightning · Poison · Water | H Explosion · H Snow hit · H Electro hit · E FX_Purple_Hit_02 · H Sparks explode blue | 0.16 · 0.35 · 0.5 · 0.15 · 0.25 | 원래 / 번개 · 물 | 1~1.5 · 0.7~1초 |
| burst.Fire · Ice · Poison · Water | H Ground AOE explosion · Snow AOE · Red energy explosion · Smoke AOE explosion | 0.33~0.4 | 원래 / 독 · 물 | 1.6~2 · 1초 |
| fusion.fusion_fire_water · water_poison · fire_lightning · ice_water | H Meteors AOE · Smoke AOE explosion · Explosion · Freeze circle | 0.33 · 0.33 · 0.22 · 0.38 | 원래 / 오염 | 1.5~2 · 1초 |
| reaction.toxic_ignite(_cloud) · toxic_burst · gas_explosion | E FX_Purple_Hit_02 · H Red energy explosion · H Ground AOE explosion | 0.2 · 0.4 · 0.33 | 원래 / 독 | 1초 안 |
| reaction.evaporate(4) · extinguish | H Smoke puff (증기) | 0.09~0.12 | 흰 증기 | 1.5 · 1초 |
| reaction.contaminate | E FX_Green_Hit | 0.18 | 원래 | 0.7초 |
| reaction.freeze(3) · thaw(2) · shatter | H Freeze circle · Sparks explode blue · Snow hit | 0.13 · 0.15 · 0.45 | 원래 / 얼음 | 1초 안 |
| reaction.shock · shock_water · shock_contaminated | H Electro hit + **번개 선**(같은 순간 감전 대상끼리 이음) | 0.5 | 번개 | 0.8초 |
| line.Lightning · line.Fire | 번개 선 색 (LineRenderer, 가산 재질 `Art/FX/Generated/LightningLine.mat`) | | 노랑 · 주황 | 0.25초 |

코드로 만든 것 (`Art/FX/Generated` PNG 64² + `Data/Visuals/FxTiles` 타일, 흰색 + 알파 — 색은 덮개 · 조준 색):
- 바닥 무늬: `fx_scorch`(화염 지대) · `fx_haze`(독구름) · `fx_water`(물결, 칸끼리 이어짐) · `fx_dirty_water` · `fx_ice`(금 + 반사 줄).
- 조준 데칼: `aim_path`(점) · `aim_affected`(테두리) · `aim_cursor`(모서리 괄호) · `aim_blocked`(X).
- 후처리: `Settings/Rendering/PostFX.asset` Bloom (threshold 1.1 · intensity 0.6 · scatter 0.6) + 씬 `PostFX` Global Volume.

남은 것 (다음 다듬기 후보): 움직이는 물 셰이더 · 빙판 반사, 얼음 · 독 · 물 투사체 몸체(지금은 불꽃 튐 흔적), 화염 지대 불꽃(지금은 불씨), 융합 시전 발밑 마법진(5-1 공통, 2차), 조준 커서 위아래 움직임.

# BALANCE.md — 수치 · 검산 · 실험

> 수치의 기준 문서. 코드에는 숫자를 쓰지 않고, 아래 "데이터 위치"의 에셋만 고친다.
> 값을 바꾸면 이전 값을 지우지 말고 `→ 이후 변경 (날짜, 이유)`로 이어 적는다.
> 각 표의 검산은 같은 내용의 단위 테스트가 있다 (테스트 이름 표기).

---

## 0. 데이터 위치

| 번호 | 내용 | 에셋 | 순수 로직 타입 | 상태 |
|---|---|---|---|---|
| 1 | 게임 전역 (틱 · 행동 비용) | `Assets/_Project/Data/Config/GameConfig.asset` (`GameConfigAsset`) | `ProjectM.Core.GameConfig` | 사용 중 |
| 2 | 맵 생성 | `Assets/_Project/Data/Dungeon/DungeonGenConfig.asset` (`DungeonGenConfigAsset`) | `ProjectM.Dungeon.DungeonGenConfig` | 사용 중 |
| 3 | 유닛 (체력 · 피해 · 시야) · 적 배치 | `Assets/_Project/Data/Units/{Player, MeleeEnemy, SpawnConfig}.asset` (`UnitDefAsset`, `SpawnConfigAsset`) | `ProjectM.Units.UnitDef`, `ProjectM.Units.SpawnConfig` | 사용 중 |
| 4 | 마나 · 주문 장착 칸 | `Assets/_Project/Data/Magic/MagicConfig.asset` (`MagicConfigAsset`) + `Player.asset`의 `_maxMana` · `_startingSpells` | `ProjectM.Magic.MagicConfig`, `UnitDef.MaxMana` | 사용 중 |
| 5 | 주문 | `Assets/_Project/Data/Spells/SpellCatalog.asset` (`SpellCatalogAsset`) | `ProjectM.Magic.SpellDef` · `SpellBook` | 사용 중 |
| 6 | 상태 이상 | `Assets/_Project/Data/Statuses/StatusCatalog.asset` (`StatusCatalogAsset`) | `ProjectM.Status.StatusDef` · `StatusBook` | 사용 중 |
| 7 | 지형 효과 | `Assets/_Project/Data/Terrains/TerrainCatalog.asset` (`TerrainCatalogAsset`) | `ProjectM.Status.TerrainDef` · `TerrainBook` | 사용 중 |
| 8 | 반응 규칙 표 | `Assets/_Project/Data/Reactions/ReactionCatalog.asset` (`ReactionCatalogAsset`) | `ProjectM.Status.ReactionDef` · `ReactionBook` · `ReactionConfig` | 사용 중 |
| 9 | 아이템 | `Assets/_Project/Data/Items/ItemCatalog.asset` (`ItemCatalogAsset`) | `ProjectM.Items.ItemDef` · `ItemBook` | 사용 중 |
| 10 | 융합 규칙 표 | `Assets/_Project/Data/Fusions/FusionCatalog.asset` (`FusionCatalogAsset`) | `ProjectM.Magic.FusionDef` · `FusionBook` | 사용 중 (3-2b) |
| 11 | 바닥 아이템(마법서) 배치 | `Assets/_Project/Data/Items/LootConfig.asset` (`LootConfigAsset`) | `ProjectM.Items.LootConfig` · `Loot` | 사용 중 (3-5) |
| 13 | 무기 (3-7a) | `ItemCatalog.asset`의 무기 줄 · 공통 값 (`ItemCatalogAsset`) — 9번과 같은 에셋 | `ProjectM.Items.WeaponDef` · `WeaponRules` · `Weapons` · `ProjectM.Units.EquippedWeapon` | 사용 중 (3-7a) |
| 12 | 경험치 · 레벨업 | `Assets/_Project/Data/Units/ProgressionConfig.asset` (`ProgressionConfigAsset`) + 유닛 에셋의 명중 · 회피 · 경험치 (3번) | `ProjectM.Units.ProgressionConfig` · `Progression` | 사용 중 (3-6) |
| 15 | 갑옷 (3-10a) | `ItemCatalog.asset`의 갑옷 줄 · 갑옷 공통 값 + `Player.asset`의 `_startingArmor` + `LootConfig.asset`의 `_armorsPerRun` | `ProjectM.Items.ArmorDef` · `ArmorRules` · `Armors` · `ProjectM.Units.EquippedArmor` | 사용 중 (3-10a) |
| 18 | 허기 · 음식 (3-14) | `Assets/_Project/Data/Units/HungerConfig.asset` (`HungerConfigAsset`) + `ItemCatalog.asset`의 음식 5줄 | `ProjectM.Units.HungerConfig` · `Hunger` · `HungerSystem` · `EatCommand` | 사용 중 (3-14) |
| 17 | 함정 (3-12a) | `Assets/_Project/Data/Dungeon/TrapCatalog.asset` (`TrapCatalogAsset`) + `SpellCatalog.asset`의 `trap_*` 5줄 | `ProjectM.Dungeon.TrapBook` · `Traps` · `TrapSystem` | 사용 중 (3-12a) |
| 16 | 층 꾸미기 (풀 · 물 웅덩이) | `Assets/_Project/Data/Dungeon/FloorDecorConfig.asset` (`FloorDecorConfigAsset`) | `ProjectM.Dungeon.FloorDecorConfig` · `FloorDecor` · `GrassTrampleSystem` | 사용 중 (3-11a) |
| 21 | 직업 — 루멘 빛의 창 (3-21) | `SpellCatalog.asset`의 `light_spear_*` 줄(무기 강화 필드) + `Data/Units/CharacterCatalog.asset`(`ClassId` · `StartingSpells`) + `Data/Units/ClassCatalog.asset`(`ClassCatalogAsset` — 형태, 3-21b) | `ProjectM.Magic.SpellDef` · `SpellModifiers` · `UnitDef.WithExtraStartingSpells` · `ProjectM.Units.ClassBook` · `ClassForms` | 사용 중 (3-21a · b) |
| 22 | 직업 — 시아 무도가 (3-22) | `SpellCatalog.asset`의 `sia_*` 줄 + `CharacterCatalog.asset` 시아 줄 (+ 3-22b · c `ClassCatalog.asset` 시아 줄) · 너클 = `ItemCatalog.asset` (13번) | 21번과 같음 | 사용 중 (3-22a) |
| 14 | 물약 · 주문서 (효과 · 식별 · 무작위 배치 · 던지기) | `Assets/_Project/Data/Items/ConsumableConfig.asset` (`ConsumableConfigAsset`) + `ItemCatalog.asset`의 `Effect` · `Weight` (9번) | `ProjectM.Items.ConsumableConfig` · `Consumables` · `Identification` · `Throwing` | 사용 중 (3-9) |

**데이터 구조 규칙 (0-4 결정)**
- 에셋은 `ScriptableObject`(어셈블리 `ProjectM.Runtime`, 네임스페이스 `ProjectM.Data`)이고, `ToXxx()`로 순수 C# 객체를 만들어 Simulation에 넘긴다. Simulation은 ScriptableObject를 모른다 (CLAUDE.md 핵심 설계 규칙 1).
- 순수 객체 생성자가 값 범위를 검사한다. 잘못된 값은 게임 시작 시 예외로 드러난다.
- 하위 폴더는 처음 쓰는 항목에서 만든다 (빈 폴더를 미리 만들지 않는다).

---

## 1. 게임 전역 — `GameConfig.asset`

| 항목 | 필드 | 값 | 근거 |
|---|---|---|---|
| 기본 행동 비용 | `_defaultActionCostTicks` | **1틱** | GDD 2-4 [확정] "초기에는 전부 1틱" |
| 실시간 틱 간격 | `_realtimeTickIntervalSeconds` | **1.0초** | GDD 3-1 [확정] "1초마다 1틱" |

검산 (`GameConfigTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
범위 (`GameConfigTests.Constructor_*`): 행동 비용 ≥ 1, 틱 간격 > 0.

3-27 실시간 모드(결정 2026-11-05): 이 간격마다 세계가 1틱 — 플레이어 행동은 틱을 쓰지 않는다(즉시). 한 프레임에 따라잡는 틱은 최대 `RealTimeClock.MaxTicksPerUpdate`(5, 코드 상수 — 멈췄다 돌아온 프레임 보호용, 게임 수치 아님).

---

## 2. 맵 생성 — `DungeonGenConfig.asset`

결정 (2026-10-16): 이전 `MapGenerator`(git 75121da) 값을 그대로 쓴다. 조정은 Phase 4 재미 검증에서.
크기 뽑기 = `[0, 뽑기범위)`에서 뽑고 최소 미만이면 최소를 더함 → 결과 `[최소, 뽑기범위)`, 그중 `[최소, 2·최소)`가 두 배 자주 나옴 (의도된 분포).

| 항목 | 필드 | 값 | 결과 범위 | 비고 |
|---|---|---|---|---|
| 맵 크기 | `_width` × `_height` | **50 × 50** | — | 바깥 한 줄은 항상 벽 |
| 방 개수 | `_roomCount` | **10** | — | 첫 방 = 시작, 마지막 방 = 계단 |
| 방 크기 뽑기 범위 | `_roomSizeRoll` | **20** | 7 ~ 19 | 이전 코드 `Room_maxsize` |
| 방 최소 크기 | `_roomMinSize` | **7** | (7~13이 2배) | |
| 벽 덩어리 개수 | `_wallBlockCount` | **20** | — | 이전 코드 mode 2 (방 개수 × 2) |
| 벽 덩어리 크기 뽑기 범위 | `_wallBlockSizeRoll` | **10** | 3 ~ 9 | 이전 코드 mode 2 (최대 / 2) |
| 벽 덩어리 최소 크기 | `_wallBlockMinSize` | **3** | (3~5가 2배) | |
| 벽 뚫기 비용 배수 | `_wallTunnelCostMultiplier` | **5** | — | 1-5 신규. 복도가 벽 1칸 뚫기 = 바닥 5칸 돌아가기 |
| 배치 재시도 상한 | `_placementAttemptLimit` | **1000** | — | 1-5 신규 안전장치 (이전 코드는 무한 반복 가능) |
| 막다른 길 잇기 최소 걸음 | `_deadEndLoopMinWalkDistance` | **10** | — | 2026-10-16 신규. 막다른 칸에서 벽을 뚫어 이을 상대는 걸어서 10걸음 이상 떨어진 칸 (자기 길 옆에 붙이지 않고 순환로를 만듦) |
| 한 판 층 수 | `_floorCount` | **5** | — | 3-4a (사용자 결정 2026-10-18): 5층 단위로 던전 테마가 바뀌는 구조의 첫 5층. 1~4층 일반, **5층 = 보스 층**(내려가는 계단 · 적 재생성 없음, 보스는 3-4b). 2층부터 시작 칸 = 올라가는 계단 |

| 보스 방 크기 | `_bossRoomWidth` × `_bossRoomHeight` | **21 × 15** | — | 3-4b (결정 2026-10-18 "큰 방 하나"): 마지막 층 = 맵 가운데 방 하나 + 기둥 4개(방을 4등분한 점 ✎), 왼쪽 끝 가운데 = 올라가는 계단, 오른쪽 끝 가운데 = 보스. 난수 안 씀 |

층 생성 순서 (3-4a): 1층 생성 → (내려갈 때마다) 다음 층 생성 — 모두 같은 맵 생성 수열(MapGen)을 이어 쓴다 → **같은 시드 = 같은 층들**. 계단 놓기는 난수를 쓰지 않아 1층 결과는 3-4a 전과 같다.

검산 (`DungeonGeneratorTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
분포 (`DungeonGeneratorTests.RollSize_KeepsIntendedDistribution`): 7~13 칸당 빈도 / 14~19 칸당 빈도 = 1.8~2.2.
결과 (시드 20261016 예): 바닥 991 · 벽 1508 · 계단 1. → 막다른 길 잇기 후 (2026-10-16) 바닥 993 · 벽 1506 · 계단 1.
막다른 길 (`DungeonGeneratorTests.ManySeeds_NoDeadEnds`): 시드 200개 모두 막다른 칸 0.
재현 (`DungeonGeneratorTests.RecordedSeed_MatchesBalanceResult`): 위 시드 20261016 결과 + 시작 (2, 26)을 고정 검사. 같은 시드 = 같은 맵 · 적 배치 (`ManySeeds_SameSeed_ByteIdenticalFloorAndSpawns`).

---

## 3. 유닛 · 적 배치 — `Data/Units/`

결정 (2026-10-16): Q3 임시값. 고정 피해(난수 없음). 조정은 Phase 4 재미 검증에서.

| 유닛 | 에셋 | VisualId | 체력 `_maxHp` | 근접 피해 `_attackDamage` | 시야 반지름 `_sightRange` |
|---|---|---|---|---|---|
| 플레이어 | `Player.asset` | `player` | **20** | **3** | **8** |
| 근접 적 | `MeleeEnemy.asset` | `enemy_melee` | **8** | **2** | **6** |
| 임시 보스 (3-4b) | `Boss.asset` | `boss` | **60** | **5** | **10** |

명중 · 회피 · 경험치 (3-6, 결정 2026-10-19 — SPD 영웅 · 쥐 · 구(Goo) 값):

| 유닛 | 명중 `_accuracy` | 회피 `_evasion` | 처치 경험치 `_expReward` | 경험치 최고 레벨 `_expMaxLevel` |
|---|---|---|---|---|
| 플레이어 | **10** | **5** | 0 | 0 |
| 근접 적 | **8** | **2** | **1** | **5** |
| 임시 보스 | **10** | **8** | **10** | **30** |

힘 `_strength` (3-7b, SPD): 플레이어 **10**, 적 · 보스 0(무기를 들지 않음). 무기 요구 힘(13번)과 비교 — 모자란 1당 명중 ÷ 1.5, 남는 1당 무기 피해 + 0~1 굴림. 맨손은 힘 영향 없음.

근접 명중 (SPD `Char.hit`): 명중 굴림 U·명중 ≥ 회피 굴림 V·회피(U, V는 0~1 균등)이면 맞음. 명중 a ≥ 회피 d이면 확률 = 1 − d / (2a), a < d이면 a / (2d). 회피 0 · 행동 불가(마비 · 빙결) 대상은 항상 맞음. 주문은 항상 명중.

임시 보스 (결정 2026-10-18, 사용자): 마지막 층(5층)에 혼자, 근접 적과 같은 AI(쫓아와 공격). 처치 = 승리. 진짜 보스 패턴은 Phase 6. 검산 `BossTests.ProjectAssets_MatchBalanceTable`.
결과: 근접만으로 20대(3 × 20 = 60), 보스는 플레이어를 4대(5 × 4 = 20)에 쓰러뜨림 — 반응 · 융합 · 지형 없이는 못 이김. 예: 폭발 마법(8) · 젖음 + 감전(5) · 빙결 + 파쇄(6)

| 배치 | 필드 | 값 | 비고 |
|---|---|---|---|
| 층당 적 수 | `_enemyCount` | **5** | 놓을 칸이 모자라면 있는 만큼 |
| 시작 칸과 최소 거리 | `_enemyMinDistanceFromStart` | **8** | 체비셰프 거리, 바닥 칸만(계단 제외) |
| 적 재생성 주기 | `_respawnIntervalTicks` | **30틱** | 3-4a (사용자 결정 2026-10-18). (틱 + 1)이 30의 배수인 틱의 ⑤ 단계에 1마리 — 플레이어 시야 밖 바닥 칸(빈 칸, 계단 제외) 중 무작위(재생성 난수 `RngStream.Spawn`). 지금 층에서만, 보스 층 제외 |
| 재생성 상한 | `_respawnMaxEnemies` | **5** | 지금 층에 살아 있는 적이 5 이상이면 만들지 않음 (처음 배치 수와 같게 — 잡은 만큼만 다시 채워짐) |

검산 (`UnitCombatTests.ProjectAssets_MatchBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 명중률 (3-6, `ProgressionTests.RollHit_SpdRatVsHero_HitRateRoughlyMatches`): 플레이어 → 근접 적 1 − 2/20 = **90%**, 근접 적 → 플레이어 1 − 5/16 ≈ **69%**, 플레이어 → 보스 1 − 8/20 = **60%**, 보스 → 플레이어 1 − 5/20 = **75%**. 아래 "3대 · 10대"는 명중했을 때 기준 — 평균은 적 3.3번 휘두름, 적은 플레이어를 약 14.5번 휘둘러야 잡음
- 재생성 (3-4a): 마나가 바닥에서 가득 차는 30틱마다 적 1마리 — 한 층에 오래 머물며 쉬면 잡은 적이 다시 채워진다 (상한 5) — `FloorTests.Respawn_*`
- 플레이어가 적을 잡는 데 **3대** (3 × 3 = 9 ≥ 8) — `UnitCombatTests.PlayerKillsEnemyInThreeHits_EnemyRemoved`
- 적 한 마리가 플레이어를 잡는 데 **10대** (2 × 10 = 20) — `UnitCombatTests.PlayerDiesAfterTenHits_ClockStops`
- 1:1로 붙어 싸우면 플레이어가 적 하나에 4 잃음 (적이 2번 반격). 회복이 없으므로 1:1을 4번 하면 체력 4, 5번째 1:1에서 같이 죽는다 (층의 적 5마리 = 회복 없이는 빠듯함)
- 시야 6 < 플레이어 8 → 플레이어가 먼저 본다. 1-7부터 시야 = 원 반지름(dx² + dy² ≤ r² + r) · 벽에 가려짐 (1-6에서는 체비셰프 거리 · 벽 무시)

---

## 4. 마나 · 빠른 칸 · 인벤토리 — `Data/Magic/MagicConfig.asset`

결정 (2026-10-16): Q1 · Q2 임시값. 틱당 회복만 (처치 회복 없음). 조정은 Phase 4 재미 검증에서.

| 항목 | 위치 · 필드 | 값 | 비고 |
|---|---|---|---|
| 플레이어 최대 마나 | `Player.asset` `_maxMana` | **10** | 시작 시 가득 |
| 적 최대 마나 | `MeleeEnemy.asset` `_maxMana` | **0** | 마나 없음 · 회복 없음 |
| 회복 주기 | `_manaRegenIntervalTicks` | **3틱** | (틱 + 1)이 3의 배수인 틱의 ⑤ 단계 |
| 회복량 | `_manaRegenAmount` | **1** | 최대치에서 멈춤 |
| 빠른 칸 | `_spellSlotCount` (이름은 2-1 그대로) | **6** | 키 1 ~ 6, 주문 · 아이템 등록 (3-2a — 장착 칸 4개를 대신함) |
| 인벤토리 칸 | `_inventorySlotCount` | **20** | 고정 칸 (3-2a 사용자 결정). 같은 주문은 한 번만, 아이템은 여러 개, 꽉 차면 못 넣음 |
| 체력 자동 회복 주기 | `_hpRegenIntervalTicks` | **10틱** | 2-6 (2026-10-17 사용자 요청). (틱 + 1)이 10의 배수인 틱의 ⑤ 단계, 마나 회복 다음. 0이면 없음 |
| 체력 자동 회복량 | `_hpRegenPercent` | **1%** | 최대 체력의 1%, 소수점은 **올림** (결정 2026-10-20, 사용자 — 소수점 누적안은 되돌림) → 최대 체력 20이면 10틱마다 **1**, 최대 101~200이면 2. 플레이어만, 상태 이상 중에도 회복, 최대치에서 멈춤 |
| 시작 주문 | `Player.asset` `_startingSpells` | **없음** | 3-1부터 주문은 게임 중 획득 (REACTIONS.md D8). 시작 주문이 있으면 인벤토리 · 빠른 칸 앞쪽부터. 지금은 디버그 창 "[상태 변경] 인벤토리 · 빠른 칸"에서 넣는다 |

검산 (`ManaTests.ProjectAssets_MatchBalanceTable`): 에셋 값이 위 표와 같다.
결과: 바닥(0)에서 가득(10)까지 30틱. 체력은 1 → 20까지 190틱 (10틱마다 1). 2-2 주문 마나 비용은 이 회복 속도를 기준으로 정한다 (예: 비용 3 = 9틱마다 한 번 꼴).

---

## 5. 주문 — `Data/Spells/SpellCatalog.asset`

결정 (2026-10-16): Q4 임시값. 항상 명중, 고정 피해. 사거리 = 체비셰프. 시전 = 기본 행동 비용(1틱). 조정은 Phase 4 재미 검증에서.

| ID | 속성 | 모양 | 사거리 | 피해 | 마나 | 반지름 | 비고 |
|---|---|---|---|---|---|---|---|
| `fire_basic` | Fire | Bolt (단일, 첫 유닛) | **6** | **5** | **3** | 0 | + 화상 70% (6번) → 걸리면 한 발 처치 |
| `ice_basic` | Ice | Bolt | **6** | **3** | **2** | 0 | + 둔화 70% (6번) |
| `lightning_basic` | Lightning | Beam (직선 관통, 벽에서 멈춤) | **5** | **3** | **4** | 0 | 일렬로 선 적 여럿, + 30% 마비 (6번) |
| `poison_basic` | Poison | Burst (투사체가 멈춘 칸 중심) | **5** | **2** | **3** | **1** (3×3) | 범위 안 시전자도 맞음, + 중독 70% (6번) |
| `water_basic` | Water | Burst | **5** | **0** | **2** | **1** (3×3) | 3-2 (원본 6-1). + 젖음 100% + 물 타일 3×3. 피해 0이라 피해 줄 없음. 게임 중 획득 — 지금은 디버그 창. 표시 이름 물보라 |

표시 이름 (2-5, 필드 `DisplayName` — 수치가 아니라 HUD · 로그 표시 전용, 규칙에 쓰지 않음): `fire_basic` 불꽃 화살 · `ice_basic` 얼음 화살 · `lightning_basic` 번개 줄기 · `poison_basic` 독 폭발. 비어 있으면 ID를 보인다.

주문 → 상태 이상 (2-3, 6번 규칙): `fire_basic` → 화상, `ice_basic` → 둔화, `lightning_basic` → 마비 **30%**, `poison_basic` → 중독. 확률은 맞은 적마다 따로. 필드 `AppliesStatus` · `StatusChancePercent`.
→ 이후 변경 (2026-10-17, 3-1 — REACTIONS.md D3): 화상 · 둔화 · 중독 100% → **70%**. 마비 30% 그대로.
시작 장착 칸 (2026-10-17, 3-1 D8): `Player.asset` `_startingSpells` → **빈 칸**. 주문은 게임 중 획득(Phase 6), 지금은 디버그 창에서 넣는다.


**플레이어 사거리 무한 (3-25b, 결정 2026-11-05 — 사용자 "주문을 발사할 때 멀리 사거리는 제한이 없게"):** 플레이어가 쏘는 **7속성 주문 · 융합** 중 날아가는 모양(Bolt · Beam · Burst · Multi)은 위 표의 사거리를 쓰지 않고 **맵 어디든**(사거리 = 맵 가로 · 세로 중 큰 값) — 보이는 칸이어야 하고 벽 · 조준선 막힘은 그대로, 빔은 벽까지 뻗음. 적 주문 · 함정 주문 · 무속성 직업 기술(빛의 창 · 시아 기술) · 돌진 · 도약 · 진각은 표의 사거리 그대로. 그림자 기록(23번)도 원래 사거리로 굳힌다. 규칙 = `SpellModifiers.HasUnlimitedRange`. 영향: 속성 주문의 사거리 특성 · 무기 강화 사거리는 의미 없어짐(지금은 무속성 기술에만 있음). 검산 `SpellTests.UnlimitedRange_PlayerElementalOnly`
검산 (`SpellTests.ProjectAsset_MatchesBalanceTable`): 에셋 값(상태 · 확률 포함)이 위 표와 같다.
결과 (2-2, 직접 피해만):
- 가득 찬 마나(10)로 불 3번(9) → 적 하나 처치 + 하나 반쯤. 근접(3)보다 강하지만 회복이 3틱에 1이라 연사 불가
- 마나당 피해: 불 1.67 · 얼음 1.5 · 번개 0.75 × 맞은 수 · 독 0.67 × 맞은 수 → 번개는 2마리 이상, 독은 3마리 이상 모였을 때 이득
- 2-3 이후 상태 피해 포함은 6번 결과 참고

---

## 6. 상태 이상 — `Data/Statuses/StatusCatalog.asset`

결정 (2026-10-16): Q5 임시값. 같은 상태 중첩 = 지속 갱신(남은 틱을 더 긴 쪽으로, 강도 그대로). 걸린 틱의 ④ 단계부터 센다.

| 상태 | 거는 주문 · 확률 | 지속 `DurationTicks` | 틱당 피해 `DamagePerTick` | 행동 비용 추가 `ExtraActionCostTicks` | 행동 불가 `PreventsAction` |
|---|---|---|---|---|---|
| Burn 화상 | 불 70% | **3** | **1** | 0 | 아니오 |
| Slow 둔화 | 얼음 70% | **3** | 0 | **+1** (행동 2틱) | 아니오 |
| Poison 중독 | 독 70% · 가스 폭발 70% | **5** | **1** | 0 | 아니오 |
| Paralyze 마비 | 번개 **30%** | **2** | 0 | 0 | **예** |
| Wet 젖음 | 물 **100%** · 물 타일 · 오염된 물 위 매 턴 | **3** | 0 | 0 | 아니오 — 효과 없음, 반응 트리거 (증발 · 빙결 · 3-3b 감전) |
| Frozen 빙결 | 빙결 · 결빙 반응 · 얼음 + 물 융합 — **100% 확정** (3-3a) | **2** | 0 | 0 | **예** — 마비와 같은 규칙. 면역 없음 (결정 2026-10-18). 불 = 해동 · 번개 = 파쇄로 풀림 |
| Haste 신속 | 신속 물약 (3-13a) | **20** (SPD) | 0 | 0 | 아니오 — `MoveSteps` **2**: 이동 한 번에 두 칸까지 1틱 (결정 2026-11-04 "이동 두 번에 1틱"). 지나간 첫 칸의 함정 · 풀은 안 밟음 |
| Invisible 투명 | 투명 물약 (3-13a) | **20** (SPD) | 0 | 0 | 아니오 — `HiddenFromEnemies`: 적 AI가 못 봄 (공격해도 안 풀림 — SPD와 다름, Claude 임시) |
| Levitating 부양 | 부양 물약 (3-13a) | **20** (SPD) | 0 | 0 | 아니오 — `Levitates`: 지형 효과 · 함정 · 풀 밟기를 받지 않음 |
| MindVision 정신 시야 | 정신 시야 물약 (3-13a) | **20** (SPD) | 0 | 0 | 아니오 — `SensesEnemies`: 층의 모든 적이 화면에 보임 (지도는 안 밝힘) |
| Asleep 잠 | 자장가 주문서 (3-13b) — 보이는 적 | **30** (Claude 임시 — SPD는 깰 때까지) | 0 | 0 | **예** — `WakesOnDamage`: 피해를 받으면 풀림 |
| Terrified 공포 | 공포 주문서 (3-13b) — 보이는 적 | **20** (SPD) | 0 | 0 | 아니오 — `Flees`: 플레이어에게서 멀어지고 공격 안 함 |

젖음 (3-2): 물 위에 있는 동안 매 턴 3으로 갱신 → 물이 사라지거나 떠나면 2턴 더 젖어 있다 (지속 3 = ④ 처리 3번, 마지막으로 갱신된 틱의 ④에서 1 소모). — `WaterTests.StandingOnWater_StaysWet_ThenWearsOff`

검산 (`StatusTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 불 한 발 = 즉시 5 + 화상 3 = **8** → 적(체력 8) 한 발 처치(2턴 뒤). 마나당 피해 2.67 — `StatusTests.Fire_BurnsForThreeTicks_AndKills` (확률 100% 고정 시험)
  → 3-1부터 화상 70%: 기대 피해 5 + 0.7 × 3 = **7.1** (30%는 5에서 멈춤)
- 독 폭발 = 즉시 2 + 중독 5 = 대상마다 **7** → 마나당 2.33 × 맞은 수. 범위 안 시전자도 7. 3-1부터 중독 70% → 기대 5.5 (독구름 위에 남으면 구름이 매 턴 100%로 걸어 결국 7)
- 얼음 = 즉시 3 + 둔화 3틱 → 걸린 동안 적 행동이 절반 (0 · 2틱에 행동, 틱 2 끝에 풀림) — `StatusTests.Slow_DoublesActionCostWhileActive`
- 번개 = 즉시 3 + 30% × 2턴 정지. 4마리를 맞히면 평균 1.2마리 마비. 1000번 시험에서 250~350 — `StatusTests.ParalyzeChance_IsSeededAndNearThirtyPercent`

---

## 7. 지형 효과 — `Data/Terrains/TerrainCatalog.asset`

결정 (2026-10-16): Q8 임시값. 기본 지형 위 별도 레이어, 한 칸에 여러 효과 공존(같은 종류는 지속 갱신). 그 칸 유닛에게 ③ 단계마다 상태 부여(플레이어 포함). 확산 없음. 지속은 질문에 없어 임시 4틱.

| 지형 효과 | 남기는 주문 · 칸 | 지속 `DurationTicks` | 거는 상태 `AppliesStatus` |
|---|---|---|---|
| FireZone 화염 지대 | ~~불 — 투사체가 멈춘 칸 1개~~ → 3-1부터 불 + 물 융합으로만 (3-2b부터 용암 분출 — 3×3, 지속은 융합 표의 **6**, 10번) | **4** | Burn 화상 |
| PoisonCloud 독구름 | 독 — 폭발 3×3 | **4** | Poison 중독 |
| Water 물 타일 | 물 — 폭발 3×3 (3-2). **화염 지대 칸에는 안 깔림** (`BlockedBy` FireZone) | **12** | Wet 젖음 |
| ContaminatedWater 오염된 물 | 물 타일 × 독 = 오염 (남은 틱 유지) · 물 + 독 융합(3-2b, 10번) | **8** (융합으로 생길 때) | Wet 젖음 + **매 틱 피해 1** (`DamagePerTick`, D7) |
| IcePatch 빙판 | 물 타일 · 오염된 물 × 얼음 = 결빙 (**8**로 새로 정함) · 얼음 + 물 융합 (3-3a). 불을 맞으면 물 타일 **12** | **8** | Slow 둔화 |
| ParalyticGas 마비 가스 | 마비 가스 물약 (3-9, 14번) — 깨진 칸에서 2걸음 | **4** (Claude 임시, 독구름과 같게) | Paralyze 마비 |

필드 `SpellCatalog` 항목의 `LeavesTerrain`: `poison_basic` → PoisonCloud, `water_basic` → Water (3-2). (`fire_basic` → FireZone은 2026-10-17 3-1 D2로 삭제)
지형이 매 턴 거는 상태는 **100%** (2026-10-17 사용자 확인 — 요청표의 50%는 쓰지 않음).

**불 번짐 (3-11b, 결정 2026-11-04 — SPD)** — `TerrainCatalog.asset`의 공통 값, `FireSpreadSystem`(③ 지형 처리 다음)

| 값 | 필드 | 값 | 뜻 |
|---|---|---|---|
| 풀이 타는 시간 | `_grassBurnTicks` | **3** 틱 | 번져 붙은 칸의 화염 지대 지속 (점화 반응 지속과 같게) [임시] |
| 한 틱 번짐 상한 | `_fireSpreadMaxPerTick` | **8** 칸 | 넘는 칸은 다음 틱 (칸 인덱스 순) — 규칙 10 안전장치 [임시] |

- 화염 지대가 있는 칸마다: 그 칸의 풀은 바닥이 되고, **상하좌우** 이웃 풀에 불이 붙는다. 이번 틱에 붙은 불은 다음 틱부터 번짐 → 한 틱에 한 칸씩 퍼진다.
- 젖게 하는 지형(물 · 오염된 물 — `AppliesStatus = Wet`)이 있는 칸은 타지 않는다 (웅덩이가 불을 막음). 화염 지대와 같은 칸에 그런 지형이 있으면 **불이 꺼진다**(번지지 않음 — 2026-11-04 사용자 요청 "오염된 물에 닿아도 불이 꺼지게". 물은 원래 화염 지대 위에 안 깔리고, 오염된 물은 깔리므로 오염 물결 융합 등).
- 풀이 다 타면 불이 꺼진다 (풀은 다시 자라지 않음). 화염 지대 위 유닛은 매 틱 화상 — 플레이어도.
- 융합 용암 분출(화염 지대 6틱)도 이웃 풀로 번진다.

검산 (`TerrainTests.ProjectAsset_MatchesBalanceTable`, `FireSpreadTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 화염 지대에 4틱 내내 서 있으면 화상이 매 틱 갱신 → 지대 4틱 + 사라진 뒤 화상 2틱 = 피해 **6** — `TerrainTests.StandingInFireZone_KeepsBurning`
- 독구름은 놓인 틱에 이미 중독(5틱)이 걸려 있으므로, 머물면 중독이 계속 5로 갱신 → 떠난 뒤에도 최대 5틱
- ~~빈 칸에 불을 쏘면 길목 막기로 쓸 수 있다~~ → 3-1부터 불 단독으로는 화염 지대가 생기지 않음

---

## 8. 반응 규칙 — `Data/Reactions/ReactionCatalog.asset`

결정 (2026-10-17): 사용자 반응 규칙 요청표 → REACTIONS.md. 3-1은 지금 있는 속성 · 상태 · 지형으로 되는 5종, 3-2는 물 기본 + 증발 · 오염 5종. 빙결 · 빙판 계열은 3-3 물+얼음, 감전 · 수면 감전은 3-3 물+번개.

**안전장치**

| 항목 | 필드 | 값 | 근거 |
|---|---|---|---|
| 주문당 반응 최대 | `_maxReactionsPerCast` | **4** | 요청표 4-5 |
| 추가 피해 상한 | `_extraDamageCapMin` | max(주문 피해, **3**) | 요청표 4-5 ✎ |
| 폭발 피해 상한 | `_explosionDamageCapMin` | max(주문 피해, **5**) | 사용자 "폭발 상한을 올려줘"(D6) — 5는 Claude가 정함 (유독성 폭발 최대 = 중독 5틱) |

**3-1 반응 5종**

| ID | 이름 | 대상 × 속성 | 우선 | 결과 | 소모 |
|---|---|---|---|---|---|
| `toxic_ignite` | 독화 | 중독(상태) × 불 | 10 | 추가 피해 **+2** | 아니오 |
| `toxic_ignite_cloud` | 독화 | 독구름(지형) × 불 | 10 | 구름 위 대상 **+2** (위와 같은 그룹 — 대상당 1회) | 아니오 |
| `toxic_burst` | 유독성 폭발 | 중독(상태) × 번개 | 20 | 반지름 **1** 폭발, 피해 = 남은 중독 틱 × 1 (≤ 5) | 아니오 |
| `gas_explosion` | 가스 폭발 | 독구름(지형) × 번개 | 20 | 구름 제거 + 반지름 **1** 폭발 **2** + 중독 **70%** | 예 |
| `extinguish` | 소화 | 화염 지대(지형) × 얼음 | 0 | 지대 위 대상 **+2** → 지대 제거 | 예 |

**3-2 물 반응 5종** (증발 4줄은 한 그룹 `evaporate` — 대상당 추가 피해 1회)

| ID | 이름 | 대상 × 속성 | 우선 | 결과 | 소모 |
|---|---|---|---|---|---|
| `evaporate` | 증발 | 젖음(상태) × 불 | 10 | **+2**, 젖음 해제 → 그 뒤 불의 화상 판정 | 예 |
| `evaporate_water` | 증발 | 물 타일(지형) × 불 | 10 | 위 대상 **+2**, 물 제거 | 예 |
| `evaporate_contaminated` | 증발 | 오염된 물(지형) × 불 | 10 | 위 대상 **+2**, 오염된 물 제거 (원본 "물 타일과 동일 ✎") | 예 |
| `evaporate_fire_zone` | 증발 | 화염 지대(지형) × 물 | 10 | 위 대상 **+2**, 지대 유지 | 아니오 |
| `contaminate` | 오염 | 물 타일(지형) × 독 | 0 | 물 → 오염된 물 (`TransformTerrainTo`, 남은 틱 유지) | 변환 |

**3-3a 얼음 반응 6종** (빙결 3줄 한 그룹 `freeze`, 해동 2줄 한 그룹 `thaw`)

| ID | 이름 | 대상 × 속성 | 우선 | 결과 | 소모 |
|---|---|---|---|---|---|
| `freeze` | 빙결 | 젖음(상태) × 얼음 | 30 | 빙결 **100%** (`TargetStatus`), 젖음 해제 | 예 |
| `freeze_water` | 결빙 | 물 타일(지형) × 얼음 | 30 | 물 → 빙판 **8틱** (`TransformDurationTicks`), 위 대상 빙결 100% | 변환 |
| `freeze_contaminated` | 결빙 | 오염된 물(지형) × 얼음 | 30 | 오염된 물 → 빙판 **8틱**, 위 대상 빙결 100% (원본 "물 타일과 동일 ✎") | 변환 |
| `thaw` | 해동 | 빙결(상태) × 불 | 30 | **+1**, 빙결 해제 | 예 |
| `thaw_ice` | 해동 | 빙판(지형) × 불 | 0 | 빙판 → 물 타일 **12틱** | 변환 |
| `shatter` | 파쇄 | 빙결(상태) × 번개 | 30 | **+3**, 빙결 해제 | 예 |

**3-3b 감전 3종** (한 그룹 `shock`)

| ID | 이름 | 대상 × 속성 | 우선 | 결과 | 전이 `Spread` · `SpreadRange` · `SpreadMaxTargets` · `SpreadDamagePercent` |
|---|---|---|---|---|---|
| `shock` | 감전 | 젖음(상태) × 번개 | 20 | **+2** | StatusChain · **3**칸씩 · **0**(제한 없음) · **50%** → (3 + 2) × 50% = **2** |
| `shock_water` | 수면 감전 | 물 타일(지형) × 번개 | 20 | — | TerrainFlood · **8**칸(8방향) · **4**명 · **50%** → 3 × 50% = **1** |
| `shock_contaminated` | 수면 감전 | 오염된 물(지형) × 번개 | 20 | — | 물 타일과 같음 |

결과 (3-3b): 물보라 3×3로 적 무리를 적시고 번개(마나 2 + 4 = 6) → 직접 맞은 적 3 + 2, 사슬로 이어진 젖은 적마다 2. 내가 젖어 있고 3칸 안이면 나도 2 — `ShockTests`

**3-11b 풀 점화 2종** (한 그룹 `grass_ignite`, 칸 반응 `Trigger = Tile` — 결정 2026-11-04)

| ID | 이름 | 대상 × 속성 | 우선 | 결과 |
|---|---|---|---|---|
| `grass_ignite` | 풀에 불이 붙음 | 짧은 풀(칸) × 불 | 0 | 칸 → 바닥 (`TransformTileTo`) + 화염 지대 **3**틱 (`PlaceTerrain` · `TransformDurationTicks`) |
| `tallgrass_ignite` | 풀에 불이 붙음 | 높은 풀(칸) × 불 | 0 | 같음 |
| `door_ignite` · `opendoor_ignite` | 문에 불이 붙음 | 닫힌 · 열린 문(칸) × 불 (3-11c, 한 그룹 `door_ignite`) | 0 | 같음 |

칸만 바꾸는 줄이라 4회 상한에 세지 않는다(지형만 바꾸는 줄과 같음). 불 투사체는 **맞은 칸만** 점화 (지나간 칸은 아님 — 주문의 지형 칸 규칙). 번짐은 7번 "불 번짐".

엔진 규칙 보강 (3-3a): 대상 상태를 거는 지형 줄은 그 칸에 유닛이 있으면 "지형만 바꾸는 줄"이 아니다 (4회 상한에 셈, 유닛마다 그룹 1회).

엔진 규칙 보강 (3-2): ① 같은 그룹이 이미 발동한 대상의 줄은 피해 · 로그 없이 **트리거 소모 · 변환만** 한다 (젖은 적이 물 위에서 불을 맞으면 +2 한 번, 젖음과 물 둘 다 사라짐). ② **지형만 바꾸는 줄**(폭발 없음, 빈 칸이거나 추가 피해 0 — 오염 · 빈 칸의 증발 · 소화)은 주문당 4회 상한에 세지 않고 로그는 반응당 한 줄 ✎ Claude가 정함 (물 웅덩이 9칸에 독을 쏘면 9칸 모두 오염). ③ 피해 0 주문(물)은 피해 이벤트를 내지 않는다. 추가 피해 상한은 max(0, 3) = 3.

검산 (`ReactionTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 중독(5틱)된 적에 번개 = 번개 3 + 유독성 폭발 5 = **8** → 적(체력 8) 한 방, 둘레 3×3에도 5. 중독을 소모하지 않아 매 번개마다 다시 터진다 (마나 4가 유일한 제한) — `ReactionTests.ToxicBurst_ExplodesByRemainingPoison`
- 독 → 번개 콤보: 마나 3 + 4 = 7로 무리 전체에 독 2 + (중독 70%면) 폭발 최대 5. 플레이어가 폭발 범위에 있으면 같이 맞는다 — `ReactionTests.Explosion_AlsoHitsCasterInRange`
- 독구름 위로 번개 광선 = 지나간 구름 칸마다 가스 폭발(주문당 4회 상한), 겹친 대상은 1회만 — `ReactionTests.GasExplosion_ConsumesCloud_OverlapHitsOnce`
- 물 → 얼음 콤보 (3-3a): 마나 2 + 2 = 4로 3×3 젖음 → 얼음 3 + 빙결 2턴, 웅덩이가 빙판(8턴 둔화)으로. 이어서 번개면 파쇄 3 + 3 = 6, 불이면 해동 5 + 1 = 6 (+ 빙판이 다시 물로) — `IceTests`
- 물 → 불 콤보 (3-2): 마나 2 + 3 = 5로 불 5 + 증발 2 = **7** + 화상 70% (기대 9.1) — 불 단독(7.1)보다 2 높지만 마나 2와 한 턴이 더 든다. 물 웅덩이에 독을 쏘면 9칸이 오염된 물(위 유닛 매 틱 1 + 젖음)로 바뀌어 길목 막기 · 지속 피해 지대가 된다 — `WaterTests`

---

## 9. 아이템 — `Data/Items/ItemCatalog.asset`

결정 (2026-10-17, 3-2a 사용자 결정): 인벤토리(20칸) · 빠른 칸(6개)에 주문과 아이템을 함께 둔다. 아이템은 지금 마도구 하나. 얻는 방법(제작 · 획득)은 Phase 6, 그 전까지 디버그 창.

| ID | 이름 `DisplayName` | 사용 `Use` | 비고 |
|---|---|---|---|
| `fusion_device` | 마법 융합기 | Fusion | 사용 → 주문 두 개를 골라 융합 시전 (3-2b, REACTIONS.md 5번 · 이 문서 10번). 사용 자체는 차례를 쓰지 않고, 시전할 때 행동 1회 |
| `spellbook_fire` | 불꽃 화살 마법서 | LearnSpell → `fire_basic` | 3-5: 사용 → 책 소모 · 주문 배움 · 빈 빠른 칸 자동 등록, 차례 안 씀. 바닥에서 주움 (11번) |
| `spellbook_ice` | 얼음 화살 마법서 | LearnSpell → `ice_basic` | 〃 |
| `spellbook_lightning` | 번개 줄기 마법서 | LearnSpell → `lightning_basic` | 〃 |
| `spellbook_poison` | 독 폭발 마법서 | LearnSpell → `poison_basic` | 〃 |
| `spellbook_water` | 물보라 마법서 | LearnSpell → `water_basic` | 〃 |
| `shortsword` · `hand_axe` · `spear` · `quarterstaff` · `dirk` · `sickle` | 무기 6종 | EquipWeapon | 3-7a: 사용 → 든다(차례 1), 수치는 13번 |
| `potion_strength` | 힘 물약 | Potion · Strength, `Amount` **1** | 3-7b: 사용 → 힘 +1, 물약 소모, 차례 1 (결정 2026-10-19). 바닥에서 주움 (11번). 3-9부터 `Use` 이름 StrengthPotion → Potion (값 4 그대로) + 효과 줄 |
| `scroll_upgrade` | 강화 주문서 | Scroll · Upgrade, `Amount` **1** | 3-7b: 사용 → 든 무기 강화 +1(무기 자체에 — 내려놓아도 남음), 주문서 소모, 차례 1. 든 무기가 없으면 쓰지 않음. 3-9부터 UpgradeScroll → Scroll (값 5) |
| `potion_*` 7 · `scroll_*` 6 | 3-9 물약 · 주문서 | Potion · Scroll + `Effect` · `Weight` | 14번 표 |

필드 `SpellId` = 마법서가 가르치는 주문 (LearnSpell만, 3-5).

검산 (`InventoryTests.ProjectAsset_MatchesBalanceTable` — 27개, `StrengthUpgradeTests.ProjectAssets_MatchBalanceTable` — 물약 · 주문서, `SpellbookTests.ProjectAssets_MatchBalanceTable` — 마법서 → 주문): 에셋 값이 위 표와 같다.

---

## 10. 융합 — `Data/Fusions/FusionCatalog.asset`

결정 (2026-10-17, REACTIONS.md 5번 F1~F5 · 3-2b 결정): 융합 주문은 두 주문에서 계산하고, 표에는 결과(지형 · 상태)만 둔다. 얼음 + 물(빙판 · 빙결)은 3-3a에서 추가.

**계산 규칙** (`FusionBook.Build`)

| 항목 | 규칙 | 근거 |
|---|---|---|
| 마나 | 두 주문 합 | F2 |
| 행동 비용 | 기본 1회 (`GameConfig._defaultActionCostTicks`) — 규칙 10의 예외 | F2 |
| 피해 | 두 주문 합 | 3-2b 결정 |
| 모양 · 사거리 · 반지름 · 속성 | 두 주문 중 큰 모양(Bolt < Beam < Burst)의 주문 것, 같으면 첫 주문 | F3 · 3-2b 결정 |
| 상태 · 지형 | 표의 것만 (두 주문의 상태 · 지형은 안 걸림) | 3-2b 결정 |
| 반응 | 일으키지 않음 (`TriggersReactions = false`) | F4 |
| 적 사용 | 없음 (플레이어만) | F5 |

**융합 4종**

| ID | 이름 `DisplayName` ✎ | 조합 | 지형 `LeavesTerrain` | 지속 `TerrainDurationTicks` | 상태 | 결과 (BALANCE 5번 주문 기준) |
|---|---|---|---|---|---|---|
| `fusion_fire_water` | 용암 분출 | 불 + 물 | FireZone 화염 지대 | **6** (지형 규칙 4를 덮어씀) | 없음 | Burst 3×3 · 사거리 5 · 피해 5 + 0 = **5** · 마나 3 + 2 = **5**. 지대 위 매 턴 화상 100% |
| `fusion_water_poison` | 오염 물결 | 물 + 독 | ContaminatedWater 오염된 물 | **8** | 없음 | Burst 3×3 · 사거리 5 · 피해 0 + 2 = **2** · 마나 2 + 3 = **5**. 위 매 턴 피해 1 + 젖음 |
| `fusion_fire_lightning` | 폭발 마법 | 불 + 번개 | 없음 | — | 없음 | Beam 직선 관통 · 사거리 5 · 피해 5 + 3 = **8** · 마나 3 + 4 = **7** |
| `fusion_ice_water` (3-3a) | 빙결 파도 | 얼음 + 물 | IcePatch 빙판 | **8** | Frozen 빙결 **100%** | Burst 3×3 · 사거리 5 · 피해 3 + 0 = **3** · 마나 2 + 2 = **4**. 빙판 위 매 턴 둔화 |

이름은 표시 전용(✎ Claude가 정함). 상태를 거는 융합을 추가할 때는 `StatusChancePercent`를 꼭 적는다 — 인스펙터 새 줄은 0%로 시작한다 (0%면 `OnValidate`가 경고).

검산 (`FusionTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다. 계산은 `FusionTests.Build_SumsManaAndDamage_TakesLargerShape`.
결과:
- 용암 분출 = 즉시 5 + 지대 위 화상(1/턴)이 지대 6턴 동안 계속 갱신 → 머물면 최대 5 + 6 + 2 = 13. 물 → 불 콤보(마나 5 · 행동 2회 · 7 + 화상 70%)와 마나는 같고 행동이 1회 적다 — `FusionTests.FireWater_LeavesFireZoneSixTicks_NoSpellStatusOrWater`
- 폭발 마법 = 일렬 대상마다 8 → 적(체력 8) 한 방, 마나 7이라 가득 찬 마나로 1번. 중독된 적이나 독구름 위여도 반응이 없다 — `FusionTests.FireLightning_BeamEight_DoesNotReact`
- 오염 물결 = 즉시 2 + 위에 있으면 매 턴 1 (8턴) — `FusionTests.WaterPoison_LeavesContaminatedWaterEightTicks`

---

## 11. 바닥 아이템(마법서) 배치 — `Data/Items/LootConfig.asset`

결정 (2026-10-19, 3-5 사용자 결정 — 모두 추천안): 한 판에 속성별 마법서를 **한 권씩**(지금 5권) 마지막(보스) 층을 뺀 층(1~4층)에 나눠 놓는다 · 층당 1~2권 · **1층 최소 1권**(빈손 시작 보장) · 걸어 들어가면 자동으로 줍기 · 사용하면 책 소모 + 빈 빠른 칸 자동 등록.

| 값 | 필드 | 값 | 비고 |
|---|---|---|---|
| 층당 최대 마법서 | `_maxBooksPerFloor` | **2** | 1층부터 1권씩 채우고 남는 책은 아직 2권 미만인 층에 무작위로 더 |
| 한 판 강화 주문서 수 (3-7b) | `_upgradeScrollsPerRun` | **3** | 1~4층에 무작위 (같은 아이템 여러 개), 무기 계획 뒤에 붙임 |
| 한 판 힘 물약 수 (3-7b) | `_strengthPotionsPerRun` | **2** | 〃 — 2개 다 마시면 힘 12 = 무기 요구 힘 (명중 벌점 없음) |
| 한 판 무기 수 (3-7a) | `_weaponsPerRun` | **2** | 무기 6종을 시드로 섞어 앞에서 2개(같은 종 안 겹침) — **첫 무기는 반드시 1층**(사용자 요청 2026-10-19), 둘째는 1~4층에 무작위. 책 계획 뒤에 붙여 책 · 맵 · 적 배치는 그대로 (결정 2026-10-19) |
| 시작 칸에서 최소 거리 | `_minDistanceFromStart` | **3** | 칸(대각선 포함). 후보가 모자라면 거리를 줄여 다시 찾음. 계단 · 벽 칸 · 다른 아이템 칸 제외 |

난수: `RngStream.Loot`(판 시작에 책 순서 섞기 · 층 배정, 처음 가는 층에서 칸 고르기) — 맵 · 적 배치(MapGen)는 바뀌지 않는다. 같은 시드 + 같은 진행 = 같은 책 위치.
지금 결과 (책 5 · 일반 층 4): 1~4층 모두 1권 + 무작위 한 층에 1권 더.

검산 (`SpellbookTests.ProjectAssets_MatchBalanceTable` · `Plan_FiveBooks_OnePerSpell_FloorsOneToFour_OneOrTwoEach` — 시드 30개).

---

**금화 (3-15a, 결정 2026-11-04 — 바닥 무더기 + 적, SPD)** — `LootConfig.asset`의 금화 값 [임시]

| 값 | 필드 | 값 |
|---|---|---|
| 층마다 무더기 | `_goldPilesPerFloor` | **3** (처음 가는 층, 시작 칸에서 `_minDistanceFromStart` 이상 · 아이템 없는 맨땅) |
| 무더기 금액 | `_goldMinBase` + `_goldMinPerFloor` × 층 ~ `_goldMaxBase` + `_goldMaxPerFloor` × 층 | **30 + 10 × 층 ~ 60 + 20 × 층** (1층 40~80, 4층 70~140) |
| 적이 떨어뜨림 | `_enemyGoldPercent` · `_enemyGoldMin` ~ `_enemyGoldMax` | **20**% · **5 ~ 15** |

금화(`gold`, `ItemUse.Gold`)는 걸어 들어가면 배낭이 아니라 지갑(`RunState.Gold`, 스냅샷 v21)으로. HUD 갑옷 줄 옆에 표시. 검산 (`GoldTests.ProjectAsset_MatchesBalanceTable`).

**던지는 무기 (3-16a, 결정 2026-11-04 — 다트 · 투창, SPD)** — `ItemCatalog.asset`의 `Thrown` 줄 + `LootConfig.asset` 묶음 [임시]

| 무기 | ID | 피해 | 부러짐 | 값 | 묶음 |
|---|---|---|---|---|---|
| 다트 | `dart` | **1 ~ 3** | **0**% (늘 떨어진 칸에 남음) | 5 | **5**개 |
| 투창 | `javelin` | **3 ~ 8** | **33**% | 25 | **2**개 |

배낭 → 던지기(물약과 같은 조준 · 사거리 8). 떨어진 칸의 적대 유닛에게 명중 굴림(근접과 같은 `Combat.RollHit`) → 피해 − 방어 굴림. 층마다 `_thrownPerFloor` **1**묶음(Loot 난수, 금화 다음). 겹친다.

## 12. 경험치 · 레벨업 — `Data/Units/ProgressionConfig.asset`

결정 (2026-10-19, 3-6 사용자 결정): Shattered Pixel Dungeon 방식 — 필요 경험치 = 5 + 5 × 레벨, 레벨업마다 최대 체력 +5 · 최대 마나 +1 · 명중 +1 · 회피 +1(현재 체력 · 마나도 같은 만큼 오름). 적마다 최고 레벨이 있어 플레이어 레벨이 그보다 높으면 경험치 0 (반복 사냥 방지 — 적 재생성 때문에 필요).

| 값 | 필드 | 값 | 비고 |
|---|---|---|---|
| 최대 레벨 | `_maxLevel` | **30** | SPD와 같음. 도달하면 남는 경험치는 버림 |
| 필요 경험치 기본 | `_expBase` | **5** | 다음 레벨 필요량 = 기본 + 레벨 × 레벨당, 레벨마다 0부터 |
| 필요 경험치 레벨당 | `_expPerLevel` | **5** | 1→2 = 10, 2→3 = 15, 10→11 = 55 |
| 레벨당 최대 체력 | `_hpPerLevel` | **5** | SPD 20 + 5 × (레벨 − 1) — 플레이어 시작 20이 SPD와 같음 |
| 레벨당 최대 마나 | `_manaPerLevel` | **1** | SPD에 없음 (사용자 결정 — A안). 레벨 10 = 최대 마나 19 |
| 레벨당 명중 | `_accuracyPerLevel` | **1** | SPD |
| 레벨당 회피 | `_evasionPerLevel` | **1** | SPD |

결과 (근접 적만 잡을 때): 레벨 2까지 10마리, 레벨 5까지 70마리(10 + 15 + 20 + 25) — 근접 적은 레벨 5까지만 경험치를 줘서 레벨 6부터는 보스(10)뿐. 지금 한 판(층당 5마리 + 재생성) 기준으로 레벨 3~4 근처 예상 — Phase 4 재미 검증에서 적 종류 · 경험치로 조정.

검산 (`ProgressionTests.ProjectAssets_MatchBalanceTable` · `ExpToNext_IsSpdFormula` · `GainExp_*`).

---

## 13. 무기 — `Data/Items/ItemCatalog.asset` (무기 줄 · 공통 값)

결정 (2026-10-19, 3-7a — `Docs/장비_데이터.md` 4번): 사용자 값(SPD 2티어 크기)을 그대로 넣고 **나누기 3**(비율 유지)으로 게임 크기에 맞춤. 무기는 최소~최대 굴림(`RngStream.Combat`), 맨손은 `_attackDamage` 고정 3. 주문은 고정 피해 그대로.

| 공통 | 필드 | 값 | 비고 |
|---|---|---|---|
| 피해 나누기 | `_weaponDamageDivisor` | **3** | 최대 = round((최대 피해 + 효율 × 강화) / 3), 0.5 올림 |
| 최소 피해 기본 | `_weaponMinDamage` | **2** | 최소 = max(1, round((2 + 강화) / 3)) — SPD 2티어 |

| ID | 이름 | 최대 피해 `MaxDamage` | 강화 효율 | 명중 `Accuracy` | 비용 (틱) | 거리 | 게임 피해 (+0) |
|---|---|---|---|---|---|---|---|
| `shortsword` | 소검 | **15** | **3** | 1.0 | 1 | 1 | 1 ~ 5 |
| `hand_axe` | 손도끼 | **12** | **3** | **1.32** | 1 | 1 | 1 ~ 4 |
| `spear` | 창 | **20** | **4** | 1.0 | **2** | **2** | 1 ~ 7 |
| `quarterstaff` | 육척봉 | **12** | **3** | 1.0 | 1 | 1 | 1 ~ 4 · 방어 `Defense` **2** (3-7c) |
| `dirk` | 더크 | **12** | **3** | 1.0 | 1 | 1 | 1 ~ 4 · 기습 `SurpriseMinPercent` **67** → 3 ~ 4 (3-7c) |
| `sickle` | 손낫 | **20** | **3** | **0.68** | 1 | 1 | 1 ~ 7 |
| `knuckle` (3-22a) | 너클 | **5** | **3** | 1.0 | 1 | 1 | 1 ~ 2 **× 2타** (`HitsPerAttack` **2** — 명중 · 피해 · 방어를 타격마다 따로) · 요구 힘 **10** · +1 1 ~ 3. 시아가 들면 시아 기술 강화 (22번) |

결과: 평균 피해 소검 3.0(맨손 3과 같음) · 손도끼 · 육척봉 · 더크 2.5 · 창 · 손낫 4.0. 근접 적(체력 8)은 소검으로 평균 2.7대. 창은 2틱이라 시간당 피해 2.0 — 대신 한 칸 떨어져 칠 수 있음. 손낫은 명중 10 × 0.68 = 6.8 vs 적 회피 2 → 약 85%.
요구 힘 `StrengthRequirement` (3-7b): 6종 모두 **12**(SPD 2티어). 강화하면 **첫 강화에 −1, 그 뒤 2단계마다 −1** (사용자 결정 2026-10-19) = 12 − ⌈강화 / 2⌉ → +1 11 · +2 11 · +3 10 · +4 10 · +5 9.
힘 효과: 시작 힘 10이면 부족 2 → 명중 ÷ 2.25 (명중 10 → 4.4, 근접 적 회피 2 상대 약 77%). 힘 물약 2개 = 힘 12 → 벌점 없음. 강화 주문서 3장을 한 무기에 = +3 → 요구 10, 최대 피해 +3(소검 2~8) — 힘 12면 남는 힘 2 → 보너스 0~2.

검산 (`StrengthUpgradeTests` — 요구 힘 표 · 명중 배수 · 보너스 굴림).

기습 · 방어 (3-7c, 결정 2026-10-19 — SPD):
- 기습 = 플레이어가 치는 적이 플레이어를 못 본 상태(적 기억 `LastSeenTargetPos` 없음 — 배회 · 아직 못 알아챔 · 쫓다 놓침). 기습은 **항상 명중**(맨손 포함), 더크는 최소 피해 = 최소 + (최대 − 최소) × 67% (0.5 올림): +0 3 ~ 4 · +2 4 ~ 6.
- 방어 = 근접 피해 − (0~방어) 굴림, 0 아래로 안 감. 지금은 육척봉 2뿐(나누기 없음). 주문 피해는 줄이지 않음. 근접 적(피해 2) 상대 평균 1 막음 = 절반.

검산 (`SurpriseDefenseTests`).

검산 (`WeaponTests.ProjectAssets_MatchBalanceTable` · `Resolve_MatchesDesignTable`).

---

## 14. 물약 · 주문서 — `Data/Items/ConsumableConfig.asset` + `ItemCatalog.asset` (3-9)

결정 (2026-10-20, 사용자 — 모두 추천안): SPD와 같은 효과 · 식별 · 무게. 쉬운 15종부터 (나머지는 다음 항목). 해로운 물약은 던진다. 수치 중 SPD에 그대로 대응하지 않는 것(반경 · 사거리 · 마비 가스 지속)은 Claude 임시값.

**종류 · 무게** (`ItemCatalog` 줄, 무게 = 층마다 무작위로 뽑힐 비율 — SPD 표 그대로, 0 = 무작위 안 됨):

| ID | 이름 | `Effect` | `Weight` | 효과 |
|---|---|---|---|---|
| `potion_strength` | 힘 물약 | Strength | 0 | 힘 +1 — 한 판 2개 보장 (11번) |
| `potion_healing` | 치유 물약 | Healing | **3** | 체력 가득 + 화상 · 둔화 · 중독 · 마비 풀기 (SPD는 서서히 — 여기는 즉시) |
| `potion_experience` | 경험 물약 | Experience | **1** | 다음 레벨까지 필요한 경험치 (= 1레벨 오름) |
| `potion_purity` | 정화 물약 | Purity | **2** | 3걸음 안 화염 지대 · 독구름 · 마비 가스 없앰 |
| `potion_toxic_gas` | 독가스 물약 | ToxicGas | **3** | 2걸음 안 독구름 (7번 4틱) |
| `potion_liquid_flame` | 액체 화염 물약 | LiquidFlame | **3** | 2걸음 안 화염 지대 (7번 4틱) |
| `potion_frost` | 냉기 물약 | Frost | **3** | 2걸음 안 유닛 빙결(6번 2틱) · 화염 지대 끔 · 물 → 빙판(8틱) |
| `potion_paralytic_gas` | 마비 가스 물약 | ParalyticGas | **2** | 2걸음 안 마비 가스 (7번 4틱) |
| `potion_haste` (3-13a) | 신속 물약 | Haste | **1** | 마신 유닛 신속 20틱 (6번) |
| `potion_invisibility` (3-13a) | 투명 물약 | Invisibility | **1** | 투명 20틱 |
| `potion_levitation` (3-13a) | 부양 물약 | Levitation | **1** | 부양 20틱 |
| `potion_mind_vision` (3-13a) | 정신 시야 물약 | MindVision | **2** | 정신 시야 20틱 |
| `scroll_mirror_image` (3-13b) | 분신 주문서 | MirrorImage | **2** | 둘레에 분신(아군) **2**명 — 체력 **1**, 모습 · 공격 · 명중 · 회피는 읽은 유닛 (`_mirrorImageCount` · `_mirrorImageHp`). 적을 찾아 공격, 없으면 곁(2칸)에. 적도 더 가까운 분신을 친다 |
| `scroll_terror` (3-13b) | 공포 주문서 | Terror | **1** | 보이는 적 공포 20틱 |
| `scroll_lullaby` (3-13b) | 자장가 주문서 | Lullaby | **1** | 보이는 적 잠 30틱 (맞으면 깸) |
| `scroll_transmutation` (3-13b) | 변환 주문서 | Transmutation | **1** | 배낭에서 고른 하나를 같은 쓰임의 다른 것으로 — 무기 강화 유지, 갑옷 같은 티어 우선, 힘 물약 · 강화 주문서는 결과에서 뺌, 마법서는 안 배운 주문 (`RngStream.Combat`) |
| `scroll_upgrade` | 강화 주문서 | Upgrade | 0 | 든 무기 +1 — 한 판 3장 보장 (11번) |
| `scroll_identify` | 식별 주문서 | Identify | **3** | 인벤토리 앞 칸부터 모르는 물약 · 주문서 한 종류 (SPD는 고르기 — 고르는 창은 나중에) |
| `scroll_magic_mapping` | 지도 주문서 | MagicMapping | **1** | 이 층 바닥 + 둘레 벽 전부 탐험 |
| `scroll_teleportation` | 순간이동 주문서 | Teleportation | **1** | 이 층의 빈 바닥(계단 제외) 무작위 (`RngStream.Combat`) |
| `scroll_recharging` | 충전 주문서 | Recharging | **1** | 마나 가득 (SPD는 마법봉 충전 → 이 게임은 마나) |
| `scroll_rage` | 격노 주문서 | Rage | **1** | 이 층의 모든 적이 읽은 칸을 기억 → 몰려옴 |
| `scroll_retribution` | 응징 주문서 | Retribution | **1** | 보이는 적마다 아래 공식 피해 |

**ConsumableConfig** (`ConsumableConfigAsset` 필드 초기값 = 아래 값):

| 항목 | 필드 | 값 | 비고 |
|---|---|---|---|
| 물약 겉모습 | `_potionLooks` | 12색 (`potion_crimson` … `potion_ivory`) | 판마다 종류에 섞어 나눔 (`RngStream.Loot`, 배치 계획 모두 뒤) |
| 주문서 겉모습 | `_scrollLooks` | 12룬 (`scroll_tyr` … `scroll_oethel`) | 〃 |
| 층마다 무작위 물약 | `_randomPotionsPerFloor` | **2** | 1~마지막 전 층마다, 무게대로 (겹칠 수 있음) |
| 층마다 무작위 주문서 | `_randomScrollsPerFloor` | **2** | 〃 |
| 가스 퍼짐 | `_gasRadius` | **2** 걸음 | 벽을 돌아 셈 (벽 너머로 새지 않음). 독가스 · 화염 · 냉기 · 마비 가스 |
| 정화 퍼짐 | `_purityRadius` | **3** 걸음 | |
| 던지기 사거리 | `_throwRange` | **8** 칸 | 벽 앞 · 유닛 칸에서 멈춤, 보이는 칸만 |
| 치유가 푸는 상태 | `_healingCures` | 화상 · 둔화 · 중독 · 마비 | |
| 정화가 없애는 지형 | `_purityClears` | 화염 지대 · 독구름 · 마비 가스 | 물 · 빙판은 그대로 |
| 응징 | `_retributionBasePercent` · `_retributionHpPercent` · `_retributionPowerScale` · `_retributionPowerCap` | **10** · **22.5** · **4.45** · **4** | SPD 그대로: 피해 = 최대 체력 × 10% + 현재 체력 × 힘 × 22.5%, 힘 = min(4, 잃은 체력 비율 × 4.45) |

쓰기 규칙: 마시기 · 읽기 · 던지기 모두 차례 1. 마시거나 읽으면 그 종류가 식별된다. 던진 물약은 범위 물약(정화 · 가스 · 화염 · 냉기)만 효과가 나고 식별되며, 나머지는 깨져 사라진다(SPD). 주문서는 던지지 않는다.

결과:
- 한 판(5층) 물약 · 주문서 = 보장 5 + 4층 × 4 = **21개**. 무작위 물약 하나가 치유일 확률 3/17 ≈ 18%, 무작위 주문서 하나가 식별일 확률 3/8.
- 응징 검산: 내 체력 10/20 → 힘 2.225 → 체력 200인 적 = 20 + 100.125 → **120**. 체력 가득이면 힘 0 → 최대 체력 10%만. 근접 적(체력 8)은 내 체력이 반이면 0.8 + 8 × 2.225 × 0.225 = 4.8 → **5**.
- 가스 2걸음 = 열린 방에서 5×5 (25칸). 던지기 사거리 8이면 플레이어가 범위(2) 밖에서 던질 수 있다. 마시면 자기도 맞는다 (규칙 10).

검산 (`ConsumableTests.ProjectAssets_MatchBalanceTable` · `Retribution_HitsVisibleEnemiesOnly_BalanceValue` · `Retribution_FullHp_OnlyBasePart`).

---

## 15. 갑옷 — `ItemCatalog.asset` (갑옷 줄 · 공통 값) (3-10a)

결정 (2026-10-20, 사용자 — 모두 추천안): SPD 5종 전부 · 방어는 무기처럼 ÷3 · 모델은 Varco로(그 전까지 회색 상자) · 갑옷 → 인벤토리 UI 순서.
SPD 규칙 그대로: 방어 굴림 = 강화 ~ 티어 × (2 + 강화) (SPD Armor.DRMin/DRMax, 고른 굴림 — SPD는 정규분포), 요구 힘 = 8 + 2 × 티어. 근접 피해만 줄인다(주문 X).

| 공통 | 필드 | 값 | 비고 |
|---|---|---|---|
| 방어 나누기 | `_armorDefenseDivisor` | **3** | 게임 방어 = round((굴림 − 벌점) / 3), 0 이상 |
| 힘 모자람 벌점 | `_armorDeficitPenalty` | **2** | 모자란 힘 1당 굴림 − 2 (SPD Hero.drRoll, 나누기 전). 회피도 1당 ÷ 1.5 (SPD) |

| ID | 이름 | 티어 `Tier` | 요구 힘 | SPD 방어 (+0) | 게임 방어 (+0) | +3 |
|---|---|---|---|---|---|---|
| `armor_cloth` | 천 갑옷 | 1 | **10** | 0~2 | 0~1 | 요구 8 · 1~2 |
| `armor_leather` | 가죽 갑옷 | 2 | **12** | 0~4 | 0~1 | 요구 10 · 1~3 |
| `armor_mail` | 사슬 갑옷 | 3 | **14** | 0~6 | 0~2 | 요구 12 · 1~5 |
| `armor_scale` | 비늘 갑옷 | 4 | **16** | 0~8 | 0~3 | 요구 14 · 1~7 |
| `armor_plate` | 판금 갑옷 | 5 | **18** | 0~10 | 0~3 | 요구 16 · 1~8 |

- 요구 힘 강화 감소는 무기와 같은 규칙(첫 강화 −1, 그 뒤 2단계마다 −1 — 사용자 결정 2026-10-19). SPD 원래 공식(삼각수)과 다름.
- 플레이어는 **천 갑옷을 입고 시작** (`Player.asset` `_startingArmor` = `armor_cloth`, SPD). 한 판에 갑옷 **2벌** (`LootConfig` `_armorsPerRun`, 2티어 이상 중 겹치지 않게, 1~4층).
- 강화 주문서: 든 무기 먼저, 없으면 입은 갑옷 (`UpgradeTarget.Auto`) — 고르는 창은 3-10b 인벤토리 UI에서.
- 육척봉 방어(0~2, 13번)와 함께면 둘 다 굴려 더함.

결과: 시작(힘 10 · 천 갑옷) 근접 적 피해 2 → 평균 약 0.5 막음. 판금은 힘 18이 필요 — 힘 물약 2개(12)로는 6 모자라 굴림 −12 → 사실상 방어 0 · 회피 ÷ 11. 사슬(14)도 힘 12면 2 모자라 0~6 − 4 → 0~1 · 회피 ÷ 2.25 → 지금 판(힘 물약 2개)에서는 가죽(12) +강화가 현실적인 선택.

검산 (`ArmorTests` — `Resolve_MatchesSpdFormula` · `ShownDefense_DividedByThree` · `ProjectAssets_MatchBalanceTable`).

---

## 16. 층 꾸미기 (풀 · 물 웅덩이) — `Data/Dungeon/FloorDecorConfig.asset` (3-11a)

결정 (2026-11-04, 사용자): 지형에 무작위로 **물 웅덩이**(사라지지 않는 물 지형 — 7번 Water를 지속 `Permanent`로) · **풀**(짧은 풀 · 높은 풀 칸, SPD). 수치는 Claude 임시값 [임시] — 재미 검증에서 조정.

| 값 | 필드 | 값 | 뜻 |
|---|---|---|---|
| 풀 덩어리 수 (층마다) | `_grassPatchCount` | **6** | 방 안 무작위 바닥 칸에서 상하좌우로 넓힘 |
| 풀 덩어리 크기 | `_grassPatchMinSize` ~ `_grassPatchMaxSize` | **4 ~ 14** 칸 | 덩어리마다 고름 (막히면 더 작게) |
| 높은 풀 확률 | `_tallGrassPercent` | **50** % | 풀 칸마다 — 나머지는 짧은 풀 |
| 물 웅덩이 수 (층마다) | `_puddleCount` | **4** | 풀보다 먼저 놓음 (웅덩이 칸에는 풀 없음) |
| 웅덩이 크기 | `_puddleMinSize` ~ `_puddleMaxSize` | **3 ~ 9** 칸 | |
| 숨겨진 문 확률 (3-12b) | `_hiddenDoorPercent` | **15** % | 놓은 문마다 — 막아도 층이 끊기지 않는 자리(우회로 있음)만, 아니면 보통 문 [임시] |
| 문 확률 (3-11c) | `_doorPercent` | **70** % | 방 입구(복도가 방에 들어가는 한 칸 폭 길목)마다 — 나머지는 빈 입구. 꾸미기 맨 처음 [임시] |

- 바닥(Floor) 칸만 바꾼다 — 계단 · 시작 칸 · 복도 밖 벽은 그대로. 보스 층은 꾸미지 않는다. 난수 `RngStream.Decor` (맵 모양 · 적 배치 불변).
- 높은 풀: 걸을 수 있음 · **시야를 가림**(풀 칸 자체는 보임) · 주문 · 투척 경로는 막지 않음 · 유닛(적 포함)이 들어서면 짧은 풀 (⑤). 짧은 풀은 장식.
- 문 (3-11c, SPD): 닫힌 문은 걸을 수 있음 · **시야를 가림** · 주문 경로는 막지 않음. 유닛(적 포함)이 서 있는 동안 열린 문(⑤ `DoorSystem`), 비면 닫힘. 불에 탐(점화 반응 · 번짐 → 바닥 + 화염 지대 3틱). 문 자리 = 방 칸이 아닌 바닥 중 한 축이 모두 벽이고 다른 축의 한쪽만 방 칸인 칸, 서로 붙은 자리는 하나만.
- **특수 방 (3-15b, 결정 2026-11-04 — SPD)**: 층마다 `_specialRoomPercent` **70**%로 하나, 종류 무게 보물 방 **3** · 무기고 **2** · 도서관 **2** · 정원 **2** · 연구실 **2**(3-16c, BALANCE 19번), 안쪽 **3~5**칸 정사각(벽 덩어리 속에 파고 입구 한 칸 — 둘레 한 줄이 모두 벽인 자리만). 보물 방 = **잠긴 문**(`LockedDoor`) + 안에 `_vaultItems`(강화 주문서 · 힘 물약 · 경험 물약 · 사슬 갑옷 · 창) 중 **2**개, 같은 층 방 밖 맨땅(시작 칸 3칸 이상)에 **쇠 열쇠**(`key_iron`). 무기고 = 무기 · 갑옷 **2**, 도서관 = 주문서 **3**, 정원 = 높은 풀 가득 + 음식 **2**. 열쇠를 가지고 잠긴 문에 부딪히면(상하좌우) 차례 1 · 열쇠 하나를 써서 닫힌 문으로. 열쇠는 층을 가리지 않음(SPD는 그 층만 — Claude 임시) [임시]
- **상점 (3-15c, 결정 2026-11-04 — 3층 · 사기 + 팔기 · 2026-11-05 사용자: 구역 시작층 6 · 11 · 16 · 21층에도)**: `_shopFloor` **3**층 + `_moreShopFloors` **6 · 11 · 16 · 21**층에 상점 방(4~5칸, 보통 문) + 상인(중립, 체력 **999**, 입구에서 가장 먼 칸) + 가격표 물건 `_shopStockCount` **6**개(`_shopStock` 12종 중 겹치지 않게). 사는 값 = 아이템 값 × `_shopBuyPercent` **100**% — 가격표 칸에 걸어 들어가면 금화가 충분할 때 삼(모자라면 안내). 파는 값 = 값 × `_shopSellPercent` **50**% — 상인에게 부딪히거나 상인을 누르면(멀면 옆까지 걸어감) 배낭에서 고름(차례 안 씀 · 창 왼쪽에 상인 일러스트). **아이템 값**(`ItemCatalog` `Value`, Setup이 0인 줄만 채움): 무기 **60** · 갑옷 **30 × 티어** · 물약 · 주문서 **30**(힘 · 강화 **150**, 경험 **100**) · 식량 **20** · 빵 **10** · 고기 **5** · 마법서 **100** · 융합기 **200** · 열쇠 **50** · 금화 0 [임시]
- **화난 상인 (3-29, 결정 2026-11-05 — 사용자)**: 상인을 **주문 · 던지기(반응 포함)**로 다치게 하면 그 판 상점은 끝 — 상인이 적이 되고 체력 = 그 구역 보스 체력 × `_angryKeeperHpPercent` **150**%(공격 · 명중 · 회피 = 보스 값), 가격표 물건을 모두 챙긴다. 차례마다: 체력 < `_angryKeeperHealBelowPercent` **50**%면 치유 물약 → `_angryKeeperItemPercent` **35**% 확률로 보이는 나에게 해로운 물약(독가스 · 액체 화염 · 냉기 · 마비 가스) · 던지는 무기를 던짐 → 아니면 쫓아와 근접. 쓴 물건은 사라지고, 쓰러뜨리면 남은 물건이 전부 그 칸에. 근접(부딪힘)은 그대로 팔기. 예: 3층(1구역 보스 슬라임 소녀) 상인. 검산 `AngryShopkeeperTests.HudText_And_ProjectAssets`
- 웅덩이는 물 지형 그대로 반응한다 — 증발(불)이면 사라짐, 오염(독)이면 **영구 오염된 물**(남은 틱 유지), 결빙(얼음)이면 빙판 8틱 → 녹으면 물 12틱 → 사라짐.

검산 (`FloorDecorTests.ProjectAsset_MatchesBalanceTable`). 결과: 50×50 층에서 웅덩이 칸 최대 4 × 9 = 36, 풀 칸 최대 6 × 14 = 84.

---

## 17. 함정 — `Data/Dungeon/TrapCatalog.asset` + `SpellCatalog.asset`의 `trap_*` (3-12a)

결정 (2026-11-04, 사용자 — 모두 추천안): **속성 함정 7종**(지금 있는 지형 · 상태를 씀, 함정도 반응을 일으킴) · 찾기는 SPD식(3-12b) · **적도 밟으면 발동**(적은 피하지 않음, 플레이어 클릭 이동은 드러난 함정을 피함). 수치는 Claude 임시값 [임시].

**배치** (`TrapCatalogAsset`): 층마다 **4**개 (`_trapsPerFloor`) · 숨김 **85**% (`_hiddenPercent`) · 시작 칸에서 **3**칸 이상 (`_minDistanceFromStart`). 맨땅(바닥 · 풀) 중 지형 효과 없는 칸, 꾸미기 맨 끝(Decor 난수), 보스 층 없음. 적 배치 · 재생성은 함정 칸을 피한다.

| 함정 | `TrapType` | 효과 주문 (함정 칸 중심 3×3, 시전자 없음 · 마나 0) | 특수 | 무게 |
|---|---|---|---|---|
| 화염 함정 | Fire | `trap_fire` 불 — 피해 **2** + 화상 100% + 화염 지대(4틱) | — | 2 |
| 독가스 함정 | ToxicGas | `trap_toxic` 독 — 피해 0 + 중독 100% + 독구름(4틱) | — | 2 |
| 마비 가스 함정 | ParalyticGas | `trap_paralytic` **바람**(반응 없음) — 마비 가스(4틱) | — | 1 |
| 물 함정 | Flood | `trap_flood` 물 — 젖음 100% + 물 타일(12틱) | — | 2 |
| 번개 함정 | Shock | `trap_shock` 번개 — 피해 **3** + 마비 30% (젖었으면 감전) | — | 2 |
| 순간이동 함정 | Teleport | — | 밟은 유닛을 무작위 빈 칸으로 (순간이동 주문서와 같음) | 1 |
| 경보 함정 | Alarm | — | 층의 모든 적이 함정 칸을 안다 (몰려옴) | 1 |

**식물 (3-16b, 결정 2026-11-04 — 씨앗을 심으면, SPD)** — 같은 표의 무게 **0** 줄(무작위 배치 없음), 늘 보임

| 식물 | 씨앗 | 효과 |
|---|---|---|
| 화염꽃 | `seed_firebloom` | `plant_firebloom` 불 3×3 — 피해 2 + 화상 + 화염 지대 |
| 얼음버섯 | `seed_icecap` | `plant_icecap` 얼음 3×3 — **빙결** 100% |
| 독이끼 | `seed_sorrowmoss` | `plant_sorrowmoss` 독 3×3 — 중독 + 독구름 |
| 햇볕풀 | `seed_sungrass` | 밟은 유닛 회복 최대 체력 × `_healPercent` **50**% |
| 사라짐잎 | `seed_fadeleaf` | 밟은 유닛 순간이동 |

씨앗: 사용 = 발밑에 심기(차례 1 — 그 자리에서 바로 발동), 던지기 = 떨어진 칸에 심기(못 심는 칸이면 씨앗이 남음). 층마다 `_seedsPerFloor` **2**개(LootConfig), 높은 풀을 밟으면 `_seedDropPercent` **5**%로 하나 [임시]. 값 10.

- 발동 = ⑤ `TrapSystem`: 함정 칸에 선 살아 있는 유닛(EntityId 순) — 숨겨진 것도. 한 번 발동하면 사라진다. 효과 순서는 주문과 같다(피해 → 반응 → 상태 → 지형). 함정 주문은 배우지 않는다(마법서 없음).
- 사망 원인은 주문 이름(예: "화염 함정").

**찾기 (3-12b, SPD)** — `TrapCatalogAsset`의 찾기 값

| 값 | 필드 | 값 | 뜻 |
|---|---|---|---|
| 찾기 반경 | `_searchRadius` | **2** 칸 | 찾기 행동(**Z** 키)이 둘레(대각선 포함)의 숨겨진 함정 · 문을 **확실히** 드러냄 |
| 찾기 비용 | `_searchCostTicks` | **2** 틱 | |
| 빠른 찾기 반경 | `_quickSearchRadius` | **1** 칸 | 3-24 정보 모드(E)에서 E를 한 번 더 — 주변 8칸을 확실히 |
| 빠른 찾기 비용 | `_quickSearchCostTicks` | **1** 틱 | |
| 저절로 발견 | `_passiveSearchPercent` | **10** % | 플레이어 둘레 `_passiveSearchRadius`(**1**)칸의 숨겨진 것마다 매 틱 (난수 `RngStream.Search`) |

- 지도 주문서(14번)는 층의 숨겨진 함정 · 문을 모두 드러낸다.
- 숨겨진 문 = 칸 종류 `HiddenDoor`: 찾기 전엔 벽(못 지나감 · 시야 막음 · 벽으로 그림 — 적도 못 지나감), 찾으면 닫힌 문.
- 결과: 10%면 숨겨진 함정 옆에 한 턴 서 있을 때 찾을 확률 10%, 지나가며 옆을 2턴 스치면 약 19%. 의심되면 찾기(2턴)로 확실히.

검산 (`TrapTests.ProjectAssets_MatchBalanceTable`).

---

## 18. 허기 · 음식 — `Data/Units/HungerConfig.asset` + `ItemCatalog.asset`의 음식 줄 (3-14)

결정 (2026-11-04, 사용자 — 모두 추천안): 수치 **SPD 그대로** · 음식 = **식량 · 작은 빵 · 정체불명 고기 + 불 요리** · 배치 = **층마다 식량 1 보장 + 가끔 빵**, 판 시작 배낭에 식량 1.

| 값 | 필드 | 값 | 뜻 |
|---|---|---|---|
| 배고픔 | `_hungryAt` | **300** | 허기(플레이어만, 매 턴 +1)가 이 값 이상 — 알림만 |
| 굶주림 | `_starvingAt` | **450** | 이 값에서 멈춤 · 매 턴 피해 `_starveDamage` **1** · 자동 회복 멈춤 |
| 먹기 비용 | `_eatTicks` | **3** 턴 | SPD |
| 정체불명 고기 | `_riskyFoodBadPercent` · `_riskyFoodStatuses` | **60**% · 화상 · 둔화 · 중독 중 하나 | [임시] (SPD는 5가지 중 하나 · 없음) |
| 고기 떨어뜨리기 | `_enemyDropItem` · `_enemyDropPercent` | 정체불명 고기 · **15**% | 적이 죽을 때 그 칸 [임시] |
| 층 음식 | `_floorFoodItem` · `_bonusFoodItem` · `_bonusFoodPercent` | 식량 · 작은 빵 · **50**% | 1~마지막 전 층 (Loot 난수, 계획 끝) |
| 시작 음식 | `_startingFood` | 식량 | |

| 음식 | ID | 허기 감소 | 비고 |
|---|---|---|---|
| 식량 | `food_ration` | **300** | |
| 작은 빵 | `food_bread` | **150** | |
| 정체불명 고기 | `food_mystery_meat` | **150** | 위험 — 바닥에서 **불**을 맞으면 구운 고기, **얼음**이면 얼린 고기 (주문 · 함정 효과 칸) |
| 구운 고기 | `food_cooked_meat` | **150** | 안전 |
| 얼린 고기 | `food_frozen_meat` | **150** | 안전 |

결과: 450턴(약 층 2~3개)을 안 먹으면 굶주림 — 층마다 식량 1(300)이라 서두르지 않아도 모자라지 않음. 음식은 겹친다(배낭 한 칸).

검산 (`HungerTests.ProjectAsset_MatchesBalanceTable`).

---

## 20. 구역 — `Data/Units/ZoneCatalog.asset` (3-17a)

결정 (2026-11-04, 사용자): 5구역 · 25층 (SPD 전체). 이번 묶음은 1~2구역, 3~5구역은 2구역 적을 세게 해서 채움 [임시].

| 구역 | 층 | 이름 | 세기 % | 보스 (마지막 층) | 적 표 (무게) |
|---|---|---|---|---|---|
| 1 | 1~5 | 숲 속 동굴 | **100** | 5층 `boss_slime_girl` 슬라임 소녀 (3-17d) | 쥐 3 · 토끼 2 · 슬라임 2 · 고블린 주술사 1 (3-17b) |
| 2 | 6~10 | 사막 피라미드 | **100** (3-17c — 적 줄이 세기를 가짐) | 10층 `boss_pharaoh` 파라오 술사 (3-17d) | 전갈 3 · 자칼 2 · 미라 2 · 사막 주술사 1 |
| 3 | 11~15 | 얼음 동굴 (3-18) | **100** (적 줄이 세기를 가짐 — 이전 130%) | 15층 `boss_ice_witch` 얼음 마녀 | 서리 박쥐 3 · 눈늑대 2 · 얼음 골렘 2 · 서리 주술사 1 |
| 4 | 16~20 | 용암 동굴 (3-19) | **100** (적 줄이 세기를 가짐 — 이전 160%) | 20층 `boss_lava_lord` 용암 군주 | 화염 도롱뇽 3 · 불꽃 임프 2 · 마그마 골렘 2 · 화염 주술사 1 |
| 5 | 21~25 | 시간 신전 폐허 (3-20) | **100** (적 줄이 세기를 가짐 — 이전 200%) | 25층 `boss_time_guardian` 시간의 수호자 — 처치 = 승리 (이전 임시 `boss`) | 녹슨 시계병 3 · 시간 잔상 2 · 신전 수호상 2 · 시간 사제 1 |
| 6 (3-23a) | 26 | 시간의 제단 | — | 없음 (제단 — `AltarItemId` = `time_hourglass`) | 없음 — 큰 방 하나(보스 방과 같은 21 × 15) · 적 · 재생성 · 꾸미기 없음, 보스 자리에 **시간의 모래시계** — 주우면 **승리**. 25층 보스는 이제 중간 보스(봉인 계단). 해 색 (1, 0.9, 0.65) |

- 구역 해 색 (3-17c, `LightColor`): 1구역 (0.85, 1, 0.88) 옅은 초록 · 2구역 (1, 0.84, 0.58) 모래빛 · 3구역 (0.75, 0.88, 1) 옅은 하늘색 (3-18) · 4구역 (1, 0.68, 0.5) 붉은 주황 (3-19) · 5구역 (0.82, 0.72, 1) 보랏빛 (3-20).
- 구역 웅덩이 (3-18, `PuddleTerrain`): 층 꾸미기 웅덩이(16번 — 수 · 크기 그대로)의 지형. None = 물. **3구역 = 빙판**(영구 — 위 유닛 매 턴 둔화, 불 = 해동 → 물 12틱 → 사라짐). **4구역 = 용암**(영구 화염 지대 — 위 유닛 매 턴 화상, 얼음 주문 = 소화 · 냉기 물약으로 꺼짐, 물은 안 깔리고 증발만). 나머지 구역 물.
- 구역 풀 (3-19, `NoGrass`): 4구역 · 5구역(3-20, 황폐한 돌 신전 — 물 웅덩이는 그대로)은 풀 덩어리를 깔지 않는다. 4구역은 용암이 이웃 풀로 번져(7번 불 번짐) 층이 타는 것을 막으려고. 특수 방 정원(높은 풀)은 그대로, 용암 웅덩이 옆 문은 탈 수 있음.

- 세기 = 적 · 보스의 체력 · 피해 · 명중 · 회피 × % (내림, 체력 최소 1). 경험치 · 시야는 그대로. [임시]
- 적 종류 줄 (ID = 모습 ID): `enemy_melee` 적 8 · 2 · 시야 6 · 명중 8 · 회피 2 · 경험 1 · 최고 5 (BALANCE 3번과 같음), `boss` 보스 60 · 5 · 10 · 10 · 8 · 10 · 30.
- 중간 보스 층(5 · 10층)은 보스 뒤에 봉인된 계단 — 보스를 쓰러뜨리면 열린다. 재생성은 보스 층에서 하지 않는다.
- 한 번만 놓는 아이템(마법서 · 무기 · 갑옷 · 강화 주문서 · 힘 물약)은 `_lootPlanFloors` **10**층까지에만 나눔. 음식 · 무작위 물약 · 주문서 · 금화는 모든 층.

### 20-1. 적 종류 (3-17b~, 같은 에셋의 적 줄 — 수치 Claude [임시])

| ID | 이름 | 구역 · 무게 | 체력 | 피해 | 시야 | 명중 | 회피 | 경험 · 최고 | 행동 |
|---|---|---|---|---|---|---|---|---|---|
| `enemy_rat` | 쥐 | 1 · **3** | 8 | 2 | 6 | 8 | 2 | 1 · 5 | 기본 근접 |
| `enemy_bunny` | 토끼 | 1 · **2** | 5 | 1 | 7 | 8 | 4 | 1 · 5 | 빠름 — 이동 2칸 |
| `enemy_slime` | 슬라임 | 1 · **2** | 12 | 2 | 5 | 6 | 1 | 2 · 6 | 맞으면 자기 칸에 물, 불 **50%** |
| `enemy_goblin_shaman` | 고블린 주술사 | 1 · **1** | 6 | 1 | 7 | 6 | 3 | 2 · 7 | 마나 **4**, `enemy_fire_bolt` 불씨(불 · 화살 · 사거리 5 · 피해 2 · 마나 2 · 화상 30%), 거리 **3** 유지(마나 있을 때) |

| `enemy_scorpion` | 전갈 | 2 · **3** | 12 | 3 | 6 | 12 | 4 | 3 · 10 | 명중 시 중독 **40%** |
| `enemy_jackal` | 자칼 | 2 · **2** | 9 | 3 | 8 | 12 | 6 | 3 · 10 | 빠름 — 이동 2칸 |
| `enemy_mummy` | 미라 | 2 · **2** | 22 | 4 | 5 | 10 | 1 | 4 · 12 | 느림 — 이동 2틱, 불 **150%** · 독 **50%** |
| `enemy_desert_shaman` | 사막 주술사 | 2 · **1** | 10 | 2 | 8 | 10 | 5 | 4 · 12 | 마나 **6**, `enemy_sand_bolt` 모래 화살(대지 · 화살 · 사거리 6 · 피해 3 · 마나 3 · 둔화 30%), 거리 **3** 유지 |

| `enemy_frost_bat` (3-18) | 서리 박쥐 | 3 · **3** | 10 | 3 | 8 | 15 | 9 | 5 · 15 | 빠름 — 이동 2칸 |
| `enemy_snow_wolf` | 눈늑대 | 3 · **2** | 16 | 4 | 7 | 15 | 6 | 6 · 15 | 명중 시 둔화 **40%** |
| `enemy_ice_golem` | 얼음 골렘 | 3 · **2** | 32 | 5 | 5 | 12 | 2 | 7 · 16 | 느림 — 이동 2틱, 맞으면 자기 칸에 빙판, 불 **150%** · 얼음 **50%** |
| `enemy_frost_shaman` | 서리 주술사 | 3 · **1** | 14 | 3 | 8 | 13 | 7 | 7 · 16 | 마나 **8**, `enemy_frost_bolt` 서리 화살(얼음 · 화살 · 사거리 6 · 피해 4 · 마나 3 · 둔화 40%), 거리 **3** 유지. 젖은 대상이면 빙결 반응 |

| `enemy_fire_salamander` (3-19) | 화염 도롱뇽 | 4 · **3** | 18 | 5 | 7 | 18 | 7 | 7 · 20 | 명중 시 화상 **40%** |
| `enemy_flame_imp` | 불꽃 임프 | 4 · **2** | 12 | 4 | 8 | 18 | 11 | 7 · 20 | 빠름 — 이동 2칸 |
| `enemy_magma_golem` | 마그마 골렘 | 4 · **2** | 40 | 6 | 5 | 14 | 2 | 9 · 21 | 느림 — 이동 2틱, 맞으면 자기 칸에 화염 지대, 불 **0%** · 물 **150%** · 얼음 **150%** |
| `enemy_fire_shaman` | 화염 주술사 | 4 · **1** | 18 | 4 | 8 | 16 | 8 | 9 · 21 | 마나 **9**, `enemy_fireball` 불덩이(불 · 화살 · 사거리 6 · 피해 5 · 마나 3 · 화상 40%), 거리 **3** 유지. 중독된 대상이면 독화, 젖은 대상이면 증발 |

| `enemy_clockwork_soldier` (3-20) | 녹슨 시계병 | 5 · **3** | 24 | 6 | 7 | 21 | 8 | 9 · 25 | 명중 시 둔화 **50%** (시간 늦춤) |
| `enemy_time_afterimage` | 시간 잔상 | 5 · **2** | 15 | 5 | 8 | 21 | 14 | 9 · 25 | 빠름 — 이동 2칸 |
| `enemy_temple_guardian` | 신전 수호상 | 5 · **2** | 50 | 7 | 5 | 16 | 2 | 11 · 26 | 느림 — 이동 2틱, 명중 시 마비 **25%** (멈춘 시간), 독 **0%** · 번개 **150%** (돌) |
| `enemy_time_priest` | 시간 사제 | 5 · **1** | 22 | 5 | 8 | 19 | 10 | 11 · 26 | 마나 **10**, `enemy_time_bolt` 시간 번개(번개 · 화살 · 사거리 6 · 피해 5 · 마나 3 · 마비 30%), 거리 **3** 유지, 붙으면 순간이동 **40%**. 젖은 대상이면 감전 |

'시간' 적의 효과는 기존 둔화 · 마비 · 빠름 · 순간이동이다 — 진짜 시간 마법(정지 · 되감기)은 Phase 8 (TIME_MAGIC.md 전).

### 20-2. 패턴 보스 (3-17d — 수치 Claude [임시])

| ID | 이름 | 층 | 체력 | 근접 | 명중 · 회피 | 경험 · 최고 | 패턴 |
|---|---|---|---|---|---|---|---|
| `boss_slime_girl` | 슬라임 소녀 | 5 | 60 | 4 | 10 · 8 | 10 · 30 | 맞으면 물. 힘 모으기 **35%** (사거리 안이면 차례마다): `boss_slime_slam` 슬라임 강타 — 물 · 3×3 · 사거리 3 · 피해 **6** · 젖음 100% · 물 지형 |
| `boss_pharaoh` | 파라오 술사 | 10 | 80 | 4 | 14 · 10 | 20 · 30 | 마나 **9** · 모래 화살(`enemy_sand_bolt`) · 거리 2 유지. 힘 모으기 **30%**: `boss_sand_storm` 모래 폭풍 — 대지 · 3×3 · 사거리 6 · 피해 **7** · 둔화 50%. 붙으면 순간이동 **40%**(4~8칸). 체력 **50%** 이하에서 미라 **2** 소환(한 번) |
| `boss_ice_witch` (3-18) | 얼음 마녀 | 15 | 110 | 5 | 16 · 12 | 30 · 30 | 마나 **9** · 서리 화살(`enemy_frost_bolt`) · 거리 2 유지 · 얼음 **50%**. 힘 모으기 **30%**: `boss_blizzard` 눈보라 — 얼음 · 3×3 · 사거리 6 · 피해 **8** · 둔화 70% (젖어 있으면 빙결 반응). 체력 **50%** 이하에서 서리 박쥐 **3** 소환(한 번). 순간이동 없음 |
| `boss_lava_lord` (3-19) | 용암 군주 | 20 | 140 | 6 | 18 · 12 | 40 · 30 | 근접 · 맞으면 화염 지대 · 불 **0%** · 물/얼음 **150%**. 힘 모으기 **35%**: `boss_lava_eruption` 용암 분출 — 불 · 3×3 · 사거리 6 · 피해 **9** · 화상 70% · 화염 지대. 체력 **50%** 이하에서 마그마 골렘 **2** 소환(한 번). 원거리 주문 · 순간이동 없음 |
| `boss_time_guardian` (3-20) | 시간의 수호자 | 25 (처치 = 승리) | 200 | 7 | 22 · 15 | 50 · 30 | 마나 **12** · 시간 번개(`enemy_time_bolt`) · 거리 2 유지. 힘 모으기 **35%**: `boss_time_collapse` 시간 붕괴 — 번개 · 3×3 · 사거리 6 · 피해 **12** · 마비 50%. 붙으면 순간이동 **40%**. 체력 **50%** 이하에서 시간 잔상 **3** 소환(한 번) |
| `boss` | 보스 (임시 — 3-20부터 쓰지 않음, 적 줄만 남김) | — | 60 × 200% | 5 × 200% | | 10 · 30 | 근접만 |

- 힘 모으기: 차례 1 = 플레이어 칸 중심 주문 범위에 예고 칸(지형 `Warning` **2**틱, 효과 없음) → 다음 차례 = 그 칸에 주문(보스 칸 제외). 행동 순서: 모은 힘 풀기 → 소환 → (보이면) 순간이동 → 힘 모으기 → 원거리 주문 → 거리 벌림 → 근접.

- 속성 배수는 주문(그 주문의 속성) · 화상 · 중독 · 화염 지대 · 독구름 피해에만. 근접 · 반응 · 아이템 피해는 그대로.

검산 (`ZoneTests.ProjectAsset_MatchesBalanceTable` · `ProjectAsset_IceCave_MatchesBalanceTable`(3-18) · `ProjectAsset_LavaCave_MatchesBalanceTable`(3-19) · `ProjectAsset_TimeTemple_MatchesBalanceTable`(3-20), `EnemyTraitTests.ProjectAsset_MatchesBalanceTable`, `DungeonGeneratorTests` 층 수 25).
결과 (3-18, 손 계산): 시작 근접(소검 평균 3)으로 박쥐 약 3대 · 골렘 약 11대 — 골렘은 불 주문(5 × 150% = 7)이 2배 이득, 얼음 주문은 절반. 물보라로 적신 뒤 서리 주술사 · 눈보라에 맞으면 빙결 2턴 — 얼음 동굴에서는 젖은 상태가 위험.
결과 (3-19, 손 계산): 마그마 골렘 · 용암 군주는 불 주문이 0 — 물보라(젖음) → 얼음(빙결 · 150%) · 번개(감전)가 답. 얼음 화살 3 × 150% = 4, 골렘 40은 근접과 섞어 약 8번. 용암 웅덩이 위에 서면 매 턴 화상 1(3턴 갱신) — 지나가기만 해도 약 3.

---

## 21. 직업 — 루멘 빛의 창 (3-21, `Docs/직업_루멘.md`)

결정 (2026-11-05, 사용자 — 문서 1번 C1~C9, 수치는 [제안] 그대로 확정 "그대로 괜찮아"). 데이터 = `SpellCatalog.asset` 줄 + `CharacterCatalog.asset` 루멘 줄(`ClassId` = `lumen`, `StartingSpells`).

**3-21a 공통 기술 (시작부터, 빠른 칸 1 · 2)** — 무속성(`Element.Neutral`: 반응 · 약점 · 저항 없음), 항상 명중 · 행동 1틱

| ID | 이름 | 모양 | 사거리 | 피해 | 마나 | 창 강화 `BoostWeaponId` · `BoostDamagePercent` · `BoostRange` |
|---|---|---|---|---|---|---|
| `light_spear_thrust` | 빛의 창 찌르기 | Beam (직선 관통) | **2** | **4** | **2** | `spear` · **150**% · **+1** → 사거리 3 · 피해 6 |
| `light_spear_throw` | 빛의 창 던지기 | Bolt | **5** | **4** | **3** | `spear` · **150**% · **+1** → 사거리 6 · 피해 6 |

- 창 강화 피해 = 피해 × % (내림). 마나 · 모양은 그대로. 적용 = `SpellModifiers.For` 한 곳(미리보기 · 사거리 표시 · 시전 공용).
- 결과: 마나당 피해 찌르기 2(1명) ~ 4(2명 일렬) · 던지기 1.33 — 불꽃 화살(5/3 = 1.67)보다 약하지만 반응이 없어 젖음 · 빙결 같은 상태를 깨지 않는다. 창을 들면 6/2 = 3 · 6/3 = 2. 창(2틱 공격 · 2칸)과 같이 쓰는 근접형 빌드의 바탕.

검산 (`LightSpearTests.ProjectAssets_MatchBalanceTable`).

**3-21b 형태 — 5층 보스 뒤 한 번 선택** (`Data/Units/ClassCatalog.asset` 루멘 줄 `ChooseAtFloor` **5**). 모두 무속성 · 창 강화 150% · +1

| 형태 | ID | 이름 | 모양 | 사거리 | 피해 | 마나 | 비고 |
|---|---|---|---|---|---|---|---|
| 근접형 `melee` | `light_spear_flurry` | 연속 찌르기 | Bolt · `HitCount` **3** | **1** | **2** × 3 | **3** | 한 대상 3번 = 6 |
| | `light_spear_pierce` | 관통 찌르기 | Beam | **3** | **5** | **3** | |
| | `light_spear_charge` | 돌진 | Dash (새 모양) | **4** | **6** | **4** | 첫 유닛 바로 앞까지 이동 후 맞힘, 없으면 이동만 |
| 원거리형 `ranged` | `light_spear_lance` | 관통 발사 | Beam | **7** | **5** | **4** | |
| | `light_spear_volley` | 다중 발사 | Multi (새 모양) · `MaxTargets` **3** | **6** | **4** | **5** | 사거리 6은 Claude 임시 (문서에 없던 값) |
| | `light_spear_throw_plus` | 빛의 창 던지기+ | Bolt | **7** | **4** | **2** | 던지기를 같은 칸에서 바꿈 (`ReplaceSpells`) |

결과: 근접형 마나당 피해 연속 2 · 관통 1.67 × 맞은 수 · 돌진 1.5(+ 이동). 원거리형 관통 1.25 × 맞은 수 · 다중 0.8 × 맞은 수(3명이면 2.4) · 던지기+ 2. 창을 들면 모두 × 1.5 (연속 3 × 3 = 9, 사거리 1 → 2).

검산 (`ClassFormTests.ProjectAssets_MatchBalanceTable`).

**3-21c 특성** (`ClassCatalog.asset` 루멘 줄 `Tiers` · `Talents`, SPD식): 레벨업마다 1점 — 1단계 = 레벨 **2~6**(5점, 공통 4개 · 각 최대 **2**), 2단계 = 레벨 **7~12**(6점, 고른 형태 4개 · 각 최대 **3**). 2단계 점수는 형태 전에도 모이고 형태를 고른 뒤 쓴다. 레벨 13 이후는 점수 없음 [미정]. 값은 점수별 누적 값.

| 단계 | ID | 이름 | 효과 (`TalentEffect`) | 1 / 2 / 3점 |
|---|---|---|---|---|
| 1 | `light_mastery` | 빛의 숙련 | SpellDamage (빛의 창 8개) | +1 / +2 |
| 1 | `mana_cycle` | 마나 순환 | SpellKillMana (8개) — 적을 쓰러뜨리면 마나 | +1 / +2 |
| 1 | `spear_handling` | 창 다루기 | WeaponAccuracy (`spear`) — 근접 명중 | +2 / +4 |
| 1 | `light_blessing` | 빛의 가호 | MaxHp — 찍을 때 바로 (현재 체력도) | +3 / +6 |
| 2 근접 | `endless_thrust` | 끝없는 찌르기 | SpellHitCount (연속 찌르기) | +1 / +2 / +3 |
| 2 근접 | `long_blade` | 긴 창날 | SpellRange (관통 찌르기) | +1 / +2 / +3 |
| 2 근접 | `dash` | 질주 | SpellRange (돌진) + SpellStatusChance 둔화 | +1 / +2 / +2 · 3점 둔화 100% |
| 2 근접 | `spear_momentum` | 창의 기세 | HitManaChance (근접 공격 · 근접형 빛의 창 4개) — 맞으면 마나 +1 | 20 / 40 / 60% |
| 2 원거리 | `light_rain` | 빛의 비 | SpellMaxTargets (다중 발사) | +1 / +2 / +3 |
| 2 원거리 | `piercing_light` | 꿰뚫는 빛 | SpellDamage (관통 발사) | +1 / +2 / +3 |
| 2 원거리 | `light_hand` | 가벼운 손 | SpellManaCost −1 + SpellRange (던지기+) | 마나 −1 · 사거리 +0 / +1 / +2 |
| 2 원거리 | `shadow_hunt` | 그림자 사냥 | UnseenDamagePercent (8개) — 나를 못 본 적 | +25 / +50 / +75% |

- 순서: 특성 더하기 → 창 강화(× 150% 내림). 예: 빛의 숙련 1점 + 창 = (4 + 1) × 1.5 = **7**. 마나는 0 아래로, 사거리 · 횟수 · 대상 수는 1 아래로 안 감.
- 확률(창의 기세)은 `RngStream.Combat` — 특성이 없으면 굴리지 않아 기존 시드 결과는 그대로.
- 결과: 레벨 12에 1단계 4개 중 2개 반(5점), 2단계 2개(6점) 정도 — 모두 찍을 수는 없다(1단계 8점 · 2단계 12점 필요).

검산 (`TalentTests.ProjectAsset_MatchesBalanceTable`).

---

## 23. 지난 회차 플레이어 보스 (3-23b) — `ZoneCatalog.asset` 그림자 값 + 저장 파일 `ghost.json`

결정 (2026-11-05, 사용자 — 모두 추천안): 26층 시간의 모래시계를 얻은 순간의 플레이어를 기록으로 남기고(기록 **하나** — 덮어씀), 다음 판부터 그 기록을 쓰러뜨릴 때까지 **마지막 보스 층(25층)의 보스 = 그 기록**. 남는 것 = 모습 · 레벨 · 체력 · 마나 · 명중(특성 포함) · 회피 · 힘 · 든 무기 · 입은 갑옷 · 빠른 칸 주문(특성 · 무기 강화를 반영해 굳힌 값, 도약 제외). 쓰러뜨리면 기록이 지워져 다음 판은 시간의 수호자.

| 값 | 필드 | 값 |
|---|---|---|
| 체력 배율 | `_ghostHpPercent` | **300**% (결정 — 체력만 3배, 나머지는 기록 그대로) |
| 처치 경험치 · 최고 레벨 | `_ghostExpReward` · `_ghostExpMaxLevel` | **50** · **30** (시간의 수호자와 같음) |

- 행동: `MeleeChaserAi` — 주문을 빠른 칸 순서로 시도(처음 쓸 수 있는 것), 거리 벌림 없음 · 붙으면 근접(기록의 무기). 주문 ID = `ghost_` + 원래 ID (판 주문 표에 더함, 화면 이름은 원래 이름). 이름 "<캐릭터>의 그림자".
- 결과 (예): 레벨 12 루멘(체력 75 · 창 +3) → 그림자 체력 225 — 시간의 수호자(200)와 비슷, 대신 자기 빛의 창 기술을 그대로 씀. 적은 마나 회복이 없다면 마나가 떨어진 뒤엔 근접만 (확인 필요).

검산 (`GhostTests.ProjectAssets_MatchBalanceTable`).

---

## 22. 직업 — 시아 무도가 (3-22, `Docs/직업_시아.md`)

결정 (2026-11-05, 사용자 — 문서 1번 S1~S7, [제안] 수치 "그대로 진행"). 데이터 = `SpellCatalog.asset` `sia_*` 줄 + `CharacterCatalog.asset` 시아 줄(`ClassId` = `sia`, `StartingSpells`).

**3-22a 기본 기술 (시작부터, 빠른 칸 1)**

| ID | 이름 | 모양 | 사거리 | 피해 | 마나 | 너클 강화 |
|---|---|---|---|---|---|---|
| `sia_leap` | 도약 | Leap (뛰어넘기 — 새 모양) | **3** | 0 | **2** | `knuckle` · 150% · **+1** → 사거리 4 |

- 도약: 사거리 안의 빈 걸을 수 있는 칸 · 보이는 칸 · 조준선이 벽에 막히지 않으면(유닛은 넘음) 바로 이동, 행동 1틱. 지나간 칸은 밟지 않음(도착 칸의 함정만).
- 결과: 마나 10이면 5번 — 3칸 × 5 = 15칸을 5턴에. 걸어서 3칸 = 3턴이므로 한 번에 2턴을 버는 셈, 적 사이를 넘어 포위를 빠져나갈 때 핵심.
- 너클 2타: 평균 1.5 × 2 = **3** (소검 평균 3과 같음), 요구 힘 10이라 시작 힘으로 벌점 없음. 명중 굴림이 2번이라 빗나감이 고르게 나뉜다.

검산 (`SiaTests.ProjectAssets_MatchBalanceTable`).

**3-22b 형태 — 5층 보스 뒤 한 번 선택** (`ClassCatalog.asset` 시아 줄 `ChooseAtFloor` **5**). 기술은 모두 무속성 · 너클 150% · +1

| 형태 | ID | 이름 | 모양 | 사거리 | 피해 | 마나 | 비고 |
|---|---|---|---|---|---|---|---|
| 연타 `combo` | (형태 효과 `Passive`) | 연타 자세 | — | — | 기본 근접 공격 **+1** (`MeleeDamage`, 타격마다 · 방어 전) | — | 너클 2타면 2타 모두 +1 |
| | `sia_combo` | 연타 | Bolt · `HitCount` **4** | **1** | **2** × 4 | **3** | 한 대상 8 |
| | `sia_rush` | 질풍 연격 | Dash · `HitCount` **2** | **3** | **3** × 2 | **4** | 첫 적 앞까지 달려가 6 |
| 강타 `smash` | `sia_crush` | 붕권 | Beam | **3** | **6** | **4** | 직선 관통 |
| | `sia_quake` | 진각 | **Around (새 모양)** · 반지름 **1** | **1** (조준 = 방향만) | **5** | **4** | 내 둘레 8칸, 나는 안 맞음 |
| | `sia_fist` | 정권 | Bolt | **1** | **9** | **3** | 너클이면 13 · 사거리 2 |

결과: 연타는 마나당 피해 2.67(연타 8/3) · 1.5(질풍), 강타는 1.5 × 맞은 수(붕권) · 1.25 × 맞은 수(진각 — 둘러싸이면 최대 8명 40) · 3(정권). 연타 자세 +1은 맨손 3 → 4(+33%), 너클 평균 3 → 5(+67%) — 연타는 너클과 궁합이 좋다.

검산 (`SiaFormTests.ProjectAssets_MatchBalanceTable`).

**3-22c 특성** (`ClassCatalog.asset` 시아 줄 — 루멘과 같은 단계: 1단계 레벨 2~6 · 각 최대 2, 2단계 7~12 · 각 최대 3). ID는 `sia_` 접두어.

| 단계 | ID | 이름 | 효과 (`TalentEffect`) | 1 / 2 / 3점 |
|---|---|---|---|---|
| 1 | `sia_tough_body` | 단련된 몸 | MaxHp | +3 / +6 |
| 1 | `sia_light_feet` | 가벼운 발 | SpellRange (도약) | +1 / +2 |
| 1 | `sia_ki_cycle` | 기 순환 | SpellKillMana (시아 기술 6개) | +1 / +2 |
| 1 | `sia_knuckle_mastery` | 너클 숙련 | WeaponAccuracy (`knuckle`) | +2 / +4 |
| 2 연타 | `sia_quick_hands` | 빠른 손 | MeleeExtraHitChance — 기본 공격이 맞으면 한 번 더 (공격 속도) | 15 / 30 / 45% |
| 2 연타 | `sia_endless_combo` | 끝없는 연타 | SpellHitCount (연타) | +1 / +2 / +3 |
| 2 연타 | `sia_gale` | 질풍 | SpellRange (질풍 연격) | +1 / +2 / +3 |
| 2 연타 | `sia_momentum` | 기세 | HitManaChance (근접 공격 · 연타 · 질풍 연격) | 20 / 40 / 60% |
| 2 강타 | `sia_long_crush` | 긴 붕권 | SpellRange (붕권) | +1 / +2 / +3 |
| 2 강타 | `sia_earth_ring` | 땅울림 | SpellDamage (진각) | +1 / +2 / +3 |
| 2 강타 | `sia_one_strike` | 일격필살 | SpellDamage (정권) | +2 / +4 / +6 |
| 2 강타 | `sia_shock` | 충격 | SpellStatusChance 마비 (붕권 · 진각 · 정권) | 10 / 20 / 30% |

- 결과: 빠른 손 3점 + 너클 = 한 번 공격에 2타 + 45%로 1타 더 → 기대 타격 2.45 (연타 자세 +1이면 한 대 2~3). 일격필살 3점 + 너클 = (9 + 6) × 1.5 = **22** 한 방. 충격 마비는 반응이 아닌 상태 확률(전투 난수).

검산 (`SiaTalentTests.ProjectAsset_MatchesBalanceTable`).

---

## 19. 연금술 — `ItemCatalog.asset`의 씨앗 줄 `BrewsInto` + `FloorDecorConfig.asset` 연구실 (3-16c)

연구실(특수 방, 무게 **2** — 보물 3 · 무기고 2 · 도서관 2 · 정원 2 · 연구실 2) 가운데에 연금 솥(`TileType.AlchemyPot`, 걸을 수 없음 · 시야 안 가림) + 씨앗 **2**개(`_labItems` 5종 중, 겹쳐도 됨). 솥에 부딪히면 씨앗 **3**개를 고르고(`Alchemy.SeedsPerBrew`, 코드 상수 — SPD 규칙), 가장 많이 넣은 씨앗의 물약 하나(같으면 먼저 고른 것). 만든 물약은 식별된다. 차례를 쓰지 않는다.

| 씨앗 | 물약 | 비고 |
|---|---|---|
| `seed_firebloom` 화염꽃 | `potion_liquid_flame` 액체 화염 | SPD 같음 |
| `seed_icecap` 얼음버섯 | `potion_frost` 냉기 | SPD 같음 |
| `seed_sorrowmoss` 독이끼 | `potion_toxic_gas` 독가스 | SPD 같음 |
| `seed_sungrass` 햇볕풀 | `potion_healing` 치유 | SPD 같음 |
| `seed_fadeleaf` 사라짐잎 | `potion_invisibility` 투명 | SPD는 순간이동 주문서 — 물약만 만들도록 바꿈 [임시] |

검산 (`AlchemyTests.ProjectAsset_MatchesBalanceTable`, `SpecialRoomTests.ProjectAsset_MatchesBalanceTable`).

---

## 참고 — 초기 밸런스 기준 (GDD 원본 15장, [임시])

테스트 시작용 범위. 실제 값은 각 항목 착수 때 정해 위 번호 표로 옮긴다.

| 항목 | 초기 권장안 | 옮길 번호 |
|---|---|---|
| 조합 추가 피해 | 기본 주문 피해의 25~50% | 8 |
| 일반 지속 효과 | 2~3틱 | 6 · 7 |
| 연쇄 공격 | 주문당 최대 3~4개 대상 | 5 · 8 |
| 동일 상태 중첩 | 지속 갱신 또는 강도 증가 중 하나 | 6 |
| 강한 제어 효과 | 짧은 지속 + 보스 저항 | 6 |
| 조합으로 인한 추가 행동 | 금지 (예외: 마법 융합기 융합 시전 — 마나 합 · 행동 1회, REACTIONS.md F2) | 8 |
| 무한 발동 방지 | 주문당 발동 횟수 · 대상 수 제한 (마나로 일부 대체) | 4 · 5 · 8 |

---

## 24. 유물 (3-28a) — `ItemCatalog.asset`의 `Relic` 줄(`RelicEffects`) + `LootConfig.asset` 유물 값

결정 (2026-11-05, 사용자): 유물 칸 **2칸** · 같은 종류는 한 칸만 · 효과는 특성 효과 표(`TalentEffect`)와 같은 데이터 — 강화 단계 n이면 `Values[n]`(강화는 3-28b, 지금은 값 하나). 이름은 가제, 값은 초안(Phase 4에서 조정).

| ID | 이름 | 효과 | 값 (강화 0) | 대상 |
|---|---|---|---|---|
| `relic_elemental_crystal` | 원소 증폭 수정 | `SpellDamage` | **+2** | `*elemental` (속성 주문 — 무속성 직업 기술 제외) |
| `relic_life_stone` | 생명석 부적 | `MaxHp` | **+10** | 끼면 최대 · 현재 체력 +10, 빼면 −10 (체력 최소 1) |
| `relic_mana_spring` | 마나 샘 펜던트 | `ManaRegenInterval` (새 효과 14) | **−1틱** | 마나 회복 3틱 → 2틱마다 (최소 1틱) |
| `relic_predator_fang` | 포식자의 이빨 | `HitManaChance` | **25%** | `*` (근접 · 모든 주문이 맞으면 마나 +1) |
| `relic_shadow_cloak` | 그림자 망토 | `UnseenDamagePercent` | **+30%** | `*` (나를 못 본 적에게 주문 피해) |

| 값 | 필드 (`LootConfigAsset`) | 값 | 뜻 |
|---|---|---|---|
| 판마다 바닥 유물 | `_relicsPerRun` | **2** | 유물 종류 중 시드로 고름(안 겹침), 1~마지막 전 층 (`RngStream.Loot`, 계획 끝 — 겉모습 뒤) |
| 보스 보상 | `_bossRelicPercent` | **100%** | 구역 보스(`RunState.BossId`)가 죽은 칸에 유물 하나 (종류 무작위) |

- 유물 값(`Value`) = 0 → 상점에서 팔 수 없음 (에테르 경제는 3-28b 분해로만).
- 결과: 한 판에 바닥 2 + 보스 5(5 · 10 · 15 · 20 · 25층) ≈ 7개 → 3-28b에서 분해 6 에테르 × 2 = 원하는 유물 하나(12).
- 검산 `RelicTests.ProjectAssets_MatchBalanceTable`.

**마법 제작대 · 에테르 (3-28b, 결정 2026-11-05 — 사용자)** — `FloorDecorConfigAsset` 작업실 값 + 유물 줄 `Values`

| 값 | 필드 | 값 | 뜻 |
|---|---|---|---|
| 만들기 | `_relicCraftCost` | **12** 에테르 | 목록에서 유물 하나 (배낭이 가득이면 안 씀) |
| 분해 | `_relicDisassembleGain` | **6** 에테르 | 배낭 유물 하나 — 강화 단계와 무관(강화는 사라짐), 낀 유물은 못 함 |
| 강화 | `_relicUpgradeCosts` | **6 / 9 / 12** | +1 · +2 · +3, 최대 +3 (길이 = 최대). 풀 강화 = 27 |
| 작업실 방 | `_workshopMinSize` · `_workshopMaxSize` | **3 ~ 4** | 가운데 칸 = 마법 제작대(`TileType.ArtifactBench` — 못 지나감 · 시야 안 막음) |
| 작업실 층 | (규칙) | 구역마다 **1층** | 1층 · 보스 층 · 제단 구역 제외, 판 시드로 계산(`Workbench.IsWorkshopFloor` — 난수 흐름 안 씀) |

| 유물 | 강화 0 | +1 | +2 | +3 |
|---|---|---|---|---|
| 원소 증폭 수정 (속성 주문 피해) | +2 | +3 | +4 | +5 |
| 생명석 부적 (최대 체력) | +10 | +15 | +20 | +25 |
| 마나 샘 펜던트 (회복 간격) | −1 (3→2틱) | −1 | −2 (→1틱) | −2 |
| 포식자의 이빨 (맞힘 마나 %) | 25 | 35 | 45 | 55 |
| 그림자 망토 (못 본 적 주문 피해 %) | 30 | 45 | 60 | 75 |

- 결과: 유물 2개 분해 = 원하는 유물 1개 · 1개 분해 = +1 강화. 한 판 유물 ≈ 7개(바닥 2 + 보스 5) → 2~3개를 지키고 나머지로 강화하는 정도.
- 검산 `WorkbenchTests.ProjectAssets_MatchBalanceTable`.

**시간 유물 4종 (3-28c, 결정 2026-11-05 — 사용자)** — `ItemCatalog.asset` 유물 줄 `TimeKind` · `TimeGauge` · `TimePower` + `_relicChargeTicks`

유물 칸에 껴야 쓰고, 끼면 빈 빠른 칸에 자동 등록 → 빠른 칸으로 발동(틱 밖). 게이지는 처음 가득, **10턴마다 1칸**(`_relicChargeTicks`). 발동하면 게이지를 전부 쓴다(타임머신은 고른 턴 수만큼).

| 유물 | 종류 | 게이지 (강화 0 / +1 / +2 / +3) | 뜻 |
|---|---|---|---|
| 마도구 회중시계 | `PocketWatch` | **5 / 6 / 7 / 8** | 내 시간을 멈춤 = 그 턴 동안 행동 못 함 · 무적(피해 · 새 상태 없음) · 내 상태 · 허기 · 회복 멈춤. 세계는 흐름 |
| 타임머신 | `TimeMachine` | **5 / 8 / 11 / 15** | 세계를 n턴 전으로(n ≤ 게이지 · 기록, n칸 씀). 배낭 · 빠른 칸 · 유물 · 게이지 · 에테르 · 금화 · 지금 층 바닥 아이템은 지금 그대로. 기록 = 게임 중 메모리(최대 16틱, 층이 바뀌거나 이어하면 비움) |
| 가속기 | `Accelerator` | **3 / 3 / 4 / 5** 턴 · 행동 **2 / 2 / 3 / 4**번 | 그 턴 동안 한 턴에 n번 행동(앞 n−1번은 즉시). 실시간은 세계 1틱 = n초 |
| 스톱워치 | `Stopwatch` | **3 / 4 / 5 / 6** 번 | 나를 뺀 모든 것의 시간이 멈춤 = 그 수만큼 행동이 틱 없이(피해 · 반응은 바로). 실시간도 시계 멈춤 |

- 강화는 3-28b와 같음(6 / 9 / 12 에테르). 검산 `TimeRelicTests.ProjectAssets_MatchBalanceTable`.

---

## 25. 명예의 전당 (3-33) — `ZoneCatalog.asset` 명예의 전당 값 + 저장 파일 `halloffame.json`

결정 (2026-11-05, 사용자): 타이틀 "게임 시작" 아래 **명예의 전당** — **클리어한 판만** 기록, **높은 점수 순**. 기록 = 날짜 · 캐릭터 · 모드 · 걸린 턴 · 레벨 · 처치 수(기본 묶음) + **시드** + **직업 형태 · 낀 유물**. 점수 공식은 Claude [임시].

점수 = 클리어 기본 + 처치 × 처치 점수 + 레벨 × 레벨 점수 + max(0, 기준 턴 − 걸린 턴) × 턴 점수

| 값 | 필드 | 값 |
|---|---|---|
| 클리어 기본 | `_hofClearBonus` | **10000** |
| 처치 하나당 | `_hofPerKill` | **20** |
| 레벨 하나당 | `_hofPerLevel` | **300** |
| 빠르기 기준 턴 · 남은 턴당 | `_hofTurnBudget` · `_hofPerTurnSaved` | **20000** · **1** |
| 남기는 기록 수 | `_hofMaxEntries` | **20** (화면에는 위 10개) |

- 같은 점수면 적은 턴이 위, 그것도 같으면 먼저 남긴 기록이 위. 턴 = 판 전체 틱(실시간 모드도 1초 = 1틱이라 같은 기준).
- 결과 (예): 12,000턴 · 레벨 15 · 처치 200 → 10000 + 4000 + 4500 + 8000 = **26,500점**. 20,000턴보다 늦으면 빠르기 점수 0.
- 판 밖 데이터 — World 스냅샷에 넣지 않음 (3-23b 그림자 기록과 같음). 결과 화면에 "명예의 전당 N위에 올랐다! (점수)".

검산 (`HallOfFameTests.ProjectAsset_MatchesBalanceTable` · `Score_IsClearBonusPlusKillsLevelsAndSavedTurns`).

---

## 측정 기준선 · 실험

Phase 4 측정 봇 이후 작성. 형식은 `Docs/Source/claude_all_type_genre.md` 17-4.

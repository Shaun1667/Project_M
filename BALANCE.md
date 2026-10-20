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
| 체력 자동 회복량 | `_hpRegenPercent` | **1%** | 최대 체력의 1%를 올림 · 최소 1 → 최대 체력 20이면 10틱마다 **1** (실제 5%). 플레이어만, 상태 이상 중에도 회복, 최대치에서 멈춤 |
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

검산 (`TerrainTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
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

## 측정 기준선 · 실험

Phase 4 측정 봇 이후 작성. 형식은 `Docs/Source/claude_all_type_genre.md` 17-4.

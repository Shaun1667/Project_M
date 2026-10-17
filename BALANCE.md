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

| 배치 | 필드 | 값 | 비고 |
|---|---|---|---|
| 층당 적 수 | `_enemyCount` | **5** | 놓을 칸이 모자라면 있는 만큼 |
| 시작 칸과 최소 거리 | `_enemyMinDistanceFromStart` | **8** | 체비셰프 거리, 바닥 칸만(계단 제외) |

검산 (`UnitCombatTests.ProjectAssets_MatchBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 플레이어가 적을 잡는 데 **3대** (3 × 3 = 9 ≥ 8) — `UnitCombatTests.PlayerKillsEnemyInThreeHits_EnemyRemoved`
- 적 한 마리가 플레이어를 잡는 데 **10대** (2 × 10 = 20) — `UnitCombatTests.PlayerDiesAfterTenHits_ClockStops`
- 1:1로 붙어 싸우면 플레이어가 적 하나에 4 잃음 (적이 2번 반격). 회복이 없으므로 1:1을 4번 하면 체력 4, 5번째 1:1에서 같이 죽는다 (층의 적 5마리 = 회복 없이는 빠듯함)
- 시야 6 < 플레이어 8 → 플레이어가 먼저 본다. 1-7부터 시야 = 원 반지름(dx² + dy² ≤ r² + r) · 벽에 가려짐 (1-6에서는 체비셰프 거리 · 벽 무시)

---

## 4. 마나 · 주문 장착 칸 — `Data/Magic/MagicConfig.asset`

결정 (2026-10-16): Q1 · Q2 임시값. 틱당 회복만 (처치 회복 없음). 조정은 Phase 4 재미 검증에서.

| 항목 | 위치 · 필드 | 값 | 비고 |
|---|---|---|---|
| 플레이어 최대 마나 | `Player.asset` `_maxMana` | **10** | 시작 시 가득 |
| 적 최대 마나 | `MeleeEnemy.asset` `_maxMana` | **0** | 마나 없음 · 회복 없음 |
| 회복 주기 | `_manaRegenIntervalTicks` | **3틱** | (틱 + 1)이 3의 배수인 틱의 ⑤ 단계 |
| 회복량 | `_manaRegenAmount` | **1** | 최대치에서 멈춤 |
| 주문 장착 칸 | `_spellSlotCount` | **4** | 키 1 ~ 4 |
| 시작 주문 | `Player.asset` `_startingSpells` | **fire_basic · ice_basic · lightning_basic · poison_basic** | 임시 ID, 주문 내용은 2-2 (5번) |

검산 (`ManaTests.ProjectAssets_MatchBalanceTable`): 에셋 값이 위 표와 같다.
결과: 바닥(0)에서 가득(10)까지 30틱. 2-2 주문 마나 비용은 이 회복 속도를 기준으로 정한다 (예: 비용 3 = 9틱마다 한 번 꼴).

---

## 5. 주문 — `Data/Spells/SpellCatalog.asset`

결정 (2026-10-16): Q4 임시값. 항상 명중, 고정 피해. 사거리 = 체비셰프. 시전 = 기본 행동 비용(1틱). 조정은 Phase 4 재미 검증에서.

| ID | 속성 | 모양 | 사거리 | 피해 | 마나 | 반지름 | 비고 |
|---|---|---|---|---|---|---|---|
| `fire_basic` | Fire | Bolt (단일, 첫 유닛) | **6** | **5** | **3** | 0 | + 화상 70% (6번) → 걸리면 한 발 처치 |
| `ice_basic` | Ice | Bolt | **6** | **3** | **2** | 0 | + 둔화 70% (6번) |
| `lightning_basic` | Lightning | Beam (직선 관통, 벽에서 멈춤) | **5** | **3** | **4** | 0 | 일렬로 선 적 여럿, + 30% 마비 (6번) |
| `poison_basic` | Poison | Burst (투사체가 멈춘 칸 중심) | **5** | **2** | **3** | **1** (3×3) | 범위 안 시전자도 맞음, + 중독 70% (6번) |

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
| FireZone 화염 지대 | ~~불 — 투사체가 멈춘 칸 1개~~ → 3-1부터 불 + 물 융합으로만 (지금은 생기지 않음) | **4** | Burn 화상 |
| PoisonCloud 독구름 | 독 — 폭발 3×3 | **4** | Poison 중독 |

필드 `SpellCatalog` 항목의 `LeavesTerrain`: `poison_basic` → PoisonCloud. (`fire_basic` → FireZone은 2026-10-17 3-1 D2로 삭제)
지형이 매 턴 거는 상태는 **100%** (2026-10-17 사용자 확인 — 요청표의 50%는 쓰지 않음).

검산 (`TerrainTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 화염 지대에 4틱 내내 서 있으면 화상이 매 틱 갱신 → 지대 4틱 + 사라진 뒤 화상 2틱 = 피해 **6** — `TerrainTests.StandingInFireZone_KeepsBurning`
- 독구름은 놓인 틱에 이미 중독(5틱)이 걸려 있으므로, 머물면 중독이 계속 5로 갱신 → 떠난 뒤에도 최대 5틱
- ~~빈 칸에 불을 쏘면 길목 막기로 쓸 수 있다~~ → 3-1부터 불 단독으로는 화염 지대가 생기지 않음

---

## 8. 반응 규칙 — `Data/Reactions/ReactionCatalog.asset`

결정 (2026-10-17): 사용자 반응 규칙 요청표 → REACTIONS.md. 3-1은 지금 있는 속성 · 상태 · 지형으로 되는 5종만. 나머지(젖음 · 빙결 · 물 계열)는 3-2 이후.

**안전장치**

| 항목 | 필드 | 값 | 근거 |
|---|---|---|---|
| 주문당 반응 최대 | `_maxReactionsPerCast` | **4** | 요청표 4-5 |
| 추가 피해 상한 | `_extraDamageCapMin` | max(주문 피해, **3**) | 요청표 4-5 ✎ |
| 폭발 피해 상한 | `_explosionDamageCapMin` | max(주문 피해, **5**) | 사용자 "폭발 상한을 올려줘"(D6) — 5는 Claude가 정함 (유독성 폭발 최대 = 중독 5틱) |

**반응 5종**

| ID | 이름 | 대상 × 속성 | 우선 | 결과 | 소모 |
|---|---|---|---|---|---|
| `toxic_ignite` | 독화 | 중독(상태) × 불 | 10 | 추가 피해 **+2** | 아니오 |
| `toxic_ignite_cloud` | 독화 | 독구름(지형) × 불 | 10 | 구름 위 대상 **+2** (위와 같은 그룹 — 대상당 1회) | 아니오 |
| `toxic_burst` | 유독성 폭발 | 중독(상태) × 번개 | 20 | 반지름 **1** 폭발, 피해 = 남은 중독 틱 × 1 (≤ 5) | 아니오 |
| `gas_explosion` | 가스 폭발 | 독구름(지형) × 번개 | 20 | 구름 제거 + 반지름 **1** 폭발 **2** + 중독 **70%** | 예 |
| `extinguish` | 소화 | 화염 지대(지형) × 얼음 | 0 | 지대 위 대상 **+2** → 지대 제거 | 예 |

검산 (`ReactionTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
결과:
- 중독(5틱)된 적에 번개 = 번개 3 + 유독성 폭발 5 = **8** → 적(체력 8) 한 방, 둘레 3×3에도 5. 중독을 소모하지 않아 매 번개마다 다시 터진다 (마나 4가 유일한 제한) — `ReactionTests.ToxicBurst_ExplodesByRemainingPoison`
- 독 → 번개 콤보: 마나 3 + 4 = 7로 무리 전체에 독 2 + (중독 70%면) 폭발 최대 5. 플레이어가 폭발 범위에 있으면 같이 맞는다 — `ReactionTests.Explosion_AlsoHitsCasterInRange`
- 독구름 위로 번개 광선 = 지나간 구름 칸마다 가스 폭발(주문당 4회 상한), 겹친 대상은 1회만 — `ReactionTests.GasExplosion_ConsumesCloud_OverlapHitsOnce`

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
| 조합으로 인한 추가 행동 | 금지 | 8 |
| 무한 발동 방지 | 주문당 발동 횟수 · 대상 수 제한 (마나로 일부 대체) | 4 · 5 · 8 |

---

## 측정 기준선 · 실험

Phase 4 측정 봇 이후 작성. 형식은 `Docs/Source/claude_all_type_genre.md` 17-4.

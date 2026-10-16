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
| 4 | 마나 | `Data/Config/` 또는 `Data/Magic/` (2-1) | — | 예정 |
| 5 | 주문 | `Data/Spells/` (2-2) | — | 예정 |
| 6 | 상태 이상 | `Data/Statuses/` (2-3) | — | 예정 |
| 7 | 지형 | `Data/Terrains/` (2-4) | — | 예정 |
| 8 | 반응 규칙 표 | `Data/Reactions/` (3-1) | — | 예정 |

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

검산 (`DungeonGeneratorTests.ProjectAsset_MatchesBalanceTable`): 에셋 값이 위 표와 같다.
분포 (`DungeonGeneratorTests.RollSize_KeepsIntendedDistribution`): 7~13 칸당 빈도 / 14~19 칸당 빈도 = 1.8~2.2.
결과 (시드 20261016 예): 바닥 991 · 벽 1508 · 계단 1.

---

## 3. 유닛 · 적 배치 — `Data/Units/`

결정 (2026-10-16): Q3 임시값. 고정 피해(난수 없음). 조정은 Phase 4 재미 검증에서.

| 유닛 | 에셋 | VisualId | 체력 `_maxHp` | 근접 피해 `_attackDamage` | 시야 `_sightRange` |
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
- 시야 6 < 플레이어 8 → 플레이어가 먼저 본다 (1-7 전에는 벽 무시)

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

## 4. 측정 기준선 · 실험

Phase 4 측정 봇 이후 작성. 형식은 `Docs/Source/claude_all_type_genre.md` 17-4.

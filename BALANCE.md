# BALANCE.md — 수치 · 검산 · 실험

> 수치의 기준 문서. 코드에는 숫자를 쓰지 않고, 아래 "데이터 위치"의 에셋만 고친다.
> 값을 바꾸면 이전 값을 지우지 말고 `→ 이후 변경 (날짜, 이유)`로 이어 적는다.
> 각 표의 검산은 같은 내용의 단위 테스트가 있다 (테스트 이름 표기).

---

## 0. 데이터 위치

| 번호 | 내용 | 에셋 | 순수 로직 타입 | 상태 |
|---|---|---|---|---|
| 1 | 게임 전역 (틱 · 행동 비용) | `Assets/_Project/Data/Config/GameConfig.asset` (`GameConfigAsset`) | `ProjectM.Core.GameConfig` | 사용 중 |
| 2 | 맵 생성 | `Data/Dungeon/` (1-5에서 생성) | — | 예정 |
| 3 | 유닛 (체력 · 피해) | `Data/Units/` (1-6) | — | 예정 |
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

## 2. 맵 생성 — 참고값 (1-5에서 결정)

이전 `MapGenerator.cs`(Reference/MapGenerator_75121da.cs)와 그 씬에서 쓰던 값. **구속력 없음.**

| 항목 | 이전 값 | 비고 |
|---|---|---|
| 맵 크기 | 50 × 50 | `SampleScene 1` 인스펙터 값 |
| 방 개수 | 10 (특정 조건에서 2배) | 코드 상수 |
| 방 크기 | 7 ~ 20 (특정 조건에서 3 ~ 10) | 코드 상수 |
| 시드 | `System.Random` 직접 생성 | CLAUDE 규칙 7 위반 → 1-1 `IRng`로 교체 |

---

## 3. 초기 밸런스 기준 (GDD 원본 15장, [임시])

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

Phase 4 측정 봇 이후 작성. 형식은 `claude_all_type_genre.md` 17-4.

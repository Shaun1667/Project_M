# CLAUDE.md

## 프로젝트 개요
턴제 탑다운 그리드 로그라이크. 7개 기본 속성(불 · 얼음 · 번개 · 독 · 대지 · 바람 · 물)과 희귀한 시간 마법을 마나로 사용하고,
**상태 × 속성 규칙**으로 마법끼리 연결되는 전투가 핵심이다. 기본 게임 구조는 Shattered Pixel Dungeon을 참고한다.

- 기획은 `GDD.md`, 작업 순서와 작업 일지는 `TASKS.md`, 수치는 `BALANCE.md`.
- **새 세션을 시작하면** 이 문서 → `TASKS.md` 맨 위 "진행 요약" 순서로 읽고, 다음 항목을 확인한 뒤 시작한다 다른 PC에서 이어 갈 때는 `Docs/진행상황.md`도 읽는다. 진행 요약에 "다음 작업: 미정"이면 후보를 사용자에게 먼저 묻는다.
- 수치는 BALANCE.md와 데이터 에셋 기준이며 코드에 임의로 넣지 않는다.

## 참조 문서
| 문서 | 역할 | 상태 |
|---|---|---|
| `CLAUDE.md` | AI 작업 규칙 (이 문서) | 사용 중 |
| `TASKS.md` | 진행 요약 · 작업 순서 · 작업 일지 · 다른 PC 준비 절차 | 사용 중 |
| `GDD.md` | 기획서 (v0.3을 정리, 이후 기준) | 사용 중 |
| `BALANCE.md` | 수치 · 검산 · 실험, 데이터 에셋 위치 표 | 사용 중 |
| `README.md` | 문서 지도 · 빠른 시작 | 사용 중 |
| `Docs/Source/마법시스템_기획서_v0.3.md` | 원본 기획서 (보존용, 고치지 않음 — 기준은 GDD.md) | 보존 |
| `Docs/Source/claude_all_type_genre.md` | 장르 무관 협업 가이드 (방법론) | 보존 |
| `REACTIONS.md` | 상태/지형 × 속성 반응 규칙 — 구현 기준 (원본: `Docs/Source/반응규칙_요청표_1.md`) | 사용 중 (3-1~) |
| `TIME_MAGIC.md` | 시간 마법 상세 설계 | Phase 8 착수 전 작성 |
| `Docs/ART_ASSETS.md` | 3D 그래픽 에셋 목록 (Varco) · 규격 · 우선순위 | 사용 중 (G-) |
| `Docs/장비_데이터.md` | 근접 무기 6종 데이터 (사용자 제공 · SPD 2티어) · 착수 때 결정할 것 | 정리만 (구현 전) |
| `Docs/소모품_에셋.md` | 물약 12색 · 주문서 12룬 Varco 이미지 목록 (3D 전) | 사용 중 |
| `Docs/진행상황.md` | 다른 PC · 새 세션 인수인계 요약 (인수인계할 때마다 덮어씀) | 사용 중 |

## 협업 방식 (사용자와 합의된 진행 방식)
- 사용자와는 한국어로 대화한다. 1인 개발.
- 설계 결정은 선택지 + **추천안**으로 묻는다. 사용자는 대체로 추천안을 고른다 — 그래도 묻고, 답을 `결정 (날짜)`로 남긴다.
- 한 항목이 끝나면 **작업 브랜치**(`phaseN-…`)에 커밋하고 보고한다. 사용자가 "main에 병합하고 푸시"라고 하면 fast-forward 병합 · 푸시 · 브랜치 삭제 후 다음 항목으로 간다. 지시 없이 main에 직접 커밋하거나 푸시하지 않는다.
- 보고에는 한 일 · 확인한 것 · 확인 못 한 것 · 확인 방법 · 커밋 상태를 적는다.
- **테스트 실행은 사용자가 한다 (결정 2026-10-18):** 사용자가 다시 지시할 때까지 Claude는 테스트를 실행하지 않는다. 테스트 코드 · 검산은 계속 작성하고, 컴파일 오류 확인 · Setup 메뉴 실행은 한다. 보고에 "테스트 작성, 실행 안 함"을 적는다.

## 작업 원칙
- 한 번에 하나의 기능 단위만. TASKS.md의 현재 항목만 구현한다.
- 요청받지 않은 시스템(세이브, 시간 마법, 실시간 모드 등)을 미리 만들지 않는다. **단, CLAUDE.md "핵심 설계 규칙"의 구조 요구사항은 처음부터 지킨다.**
- 구현 전에 문서를 읽고, 구현 후 문서를 고친다. 문서와 코드가 다르면 문서를 실제 기준으로 고치고 알린다.
- 불확실한 설계 결정은 임의로 정하지 않고 선택지 2~4개 + 추천안 + 영향으로 질문한다. 답은 `결정 (YYYY-MM-DD): … — 이유` 형식으로 TASKS에 남긴다.
- 구현 후 확인 방법(어느 씬에서 · 무엇을 눌러 · 무엇이 보이면 정상)을 함께 안내한다.
- 에디터 수작업은 코드로 우회하지 말고 수동 작업 목록으로 안내한다. 반복 생성 작업은 여러 번 실행해도 결과가 같은 에디터 메뉴로 만든다.
- 완료 = 테스트 통과 + 확인 방법 안내 + 사용자 플레이 확인. 구현만 끝난 상태는 `— 플레이 확인 대기`로 표시한다.
- 핵심 루프가 돌면 비주얼 작업 전에 멈추고 플레이한다 (Phase 4 재미 검증).
- 실패 · 생략 · 미확인은 숨기지 않고 그대로 보고한다.
- 겪은 문제는 원인 · 해결 · 검사 방법과 함께 TASKS의 "피드백"에 적고, 반복되면 체크리스트 문서로 옮긴다.

## 기술 스택
| 항목 | 현재 값 | 상태 |
|---|---|---|
| 엔진 | Unity 6.6 (6000.6.0f1) | 확정 |
| 렌더 파이프라인 | **URP 17.6.0 + Universal Renderer (3D)** (`Assets/_Project/Settings/Rendering/URP3D.asset`) — ~~2D Renderer~~ | 변경 2026-10-18 (사용자 결정, G-1), 이후 변경 금지 |
| 그래픽 방식 | **2D 픽셀 그래픽으로 전환 중 (결정 2026-10-20 · 21, 사용자 — Phase P)**: 64px 칸 · 위에서 비스듬히(SPD식) · AI 픽셀 이미지(격자 맞춤) · 정사영 카메라 정수 배율 · 렌더러는 Universal 그대로(조명 없는 스프라이트 재질). Main Scene Setup의 `Pixel2D` 상수가 켜져 있음 — 단계(맵 → 유닛 → 아이템 → 이펙트 → 3D 정리)는 TASKS Phase P. 아래는 이전 값: ~~**실시간 3D 모델 (Varco 제작) + 고각도 카메라** — 1칸 = 1 m, 바닥 · 지형 효과 · 안개 · 조준은 바닥에 눕힌 Tilemap, 벽 · 유닛은 3D. 목록은 `Docs/ART_ASSETS.md` — ~~2D 스프라이트 + 고각도 아트~~ | 변경 2026-10-18 (사용자 결정, G-1), 이후 변경 금지 |
| 입력 | **Input System 패키지 1.20.1**, Active Input Handling = New만. `UnityEngine.Input` 사용 금지, UI는 `InputSystemUIInputModule` | 확정 2026-10-14, 변경 금지 |
| UI | **uGUI + TextMeshPro** | 확정 2026-10-14, 변경 금지 |
| 맵 표현 | Tilemap (com.unity.modules.tilemap) | 표현 레이어 전용. 규칙은 Tilemap을 모른다 |
| 테스트 | Unity Test Framework 1.8.0 (EditMode) | 확정 |
| 버전 관리 | Git (원격 GitHub, 1인 개발) | 확정 |
| 에디터 연동 | Unity MCP (`unity-mcp`, Project_M 연결 확인됨) | 사용 가능 |

## 폴더 구조 (확정 2026-10-14)
```
Assets/
  _Project/
    Scripts/
      Simulation/      # asmdef ProjectM.Simulation — 순수 로직, noEngineReferences: true
        Core/          # Tick, Rng, EntityId, Grid, Events, State
        Magic/         # 마나, 주문, 속성, 마법서
        Status/        # 상태 이상, 지형 효과, 반응 규칙 엔진
        Units/         # 플레이어, 적, 행동, AI
        Dungeon/       # 맵 생성
      Runtime/         # asmdef ProjectM.Runtime — Unity 어댑터 (Simulation · InputSystem · TMP · uGUI 참조)
        Presentation/  # 이벤트 구독 → 타일 · 스프라이트 · 이펙트 · 카메라
        Input/         # 입력 → 커맨드
        UI/
      Editor/          # asmdef ProjectM.Editor (Editor 전용) — Setup 메뉴, 검사 도구
    Data/            # ScriptableObject (주문, 상태, 지형, 반응 규칙, 수치)
    Settings/        # 렌더 파이프라인 등 프로젝트 설정 에셋 (Rendering/URP2D.asset)
    Prefabs/
    Scenes/
    Art/
    Audio/
  Tests/
    EditMode/        # asmdef ProjectM.Tests.EditMode — 순수 로직 단위 테스트
```
- 순수 로직은 asmdef **하나**(`ProjectM.Simulation`)로 묶는다. 하위 폴더별로 나누면 Magic ↔ Status 순환 참조가 생기기 쉽다. 폴더 · 네임스페이스로만 구분한다.
- Simulation은 Runtime · Editor를 참조하지 않는다 (의존 방향: Editor → Runtime → Simulation).
- `SimulationAssemblyTests`가 Simulation의 엔진 참조 0을 검사한다. 실패하면 규칙 1 위반이다.
- UI 폰트는 `Assets/_Project/Art/Fonts/Galmuri11 SDF.asset`(원본 `Galmuri11.ttf`, SIL OFL 1.1, 동적 SDF — Setup 메뉴가 생성). 새 TMP 글상자는 이 폰트를 쓴다 (기본 LiberationSans에는 한글이 없음).
- 그레이박스 타일 · 스프라이트는 `Assets/_Project/Art/Greybox/`. 메인 씬은 `Assets/_Project/Scenes/Main.unity` (빌드 씬 유일 항목).

## 코딩 규칙
- 네임스페이스: `ProjectM.Core`, `ProjectM.Magic`, `ProjectM.Status`, `ProjectM.Units`, `ProjectM.Dungeon`, `ProjectM.Data`(Runtime/Data), `ProjectM.Presentation`, `ProjectM.Input`(Runtime/Input), `ProjectM.UI`, `ProjectM.Editor`
- 그리드 이동 · 거리 · 경로는 `GridMovement`(8방향, 체비셰프, 대각선은 양옆이 모두 벽일 때만 막힘) · `GridPathfinder`(클릭 이동 `FindPath` = 곧은 길 선호, 추적 `ChaseStep` = 직선 거리가 가까워지는 칸) 한 곳만 쓴다. 입력 → 플레이어 커맨드 규칙은 `PlayerActions` 하나 (키보드 · 클릭 공용)
- 단계 시스템 기본 등록은 `World.InstallCoreSystems(sight, magic, statuses, terrains)`(③ 지형 처리 · ④ 상태 처리 · ⑤ 마나 회복 · ⑥ 사망 판정 · 시야 · ⑦ 이벤트, 마비 · 둔화 행동 규칙) — 새 World · `World.Restore` 후 모두 부르고, 돌려받은 `PlayerVisionSystem.Refresh()`로 첫 시야를 계산한다
- 마나는 `ProjectM.Magic.Mana.TrySpend` · `Mana.Restore`로만 바꾼다 (ManaChangedEvent 발행). 체력은 `Combat.ApplyDamage` — 새 피해 지점은 반드시 `DamageCause`(공격 · 주문 · 반응 · 상태 · 지형)를 넘긴다 (3-4b 사망 원인). 처치 수 · 승패 · 보스 · 사망 원인은 `World.Run`(RunState), 승패 판정은 `DeathCheckSystem` → `RunEndedEvent`.
- 경험치 · 레벨 (3-6, SPD 참고): 경험치는 `Progression.GainExp`(레벨업 = 최대 체력 · 마나 · 명중 · 회피 증가)로만, 적 처치 경험치는 `DeathCheckSystem`이 `Progression.AwardKill`로 준다 — 수치는 `ProgressionConfig.asset`(BALANCE 12번), 적의 경험치 · 최고 레벨은 유닛 에셋. 근접 명중은 `Combat.RollHit` 하나(명중 굴림 ≥ 회피 굴림, 행동 불가 = 회피 0, `RngStream.Combat`) — **주문은 판정하지 않는다(항상 명중, 결정 2026-10-19)**. `MaxHp` · `MaxMana`는 레벨업으로만 바뀐다.
- 무기 (3-7a): 무기 데이터는 `ItemCatalog`의 `EquipWeapon` 줄(SPD 크기 그대로 + 공통 나누기 3, BALANCE 13번). 장착은 `EquipWeaponCommand`(차례 1) → `Weapons.Equip` 하나 — 전투 수치를 계산해 `Unit.Weapon`(`EquippedWeapon`, 스냅샷 v13)에 둔다. 근접 피해 · 명중 배수 · 공격 비용 · 거리(창 2칸)는 `MeleeWeapon` 한 곳(`RollDamage` · `AccuracyPercentOf` · `AttackCost` · `CanReach`). 무기 배치는 `Loot.PlanWeapons`(책 계획 뒤). 바닥 모델은 `ItemVisualCatalog`, 손에 든 모습은 플레이어 프리팹의 `Weapon_<ID>`를 `EntityPresenter.ShowHeld`가 켬(지팡이 숨김).
- 힘 · 강화 (3-7b): 강화 단계는 무기 자체에(`InventoryRef.Upgrade` · `EquippedWeapon.Upgrade`), 요구 힘은 `Weapons.StrengthRequirement`(12 − ⌈강화/2⌉). 힘 효과(모자람 = 명중 ÷ 1.5ⁿ, 남음 = 피해 + 0~n)는 `MeleeWeapon` 한 곳. 힘 물약 · 강화 주문서는 `UseConsumableCommand`(차례 1) → `Consumables.Use` 하나, 배치는 `Loot.PlanConsumables`(무기 계획 뒤).
- 갑옷 (3-10a): 데이터는 `ItemCatalog`의 `EquipArmor` 줄(티어 · 요구 힘, SPD 공식 + 나누기 3, BALANCE 15번). 입기는 무기와 같은 `EquipWeaponCommand` → `Armors.Equip`, 판 시작은 `Armors.Wear`(`UnitDef.StartingArmor`). 방어 굴림 · 힘 벌점 · 회피 배수는 `Armors`(RollDefense · EvasionFactor) 한 곳, `Unit.Armor`(`EquippedArmor`, 스냅샷 v17). 강화 주문서 대상은 `UpgradeTarget`.
- 기습 · 방어 (3-7c): 기습 판정은 `MeleeWeapon.IsSurprise`(적이 플레이어를 못 봄 = `LastSeenTargetPos` 없음 → 항상 명중, 더크는 `SurpriseMin`), 방어는 `MeleeWeapon.ApplyDefense`(근접만, 0~방어 굴림) — 둘 다 `AttackCommand`에서만. 갑옷이 생기면 `DefenseOf`에 더한다.
- 물약 · 주문서 (3-9, SPD): 종류는 `ItemUse.Potion/Scroll` + `ConsumableEffect`(데이터), 효과는 `Consumables.Use/Throw` → `Apply` 한 곳(효과 하나 = case 하나, 수치는 `ConsumableConfig.asset` = BALANCE 14번). 식별은 `Identification`(판마다 겉모습 = `RunState.Looks`, 스냅샷 v16) — 화면에 아이템을 그릴 때는 아이템 ID가 아니라 `Identification.VisualOf`(바닥 `FloorItemPlacedEvent.VisualId`), 이름은 `HudText.Item`(모르면 겉모습 이름). 던지기 판정 · 미리보기는 `Throwing.Check` 하나, 무작위 배치는 `Loot.PlanRandomConsumables`(무게) → `Identification.AssignLooks`(계획 모두 뒤).
- 주문 조준 · 시전은 `SpellCasting`(`Preview` · `Check` · `TryCreateCast`) 하나 — 입력 미리보기와 실제 시전이 같은 계산을 쓴다. 주문 수치는 `SpellCatalog.asset`, 상태 · 지형 효과는 SpellDef 필드(태그)로 추가하고 조합은 반응 규칙 표에서 (규칙 9).
- 상태 이상은 `StatusEffects`(Apply · TryApply)로만 걸고, 효과는 `StatusDef` 데이터 필드(틱당 피해 · 행동 비용 추가 · 행동 불가)로만 정한다 — 상태 종류별 분기 코드를 늘리지 않는다. 확률은 `RngStream.Combat`.
- 지형 효과는 `TerrainEffects.Place`로만 놓고 `World.Terrain`(칸마다 여러 효과)에 둔다. 효과는 `TerrainDef` 데이터(지속 · 거는 상태)로만. MonoBehaviour 메서드 이름은 Unity 내장 메시지(`OnTerrainChanged` 등)와 겹치지 않게 한다.
- 반응 결과는 `ReactionDef` 필드 조합(추가 피해 · 폭발 · 소모 · 지형 변환(+지속) · 대상 상태 · 전이)으로만 — 반응 종류별 분기 코드를 늘리지 않는다 (3-3a 빙결도 `TargetStatus` 데이터).
- 반응은 `ReactionEngine.Resolve` 하나에서만 일어나고, 규칙은 `ReactionCatalog.asset`(REACTIONS.md) 데이터로만 추가한다. 주문 시전 순서는 원래 피해 → 반응 → 새 상태 → 지형. 반응 결과는 다시 반응하지 않는다. 인벤토리 · 빠른 칸은 `Items.Inventory`(TryAdd · RemoveAt · AssignQuickSlot)로만 바꾼다 (3-2a — 주문 장착 칸을 대신함).
- 바닥 아이템 · 마법서 (3-5): 바닥 아이템은 `World.FloorItems`(지금 층, 떠난 층에 함께 저장)에 `Loot.PlaceFloorItems`로만 놓고, 줍기는 `ItemPickupSystem`(⑤, 걸어 들어가면 자동), 배우기는 `Spellbooks.TryLearn`(틱 밖). 배치 계획은 `Loot.PlanSpellbooks`가 판 시작에 `RunState.PendingFloorItems`로 — 난수는 `RngStream.Loot`만 (맵 · 적 배치 불변). 새 마법서 = ItemCatalog에 `LearnSpell` + `SpellId` 한 줄 (계획이 자동으로 포함), 수치는 `LootConfig.asset`(BALANCE 11번).
- 마법 융합은 `FusionCasting`(`Check` · `TryCreateCast`) 하나 — 융합 주문은 `FusionBook.Build`가 두 주문에서 만들고(마나 · 피해 합, 큰 모양, 반응 없음), 조합 · 지형 · 지속은 `FusionCatalog.asset`(BALANCE 10번) 데이터로만 추가한다 (3-2b).
- 층 (3-4a): `World.Map` · `Explored` · `Terrain` · 유닛은 **지금 층**의 것이고 층이 바뀌면 통째로 바뀐다 — 시스템 · AI는 참조를 필드에 담아 두지 말고 매번 `world.Map`으로 읽는다. 떠난 층은 `World.StoredFloors`(스냅샷 v9)에 멈춘 채 저장. 층 이동(계단 사용)은 `FloorTransition.TryUse` 하나 — **틱을 쓰지 않는 틱 밖 동작**(입력이 부르고 이벤트를 비움), 계단 칸으로 걸어가는 것은 보통 이동. 적 재생성은 `EnemyRespawnSystem`(⑤). ID 발급기 · 난수 · 틱은 한 판 공용.
- 시야는 `ISightRule`(지금 `ShadowcastSightRule` → `FieldOfView`), 사거리 · 조준선은 `ITargetingRule`(지금 `BresenhamTargetingRule`) 한 곳만 쓴다
- 이름: 클래스 · 메서드 · 프로퍼티 PascalCase, private 필드 `_camelCase`, 지역 변수 camelCase
- 게임 로직과 엔진 컴포넌트를 분리한다. 규칙은 순수 C#으로 쓰고, MonoBehaviour는 얇은 어댑터(이벤트 구독 · 표시 · 입력 전달)만 맡는다.
- 수치(피해 · 사거리 · 지속 틱 · 마나 · 확률)는 데이터 에셋에 둔다. 에셋은 `ProjectM.Data`의 ScriptableObject이고 `ToXxx()`로 순수 객체(생성자에서 범위 검사)를 만들어 Simulation에 넘긴다. 예: `GameConfigAsset` → `GameConfig`. 위치는 BALANCE.md 0번.
- 수치를 새로 넣으면 BALANCE.md 표 + 같은 값을 검사하는 검산 테스트를 함께 추가한다.
- `Update`에서 규칙을 돌리지 않는다. 시뮬레이션은 틱으로만 진행한다. `Find`/`GetComponent`는 캐시한다.
- 결합은 C# event/Action으로 낮추고 "바뀐 것만" 알린다.
- enum은 끝에만 추가한다 (에셋이 정수로 저장함).
- 바뀔 수 있는 판정(시야, 사거리, 전이 경로, 반응 규칙)은 인터페이스 뒤에 둔다.
- 마법서 · 아이템 효과는 한 조회 지점(예: `SpellModifiers`)에서 읽고, 시뮬레이션 · 미리보기 UI · 연출이 같은 값을 쓴다.
- Unity 6.6에는 `UnityEngine.EntityId`가 있다. `UnityEngine`과 `ProjectM.Core`를 함께 쓰는 파일은 `using EntityId = ProjectM.Core.EntityId;` 별칭을 둔다.
- 시뮬레이션 → 화면은 `SimEventBus` 이벤트로만 (`Core/Events/WorldEvents.cs`). 표현 어댑터는 `Bind(bus)`/`Unbind()`로 구독하고 규칙을 갖지 않는다. 씬 연결은 `GameRoot` 하나, 씬 구성은 메뉴 `ProjectM/Setup/Main Scene`(멱등).
- HUD는 `HudModel`(순수 C#, 이벤트 → 값 · 로그) + `HudPresenter`(그리기만). 화면에 보이는 한글 문구는 `HudText` 한곳, 주문 이름은 `SpellCatalog`의 `DisplayName`(표시 전용 데이터). 새 이벤트를 로그에 넣을 때는 HudModel에 넣고 `HudModelTests`로 문구를 검사한다
- 캐릭터 선택 (2026-10-21): 고를 수 있는 캐릭터는 `CharacterCatalog.asset`(Id · 이름 · 모습 ID · 초상) 데이터로만 추가. 판 시작은 `GameRoot.BeginCharacterSelect` → `StartNewRun(모습 ID)` → `UnitDef.WithVisualId` — 지금은 외형만 다르고, 능력이 달라지면 목록 줄에 필드를 더한다.
- 개발자 디버그 기능은 디버그 창(`Editor/Debug/DebugWindow`, 메뉴 ProjectM/Debug/Debug Window)에 섹션으로 추가한다. 표시만 바꾸는 기능이 기본이고, 게임 상태를 바꾸는 기능은 [상태 변경]으로 표시한다. 런타임 쪽 진입점은 `GameRoot.DebugXxx`.
- 싱글턴 + `DontDestroyOnLoad` 남발을 피한다. 순수 로직은 생성자 주입, 어댑터는 씬 컴포지션 루트 하나에서 연결한다.

## 핵심 설계 규칙 (변경 금지)
바꾸고 싶어지면 영향 범위를 표로 정리하고 사용자 결정을 받은 뒤에만 바꾼다. 근거는 GDD 2~3장.
1. **시뮬레이션과 표현 분리.** 전투 · 상태 · 지형 · 마나 규칙은 Transform · 카메라 · 충돌체 · Tilemap을 모른다. 로직은 이벤트(`FireSpawned(3,4)`, `StatusApplied(Wet)`)를 발행하고 표현 레이어가 이를 3D 이펙트 또는 스프라이트로 바꾼다.
2. **2D 그리드 (x, y), 고도 없음.** 시야 · 사거리 · 물 흐름 · 전이 경로는 그리드 계산으로만 한다. 레이캐스트 · 물리를 쓰지 않는다. 고도가 필요해지면 타일의 선택 속성 하나로 분리한다.
3. **틱 파이프라인 하나.** 상태 지속 · 지형 지속 · 마나 회복이 모두 같은 틱 시스템을 따른다. 틱 순서는 고정하고 문서(GDD 3-3)에 적는다. 구현: `TickScheduler.Tick()` = 1틱, 새 시스템은 `AddSystem(TickPhase.X, ITickSystem)`로 정해진 단계에 넣는다. 행동은 `ICommand`(비용 틱) — GDD 3-4.
4. **시계 교체 방식.** 규칙은 "1틱 진행"만 받는다. 턴제는 플레이어 행동 확정 시, 제한해제는 1초마다 1틱. 모드별로 마법 · 상태 · AI를 따로 구현하지 않는다. deltaTime 누적이 아닌 고정 간격 틱 스케줄러 + 커맨드 큐.
5. **모든 지속시간은 틱 단위.** 모든 행동에 행동 비용(틱) 필드를 둔다 (초기값 전부 1).
6. **상태는 직렬화 가능한 순수 데이터.** 유닛 · 타일 · 상태 · 지형 · 마나를 스냅샷 한 번으로 저장 · 복원할 수 있어야 한다 (시간 마법 · 세이브 · 회귀 테스트 공용). 구현: 저장 대상 상태는 모두 `World`에 두고, AI(`ICommandSource`) · 단계 시스템(`ITickSystem`) · 커맨드는 **상태를 갖지 않는다**. 새 상태를 추가하면 `WorldSnapshot` 필드 + `FormatVersion` 증가 + `WorldSnapshotCodec` + `World.Capture/Restore`를 함께 고치고, `WorldSnapshotTests` 왕복 테스트가 계속 통과해야 한다.
7. **시드 난수 주입.** `UnityEngine.Random`, `System.Random` 직접 생성 금지. 난수 인터페이스를 주입하고 용도별로 분리한다 (전투 ≠ 맵 생성 ≠ 연출). 구현: `IRng` / `Pcg32` / `RngSet.Get(RngStream.X)`. 새 용도는 `RngStream` 끝에 추가. **같은 시드 = 같은 층(맵 · 적 배치) 반드시** (사용자 요구 2026-10-16) — 시간 · 해시 순서 · 정렬 안 된 Dictionary 순회를 결과에 쓰지 않는다. `DungeonGeneratorTests.RecordedSeed_MatchesBalanceResult`가 지킨다.
8. **모든 엔티티에 고유 ID.** 참조는 ID로 한다.
9. **시너지는 규칙 표.** "A마법 + B마법"을 하드코딩하지 않는다. 마법은 속성과 상태/지형 태그를 부여하고, 반응은 `상태/지형 × 속성 → 결과` 데이터 표로 정의한다.
10. **조합 안전장치.** 조합 추가 피해 상한, 전이 피해 < 첫 대상 피해, 주문당 발동 횟수 · 대상 수 상한, 동일 상태 중첩은 "지속 갱신" 또는 "강도 증가" 중 하나만, 플레이어도 지형 · 상태 영향을 받는다. 조합으로 인한 추가 행동은 금지 — 예외: 마법 융합기의 융합 시전(두 주문을 마나 합 · 행동 1회로, REACTIONS.md 5번 F2). 대상 수 상한의 예외: 감전 사슬 전이는 인원 제한 없음(사용자 결정 2026-10-18, REACTIONS.md 3번 — 전이 피해 50%는 유지).
11. **속성은 7개.** 변형(관통 · 추적 · 연쇄 등)은 속성이 아니라 마법서 시스템이 맡는다. 비전 계열 없음.
12. **마법 자원은 마나.**

## 아트 · 그래픽 규칙
- **2D 픽셀 전환 (Phase P, 2026-10-21~)**: Grid는 눕히지 않음 — 칸 (x, y) = 월드 (x + 0.5, y + 0.5, 0). 카메라는 `CameraController` 2D 모드(`ConfigurePixel2D` — 정사영, 1 텍셀 = 정수 화면 픽셀, 위치도 화면 픽셀에 맞춤), 화면 → 칸은 z = 0 평면. 맵은 `PixelMapPresenter` + `PixelTileSet`(바닥 = 이어지는 256px 그림을 4×4칸으로 잘라 칸 위치 % 4로 고름(`Tools/tiles_seamless.py`), 계단, **벽 = 얇은 벽 RuleTile `PixelWallTile`**(SPD식 — 벽 속은 비우고 보이는 쪽에만 16px 돌 테두리(모든 벽 같은 두께 · 16×16 블록), 아래가 바닥이면 앞면 = **모든 앞면이 한 칸 안에 윗면 40줄 + 벽돌 24줄(같은 높이)** — 위 칸도 바닥(한 칸 기둥 · 얇은 가로벽)이면 윗줄 블록으로 고리를 닫음, 옆 칸 바닥 · 벽 · 빈 칸별 18종 RuleTile 규칙(`Neighbor.Empty`). 유닛 스프라이트는 머리가 칸 위로 올라오게(결정 2026-10-22, P-3 ⓑ — `EntityPresenter` 2D 모드 `ConfigurePixel2D`: 프리팹 대신 SpriteRenderer, 발 = 칸 아래 끝 + `_footPixels`(14), 정렬 10, z = 칸 y로 아래 줄이 앞, 회전 대신 `PixelFacing` 앞 · 뒤 · 옆(왼쪽 = 뒤집기), 걷기 = 카탈로그 `WalkFront/Back/Side` 4장을 `_walkFps`로, 대기 = 그 방향 2번째 장, 피격 = 붉게. 걷기 그림이 없는 적은 카탈로그 `PixelVariants` 정지 그림을 엔티티 ID로 하나(3D 변형과 같은 고르기 · 좌우 뒤집기 · 코드 통통 튐), 그것도 없으면 `Sprite`를 `Tint`로 — 적 그림 `Art/Pixel/Enemies`, 축소 `Tools/sprite_fit.py`(초록 몸은 마젠타 배경)); 이웃 종류 Floor · Shown 등은 같은 Tilemap 타일로 판단, 그림 `Tools/tiles_wall.py` → `Tiles/Wall`, 규칙은 Main Scene Setup이 매번 다시 만듦, 패키지 `com.unity.2d.tilemap.extras`) — 64 PPU 점 필터, Setup이 Tile 에셋을 만듦. 원본 256px 텍스처는 `Docs/PixelTest/tile_src`). 게임 안 스프라이트 = 4등신 격자 맞춤(축소 `Tools/grid64.py` — 칸 가운데 색, 평균 금지), 캐릭터 원본 일러스트 = 7등신. 아래 3D 규칙은 전환이 끝날 때까지의 기준.
- 실시간 3D (결정 2026-10-18). 데이터는 비주얼 에셋을 **ID로만** 참조하고(`EntityVisualCatalog` VisualId → 프리팹, `TileVisualSet`), 표현 레이어 교체만으로 바꾼다. 규칙(Simulation)은 3D를 모른다.
- 좌표: Grid 오브젝트를 X축 90° 돌려 칸 (x, y) = 월드 (x + 0.5, 0, y + 0.5). 칸 ↔ 월드는 `Grid.GetCellCenterWorld` / 화면 → 칸은 카메라 광선과 바닥 평면(y = 0)의 교점 (물리 · 충돌체를 쓰지 않는다 — 규칙 2). 모델 원점 = 발밑 중앙.
- 그래픽 작업을 재미 검증(Phase 4)보다 먼저 한다 (변경 2026-10-18, 사용자 결정 — ~~재미 검증 전에는 그래픽 작업을 하지 않는다~~). 프리팹이 없는 VisualId는 그레이박스 도형(캡슐 · 큐브)으로 그린다.
- Varco 모델 (G-2a): 원본은 `Art/Varco3D/`(텍스처 `<이름>_base_color/_normal`, 재질 `<이름>_Mat`), 표시용 프리팹은 메뉴 `ProjectM/Setup/Varco Models`(멱등)가 `Prefabs/Varco/`에 만들고 카탈로그 · 타일 세트에 연결한다. 크기 · 방향 보정은 그 스크립트 표 = ART_ASSETS.md 12번. 새 모델은 같은 표에 한 줄 추가. Varco FBX는 묻힌 텍스처 이름이 모두 같으므로 꺼낼 때 이름을 바꾼다.
- 칸 3D 모듈은 `TileVisualSet`(타입별 Module · 기둥 · 서는 높이)에서 고르고 `TilemapPresenter`가 세운다. 높은 모듈만 시야를 따르고 낮은 바닥 판은 안개 Tilemap이 가린다. Grid cellSize z는 0 (아니면 칸 중앙이 바닥 아래).
- 유닛 움직임 (G-2b): 이벤트 핸들러는 목표만 바꾸고 `EntityPresenter.Advance(dt)`가 프레임마다 그린다 — 테스트는 `Advance`/`FinishMotions`로 진행. 새 연출은 이벤트 → `EntityView` 타이머 → `Pose` 순서로 넣고 0.4초 안에 끝낸다. 애니메이션 클립이 생기면 같은 이벤트 지점에서 재생.
- 애니메이션 (G-2c): 클립(Mixamo 등)과 Varco 모델은 뼈 이름이 달라 **Humanoid 리타깃**으로 붙인다 — `VarcoModelSetup`이 모델 · 클립을 Humanoid로 가져오고(루트 회전 · 높이는 굽고, 앞뒤 이동 XZ는 굽지 않고 루트로 빼서 `applyRootMotion = false`로 버림 — 구우면 몸이 앞으로 밀려 나감) 컨트롤러를 만든다. 위치 · 방향은 `EntityPresenter`가 맡고 Animator에는 파라미터만 넘긴다(지금 bool `Moving` = Walk). 애니메이션 모델은 코드 통통 튐 · 숨쉬기를 끈다. 새 클립 = `Assets/_Project/Animation/` + 컨트롤러 상태 한 줄 + 이벤트 지점에서 파라미터.
- 상태 · 지형 표시는 타일 오버레이와 상태 아이콘 데이터로 정의한다 (2.5D/2D 공용).
- 이펙트 (G-3): `FxPresenter`가 시뮬레이션 이벤트만 보고 띄운다 — 무엇을 띄울지는 `FxCatalog`(키 → 프리팹 · 크기 · 색 · 속도 · 길이), 표는 메뉴 `ProjectM/Setup/FX`(`FxSetup.Table` = ART_ASSETS.md 13번, Main Scene · Varco Models 다음에 실행). 새 지형 · 상태 · 주문 · 반응은 키 한 줄 추가(없는 키는 조용히 건너뜀). 가져온 팩 효과는 반복 · 수 m 데모 설정이라 Lifetime · Scale로 맞추고, 시간대별 렌더로 확인한다. 팩 재질은 URP로 변환 + **소프트 파티클 끔**(렌더러에 깊이 텍스처가 없어 연기 · 불꽃이 투명해짐). 시야 밖 효과는 띄우지 않는다.
- 입력은 연출을 기다린다 (2026-10-20 사용자 요청): `PlayerInputController.PresentationBusy`(= `EntityPresenter.IsAnimating` ‖ `FxPresenter.IsPlaying`)가 true인 동안 다음 행동을 받지 않는다. 새 연출을 넣으면 이 두 판정에 들어가는지 정한다.
- 연출은 로직을 기다리지 않는다. 로직은 즉시 계산하고 애니메이션이 따라오며, 실시간 모드에서는 1초 안에 끝나도록 연출 시간을 줄이거나 건너뛸 수 있어야 한다.

## 새 PC 환경 준비
절차는 `TASKS.md` "다른 PC에서 할 일"에 있다. 요약:
- Unity 6.6 (6000.6.0f1). 프로젝트 패키지에 `com.unity.ai.assistant`(Unity MCP 브리지 포함)가 있다.
- Claude Code에 Unity MCP를 사용자 전역으로 등록: `~/.claude.json`의 `mcpServers`에 `"unity-mcp": { "type": "stdio", "command": "C:/Users/<사용자>/.unity/relay/relay_win.exe", "args": ["--mcp"] }`. relay 실행 파일은 Unity AI Assistant가 `~/.unity/relay/`에 설치한다. 등록 후 새 세션을 열고, Unity의 MCP 연결 요청을 승인해야 도구가 보인다.
- 연결 확인: `~/.unity/mcp/connections/`에 `bridge-*.json`(project_path = 이 프로젝트)이 생기고, 콘솔 읽기 도구가 응답하면 정상.

## Git · 환경
- 한 항목 = 한 커밋. 메시지: `MM.DD/Phase-항목/PascalCase요약` (예: `10.14/1-2/TickScheduler`), 수정은 `…Fix`.
- 커밋 전 `git status`로 의도하지 않은 변경(줄바꿈만 바뀐 설정 파일, 플레이 중 저장된 설정)을 확인한다.
- 1인 개발. 여러 PC에서 작업할 때 pull 후 에디터에 열려 있던 씬은 디스크에서 다시 연다.
- 커밋 전 `git diff --cached --stat`으로 스테이징 내용을 확인한다 (미리 스테이징된 변경이 섞이지 않게).
- 에셋 이동 · 삭제는 Unity MCP(`AssetDatabase.MoveAsset/DeleteAsset`)가 확인 창 때문에 막힌다 → `git mv`/`git rm`으로 `.meta`와 함께 처리하고 Refresh.
- 참고용 원본 코드가 필요하면 Assets 밖 `Reference/`에 두고(Unity가 컴파일 안 함), 이식이 끝나면 지운다. 이전 MapGenerator는 1-5에서 이식 후 삭제 — 원본은 git 75121da.
- 에디터 스크립트에서 `EditorSceneManager.OpenScene`은 쓰지 않는 에셋을 메모리에서 내린다 → 에셋 참조는 씬을 연 **뒤에** 불러온다.
- Unity MCP: 파일을 바꾼 직후 첫 호출은 "Unity not detected"가 나거나 30분 무응답으로 끊기기 쉽다(컴파일 전) → 먼저 가벼운 호출(콘솔 읽기)로 깨우고, 창 제목에 Compiling이 없어질 때까지 기다린 뒤 무거운 호출(RunCommand · 테스트). `System.Reflection` 네임스페이스 사용 불가. 플레이 모드 진입은 에디터 포커스 없이 안 될 수 있다.
- Unity MCP로 긴 작업이 시간 초과돼도 에디터에서는 계속 진행된다 → 콘솔 로그로 결과를 확인한다.
- 테스트 실행: RunCommand 안에서 `TestRunnerApi` + `ExecutionSettings.runSynchronously = true`(EditMode)로 돌리고 콜백 결과를 `result.Log`로 받는다. 비동기 실행은 에디터 포커스 없이 진행되지 않는다. RunCommand 코드에 중첩 클래스를 쓰면 도구가 중복 생성해 컴파일 실패 → 최상위 `internal class`로.
- Setup 메뉴 · `AssetDatabase.SaveAssets` 뒤 `Galmuri11 SDF.asset`에 동적 글자 데이터가 써진다 → 커밋 전 `git checkout -- "Assets/_Project/Art/Fonts/Galmuri11 SDF.asset"`로 되돌린다 (3-4a · 3-4b에서 반복).
- 줄바꿈: `.gitattributes`(1-6)가 Unity YAML 에셋을 eol=lf로 고정한다 → ProjectSettings 줄바꿈 변경을 되돌릴 필요 없음.

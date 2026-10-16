# TASKS.md — 작업 순서 + 작업 일지

> 한 번에 한 항목. 위에서부터 진행한다. 기획은 GDD.md, 규칙은 CLAUDE.md.
> 상태: `[ ]` 대기 / `[~]` 진행 중 / `[x] … — 플레이 확인 대기` / `[x] … — 플레이 확인 완료`
> 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`

## 진행 요약 (2026-10-16 기준 — 새 세션은 여기부터 읽는다)

**현재 위치: Phase 1 — 1-1 ~ 1-6 완료 (1-6은 브랜치 `phase1-6-move-attack-enemy`, main 병합 대기). 다음 작업은 1-7 시야 · 사거리.**

| Phase | 항목 | 상태 | 커밋 |
|---|---|---|---|
| 0 세팅 | 0-0 문서 3종 · 0-1 기술 스택(URP 2D · Input System · uGUI) · 0-2 asmdef/테스트 · 0-3 기존 코드 정리 · 0-4 GameConfig/BALANCE | 완료 | 8e66215 ~ 0c2e65f |
| 1 구조 기반 | 1-1 그리드 · EntityId · PCG32 난수 | 완료 | 1ff6778 |
| | 1-2 틱 파이프라인 · 커맨드 · TurnClock | 완료 | 7d16c81 |
| | 1-3 이벤트 버스 · 표현 어댑터 · Main 씬(임시 방) | 완료 | ecf2f14 |
| | 1-4 World 스냅샷 · 왕복 테스트 | 완료 | b1103f4 |
| | 1-5 맵 생성 (이전 MapGenerator 이식 + 연결 보장 + 계단) | 완료 | 50e0c18 |
| | 1-6 8방향 이동 · 근접 공격 · 적 1종 · 체력/사망 · HUD | 완료 (병합 대기) | 브랜치 커밋 |
| | 1-7 시야 · 사거리 | 대기 | — |

**숫자:** EditMode 테스트 79개 전부 통과 · 콘솔 프로젝트 오류 0 · 씬 1개(`Assets/_Project/Scenes/Main.unity`)

**지금 플레이하면:** 매번 새 시드로 50×50 층 + 플레이어(초록) + 적 5마리(빨강). 방향키/WASD/넘패드(+Q/E/Z/C 대각선)로 한 칸씩, 클릭하면 경로 따라 이동, 적에게 부딪히면 공격. 적은 6칸 안에 들어오면 쫓아와 때린다. 왼쪽 위 HUD에 체력 · 전투 로그(영어). 죽으면 R로 새 판.

**1-7 착수 때 물어볼 것 (미정):**
1. 시야 알고리즘 — 대칭 그림자 투사(Symmetric Shadowcasting, 추천: 서로 보이면 양쪽 다 보임, 기둥 뒤 숨기 가능) / 레이 캐스트(브레젠험) / SPD식
2. 탐험 기록(안개) — 본 적 있는 칸만 흐리게 표시할지, 1-7에서 할지 비주얼 단계로 미룰지 (기록은 World 상태 → 스냅샷 대상)
3. 적의 "마지막으로 본 위치" 기억 — 지금 AI는 상태가 없어 시야 밖으로 나가면 바로 멈춤. 기억하려면 유닛 상태에 필드 추가(스냅샷 v3)

**사용자 확인 대기 (플레이 확인):**
- **1-6 플레이** (0-1 입력 전환 확인 포함) — 키 이동 · 키 반복 · 클릭 이동 · 공격 · 피격 · 사망 · R 재시작. MCP로 플레이 모드 진입이 안 돼 실제 입력은 확인 못 함 (로직은 테스트, 화면은 편집 모드 캡처로 확인)
- 1-3 · 1-5 `Main.unity` 플레이 화면
- GDD의 [제안] 항목 — 1-2 핵심 재미 · 1-3 핵심 루프 · 14 MVP 콘텐츠 목록 (3-3 틱 순서는 확정됨)

**답을 기다리는 질문:** 없음 (`.gitattributes`는 1-6에서 추가 — 결정 2026-10-16)

**사용자 제공 예정 자료:** 속성 × 상태/지형 규칙 표(GDD 8-2, 3-1 전 필요) · 조합 가시성 방향(GDD 12, Phase 5 전 필요)

---

## 현재 구현 대조표 (2026-10-14, 0-3 정리 후)
| 이전 파일 | 처리 결과 (0-3) | 새 위치 |
|---|---|---|
| `Assets/Scripts/System/GameManager.cs`, `NaviManager.cs`, `ClickManager.cs` | 삭제 — 1-2 · 1-3 · 1-6에서 새 구조로 다시 작성 | — |
| `MapGenerator.cs` (75121da) | 참고용 보관 후 1-5에서 `ProjectM.Dungeon.DungeonGenerator`로 이식 · 삭제 (원본은 git 75121da) | `Assets/_Project/Scripts/Simulation/Dungeon/` |
| `CameraController.cs` | 이동, 싱글턴 · DontDestroyOnLoad 제거, z값 인스펙터로 | `Assets/_Project/Scripts/Runtime/Presentation/CameraController.cs` |
| `CharacterManager.cs`, `ObjectManager.cs`, `MainSystem.cs`, `Player.cs`, `ObjectPool.cs` | 삭제 (빈 틀 · 주석뿐) | — |
| `Assets/Resource/TilePalette/*`, `player.png` | 이동 (GUID 유지) | `Assets/_Project/Art/Greybox/` |
| `Assets/Resource/player.prefab` | 삭제 (삭제된 `Player.cs` 참조) | — |
| `Assets/Tile/Wall.asset` | 삭제 (스프라이트 없는 빈 규칙 타일, 참조 0) | — |
| `Assets/Scenes/SampleScene.unity`, `SampleScene 1.unity` | 삭제 (삭제된 스크립트 참조) | 새 씬 `Assets/_Project/Scenes/Main.unity` |

이전 코드 · 씬은 git 기록(커밋 f7aeba3)에서 언제든 꺼낼 수 있다.

---

## Phase 0 — 세팅
목표: 컴파일 오류 없는 빈 씬 + 문서 3종 + 테스트 어셈블리 + 확정된 기술 스택.

- [x] **0-0. 문서 3종 작성** (2026-10-14)
  - 구현: `CLAUDE.md`, `GDD.md`, `TASKS.md` (원본 `Docs/Source/마법시스템_기획서_v0.3.md` + `Docs/Source/claude_all_type_genre.md` 기준)
  - 참고: GDD 1-2 핵심 재미 · 1-3 루프 · 3-3 틱 순서 · 14 MVP는 [제안] 상태 → 사용자 확인 필요

- [x] **0-1. 기술 스택 확정 (변경 금지로 묶음)** (2026-10-14) — 플레이 확인 대기
  - 착수 질문:
    1. 렌더 파이프라인 — A: Built-in 유지 / **B: URP 2D Renderer (추천: 2D 라이트 · 픽셀 퍼펙트 지원, Unity 6에서 Built-in은 신규 기능 없음)** / C: URP Universal (2.5D를 3D 메시로 갈 경우)
    2. 2.5D 시작 방식 — **A: 2D 스프라이트 + 고각도 아트 (추천: 픽셀 전환이 아트 교체만으로 끝남)** / B: 3D 메시 + 직교 카메라
    3. 입력 — A: 구 Input Manager 유지 / **B: Input System 패키지 (추천: 키 재설정 · 패드 대응, 구 방식은 신규 기능 없음)**
    4. UI — **A: uGUI + TMP 유지 (추천: 이미 설치, 자료 많음)** / B: UI Toolkit
  - 완료 조건: CLAUDE.md 기술 스택 표의 "확정 대기"가 모두 "확정"
  - 결정 (2026-10-14): 추천안 전부 채택 — URP 2D Renderer / 2D 스프라이트 + 고각도 아트 / Input System / uGUI + TMP
  - 결정 (2026-10-14): Active Input Handling = **Input System Package (New)만** (Both 아님) — 구 Input 코드를 모두 새 방식으로 바꾸므로 구 시스템 불필요
  - 진행 (2026-10-14) — 입력:
    - `com.unity.inputsystem` 1.20.1 설치
    - `ClickManager.cs`: `Input.GetMouseButtonDown(0)` / `Input.mousePosition` → `Mouse.current.leftButton.wasPressedThisFrame` / `Mouse.current.position.ReadValue()` (마우스 없을 때 null 검사)
    - `SampleScene 1.unity` EventSystem: `StandaloneInputModule` → `InputSystemUIInputModule` (씬 재저장, TilemapCollider2D 버전 경고도 같이 해소 예상)
    - `ProjectSettings.asset` `activeInputHandler: 0 → 1`
    - 콘솔 오류 0 (경고: `MapGenerator.cs` 직렬화 경고 2개, 기존 코드)
    - **에디터 재시작 전에는 새 입력 설정이 적용되지 않음** → 재시작 후 `SampleScene 1`에서 플레이 → 타일 클릭 시 콘솔에 셀 좌표 출력되면 정상 — 플레이 확인 대기
  - 진행 (2026-10-14) — URP:
    - `com.unity.render-pipelines.universal` 17.6.0 설치
    - `Assets/_Project/Settings/Rendering/Renderer2D.asset` (2D Renderer) + `URP2D.asset` 생성 → Graphics 기본 파이프라인 지정, Quality 6단계 모두 재정의 없음(기본값 사용)
    - Unity가 자동 생성: `Assets/UniversalRenderPipelineGlobalSettings.asset`, `Assets/DefaultVolumeProfile.asset`, `ProjectSettings/URPProjectSettings.asset`
    - 검증: `SampleScene` 카메라 렌더 480×270 → 분홍(셰이더 깨짐) 픽셀 0, 타일 정상 표시. 두 씬 렌더러 모두 `Sprites-Default`(unlit) — URP 2D에서 그대로 동작
    - 콘솔 오류 0
  - 참고: 스프라이트 재질을 `Sprite-Lit-Default`로 바꾸고 2D 라이트를 쓰는 것은 비주얼 단계(Phase 5)에서 결정. 지금은 unlit 유지
  - 참고: 재시작 후 `Assets/TextMesh Pro/` 필수 리소스 약 60개 파일이 바뀜(셰이더 · 폰트 에셋 실제 내용 변경 약 20개 + 나머지는 줄바꿈만, 새 셰이더 추가). Unity 6.6 · uGUI 2.6의 TMP 리소스 갱신 → 1인 개발이라 0-1 커밋(f7aeba3)에 포함
  - **0-1 완료** — 플레이 확인은 1-6으로 넘김 (`SampleScene 1` · `ClickManager`는 0-3에서 삭제됨)

- [x] **0-2. 폴더 구조 · 어셈블리 · 테스트** (2026-10-14) — 플레이 확인 대기
  - CLAUDE.md 폴더 구조로 생성, 순수 로직 asmdef (`ProjectM.Core` 등, `noEngineReferences`), 어댑터 asmdef, EditMode 테스트 asmdef
  - 샘플 테스트 1개 통과
  - 완료 조건: Test Runner에서 EditMode 테스트 통과, 콘솔 오류 0
  - 결정 (2026-10-14): 순수 로직 asmdef는 하나(`ProjectM.Simulation`), 하위 폴더는 `Scripts/Simulation/{Core, Magic, Status, Units, Dungeon}` — asmdef는 폴더 하위에 적용되므로 형제 폴더 5개면 asmdef 5개가 필요하고 Magic ↔ Status 순환 참조 위험. 어댑터도 `Scripts/Runtime/{Presentation, Input, UI}` 하나(`ProjectM.Runtime`)로
  - 구현:
    - asmdef 4개: `ProjectM.Simulation`(noEngineReferences), `ProjectM.Runtime`(Simulation · Unity.InputSystem · Unity.TextMeshPro · UnityEngine.UI), `ProjectM.Editor`(Editor 전용), `ProjectM.Tests.EditMode`(UNITY_INCLUDE_TESTS, nunit)
    - `ProjectM.Core.SimulationAssembly` (어셈블리 표식, 빈 static class)
    - 폴더: `_Project/{Data, Prefabs, Scenes, Art, Audio}`, `Tests/EditMode`
  - 테스트 1개(`SimulationAssemblyTests.Simulation_HasNoUnityEngineReferences` — Simulation이 UnityEngine/UnityEditor/Unity.* 를 참조하지 않음) — 전체 1개 통과
  - 확인: Runtime · Editor 어셈블리는 스크립트가 없어 아직 생성되지 않음(정상). 참조 이름 5개는 모두 존재 확인. 콘솔 오류 0 (경고는 기존 `MapGenerator.cs` 2개)
  - 확인 방법: `Window > General > Test Runner > EditMode` → `ProjectM.Tests.EditMode` 실행 → 1개 초록

- [x] **0-3. 기존 코드 정리** (2026-10-14) — 플레이 확인 대기
  - 착수 질문:
    1. 기존 코드 — **A: 대조표대로 교체 · 삭제하고 새 구조로 시작 (추천: 남은 로직이 거의 없고 구조 규칙과 충돌)** / B: 최대한 유지하며 리팩터링
    2. MapGenerator — A: git 이력(커밋 5810c52 등)에서 복구해 참고 / **B: 복구해서 순수 로직으로 이식 (추천: 벽 → 방 생성 로직 재사용)** / C: 버리고 새로 작성
    3. 커밋되지 않은 Unity 6.6 업그레이드 변경 — 공동 작업자와 이미 합의된 업그레이드인지, 이번에 같이 커밋해도 되는지
  - 완료 조건: 컴파일 오류 0, 빈 씬 `Assets/_Project/Scenes/Main.unity`
  - 결정 (2026-10-14): 1 = A (교체 · 삭제), 2 = B (복구 후 순수 로직으로 이식) — 추천안 채택. 3 = 미정 (커밋하지 않고 보류)
  - 진행 (2026-10-14):
    - `MapGenerator.cs`를 75121da(2026-10-02, 병합 직전 main2 쪽 최신)에서 복구, `.meta`는 HEAD에서 복원 → `MapTile` 컴파일 오류 2개 해결 (콘솔 확인)
    - 이어서 드러난 오류 13개: `Assets/TextMesh Pro/Examples & Extras` 예제 스크립트가 Unity 6.6에서 제거된 API 사용 → 폴더 삭제(git rm, 249개), `GameManager.cs`의 미사용 `using TMPro.Examples` 제거
    - 삭제 후 컴파일 결과는 확인 직전 Unity 에디터가 종료돼 미확인이었음 → 에디터 재실행 후 콘솔 오류 0 확인
  - 참고: 복구한 `MapGenerator.cs`는 주석이 CP949 인코딩이고 `using UnityEditor.Experimental.GraphView`가 있어 빌드 시 오류가 난다 → 순수 로직 이식(1-5) 때 정리
  - 피드백: 병합 충돌 해결 중 `.cs` 누락 → 컴파일 오류 → 오류가 다른 오류(TMP 예제)를 가림. 재발 방지: 병합 후 `git ls-files`로 `.cs`/`.meta` 짝 확인, 콘솔 오류 0 확인 후 커밋
  - 결정 (2026-10-14): 3 = 1인 개발이므로 Unity 6.6 업그레이드 변경을 0-1 커밋에 함께 포함
  - 구현 (2026-10-14): 위 "현재 구현 대조표"대로 정리. `Main.unity`(정사영 카메라 하나, 배경 어두운 회색)를 빌드 씬 유일 항목으로 지정. `Assembly-CSharp`(폴더 밖 스크립트) 없음
  - 확인: 콘솔 오류 · 경고 0, EditMode 테스트 1개 통과, `ProjectM.Runtime` 어셈블리가 처음으로 컴파일됨(asmdef 참조 정상), 그레이박스 타일 7개 로드 확인
  - 확인 방법: Project 창에 `Assets/_Project`, `Assets/Tests`, `TextMesh Pro`만 남았는지 · `Main.unity` 열어 플레이 시 어두운 회색 화면 + 콘솔 0
  - 피드백: MCP `AssetDatabase.DeleteAsset/MoveAsset`이 사용자 확인 창을 요구해 실패 → git mv/rm(`.meta` 함께)로 처리 후 Refresh. 에셋 이동은 `.meta`를 같이 옮기면 GUID가 유지된다
  - 피드백: 0-0 커밋에 미리 스테이징된 TMP 예제 삭제 249개가 섞임 → 푸시 전이라 soft reset 후 다시 나눠 커밋. 재발 방지: 커밋 전 `git diff --cached --stat` 확인

- [x] **0-4. 데이터 에셋 자리 정하기** (2026-10-14) — 플레이 확인 대기
  - `Assets/_Project/Data/` 하위 구조, `GameConfig` ScriptableObject 1개 (첫 수치용)
  - BALANCE.md 생성 (빈 표 + 데이터 위치 표)
  - 결정 (2026-10-14): 수치 에셋은 Runtime의 ScriptableObject(`ProjectM.Data`), `ToXxx()`로 순수 C# 객체를 만들어 Simulation에 넘김 — Simulation이 엔진을 모르게 하면서(규칙 1) 수치는 에셋에 두기 위해(규칙 3). 범위 검사는 순수 객체 생성자에서
  - 결정 (2026-10-14): 첫 수치는 GDD에서 이미 [확정]인 값만 — 기본 행동 비용 1틱(2-4), 실시간 1틱 = 1초(3-1). 새 수치는 만들지 않음
  - 구현: `ProjectM.Core.GameConfig`(순수, 생성자 검사), `ProjectM.Data.GameConfigAsset`(ScriptableObject, `Create > ProjectM > Game Config`), 에셋 `Assets/_Project/Data/Config/GameConfig.asset`, 테스트 asmdef에 `ProjectM.Runtime` 참조 추가
  - 수치: BALANCE.md 1번. 이전 맵 생성 값(50×50, 방 10개, 크기 7~20)은 BALANCE.md 2번에 참고값으로
  - 테스트 3개 추가(`GameConfigTests` — 범위 검사 2, BALANCE 1번 검산 1) — 전체 4개 통과
  - 확인: 콘솔 오류 · 경고 0
  - 확인 방법: `GameConfig.asset` 선택 → 인스펙터에 값 2개(1, 1)와 툴팁 표시 · Test Runner EditMode 4개 초록
  - 피드백: `AssetDatabase.Refresh` 명령이 30분 응답 없이 끊김(에디터는 정상, 그 뒤 컴파일 진행) → MCP 호출 전에 컴파일 상태(창 제목 "Compiling…")를 먼저 기다린다
  - 참고: 줄바꿈만 바뀐 설정 파일 3개(ProjectAuditor · Quality · ShaderGraph)는 되돌림

---

## Phase 1 — 핵심 조작 그레이박스 + 구조 기반
목표: 단색 타일 맵에서 플레이어가 한 칸씩 움직이고, 적이 반응하고, 한 행동 = 1틱으로 세계가 진행된다. (GDD 2장 구조 요구사항을 여기서 전부 깐다 — 원본 개발 순서 1)

- [x] **1-1. 그리드 · 엔티티 ID · 시드 난수** (2026-10-14) — 확인 대기 (화면 변화 없음, 테스트로 확인) — `GridMap`(순수), `EntityId`, `IRng`(용도별 분리)
  - 결정 (2026-10-14): 난수는 직접 구현한 PCG32 — `System.Random`은 .NET 버전 간 수열 보장이 없고 상태 저장 불가. PCG32는 상태(ulong 2개)를 저장 · 복원 가능 → 되감기 난수 정책(Q12)을 어느 쪽으로 정해도 대응
  - 결정 (2026-10-14): 용도별 수열(`RngStream`: MapGen · Combat · Ai · Loot)은 (런 시드, 용도 번호)만으로 SplitMix64 → PCG32 생성. 용도를 추가해도 기존 결과 불변. 연출 난수는 표현 레이어 몫
  - 결정 (2026-10-14): `TileType` = Empty 0 · Floor 1 · Wall 2 (이전 MapGenerator 번호와 동일). 걸을 수 있는 칸 = Floor. 상태 · 지형 레이어(Q8)는 2-4, 4/8방향 · 거리 방식은 1-6 · 1-7에서
  - 구현: `Core/Grid/{GridPos, TileType, GridMap}`, `Core/Entities/{EntityId, EntityIdAllocator}`, `Core/Random/{IRng, Pcg32(+Pcg32State), RngStream, RngSet}` — 모두 `ProjectM.Core`, 엔진 의존 없음
  - 스냅샷 대비: `GridMap`은 1차원 배열 하나, `EntityIdAllocator.NextValue`, `Pcg32.State`, `RngSet.CaptureStates/RestoreStates` (1-4에서 묶음)
  - 테스트 17개 추가 (`GridTests` 4, `EntityIdTests` 4, `RngTests` 9 — PCG32 공식 데모 기준값 일치, 같은 시드 재현, 상태 복원, 범위 · 치우침 없는 Range, 용도 간 독립, Simulation 소스의 기본 난수 사용 검사) — 전체 21개 통과
  - 확인: 콘솔 오류 · 경고 0
  - 확인 방법: Test Runner EditMode → 21개 초록
  - 참고: 수치 추가 없음 (BALANCE 변경 없음)
- [x] **1-2. 틱 파이프라인 + 커맨드 큐** (2026-10-14) — 확인 대기 (화면 변화 없음, 테스트로 확인) — `TickScheduler`, `ICommand`(행동 비용 틱), 시계 인터페이스(`TurnClock`만 구현). GDD 3-3 틱 순서 확정
  - 결정 (2026-10-14): 행동 비용 = 즉시 실행 + N틱 대기 (GDD 3-4) — 추천안
  - 결정 (2026-10-14): 같은 틱 행동 순서 = 플레이어 → 적 EntityId 오름차순 — 추천안
  - 결정 (2026-10-14): GDD 3-3 틱 순서 7단계 제안대로 확정. ①만 구현, ②~⑦은 `AddSystem(TickPhase, ITickSystem)` 자리
  - 구현 (`Core/Tick/`): `TickPhase`, `TickContext`(틱 · 행위자), `ICommand`(CostTicks · Execute), `ICommandSource`, `PlayerCommandSource`(입력 큐), `ITickSystem`, `WaitCommand`, `TickScheduler`(ReadyTick 기반, Tick() 1회 = 1틱), `ITickClock`, `TurnClock`(입력 하나 → 다시 입력이 필요할 때까지 진행, 안전 상한 10000틱)
  - 규칙: 틱 도중 생성된 행위자는 다음 틱부터, 제거된 행위자는 남은 차례 제외. 비용 < 1 커맨드는 예외. 플레이어는 하나
  - 테스트 14개 추가 (`TickSchedulerTests` — 입력 대기, 행동 순서, 비용 2 · 3틱, 단계 순서, 같은 단계 등록 순, 사망 · 생성 처리, 잘못된 비용 · 등록, 플레이어 제거 시 정지, Update 무시, 같은 입력 = 같은 결과) — 전체 35개 통과
  - 확인: 콘솔 오류 · 경고 0
  - 확인 방법: Test Runner EditMode → 35개 초록
  - 참고: 커맨드 비용 값은 데이터에서 넣는다(기본 `GameConfig.DefaultActionCostTicks`). 실제 커맨드 생성은 1-6에서 연결. 스케줄러 상태(CurrentTick, ReadyTick)의 스냅샷은 1-4
  - 참고: 실시간 모드의 "입력 없을 때" 처리는 Phase 9 (`RealtimeClock`) — 지금은 플레이어 입력 없이 플레이어 차례가 오면 예외
- [x] **1-3. 이벤트 시스템 + 표현 어댑터** (2026-10-14) — 플레이 확인 대기 — 로직 이벤트 → Tilemap · 스프라이트 반영
  - 결정 (2026-10-14): 이벤트는 `SimEventBus`에 쌓였다가 ⑦ 단계(`EventFlushSystem`)에서 발행 순서대로 한 번에 전달. 틱 밖(층 시작 등)에서는 직접 `Flush()`. Flush 도중 발행된 이벤트는 다음 Flush로
  - 결정 (2026-10-14): 비주얼은 ID로만 — `TileType` → `TileVisualSet`(Floor = tile_0 연회색, Wall = tile_1 진회색, Empty 미표시), `VisualId` 문자열 → `EntityVisualCatalog`("player" = player.png)
  - 구현 — Simulation (`Core/Events/`): `ISimEvent`, `SimEventBus`(타입별 구독, 버퍼, 이중 리스트), `EventFlushSystem`, 이벤트 5종 `MapLoadedEvent`(칸 복사본) · `TileChangedEvent` · `EntitySpawnedEvent`(VisualId) · `EntityMovedEvent` · `EntityRemovedEvent`. `GridMap.CopyCells()` 추가
  - 구현 — Runtime: `Presentation/{TileVisualSet, EntityVisualCatalog, TilemapPresenter, EntityPresenter}`(칸 중앙으로 즉시 이동, 연출은 비주얼 단계), `GameRoot`(컴포지션 루트, 참조 누락 시 오류 안내)
  - 구현 — Editor: 메뉴 `ProjectM/Setup/Main Scene`(`MainSceneSetup`) — 비주얼 에셋 `Data/Visuals/*` 생성 + `Main.unity`에 Grid/Terrain(Tilemap) · Entities · GameRoot 배치 · 참조 연결 · 카메라에 CameraController. 두 번 실행 시 씬 · 에셋 파일 바이트 동일(멱등) 확인
  - 임시: `GameRoot.ShowPreviewRoom()` — 12×8 벽 둘레 방 + 중앙 플레이어. **1-5에서 맵 생성으로 교체하고 삭제**
  - 테스트 10개 추가 (`SimEventBusTests` 5 — 버퍼 · 순서 · 구독 해제 · Flush 중 발행 · 틱 끝 발행 · 칸 복사, `PresenterTests` 5 — 타입별 타일 · x/y 방향 · 맵 교체 · 타일 변경 · 생성/이동/제거 위치 · Unbind) — 전체 45개 통과
  - 확인: 편집 모드에서 GameRoot 시작 처리를 실행해 캡처 — 방 12×8(벽 36 · 바닥 60), 플레이어 (6, 4) 칸 중앙, 카메라 중앙, 분홍 픽셀 0. 콘솔 프로젝트 오류 0
  - 확인 방법: `Main.unity` 열고 플레이 → 어두운 배경에 진회색 벽으로 둘러싸인 연회색 방, 가운데 초록 원(플레이어). 아무 조작 없음(이동은 1-6)
  - 피드백: Unity 6.6에 `UnityEngine.EntityId`가 있어 `UnityEngine` + `ProjectM.Core`를 함께 쓰는 파일에서 이름 충돌 → `using EntityId = ProjectM.Core.EntityId;` 별칭. CLAUDE.md 코딩 규칙에 추가
  - 피드백: `GetCellCenterWorld`는 `GridLayout`이 아니라 `Grid`에 있음 → 필드 타입 `Grid`
  - 피드백: Setup 메뉴가 `OpenScene` 전에 불러온 GameConfig 참조가 null로 저장됨 — `OpenScene`이 쓰지 않는 에셋을 메모리에서 내림(dirty 표시된 비주얼 에셋만 살아남음) → 씬을 먼저 열고 에셋을 불러오도록 수정. CLAUDE.md에 추가
  - 피드백: MCP로 플레이 모드 진입(`EnterPlaymode` delayCall)이 에디터 포커스 없이 실행되지 않음 → 편집 모드에서 `SendMessage("Awake"/"Start")` 후 캡처하고 씬을 디스크에서 다시 열어 확인(Unity 내부 assertion 1건 남음, 게임 코드와 무관)
- [x] **1-4. 직렬화 가능한 상태 + 스냅샷 왕복 테스트** (2026-10-14) — 확인 대기 (화면 변화 없음, 테스트로 확인) — "스냅샷 → 복원 → 진행 = 그냥 진행" 테스트
  - 결정 (2026-10-14): 저장 대상 상태는 `World` 하나에 모은다(맵 · ID 발급기 · 난수 · 스케줄러 · 플레이어 입력 큐). AI(`ICommandSource`)와 단계 시스템(`ITickSystem`)은 상태를 갖지 않고 World 데이터만 읽고 쓴다 — 복원 시 다시 연결만 하면 되도록. CLAUDE.md 규칙에 추가
  - 결정 (2026-10-14): 스냅샷 인코딩은 형식 버전이 붙은 자체 바이너리(`WorldSnapshotCodec`, 매직 "PMWS") — 같은 상태 = 같은 바이트라 결정론 검사가 엄격. 세이브 파일 형식은 Phase 6에서 결정
  - 구현: `Core/World`(Capture · Restore(aiFactory)), `Core/Snapshot/{WorldSnapshot(v1), RngStreamState, WorldSnapshotCodec}`, `Core/Tick/ActorState`, `TickScheduler.CaptureActors()` + 행동 틱 지정 등록 오버로드, `GridMap.FromCells()`
  - 저장 규칙: 틱 사이 + 처리 안 된 플레이어 입력이 없을 때만 Capture. 이벤트 버스 · 단계 시스템은 저장하지 않음(복원 후 호출한 쪽이 다시 등록 · 다시 그림)
  - 테스트 7개 추가 (`WorldSnapshotTests` — **저장→불러와 진행 = 저장 없이 진행(바이트 비교)**, 같은 시드 · 입력 = 같은 바이트, 스케줄러 상태 복원, Capture 독립 복사, 입력 대기 중 저장 거부, 코덱 왕복 동일 바이트, 다른 데이터 · 다른 버전 · 남는 데이터 거부) — 전체 52개 통과. 시험용 AI는 Ai 난수로 비용, Combat 난수로 칸을 뒤집어 상태가 실제로 바뀌는지도 확인
  - 확인: 콘솔 오류 · 경고 0
  - 확인 방법: Test Runner EditMode → 52개 초록 (`WorldSnapshotTests.SaveLoadThenPlay_EqualsPlayWithoutSave`)
  - 참고: 되감기 링버퍼(GDD 2-3)는 시간 마법(Phase 8)에서 이 스냅샷으로 만든다. 유닛 · 상태 · 지형 레이어가 생길 때마다 WorldSnapshot에 필드 추가 + FormatVersion 증가 + 이 왕복 테스트가 계속 통과해야 한다
  - 피드백: 파일을 바꾼 직후 첫 MCP 호출이 30분 무응답 후 끊김(0-4와 같음). 에디터는 정상, 다음 호출에서 컴파일 시작 → 파일 변경 후에는 가벼운 호출(콘솔 읽기)로 먼저 컴파일을 깨우고 대기한 뒤 무거운 호출을 한다
- [x] **1-5. 맵 생성 (1층)** (2026-10-16) — 플레이 확인 대기 — 방 + 복도, 시드 고정 시 같은 맵 (0-3 결정에 따라 MapGenerator 이식)
  - 결정 (2026-10-16): 방 겹침 · 크기 분포 불균형은 **의도된 것** — 그대로 이식 (사용자 확인). 고칠 것은 연결 끊김만
  - 결정 (2026-10-16): 연결 보장 = 벽을 뚫고 지나가기 — 복도 A*가 벽을 지날 수 있되 비용 5배라 보통은 돌아감 — 추천안
  - 결정 (2026-10-16): 수치는 이전 값 그대로 (BALANCE 2번) — 추천안
  - 결정 (2026-10-16): 도착점은 계단 타일 `TileType.StairsDown = 3`(걸을 수 있음, 파란 tile_3). 밟았을 때 동작은 3-4 — 추천안
  - 결정 (2026-10-16): 맵 바깥 한 줄은 항상 벽 — 추천안
  - 끊김 원인 (이전 코드): ① 복도 A*가 먼저 깐 벽 덩어리를 못 지나 목표가 갇히면 그 구간 복도 없음 ② 방 안에 벽 덩어리가 끼면 방이 여러 조각으로 갈라지고, 기준점이 없는 조각은 끊긴 채 남음 ③ A*의 G · H · Parent를 탐색마다 초기화하지 않아 이전 값이 남음
  - 구현 (`Simulation/Dungeon/`): `DungeonGenConfig`(생성자 검사), `DungeonFloor`(Map · Start · Exit), `CorridorPathfinder`(4방향 A*, 테두리 제외, 벽 비용 배수, 탐색마다 새 배열, 결정론적 동점 처리), `DungeonGenerator`(① 벽 덩어리 ② 방 ③ 테두리 벽 ④ 기준점 ⑤ 차례로 복도 ⑥ 시작점과 끊긴 조각을 시작점까지 잇기 ⑦ 빈칸 → 벽 ⑧ 계단). 테두리는 복도 **전에** 친다(나중에 치면 가장자리 복도가 잘림)
  - 구현 — 기타: `TileType.StairsDown`, `GridMap.IsWalkableType/IsBorder`, `World.Create(seed, buildMap)`(MapGen 난수 소비가 World에 남음), `DungeonGenConfigAsset` + 에셋 `Data/Dungeon/DungeonGenConfig.asset`, `GameRoot`(임시 방 삭제 → 시드로 층 생성 · 시작점에 플레이어 · 시드 콘솔 출력, `_fixedSeed`로 같은 맵 재현), Setup 메뉴에 계단 타일 · 던전 수치 연결
  - 삭제: `Reference/MapGenerator_75121da.cs` (이식 완료, 원본은 git 75121da)
  - 수치: BALANCE.md 2번
  - 테스트 10개 추가 (`DungeonGeneratorTests` 9 — 같은 시드 = 같은 층, 시드별로 다름, **시드 200개 모두 걸을 수 있는 칸이 시작점과 연결**, 200개 모두 테두리 벽 · 빈칸 없음 · 시작 바닥 · 계단 1개 · 시작 ≠ 도착, 크기 분포 2배 유지, A* 돌아가기 · 막히면 1칸만 뚫기, 수치 검사, World.Create가 MapGen 소비, BALANCE 2번 검산 / `GridTests` 계단 · 테두리 확인 추가) — 전체 62개 통과
  - 확인: 시드 20261016로 편집 모드 생성 · 캡처 — 50×50, 바닥 991 · 벽 1508 · 계단 1, 겹친 방 · 복도 연결 · 테두리 벽 확인. 콘솔 프로젝트 오류 0 (편집 모드 `SendMessage` 확인용 Unity assertion 1건은 게임 코드와 무관)
  - 확인 방법: `Main.unity` 플레이 → 콘솔에 `[GameRoot] 시드 …` → 플레이어(초록) 주변이 보임. 플레이할 때마다 다른 맵. 같은 맵을 보려면 GameRoot 인스펙터 `_fixedSeed`에 그 시드 입력. 맵 전체는 Scene 뷰에서 확인
  - 참고: 계단 · 시작점 외 환경 타일(물 · 풀 등) 배치는 Q15(Phase 6)
- [x] **1-6. 플레이어 이동 · 근접 공격 · 적 1종** (2026-10-16) — 플레이 확인 대기 — 클릭/키 입력 → 커맨드, 체력 · 사망 (Q3 수치는 임시값)
  - 결정 (2026-10-16): **8방향** 이동, 거리는 체비셰프. 대각선은 양옆 두 칸이 모두 걸을 수 있어야 함(벽 모서리 비스듬히 통과 금지). 대각선 공격도 같은 규칙 — 추천안
  - 결정 (2026-10-16): 입력은 **키보드 + 클릭 이동 둘 다** — 추천안
  - 결정 (2026-10-16): Q3 임시값 플레이어 체력 20 · 피해 3, 적 체력 8 · 피해 2, **고정 피해(난수 없음)**. 적 1종 = 보이면 다가와 근접 공격, 안 보이면 대기 — 추천안. 수치는 BALANCE 3번
  - 결정 (2026-10-16): `.gitattributes` 추가 (Unity YAML은 eol=lf) — 추천안
  - 정한 것 (기본값으로 진행, 바꿀 수 있음): 적 칸으로 이동 = 공격(SPD식 부딪히기). 벽에 부딪히면 차례를 쓰지 않음. 층마다 적 5마리, 시작 칸에서 8칸 이상 떨어진 바닥에 맵 생성 난수로 배치. 플레이어 사망 → 조작 멈춤 + HUD "DEAD" + R로 새 판. 적 그레이박스 = 흰 원(`unit_circle.png`, player.png와 같은 모양) 빨간 틴트
  - 구현 — Simulation: `Core/Grid/{GridMovement(8방향 · 체비셰프 · CanStep), GridPathfinder(8방향 A*, 걸음 수 최소 → 대각선 적은 길, 결정론적 동점, 목표 칸은 막힘 무시)}`, `Units/{Faction, UnitDef, Unit, UnitRegistry(칸당 1유닛, ID 순 순회), Combat.ApplyDamage, UnitDamagedEvent · UnitDiedEvent, MoveCommand, AttackCommand, PlayerActions(입력 → 커맨드 규칙), ISightRule + RangeOnlySightRule(1-7 전 임시: 거리만), MeleeChaserAi(상태 없음), DeathCheckSystem(⑥), UnitSpawner, SpawnConfig, FloorPopulator}`
  - 구현 — World: `Units` 추가, `InstallCoreSystems()`(⑥ 사망 판정 + ⑦ 이벤트 발행 — 새 World · 복원 World 공용). 스냅샷 **v2** = `UnitState[] Units` 추가(코덱 · Capture · Restore)
  - 규칙: 체력 0이 된 유닛은 같은 틱의 남은 차례에서 행동 · 공격 대상이 되지 않고, ⑥에서 맵 · 스케줄러에서 빠진다(UnitDied → EntityRemoved)
  - 구현 — Runtime: `Input/PlayerInputController`(방향키 · WASD 4방향, 넘패드 · Q/E/Z/C 대각선, 넘패드5 · 스페이스 · 마침표 대기, 누르고 있으면 0.3초 뒤 0.12초마다 반복. 클릭 = 경로 따라 0.08초마다 한 칸, 적 칸 클릭 = 다가가 한 번 공격, 자기 칸 = 대기, 키 입력 · 길 막힘 · 출발 때 없던 적이 보이면 멈춤), `UI/HudPresenter`(체력 + 로그 6줄, TMP 기본 폰트라 영어), `Data/{UnitDefAsset, SpawnConfigAsset}`, `EntityPresenter` 새 버스 연결 시 남은 모습 정리, `GameRoot`(배치 · 시스템 등록 · 카메라가 플레이어를 따라감 · 사망 처리 · R 재시작)
  - 구현 — Editor: Setup 메뉴에 유닛 에셋(`Data/Units/{Player, MeleeEnemy, SpawnConfig}.asset` — 없을 때만 BALANCE 값으로 생성) · 적 비주얼 · HUD 캔버스 · 입력 컨트롤러 연결 추가. Editor asmdef에 TMP · UI 참조
  - 테스트 17개 추가 (`GridMovementTests` 6 — 체비셰프 · 8방향 · 모서리 규칙 · 대각선/곧은 길 선호 · 벽/유닛 돌아가기 · 목표 칸 · 못 감, `UnitCombatTests` 11 — 이동/공격/무행동, 3대에 처치 · 죽은 틱 무행동, 추적 후 공격, 시야 밖 대기, 10대에 사망 · 시계 멈춤, 적끼리 겹치지 않고 포위, **유닛 · AI · 전투 포함 저장→불러와 진행 = 그냥 진행**, 유닛 스냅샷 왕복, 적 배치 재현 · 거리, 수치 검사, BALANCE 3번 검산) — 전체 79개 통과
  - 확인: 시드 20261016 편집 모드 시작 — 플레이어 (2, 26) 체력 20, 적 5마리 체력 8, 스프라이트 6개(적은 빨강), HUD "HP 20/20", 카메라가 플레이어 칸. 콘솔 프로젝트 오류 0 (편집 모드 `SendMessage` assertion 1건은 1-3과 같은 것)
  - **확인 못 한 것:** 실제 플레이 모드 입력(키 · 클릭 · 반복 속도), HUD 화면 배치, 사망 → R 재시작 화면 흐름
  - 확인 방법: `Main.unity` 플레이 → ① 방향키/WASD/넘패드로 한 칸씩, 키를 누르고 있으면 계속 이동 ② 바닥 클릭 → 그 칸까지 걸어감, 빨간 적이 새로 보이면 멈춤 ③ 빨간 적에게 부딪히기 → 로그 "You hit enemy #N for 3 (5/8)", 세 번째에 사라짐 ④ 적에게 맞으면 HUD 체력 감소 ⑤ 체력 0 → "DEAD - press R to restart", R → 새 맵
  - 참고: 적은 마지막으로 본 위치를 기억하지 않는다(시야 밖이면 즉시 멈춤). 시야는 벽을 무시한다 → 1-7
  - 피드백: Unity MCP의 비동기 테스트 실행(`TestRunnerApi.Execute`)은 에디터 포커스가 없으면 진행되지 않음 → `ExecutionSettings.runSynchronously = true`로 RunCommand 안에서 실행하면 결과를 바로 받는다. RunCommand 코드의 중첩 클래스는 도구가 중복 생성해 컴파일 실패 → 최상위 클래스로. CLAUDE.md에 추가
  - 피드백: 녹색 player.png에 빨강 틴트 → 거의 검정. 틴트할 스프라이트는 흰색이어야 함 → `unit_circle.png`
  - 피드백: Bash heredoc 여러 개를 한 명령에 넣으니 파싱 실패로 전부 안 써짐 → 파일 쓰기는 편집 도구로
- [ ] **1-7. 시야 · 사거리 (그리드 계산)** — 레이캐스트 없이

## Phase 2 — 시스템 연결: 마나와 첫 마법 (원본 개발 순서 2)
- [ ] **2-1. 마나** — 최대치 · 회복 (Q1 착수 질문), 주문 장착 임시 규칙 (Q2)
- [ ] **2-2. 주문 데이터 + 시전** — `SpellDefinition`(속성, 사거리, 범위, 피해, 마나, 부여 태그), 불 · 얼음 · 번개 · 독 기본 주문 (Q4)
- [ ] **2-3. 상태 이상** — 화상 · 빙결/둔화 · 중독, 지속 틱 · 중첩 규칙 (Q5)
- [ ] **2-4. 지형 레이어** — 타일 레이어 구조 · 우선순위 (Q8), 화염 지대 · 독구름
- [ ] **2-5. HUD 최소** — 체력 · 마나 · 상태 아이콘 · 전투 로그(텍스트)

## Phase 3 — 반응 엔진과 물 (원본 개발 순서 3~5)
- [ ] **3-1. 반응 규칙 엔진** — `상태/지형 × 속성 → 결과` 데이터 표, 안전장치(상한 · 전이 감쇠 · 대상 수), 발동 우선순위 (Q9 · Q10, **사용자 속성표 필요**)
- [ ] **3-2. 물 타일 · 젖음** — 생성 · 확산 · 제거 (Q6)
- [ ] **3-3. 대표 시너지 3종** — 물+번개(전도, 자기 피해 Q7), 물+얼음, 불+독
- [ ] **3-4. 한 판 완성** — 계단 · 층 클리어 · 사망 → 결과 화면

## Phase 4 — 재미 검증 (멈추고 플레이)
- [ ] **4-1. 30분 플레이 → 진단** — 원인 번호 매기기 → 방향(A 수치 / B 규모 비례 압력 / C 신규 시스템)
- [ ] **4-2. 측정 봇** — "적당히 잘하는 플레이어" 규칙 봇, 시드 20회: 층 도달, 사망 원인, 조합 발동 수, 마나 잉여
- [ ] **4-3. 기획 수정** — GDD 반영, 목표 곡선 설정

## Phase 5 이후 (재미 검증 후 순서 재검토)
- [ ] **5. 조합 가시성 UI** — 시전 전 범위 · 전이 경로 · 자기 피해 미리보기 (Q11, **사용자 방향 필요**)
- [ ] **6. 콘텐츠 확장** — 마법서 강화 · 변형, 적 설계, 보스, 환경 타일 배치 (Q2 · Q15)
- [ ] **7. 대지 · 바람** — 지형 · 위치 조작
- [ ] **8. 시간 마법** — TIME_MAGIC.md 작성 후 (Q12 · Q13)
- [ ] **9. 제한해제 모드** — `RealtimeClock` 추가, 두 모드 밸런스 측정 (Q14)
- [ ] **10. 비주얼 · 사운드 · 메뉴 · 세이브 · 튜토리얼**

---

## 다른 PC에서 할 일
1인 개발 (2026-10-14 확인). 다른 PC로 옮겨 이어갈 때 순서대로:

1. **원래 PC에서:** 작업 브랜치를 main에 병합 · 푸시했는지 확인 (`git status` 깨끗, `git log origin/main`에 최신 커밋)
2. **새 PC에서 받기:** `git clone https://github.com/Shaun1667/Project_M.git` (이미 있으면 `git pull`). 작성자 이메일을 같은 값으로 맞춘다 (`git config user.email`)
3. **Unity 6.6 (6000.6.0f1)로 열기** — Unity Hub에 해당 버전 설치 필요. 첫 임포트는 오래 걸린다
4. **콘솔 오류 0 확인** — `Assets/_Project/Scenes/Main.unity` 열기. 씬 참조가 비어 있으면 메뉴 `ProjectM/Setup/Main Scene` 실행(몇 번 실행해도 같은 결과)
5. **테스트:** `Window > General > Test Runner > EditMode` → `ProjectM.Tests.EditMode` 실행 → 전부 초록 (개수는 진행 요약 "숫자" 참고)
6. **플레이 확인:** Main 씬 플레이 → 생성된 층 + 첫 방의 초록 원, 콘솔에 시드
7. **Claude Code 연결 (선택):** CLAUDE.md "새 PC 환경 준비"대로 Unity MCP 등록 → 새 세션 → Unity에서 연결 승인. 세션은 프로젝트 폴더에서 연다
8. **새 세션 첫 요청 예:** "CLAUDE.md와 TASKS.md 진행 요약 읽고 1-5 진행해줘"

주의: 이 PC의 Claude 메모리(`~/.claude/projects/…`)는 옮겨지지 않는다. 필요한 내용은 모두 이 저장소의 문서(CLAUDE · TASKS · GDD · BALANCE)에 있다.

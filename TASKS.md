# TASKS.md — 작업 순서 + 작업 일지

> 한 번에 한 항목. 위에서부터 진행한다. 기획은 GDD.md, 규칙은 CLAUDE.md.
> 상태: `[ ]` 대기 / `[~]` 진행 중 / `[x] … — 플레이 확인 대기` / `[x] … — 플레이 확인 완료`
> 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`

## 현재 위치
**Phase 1 — 1-1 완료(확인 대기). 다음은 1-2 틱 파이프라인 + 커맨드 큐** (2026-10-14)

---

## 현재 구현 대조표 (2026-10-14, 0-3 정리 후)
| 이전 파일 | 처리 결과 (0-3) | 새 위치 |
|---|---|---|
| `Assets/Scripts/System/GameManager.cs`, `NaviManager.cs`, `ClickManager.cs` | 삭제 — 1-2 · 1-3 · 1-6에서 새 구조로 다시 작성 | — |
| `MapGenerator.cs` (75121da) | 컴파일 대상에서 빼고 참고용으로 보관 (CP949 → UTF-8) | `Reference/MapGenerator_75121da.cs` (Assets 밖) → 1-5 이식 후 삭제 |
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
  - 구현: `CLAUDE.md`, `GDD.md`, `TASKS.md` (원본 `마법시스템_기획서_v0.3.md` + `claude_all_type_genre.md` 기준)
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
  - 참고: 재시작 후 `Assets/TextMesh Pro/` 필수 리소스 약 60개 파일이 바뀜(셰이더 · 폰트 에셋 실제 내용 변경 약 20개 + 나머지는 줄바꿈만, 새 셰이더 추가). Unity 6.6 · uGUI 2.6의 TMP 리소스 갱신으로 보임 — 커밋 전 확인 필요
  - **0-1 완료** — 플레이 확인 대기 (재시작 후 `SampleScene 1` 타일 클릭 → 콘솔 좌표)

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
    - 삭제 후 컴파일 결과는 **미확인** (확인 직전 Unity 에디터가 종료됨)
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
- [ ] **1-2. 틱 파이프라인 + 커맨드 큐** — `TickScheduler`, `ICommand`(행동 비용 틱), 시계 인터페이스(`TurnClock`만 구현). GDD 3-3 틱 순서 확정
- [ ] **1-3. 이벤트 시스템 + 표현 어댑터** — 로직 이벤트 → Tilemap · 스프라이트 반영
- [ ] **1-4. 직렬화 가능한 상태 + 스냅샷 왕복 테스트** — "스냅샷 → 복원 → 진행 = 그냥 진행" 테스트
- [ ] **1-5. 맵 생성 (1층)** — 방 + 복도, 시드 고정 시 같은 맵 (0-3 결정에 따라 MapGenerator 이식)
- [ ] **1-6. 플레이어 이동 · 근접 공격 · 적 1종** — 클릭/키 입력 → 커맨드, 체력 · 사망 (Q3 수치는 임시값)
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
- 1인 개발 (2026-10-14 확인). 다른 PC에서 이어갈 때: pull → Unity 6.6으로 열기 → 콘솔 0 확인 → Test Runner EditMode 통과 확인

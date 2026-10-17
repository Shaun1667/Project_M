# TASKS.md — 작업 순서 + 작업 일지

> 한 번에 한 항목. 위에서부터 진행한다. 기획은 GDD.md, 규칙은 CLAUDE.md.
> 상태: `[ ]` 대기 / `[~]` 진행 중 / `[x] … — 플레이 확인 대기` / `[x] … — 플레이 확인 완료`
> 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`

## 진행 요약 (2026-10-17 기준 — 새 세션은 여기부터 읽는다)

**현재 위치: Phase 3 — 3-2b 마법 융합기 · 융합 3종 구현 완료 — 플레이 확인 대기 (브랜치 `phase3-2b-fusion`, 3-2a `phase3-2a-inventory` 위에 쌓임 — 둘 다 main 병합 대기). 다음 작업은 3-3 대표 시너지.**

| Phase | 항목 | 상태 | 커밋 |
|---|---|---|---|
| 0 세팅 | 0-0 문서 3종 · 0-1 기술 스택(URP 2D · Input System · uGUI) · 0-2 asmdef/테스트 · 0-3 기존 코드 정리 · 0-4 GameConfig/BALANCE | 완료 | 8e66215 ~ 0c2e65f |
| 1 구조 기반 | 1-1 그리드 · EntityId · PCG32 난수 | 완료 | 1ff6778 |
| | 1-2 틱 파이프라인 · 커맨드 · TurnClock | 완료 | 7d16c81 |
| | 1-3 이벤트 버스 · 표현 어댑터 · Main 씬(임시 방) | 완료 | ecf2f14 |
| | 1-4 World 스냅샷 · 왕복 테스트 | 완료 | b1103f4 |
| | 1-5 맵 생성 (이전 MapGenerator 이식 + 연결 보장 + 계단) | 완료 | 50e0c18 |
| | 1-6 8방향 이동 · 근접 공격 · 적 1종 · 체력/사망 · HUD · 막다른 길 잇기 | 완료 | e83d78d ~ c921f46 |
| | 1-7 시야(대칭 그림자 투사) · 안개 · 적 기억 · 사거리/조준선 판정 · 적 대각선 추적 | 완료 | 9e227cf ~ eb52b30 |
| 도구 | T-1 디버그 창(ProjectM/Debug/Debug Window) + 맵 전체 밝히기 | 완료 | 98f76e8 |
| | T-2 디버그 창 마나 최대로 채우기 | 완료 | main 병합 |
| 2 마나 · 첫 마법 | 2-1 마나(최대 10 · 3틱마다 1) · 주문 장착 칸 4개 · HUD MP · 적 발자국 추적 · 배회 | 완료 | 5ec5426 ~ 3f1e2ee |
| | 2-2 주문 데이터(4주문) · 조준 모드 · 미리보기 · 시전 | 완료 | 1ed80db |
| | 2-3 상태 이상(화상 · 둔화 · 중독 · 번개 마비 30%) · 유닛 틴트 · HUD 상태 | 완료 | fae175b ~ 1350b95 |
| | 2-4 지형 레이어(한 칸 여러 효과) · 화염 지대 · 독구름 · 칸 색 오버레이 | 완료 | df8cd8e |
| | 2-5 HUD 최소(한글 폰트 Galmuri11 · HP/MP 막대 · 상태 아이콘 · 왼쪽 아래 로그 8줄 · 한글 문구) | 완료 (main 직접 커밋) | fce3be4 |
| | 2-6 플레이어 체력 자동 회복(10틱마다 최대 체력 1% · 올림 · 최소 1) | 완료 | main 병합 |
| 3 반응 엔진과 물 | 3-1 반응 규칙 엔진(데이터 표 · 우선순위 · 상한 · 주문당 4회) · 반응 5종 · 불 단독 화염 지대 제거 · 상태 70% · 빈 시작 칸 + 디버그 창 칸 고르기 | 완료 | main 병합 |
| | 3-2 물 주문(물보라) · 젖음 · 물 타일 · 오염된 물 · 증발 · 오염 | 완료 | main 병합 |
| | 3-2a 인벤토리(20칸) · 빠른 칸 6개(주문 장착 칸을 대신함) · 마법 융합기 아이템 · 인벤토리에서 바로 시전 | 완료 (병합 대기) | 브랜치 커밋 |
| | 3-2b 마법 융합기 · 융합 3종(용암 분출 · 오염 물결 · 폭발 마법, 얼음 + 물은 3-3) | 완료 (병합 대기, 플레이 확인 대기) | 브랜치 커밋 |

**숫자:** EditMode 테스트 197개 전부 통과 · 콘솔 프로젝트 오류 0 · 씬 1개(`Assets/_Project/Scenes/Main.unity`) · 스냅샷 형식 **v8** (3-2b는 저장 상태 추가 없음)

**지금 플레이하면:** 매번 새 시드로 50×50 층 + 플레이어(초록) + 적 5마리(빨강). 방향키/WASD/넘패드로 한 칸씩(대각선 = W+D처럼 두 키 함께, 넘패드 7·9·1·3), 클릭하면 경로 따라 이동, 적에게 부딪히면 공격. **안 본 칸은 검정, 본 적 있는 칸은 어둡게, 지금 보이는 칸(반지름 8, 벽에 가려짐)만 밝게.** 보이지 않는 적은 안 그려진다. 적은 벽 너머를 못 보고(반지름 6), 플레이어를 못 봤으면 맵을 배회하고, 보면 쫓아오고(붙어 있으면 플레이어 발자국을 그대로 밟음), 놓치면 마지막으로 본 칸까지 가 본다. 왼쪽 위 HUD에 빨강 HP · 파랑 MP 막대(숫자 포함, MP는 3턴마다 1 회복, HP는 10턴마다 1 자동 회복) · 내 상태 아이콘(색 칸 + 남은 턴) · 주문 칸 4개(한글 이름 + 마나 비용, 모자라면 빨강) · 노란 안내 줄, 왼쪽 아래에 한글 전투 로그 8줄(같은 상태 갱신은 생략). 글꼴은 Galmuri11. **HP · MP 아래 빠른 칸 6개(주문 · 아이템). 처음엔 모두 비어 있음 — 주문 · 아이템은 디버그 창(메뉴 ProjectM/Debug/Debug Window) "[상태 변경] 인벤토리 · 빠른 칸"에서 넣는다. I 키 = 인벤토리(20칸, 방향키 · 마우스로 고르고 Enter/클릭 → 사용 · 등록하기 · 등록 해제), 빈 빠른 칸을 누르면 인벤토리가 열려 그 칸에 등록할 항목을 고른다.** 숫자 키 1~6 · 칸 클릭으로 주문 조준(가까운 적 자동 선택, Tab 다음 적, 방향키 · 마우스로 조준 칸 이동, 경로 노랑 · 맞는 칸 주황 미리보기) → Space/Enter/클릭으로 시전, Esc 취소. 불 = 단일 5 + 화상 70%(3턴 · 1), 얼음 = 단일 3 + 둔화 70%(3턴 · 행동 2턴), 번개 = 직선 관통 3 + 마비 30%(2턴 행동 불가), 독 = 3×3 폭발 2 + 중독 70%(5턴 · 1, 나도 맞음) + 독구름 3×3 4턴(그 위 유닛 매 턴 중독). 불 단독으로는 화염 지대가 생기지 않음(불 + 물 융합으로만, 3-2 이후). **반응:** 중독된 적에 불 = 독화(+2), 번개 = 유독성 폭발(남은 중독 턴만큼 주변 3×3, 나도 맞을 수 있음), 독구름에 불 = 독화, 번개 = 가스 폭발(구름 사라지고 폭발 2 + 중독 70%). 로그에 "유독성 폭발! 적 #2"처럼 나옴. 걸린 적은 색이 바뀜, 내 상태는 HUD 상태 아이콘. **물 (3-2):** 디버그 창에서 water_basic(물보라)을 넣으면 3×3에 피해 없이 젖음 + 물 타일(파랑, 12턴, 위에 있으면 계속 젖음 · 화염 지대 칸엔 안 깔림). 젖은 적이나 물 칸에 불 = 증발(+2, 젖음 · 물 제거), 물 칸에 독 = 오염(탁한 초록, 위에 있으면 매 턴 피해 1 + 젖음). 젖은 적은 파랑 틴트. **융합 (3-2b):** 디버그 창에서 fusion_device와 주문들을 넣고 융합기를 사용(빠른 칸 또는 인벤토리 "사용") → 인벤토리 제목이 "마법 융합 — 주문 두 개 고르기"로 바뀌고 주문 두 개를 고르면(첫째는 파랑 칸, 같은 칸 다시 · Esc = 첫째 취소) 융합 조준 → 시전. 불 + 물 = 용암 분출(3×3 피해 5 + 화염 지대 6턴), 물 + 독 = 오염 물결(3×3 피해 2 + 오염된 물 8턴), 불 + 번개 = 폭발 마법(직선 관통 8). 마나는 두 주문 합, 행동 1회, 반응 없음, 두 주문의 상태는 안 걸림. 조합이 없으면 "융합할 수 없는 조합" 안내. 죽으면 R로 새 판.

**융합 마법 결정 (2026-10-17, REACTIONS.md 5번 F1~F5):** 마도구 "마법 융합기"가 있어야 씀(아이템 `fusion_device`, 3-2a) · **인벤토리에서 융합기를 사용 → 인벤토리에서 주문 두 개 고르기**(변경 2026-10-17) · 마나 합 · 행동 1회(규칙 10 예외로 명시) · 모양은 두 주문 중 큰 쪽(Bolt < Beam < Burst) · 융합 결과는 반응 안 일으킴 · 적은 안 씀. 인벤토리 · 빠른 칸을 먼저(**3-2a**), 융합은 **3-2b**. 3-2b 결정: 얼음 + 물은 3-3 · 지형 융합 피해 = 두 주문 합 + 표의 효과만 · 사거리 = 모양을 정한 주문 것.

**3-3으로 넘긴 것 (3-2 범위 결정 2026-10-17):** 빙결 · 빙판 · 결빙 · 해동 · 파쇄(물 + 얼음), 감전 전이 100% · 피해 50% · 수면 감전(물 + 번개). 오염된 물의 얼음 · 번개 반응도 그때

**사용자 확인 대기 (플레이 확인):**
- **1-6 · 1-7 · T-1 · 2-1 플레이** (0-1 입력 전환 확인 포함) — 키 이동 · 키 반복 · 대각선 동시 입력 · 클릭 이동 · 공격 · 피격 · 사망 · R 재시작 · 안개 · 적이 모퉁이를 돌아 쫓아옴 · 디버그 창 · HUD 마나 줄. MCP로 플레이 모드 진입이 안 돼 실제 입력은 확인 못 함 (로직은 테스트, 화면은 편집 모드 캡처로 확인)
- 1-3 · 1-5 `Main.unity` 플레이 화면
- GDD의 [제안] 항목 — 1-2 핵심 재미 · 1-3 핵심 루프 · 14 MVP 콘텐츠 목록 (3-3 틱 순서는 확정됨)

**답을 기다리는 질문:** 없음 (`.gitattributes`는 1-6에서 추가 — 결정 2026-10-16)

**사용자 제공 자료:** 속성 × 상태/지형 규칙 표 — **받음 2026-10-17** (`Docs/Source/반응규칙_요청표_1.md` → `REACTIONS.md`). 남은 것: 조합 가시성 방향(GDD 12, Phase 5 전 필요)

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
  - 결정 (2026-10-16): **8방향** 이동, 거리는 체비셰프. 대각선은 양옆 두 칸이 모두 걸을 수 있어야 함(벽 모서리 비스듬히 통과 금지). 대각선 공격도 같은 규칙 — 추천안 (→ 아래 "변경"으로 바뀜)
  - 결정 (2026-10-16): 입력은 **키보드 + 클릭 이동 둘 다** — 추천안
  - 변경 (2026-10-16, 사용자 요청): ① 대각선은 양옆 중 **하나라도** 걸을 수 있으면 이동 · 공격 가능 — 양옆이 모두 벽인 틈만 막힘 ② 키보드 대각선은 Q/E/Z/C 대신 **두 방향키를 함께** 누름(W+D, ↑+→ 등). 첫 키 뒤 0.07초(`_comboWindow`) 동안 두 번째 키를 기다렸다 합쳐서 한 걸음, 이미 대각선이면 바로. 누르고 있으면 반복 때마다 지금 눌린 조합으로 방향을 정함. 반대 키(A+D)는 상쇄. 넘패드 7·9·1·3은 그대로
  - 영향: 직선 키 한 번 누를 때 0.07초 늦게 움직임. 경로 · 적 추적도 새 모서리 규칙을 따름 (테스트: `CanStep_DiagonalPassesOneWallCorner`, `CanStep_DiagonalBlockedWhenBothSidesAreWalls`, 벽 돌아가기 6 → 4걸음) — 전체 80개
  - 변경 (2026-10-16, 사용자 요청): **막다른 길 잇기** — 층 생성 ⑦-2 단계. 원인: 방은 벽 덩어리가 아닌 칸만 바닥이 되므로 벽 덩어리와 많이 겹친 방이 한 칸 폭 띠로 남아 끊긴 길처럼 보임(1-5 연결 검사는 "시작점에서 닿는가"만 봐서 통과). 처리: 막다른 칸(한 걸음에 갈 이웃이 1개 이하)마다 4방향으로 벽을 가장 적게 뚫어, 걸어서 10걸음(`_deadEndLoopMinWalkDistance`, BALANCE 2번) 이상 떨어진 길과 잇는다. 자기 길 옆에 붙어 가는 굴은 먼저 피하고, 없으면 제한 없이. 테두리는 안 뚫음. 막다른 칸이 없어질 때까지 반복. 같은 시드라도 맵이 조금 바뀜(20261016: 바닥 991 → 993) → 적 배치도 바뀜. 테스트 2개 추가(`ManySeeds_NoDeadEnds` 시드 200개, `IsDeadEnd_CorridorTipOnly`) — 전체 82개
  - 확인 (2026-10-16, 사용자 요청 "같은 시드면 맵이 무조건 같아야 함"): 이미 그렇게 동작함 — 맵 생성 · 적 배치는 시드에서 만든 `RngStream.MapGen`만 쓰고, 시간(`DateTime`)은 고정 시드가 0일 때 시드를 고르는 데만 씀(콘솔에 출력). 순회 순서가 결과에 영향을 주는 `Dictionary`는 모두 ID 순 정렬 후 사용. 테스트 2개 추가: `ManySeeds_SameSeed_ByteIdenticalFloorAndSpawns`(시드 50개 × 2회, 맵 · 시작 · 계단 · 적 배치 · 난수 상태까지 스냅샷 바이트 동일), `RecordedSeed_MatchesBalanceResult`(시드 20261016 = 바닥 993 · 벽 1506 · 계단 1 · 시작 (2, 26) — 에디터를 다시 켜도 같은지 지킴. 생성 규칙을 일부러 바꿀 때만 BALANCE와 함께 갱신) — 전체 84개
  - 결정 (2026-10-16): Q3 임시값 플레이어 체력 20 · 피해 3, 적 체력 8 · 피해 2, **고정 피해(난수 없음)**. 적 1종 = 보이면 다가와 근접 공격, 안 보이면 대기 — 추천안. 수치는 BALANCE 3번
  - 결정 (2026-10-16): `.gitattributes` 추가 (Unity YAML은 eol=lf) — 추천안
  - 정한 것 (기본값으로 진행, 바꿀 수 있음): 적 칸으로 이동 = 공격(SPD식 부딪히기). 벽에 부딪히면 차례를 쓰지 않음. 층마다 적 5마리, 시작 칸에서 8칸 이상 떨어진 바닥에 맵 생성 난수로 배치. 플레이어 사망 → 조작 멈춤 + HUD "DEAD" + R로 새 판. 적 그레이박스 = 흰 원(`unit_circle.png`, player.png와 같은 모양) 빨간 틴트
  - 구현 — Simulation: `Core/Grid/{GridMovement(8방향 · 체비셰프 · CanStep), GridPathfinder(8방향 A*, 걸음 수 최소 → 대각선 적은 길, 결정론적 동점, 목표 칸은 막힘 무시)}`, `Units/{Faction, UnitDef, Unit, UnitRegistry(칸당 1유닛, ID 순 순회), Combat.ApplyDamage, UnitDamagedEvent · UnitDiedEvent, MoveCommand, AttackCommand, PlayerActions(입력 → 커맨드 규칙), ISightRule + RangeOnlySightRule(1-7 전 임시: 거리만), MeleeChaserAi(상태 없음), DeathCheckSystem(⑥), UnitSpawner, SpawnConfig, FloorPopulator}`
  - 구현 — World: `Units` 추가, `InstallCoreSystems()`(⑥ 사망 판정 + ⑦ 이벤트 발행 — 새 World · 복원 World 공용). 스냅샷 **v2** = `UnitState[] Units` 추가(코덱 · Capture · Restore)
  - 규칙: 체력 0이 된 유닛은 같은 틱의 남은 차례에서 행동 · 공격 대상이 되지 않고, ⑥에서 맵 · 스케줄러에서 빠진다(UnitDied → EntityRemoved)
  - 구현 — Runtime: `Input/PlayerInputController`(방향키 · WASD · 넘패드, 대각선은 아래 "변경" 참고, 넘패드5 · 스페이스 · 마침표 대기, 누르고 있으면 0.3초 뒤 0.12초마다 반복. 클릭 = 경로 따라 0.08초마다 한 칸, 적 칸 클릭 = 다가가 한 번 공격, 자기 칸 = 대기, 키 입력 · 길 막힘 · 출발 때 없던 적이 보이면 멈춤), `UI/HudPresenter`(체력 + 로그 6줄, TMP 기본 폰트라 영어), `Data/{UnitDefAsset, SpawnConfigAsset}`, `EntityPresenter` 새 버스 연결 시 남은 모습 정리, `GameRoot`(배치 · 시스템 등록 · 카메라가 플레이어를 따라감 · 사망 처리 · R 재시작)
  - 구현 — Editor: Setup 메뉴에 유닛 에셋(`Data/Units/{Player, MeleeEnemy, SpawnConfig}.asset` — 없을 때만 BALANCE 값으로 생성) · 적 비주얼 · HUD 캔버스 · 입력 컨트롤러 연결 추가. Editor asmdef에 TMP · UI 참조
  - 테스트 17개 추가 (`GridMovementTests` 6 — 체비셰프 · 8방향 · 모서리 규칙 · 대각선/곧은 길 선호 · 벽/유닛 돌아가기 · 목표 칸 · 못 감, `UnitCombatTests` 11 — 이동/공격/무행동, 3대에 처치 · 죽은 틱 무행동, 추적 후 공격, 시야 밖 대기, 10대에 사망 · 시계 멈춤, 적끼리 겹치지 않고 포위, **유닛 · AI · 전투 포함 저장→불러와 진행 = 그냥 진행**, 유닛 스냅샷 왕복, 적 배치 재현 · 거리, 수치 검사, BALANCE 3번 검산) — 전체 79개 통과
  - 확인: 시드 20261016 편집 모드 시작 — 플레이어 (2, 26) 체력 20, 적 5마리 체력 8, 스프라이트 6개(적은 빨강), HUD "HP 20/20", 카메라가 플레이어 칸. 콘솔 프로젝트 오류 0 (편집 모드 `SendMessage` assertion 1건은 1-3과 같은 것)
  - **확인 못 한 것:** 실제 플레이 모드 입력(키 · 클릭 · 반복 속도), HUD 화면 배치, 사망 → R 재시작 화면 흐름
  - 확인 방법: `Main.unity` 플레이 → ① 방향키/WASD/넘패드로 한 칸씩, 키를 누르고 있으면 계속 이동 ② 바닥 클릭 → 그 칸까지 걸어감, 빨간 적이 새로 보이면 멈춤 ③ 빨간 적에게 부딪히기 → 로그 "You hit enemy #N for 3 (5/8)", 세 번째에 사라짐 ④ 적에게 맞으면 HUD 체력 감소 ⑤ 체력 0 → "DEAD - press R to restart", R → 새 맵
  - 참고: 적은 마지막으로 본 위치를 기억하지 않는다(시야 밖이면 즉시 멈춤). 시야는 벽을 무시한다 → 1-7
  - 피드백: Unity MCP의 비동기 테스트 실행(`TestRunnerApi.Execute`)은 에디터 포커스가 없으면 진행되지 않음 → `ExecutionSettings.runSynchronously = true`로 RunCommand 안에서 실행하면 결과를 바로 받는다. RunCommand 코드의 중첩 클래스는 도구가 중복 생성해 컴파일 실패 → 최상위 클래스로. CLAUDE.md에 추가
  - 피드백: 녹색 player.png에 빨강 틴트 → 거의 검정. 틴트할 스프라이트는 흰색이어야 함 → `unit_circle.png`
  - 피드백: Bash heredoc 여러 개를 한 명령에 넣으니 파싱 실패로 전부 안 써짐 → 파일 쓰기는 편집 도구로
- [x] **1-7. 시야 · 사거리 (그리드 계산)** (2026-10-16) — 플레이 확인 대기 — 레이캐스트 없이
  - 결정 (2026-10-16): 시야 = **대칭 그림자 투사**(Symmetric Shadowcasting, Albert Ford 방식). 서로 보이면 양쪽 다 보임, 기둥 뒤 숨기 가능, 막는 칸(벽) 자체는 보임. 범위는 원형 dx² + dy² ≤ r² + r — 추천안
  - 결정 (2026-10-16): 탐험 기록(안개)을 1-7에서 함께 — 안 본 칸 검정 · 본 적 있는 칸 어둡게(알파 0.6) · 지금 보이는 칸 그대로. 보이지 않는 칸의 적은 안 그림. 기록은 `World.Explored`(스냅샷 대상) — 추천안
  - 결정 (2026-10-16): 적은 **마지막으로 본 칸을 기억** — 놓치면 그 칸까지 가 본 뒤 잊음. 기억은 `Unit.LastSeenTargetPos`(World 상태), AI는 여전히 상태 없음 — 추천안
  - 결정 (2026-10-16): 사거리는 **판정 함수까지** — `ITargetingRule`(사거리 = 체비셰프, 조준선 = 브레젠험 직선, 벽 직전에서 멈춤 · 유닛 칸에서 멈춤(포함) · 양옆 벽 대각선 틈은 막힘). 실제 원거리 공격은 Phase 2 마법에서 사용 — 추천안
  - 정한 것: 시야 갱신은 ⑥ 단계 끝(사망 판정 뒤)에 `PlayerVisionSystem`이 매 틱 — GDD 3-3에 적음. 지금 보이는 칸은 맵 · 위치로 다시 계산되므로 저장하지 않고, 탐험 기록만 저장. 유닛은 시야를 막지 않음. 시야 거리 `_sightRange`의 의미가 "체비셰프 거리"에서 "원 반지름"으로 바뀜(값 8 · 6 그대로)
  - 구현 — Simulation: `Core/Grid/{FieldOfView(정수 분수 기울기, 결정론적), ITargetingRule + BresenhamTargetingRule}`, `GridMap.BlocksSight`, `Units/{ISightRule.ComputeVisible 추가, ShadowcastSightRule(1-6 RangeOnlySightRule 대체 · 삭제), PlayerVisionSystem + VisibilityChangedEvent}`, `MeleeChaserAi` 기억 추가, `World.Explored`, `World.InstallCoreSystems(sight)`가 시야 시스템을 돌려줌(층 시작 · 불러오기 직후 `Refresh()`)
  - 스냅샷 **v3** = `Explored` + 유닛 `HasLastSeen/LastSeenX/LastSeenY`
  - 구현 — Runtime: `Presentation/FogPresenter`(안개 Tilemap, 바뀐 칸만 다시 칠함), `EntityPresenter`가 보이는 칸의 엔티티만 그림, `GameRoot` 연결. Setup 메뉴: `Grid/Fog` Tilemap(정렬 20) · `Data/Visuals/FogTile.asset`(흰 사각형 `Art/Greybox/white_square.png`, TileFlags.None)
  - 테스트 13개 추가 (`SightAndTargetingTests` 9 — 원형 범위 정확, 기둥 뒤 숨김 · 벽 보임, 칸막이 너머 안 보임, **무작위 맵 3장 모든 바닥 칸 쌍 대칭**, CanSee 반지름, 체비셰프 사거리, 조준선 도달 · 벽 · 유닛 · 사거리 · 대각선 틈 / `UnitCombatTests` 4 — 적이 모퉁이 뒤로 숨은 플레이어를 기억한 칸으로 와서 다시 찾아 공격, 도착하면 잊음, 탐험 기록이 쌓이고 남음, 탐험 기록 · 기억 스냅샷 왕복) — 전체 97개 통과
  - 확인: 시드 20261016 편집 모드 시작 — 보이는 칸 103 + 안개 2397 = 2500, 플레이어만 보이고 적 5마리 숨김. 8걸음 이동 후 지나온 칸이 어둡게 남는 것 캡처로 확인. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드에서 안개 갱신 체감(이동할 때마다 2500칸 비교 · 바뀐 칸만 칠함), 클릭 이동 중 새 적 발견 시 멈춤(이제 벽에 가려진 적은 "보임"이 아님)
  - 변경 (2026-10-16, 사용자 요청): **적이 대각선으로 따라붙기** — 적의 한 걸음을 `GridPathfinder.ChaseStep`으로 고름: 목표까지 남은 걸음 수가 가장 적은 이웃 칸, 같으면 목표와의 직선 거리(dx² + dy²)가 가장 짧은 칸. 원인: 이전에는 클릭 이동용 경로 탐색(걸음 수가 같으면 대각선이 적은 길 선호)을 그대로 써서, 바로 옆 플레이어가 대각선으로 물러나면 적이 직선으로 꺾었음. 클릭 이동은 그대로(곧은 길 선호). 기억한 칸으로 갈 때도 같은 규칙. 테스트 2개 추가(`PlayerFleesDiagonally_EnemyFollowsDiagonally` — 세 번 연속 대각선으로 따라오고 맞지 않음, `ChaseStep_PrefersDiagonalWhenCloser_StraightWhenStraightIsCloser`) — 전체 99개
  - 피드백: 테스트 폴더 전체에 `sed -i`를 돌리니 손대지 않은 파일 10개가 줄바꿈만 바뀌어 변경으로 잡힘(내용 차이 0) → `git checkout`으로 되돌림. 재발 방지: 여러 파일 치환은 실제로 고칠 파일만 지정
  - 확인 방법: `Main.unity` 플레이 → ① 시작 방만 밝고 나머지 검정 ② 걸어가면 앞이 밝아지고 지나온 곳은 어둡게 남음 ③ 벽 너머 적은 안 보이다가 시야에 들어오면 나타남 ④ 적을 보고 모퉁이를 돌아 숨으면 적이 모퉁이까지 쫓아옴

## 도구 — 개발자 디버그 (Phase와 별개, 필요할 때 추가)
- [x] **T-1. 디버그 창 + 맵 전체 밝히기** (2026-10-16, 사용자 요청) — 플레이 확인 대기
  - 정한 것: 에디터 창(`EditorWindow`, 메뉴 **ProjectM/Debug/Debug Window**)으로 플레이 모드에서 씬의 GameRoot에 붙는다. 기능은 섹션 메서드(DrawXxx) 단위로 추가. 게임 상태를 바꾸는 기능은 섹션 제목에 [상태 변경]을 붙인다 (시드 재현 · 저장에 영향)
  - 정한 것: "맵 전체 밝히기"는 **표시만 바꾸는 켜기/끄기** — 안개 Tilemap 렌더러를 끄고 모든 엔티티를 그림. 탐험 기록 · 적 AI · 시드 재현은 그대로, 끄면 지금 시야로 돌아옴. 새 판(R)에도 유지, 플레이 모드를 다시 시작하면 꺼짐
  - 구현: `Editor/Debug/DebugWindow`(상태 섹션: 시드 · 틱 · 플레이어 위치/체력 · 적 수, 보기 섹션: 맵 전체 밝히기), `GameRoot.DebugRevealAll` · `Seed` · `PlayerId` · `PlayerDead`, `EntityPresenter.RevealAll`, `FogPresenter.RevealAll`(밝힌 동안에도 안개 타일은 계속 갱신)
  - 테스트 2개 추가 (`PresenterTests` — 시야 밖 엔티티 숨김 · 밝히기 켜고 끄기, 안개 칠하기 · 밝히기 중 갱신 유지) — 전체 101개 통과
  - 확인: 메뉴로 창 열림. 시드 20261016 편집 모드 시작 후 밝히기 켜기 → 안개 렌더러 꺼짐 · 엔티티 6개 모두 보임, 맵 전체 캡처 확인
  - **확인 못 한 것:** 플레이 모드에서 창 UI(토글 · 정보 갱신)
- [x] **T-2. 디버그 창 마나 최대로 채우기** (2026-10-17, 사용자 요청) — 플레이 확인 대기
  - 구현: `DebugWindow.DrawMana` — 섹션 "[상태 변경] 마나"에 버튼 "마나 최대로 채우기 (현재/최대)". `Mana.Restore`로 모자란 만큼 회복 → ManaChangedEvent → 이벤트 즉시 처리로 HUD MP 막대가 바로 바뀜. 이미 가득이면 버튼 비활성. 게임 상태를 바꾸므로 시드 재현 · 저장에 영향
  - 확인: Unity 에디터가 꺼져 있어 csproj 복사본으로 에디터 어셈블리 컴파일만 확인(오류 0). 새 로직이 없어(기존 `Mana.Restore` 호출) 테스트는 추가하지 않음
  - **확인 못 한 것:** 플레이 모드에서 버튼 동작 · HUD 반영
  - 확인 방법: `Main.unity` 플레이 → 메뉴 ProjectM/Debug/Debug Window → "맵 전체 밝히기" 체크 → 맵 · 적 전부 보임, 체크 해제 → 원래 시야

## Phase 2 — 시스템 연결: 마나와 첫 마법 (원본 개발 순서 2)
- [x] **2-1. 마나** (2026-10-16) — 플레이 확인 대기 — 최대치 · 회복 (Q1 착수 질문), 주문 장착 임시 규칙 (Q2)
  - 결정 (2026-10-16): 마나 = **틱당 회복**, 최대 10 · 3틱마다 1 (30틱이면 바닥에서 가득). 처치 회복 없음. 수치는 BALANCE 4번 (Q1 임시값) — 추천안
  - 결정 (2026-10-16): 주문 장착 = **칸 4개(키 1~4)**, 시작 시 불 · 얼음 · 번개 · 독 기본 주문을 하나씩 장착. 획득 · 교체는 Phase 6 (Q2 임시) — 추천안
  - 결정 (2026-10-16): 마나 표시 = HUD 첫 줄에 `MP 10/10` 숫자, 둘째 줄에 장착 칸. 막대 · 아이콘은 2-5 — 추천안
  - 정한 것: 시작 주문 ID는 임시로 `fire_basic` · `ice_basic` · `lightning_basic` · `poison_basic` (2-2 주문 데이터에서 같은 ID로 정의하거나 이름을 정함). 회복 주기는 틱 번호로만 판정 — (틱 + 1)이 3의 배수인 틱 ⑤ 단계 (상태 없음). 적은 마나 0(회복 없음). 마나를 바꾸는 곳은 `Mana.TrySpend` · `Mana.Restore` 하나. 체력 · 마나는 같은 Unit에 두고 ProjectM.Magic이 다룸
  - 구현 — Simulation: `Magic/{MagicConfig, Mana(TrySpend · Restore), ManaRegenSystem(⑤), ManaChangedEvent}`, `Unit.{Mana, MaxMana, SpellSlots}`, `UnitDef.{MaxMana, StartingSpells}`, `Unit.FromDef(..., spellSlotCount)`(시작 주문이 칸보다 많으면 예외), `UnitSpawner.SpawnPlayer` · `FloorPopulator.Populate`에 칸 수, `World.InstallCoreSystems(sight, magic)`
  - 스냅샷 **v4** = 유닛 `MaxMana · Mana · SpellSlots`(빈 칸 = 빈 문자열로 저장)
  - 구현 — Runtime: `Data/MagicConfigAsset`(`Data/Magic/MagicConfig.asset`), `UnitDefAsset._maxMana · _startingSpells`, `HudPresenter.Bind(bus, player)`(MP · 장착 칸 줄, ManaChangedEvent 구독), `GameRoot._magicConfig`, 디버그 창 플레이어 줄에 MP. Setup 메뉴: MagicConfig 생성 · 연결, HUD 상태 글상자 2줄 크기, `EnsureAsset`이 문자열 배열 지원
  - 테스트 8개 추가 (`ManaTests` — 시작 가득 · 시작 주문 장착, 칸보다 적으면 빈 칸 · 많으면 예외, 쓰기 · 모자라면 그대로, **3틱마다 1 · 최대치에서 멈춤**, 마나 없는 적은 회복 없음, 마나 · 장착 칸 스냅샷 왕복(바이트 동일), 수치 검사, BALANCE 4번 검산) — 전체 109개 통과
  - 확인: 시드 20261016 편집 모드 시작 → HUD "HP 20/20   MP 10/10" / "[1] fire_basic  [2] ice_basic  [3] lightning_basic  [4] poison_basic". 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드 HUD 배치(2줄 글상자 크기), 마나는 아직 쓰는 곳이 없어 회복은 테스트로만 확인
  - 확인 방법: `Main.unity` 플레이 → 왼쪽 위 HUD에 MP 10/10과 주문 칸 4개. 디버그 창 플레이어 줄에도 MP
  - 피드백: Setup 메뉴 `EnsureAsset`의 `value is int i` 패턴 변수와 배열 반복문 `i`가 겹쳐 컴파일 오류 → 반복 변수 이름 변경
  - 변경 (2026-10-16, 사용자 요청 — 적 AI 두 가지, 2-1 브랜치에 별도 커밋): ① **발자국 따라가기** — 플레이어가 움직이면 떠난 칸이 `Unit.PreviousPos`에 남고(`UnitRegistry.Move`), 플레이어와 붙어 있던 적(그 칸으로 한 걸음에 갈 수 있는 적)은 그 칸으로 들어간다 → 플레이어가 지나간 길을 그대로 밟음. 1-7의 "대각선으로 따라붙기"는 붙어 있는 경우 이 규칙으로 대체(떨어져서 쫓을 때는 ChaseStep 그대로). ② **배회** — 플레이어를 못 봤고 기억도 없으면 맵의 아무 바닥 칸을 목적지(`Unit.WanderTarget`)로 골라 걸어가고, 도착하거나 막히면 새로 고름. 목적지는 `RngStream.Ai` 난수 → 같은 시드 = 같은 배회. 기억한 칸에 도착한 턴은 대기 후 다음 턴부터 배회. 스냅샷 **v5** = 유닛 발자국 · 배회 목적지. 테스트: 1-7 대각선 따라붙기 테스트를 발자국 테스트로 교체(대각선 · 직선 섞인 4걸음을 그대로 따라옴, 맞지 않음), 시야 밖 대기 테스트를 배회 테스트로 교체(움직임 · 같은 시드 같은 배회), 도착 시 새 목적지, 발자국 · 목적지 스냅샷 왕복 추가 — 전체 111개. 확인: 시드 20261016 실제 맵에서 30턴 대기 → 적 5마리 모두 맵을 가로질러 이동, 턴당 약 3ms(시야 · 안개 포함)
- [x] **2-2. 주문 데이터 + 시전** (2026-10-16) — 플레이 확인 대기 — `SpellDef`(속성, 모양, 사거리, 피해, 마나, 반지름), 불 · 얼음 · 번개 · 독 기본 주문 (Q4)
  - 결정 (2026-10-16): 시전 조작 = **키보드 + 클릭 둘 다** — 숫자 키 1~4로 조준 모드(보이는 가장 가까운 적 자동 선택, Tab 다음 적, 방향키 · WASD · 넘패드 · 마우스로 조준 칸 이동), Space · Enter · 클릭 시전, Esc · 같은 숫자 키 취소. 경로 · 맞는 칸 미리보기 — 추천안
  - 결정 (2026-10-16): 4주문은 **속성별 모양** — 불 단일 투사체(피해 5 · 사거리 6 · 마나 3), 얼음 단일(3 · 6 · 2), 번개 직선 관통(3 · 5 · 4), 독 3×3 폭발(2 · 5 · 3, 범위 안이면 시전자도 맞음). BALANCE 5번 (Q4 임시값) — 추천안
  - 결정 (2026-10-16): 주문 ID는 2-1 임시 ID 그대로(`fire_basic` · `ice_basic` · `lightning_basic` · `poison_basic`) — 사용자 선택
  - 결정 (2026-10-16): **항상 명중** — 추천안
  - 정한 것: 사거리 = 체비셰프(ITargetingRule), 대상 칸이 보여야 시전 가능(ISightRule). 투사체는 조준선(브레젠험)을 따라 처음 만난 유닛에서 멈춤, 번개는 조준 방향으로 사거리 끝까지 늘려 쏘고 벽에서 멈춤, 독은 투사체가 멈춘 칸 중심. 시전 비용 = 기본 행동 비용(1틱). 상태 이상 · 지형 태그는 2-3 · 2-4에서 SpellDef 필드로 추가. 조준 상태는 입력 상태(저장 안 함) → 스냅샷 형식 그대로 v5
  - 구현 — Simulation: `Magic/{SpellDef(+ Element 7개 · SpellShape), SpellBook, SpellCasting(Preview · Check(CastBlock 이유) · TryCreateCast), CastSpellCommand, SpellPreview, SpellCastEvent}`. 미리보기와 실제 시전이 같은 계산(`SpellCasting.Preview`)
  - 구현 — Runtime: `Data/SpellCatalogAsset`(`Data/Spells/SpellCatalog.asset`, 주문 하나 = 항목 하나), `Presentation/AimPreviewPresenter`(`Grid/Aim` Tilemap 정렬 30, 경로 노랑 · 맞는 칸 주황 · 조준 칸 흰색 / 못 쓰면 빨강), `PlayerInputController` 조준 모드 + `HintChanged`, `HudPresenter`(장착 칸 마나 비용, 노란 안내 줄, "You cast …" 로그), `GameRoot`(SpellBook · 시작 주문이 카탈로그에 없으면 경고). Setup 메뉴: SpellCatalog(없는 ID만 추가) · Aim Tilemap · 연결
  - 테스트 8개 추가 (`SpellTests` — 투사체는 앞의 적만 · 마나 소모, 번개는 사거리 끝까지 관통, 독 폭발 범위 · 시전자 자기 피해, 빈 칸에 쏘면 벽 앞에서 멈추고 아무도 안 맞음, 시전 불가 이유 6가지, 시전 = 1틱 · 0이 되면 ⑥에서 제거, 수치 검사, BALANCE 5번 검산) — 전체 119개 통과
  - 확인: 시드 20261016 편집 모드에서 번개 미리보기를 그려 캡처 — 경로가 벽 앞에서 멈추고 조준 칸 표시. SpellCatalog 4주문 생성 확인. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드 조준 조작 전체(숫자 키 · Tab · 마우스 이동 · 클릭 시전 · Esc), HUD 안내 줄 배치, 미리보기 색 판독성
  - 확인 방법: `Main.unity` 플레이 → 적이 보일 때 1 → 적 칸 흰색 + 경로 노랑 + 맞는 칸 주황 → Space → 로그 "You cast fire_basic." · "You hit enemy … for 5", MP 10 → 7. 3 = 번개 일렬 관통, 4 = 독 폭발(가까이 쏘면 나도 맞음), 마나 모자라면 안내 줄에 이유
  - 피드백: 문서에 여러 줄을 넣을 때 셸 `sed a\` + 큰따옴표 조합이 줄바꿈을 잃어 한 줄로 합쳐짐 → 나눠서 고침. 여러 줄 삽입은 편집 도구로
- [x] **2-3. 상태 이상** (2026-10-16) — 플레이 확인 대기 — 화상 · 둔화 · 중독 · 마비, 지속 틱 · 중첩 규칙 (Q5)
  - 결정 (2026-10-16): 불 → **화상**(틱당 피해), 얼음 → **둔화**(행동 비용 +1틱), 독 → **중독**(틱당 피해). 빙결은 나중 — 추천안
  - 결정 (2026-10-16, 사용자 추가 요청): 번개 → **마비** — 맞은 적마다 **30%** 확률, **2틱 행동 불가**. 확률은 전투 난수(RngStream.Combat) → 같은 시드 = 같은 결과
  - 결정 (2026-10-16): 수치(Q5 임시) — 화상 3틱 · 1, 중독 5틱 · 1, 둔화 3틱, 마비 2틱. BALANCE 6번 — 추천안
  - 결정 (2026-10-16): 같은 상태 중첩 = **지속 갱신**(남은 틱을 더 긴 쪽으로, 강도 그대로) — 추천안 (규칙 10)
  - 결정 (2026-10-16): 표시 = 걸린 유닛 색 틴트(우선순위 마비 노랑 > 화상 주황 > 중독 보라 > 둔화 하늘) + HUD 첫 줄에 내 상태 · 로그에 걸림 · 풀림 · 지속 피해. 아이콘은 2-5 — 추천안
  - 정한 것: 상태 효과는 종류별 코드가 아니라 데이터 필드(틱당 피해 · 행동 비용 추가 · 행동 불가)로만 정한다. 걸린 틱의 ④ 단계부터 센다 → 불 한 발 = 즉시 5 + 그 틱부터 화상 1 × 3 = 8(적 체력 8 처치). 피해로 체력 0이 되면 ⑥에서 제거. 맞아서 죽은 대상에는 상태를 걸지 않음. 확률 100% · 0%는 난수를 굴리지 않음. 마비된 플레이어는 입력을 기다리지 않고 턴이 지나가며, 넣어 둔 행동은 풀린 뒤 실행
  - 구현 — Simulation: `Status/{StatusType(None · Burn · Slow · Poison · Paralyze), StatusDef, StatusBook, StatusInstance, StatusEffects(Apply · TryApply · CanAct · AdjustCost), StatusSystem(④), StatusChangedEvent}`, `Unit.Statuses`, `SpellDef.AppliesStatus · StatusChancePercent`, `SpellBook.Statuses`(거는 상태가 없으면 예외), `CastSpellCommand`가 피해 뒤 상태 적용, `TickScheduler.SetActionRules(canAct, adjustCost)`(상태 없는 규칙 — 마비면 커맨드를 받지 않고 1틱 미룸, 플레이어 입력도 기다리지 않음), `World.InstallCoreSystems(sight, magic, statuses)`
  - 스냅샷 **v6** = 유닛 상태 이상(종류 · 남은 틱)
  - 구현 — Runtime: `Data/StatusCatalogAsset`(`Data/Statuses/StatusCatalog.asset`), `SpellCatalogAsset` 항목에 상태 · 확률(`SetStatusIfNone`으로 기존 항목에 채움), `EntityPresenter` 상태 틴트, `HudPresenter` 상태 줄 · 로그, `GameRoot._statusCatalog`, 디버그 창 플레이어 줄에 상태. Setup 메뉴: StatusCatalog 생성 · 주문 상태 채움 · 연결. (Setup 메뉴의 안개 타일 설명 주석이 2-2에서 엉뚱한 메서드 위로 밀려 있던 것도 바로잡음)
  - 테스트 11개 추가 (`StatusTests` — 불 화상 3틱으로 처치, 지속 갱신 · 강도 그대로, 둔화 동안 2틱마다 행동, 마비 2틱 무행동, 마비된 플레이어 턴 통과 · 넣어 둔 행동 실행, **마비 확률 같은 시드 같은 결과 · 1000번 중 250~350**, 100% · 0%는 난수 미사용, 번개 관통 마비 재현, 얼음 둔화 · 독 3×3 중독, 상태 스냅샷 왕복(바이트 동일), 수치 검사, BALANCE 6번 검산. `SpellTests` 검산에 상태 · 확률 포함) — 전체 130개 통과
  - 확인: 시드 20261016 편집 모드에서 적 4마리에 상태를 하나씩 걸어 캡처 — 화상 주황 · 중독 보라 · 마비 노랑은 잘 구분, HUD "HP 20/20   MP 10/10   Burn 3", 로그 "Enemy #2: Burn (3)." 등. StatusCatalog 4종 · SpellCatalog 상태 필드 저장 확인. 콘솔 프로젝트 오류 0
  - **확인 못 한 것 / 아쉬운 점:** 플레이 모드 전체(주문으로 실제로 걸고 턴이 지나가는 체감). 빨간 적의 둔화 틴트는 회청색이라 덜 눈에 띄고, 초록 플레이어 스프라이트는 틴트가 거의 안 보임 → 플레이어 상태는 HUD로 확인, 아이콘은 2-5
  - 확인 방법: `Main.unity` 플레이 → 적에게 1(불) → 주황, 로그 "Burn (3)" → 두 턴 뒤 처치. 2(얼음) → 하늘색, 적이 한 턴씩 쉬며 다가옴. 3(번개) → 가끔 노랑(마비), 2턴 멈춤. 4(독) → 보라, 5턴 동안 매 턴 1
  - 피드백: 상태 테스트 2개가 테스트 배치 실수로 실패(독 대상이 사거리 밖, 그다음엔 독 조준선 위에 다른 적이 있어 폭발 중심이 바뀜) → 코드가 아니라 배치를 고침. 범위 · 투사체 테스트는 조준선 위 다른 유닛을 먼저 확인
- [x] **2-4. 지형 레이어** (2026-10-16) — 플레이 확인 대기 — 타일 레이어 구조 · 우선순위 (Q8), 화염 지대 · 독구름
  - 결정 (2026-10-16): 레이어 구조 = 기본 지형(GridMap) 위에 **지형 효과 레이어 따로, 한 칸에 여러 효과 공존**(같은 종류는 지속 갱신, 종류 순으로 처리) — 사용자 선택 (추천안은 칸당 하나였음)
  - 결정 (2026-10-16): 불 → **화염 지대**(투사체가 멈춘 칸), 독 → **독구름**(폭발 3×3). 얼음 · 번개는 지형 없음 — 추천안
  - 결정 (2026-10-16): 규칙 = 그 칸에 있는 유닛(플레이어 포함)에게 **매 틱 상태 부여**(화염 → 화상, 독구름 → 중독, 지속 갱신이라 머물면 계속). 확산 없음 — 추천안
  - 결정 (2026-10-16): 표시 = **칸 색 오버레이**(바닥 위 · 유닛 아래 · 안개 아래, 화염 주황 · 독구름 보라, 겹치면 색 평균) — 추천안
  - 정한 것: 지속은 두 지대 모두 **4틱**(질문에 없어 임시, BALANCE 7번). 놓인 틱의 ③ 단계부터 센다. 벽에는 놓지 않음. 지형이 남는 칸: Bolt = 멈춘 칸(빈 칸에 쏴도 남음), Beam = 지나간 칸, Burst = 폭발 범위. 적 AI는 아직 지형을 피하지 않음. 지형끼리의 반응(불 + 독 등)은 Phase 3 반응 규칙 표
  - 구현 — Simulation: `Status/TerrainEffects.cs` — `TerrainEffectType(None · FireZone · PoisonCloud)`, `TerrainDef`(지속 · 거는 상태), `TerrainBook`(거는 상태가 StatusBook에 없으면 예외), `TerrainInstance`, `TerrainLayer`(칸 인덱스 → 효과 목록, 칸 순 · 종류 순), `TerrainEffects.Place`, `TerrainSystem`(③), `TerrainChangedEvent`. `World.Terrain`, `SpellDef.LeavesTerrain`, `SpellBook.Terrains`, `SpellCasting.TerrainCells`, `World.InstallCoreSystems(sight, magic, statuses, terrains)`
  - 스냅샷 **v7** = 지형 효과(칸 인덱스 · 종류 · 남은 틱)
  - 구현 — Runtime: `Data/TerrainCatalogAsset`(`Data/Terrains/TerrainCatalog.asset`), `SpellCatalogAsset.LeavesTerrain`(`SetTerrainIfNone`), `Presentation/TerrainOverlayPresenter`(`Grid/TerrainFx` Tilemap 정렬 5), `GameRoot._terrainCatalog · _terrainOverlay`. Setup 메뉴: TerrainCatalog 생성 · 주문 지형 채움 · 오버레이 · 연결
  - 테스트 8개 추가 (`TerrainTests` — 불이 빈 칸에도 화염 지대, **지대 위에 서 있으면 4틱 내내 화상 갱신 · 사라진 뒤 2틱 더**(피해 6), 독구름 3×3 · 플레이어도 중독, 여러 효과 공존 · 둘 다 적용 · 같은 종류 갱신, 벽 무시, 지형 스냅샷 왕복(바이트 동일), 수치 검사, BALANCE 7번 검산. `SpellTests` 검산에 지형 포함) — 전체 138개 통과
  - 확인: 시드 20261016 편집 모드에서 독 · 불을 써서 캡처 — 3×3 보라, 화염이 겹친 칸은 분홍빛으로 구분. 칸마다 효과 · 남은 틱 로그 확인. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드 전체(지대 위로 걸어 들어가기 · 적이 지대를 지나가기)
  - 확인 방법: `Main.unity` 플레이 → 1(불)을 빈 칸에 → 주황 칸이 4턴 남음, 그 위로 걸어가면 HUD에 Burn. 4(독) → 보라 3×3, 적이 그 위에 있으면 계속 중독. 같은 칸에 불 + 독 → 섞인 색
  - 피드백: `TerrainOverlayPresenter`의 이벤트 처리 메서드 이름을 `OnTerrainChanged`로 했더니 Unity 내장 메시지(인자 int)와 겹쳐 콘솔 오류 → `HandleTerrainChanged`로 변경. MonoBehaviour 메서드 이름은 Unity 메시지(`OnXxx`)와 겹치지 않게
- [x] **2-5. HUD 최소** (2026-10-16) — 플레이 확인 대기 — 체력 · 마나 · 상태 아이콘 · 전투 로그(텍스트)
  - 결정 (2026-10-16): 한글 폰트 = **사용자가 폰트 파일을 직접 가져옴** → **Galmuri11** (`Assets/_Project/Art/Fonts/Galmuri11.ttf`, v2.404, © Lee Minseo, **SIL OFL 1.1 — 배포 가능**, 폰트 이름 표에서 확인). 배포 빌드에 OFL 라이선스 문구를 함께 넣을 것 (Phase 5 배포 체크리스트)
  - 진행 (2026-10-16) — 폰트 적용: Setup 메뉴 `EnsureHudFont`가 ttf에서 TMP 에셋 `Assets/_Project/Art/Fonts/Galmuri11 SDF.asset`을 만든다(없을 때만, 멱등) — **동적 SDF**(SDFAA · 샘플링 48pt = 원래 격자 12px × 4 · 여백 6 · 1024 아틀라스 · 다중 아틀라스), 영문 · 숫자 · 기호 95자 미리 등록, 한글은 쓰는 글자만 실행 중 추가, **빌드 때 동적 데이터 비움**(`m_ClearDynamicDataOnBuild` — 공개 속성이 없어 SerializedObject로 설정). HUD `Status` · `Log` 글상자에 연결. 한글 글자 추가 확인(테스트 문자 11자). 테스트 138개 통과 · 콘솔 오류 0. HUD 문구 한글화는 2-5 나머지(막대 · 아이콘 · 로그)와 함께
  - 피드백: `TMP_FontAsset.clearDynamicDataOnBuild`는 이 TMP 버전에 공개 속성이 없어 컴파일 오류 → 직렬화 필드 `m_ClearDynamicDataOnBuild`로 설정
  - 결정 (2026-10-16): 체력 · 마나 = **막대 + 숫자**(HP 빨강 · MP 파랑, uGUI Image fill, 그레이박스 단색) — 추천안
  - 결정 (2026-10-16): 상태 아이콘 = **HUD만**(플레이어 상태, 색 사각형 + 남은 턴). 적은 2-3 틴트 그대로 — 사용자 선택 (추천안은 HUD + 유닛 머리 위였음)
  - 결정 (2026-10-16): 전투 로그 = **왼쪽 아래 8줄**, 같은 상태 지속 갱신(`StatusChangedEvent.Applied`인데 이미 걸려 있던 경우)은 로그에 남기지 않음 — 새로 걸림 · 풀림 · 피해만 — 추천안
  - 정한 것: 주문 표시 이름은 **데이터**(`SpellCatalogAsset.Entry.DisplayName`, 표시 전용 — 규칙 · SpellDef에는 넣지 않음): 불꽃 화살 · 얼음 화살 · 번개 줄기 · 독 폭발 (BALANCE 5번). 상태 이름(화상 · 둔화 · 중독 · 마비) · 시전 불가 이유 · 로그 문구는 `HudText` 한곳. 상태 아이콘 색 = 유닛 틴트와 같은 표(`EntityPresenter.StatusTint`). 적 이름은 `적 #ID`. 마나가 모자란 칸의 비용은 빨강
  - 구현 — Runtime/UI: `HudModel`(순수 C# — 이벤트로 HP · MP · 장착 칸 비용 · 내 상태 · 로그 N줄을 모음, 유닛별 걸린 상태를 기억해 **지속 갱신은 로그 생략**, 죽은 유닛은 지움), `HudText`(한글 문구 · 주문 이름 표), `HudPresenter`(그리기만: 막대 Fill 앵커 · 상태 아이콘(색 칸 + "중독 5", 필요할 때 만들고 재사용) · 칸 줄 · 안내 줄(죽으면 "쓰러졌다 — R 키로 새 판" 빨강) · 로그(오래된 줄일수록 흐리게)). `PlayerInputController.Text`(조준 안내 한글), `GameRoot`가 `SpellCatalog.DisplayNames()`로 HudText를 만들어 HUD · 입력에 넘김
  - 구현 — Setup 메뉴: HUD 다시 구성(기준 1920×1080) — `HpBar` · `MpBar`(360×30, 바탕 + Fill + Label) 왼쪽 위, `StatusIcons`(가로 배치) 막대 오른쪽, `Slots` · `Hint` 그 아래, `Log`(1100×300) 왼쪽 아래 정렬. 2-4까지의 `Status` 글상자 삭제, `_logLines` 8로 덮어씀, 주문 표시 이름 채움(비어 있을 때만)
  - 테스트 7개 추가 (`HudModelTests` — 처음 값 · 칸 비용, **화염 지대 위 4틱 = 화상 로그 1번 · 풀림 1번**, 풀린 뒤 다시 걸리면 다시 기록, 한글 전투 문구 · 표시 이름(없으면 ID), 로그 8줄 제한 · 새 줄이 뒤, 플레이어 사망, 마나는 플레이어만) — 전체 145개 통과
  - 확인: 편집 모드에서 판을 시작하고 이벤트를 넣어 1920×1080 렌더 — HP 14/20 · MP 2/10 막대, 상태 아이콘 "둔화 2" · "중독 5", 칸 줄 "[1] 불꽃 화살 3MP …"(모자란 비용 빨강), 노란 안내 줄, 왼쪽 아래 로그 8줄 한글 정상. 렌더 후 씬은 디스크 상태로 다시 열어 저장하지 않음. 콘솔 프로젝트 오류 0
  - **확인 못 한 것 / 아쉬운 점:** 플레이 모드 전체(실제 조작 중 갱신 · 아이콘 늘고 줄기). 로그 글자가 밝은 바닥 타일 위에 겹치면 덜 읽힘 → 필요하면 로그 바탕 반투명 판 추가. 픽셀 폰트를 SDF로 그려 원본 도트만큼 선명하지는 않음
  - 확인 방법: `Main.unity` 플레이 → 왼쪽 위 빨강 HP · 파랑 MP 막대와 숫자, 아래 주문 칸 4개(한글 이름 + MP). 1(불)을 적에게 → 노란 안내 줄, Space → 로그 "불꽃 화살 시전" · "적 #N에게 피해 5" · "적 #N: 화상 3턴". 화염 지대 위에 서 있으면 HP 막대 옆에 주황 "화상 N" 아이콘, 로그에는 화상이 **한 번만**. 죽으면 안내 줄에 빨간 "쓰러졌다 — R 키로 새 판"
  - 피드백: ① 테스트에서 지형 없는 주문 묶음(`BalanceBookWithStatuses`)을 써서 TerrainDef가 null → `BalanceBookFull`로. ② 씬에 저장돼 있던 `_logLines = 6`이 필드 기본값 8보다 우선 → Setup에서 값을 명시해 덮어씀 (직렬화 필드 기본값을 바꿀 때는 씬 값도 Setup에서 맞출 것). ③ Unity가 백그라운드일 때 컴파일 후 어셈블리 재로딩이 5~9분 걸림 → `Logs/Editor.log`의 "Domain Reload Profiling"으로 끝을 확인한 뒤 MCP 호출
  - 착수 전 확인: 2-4는 main에 병합 · 푸시됨(df8cd8e). 작업 트리에 추적 안 된 빈 폴더 `Assets/Resource/Sprite`(+ .meta)가 있음 — 의도한 것인지 사용자 확인 필요
- [x] **2-6. 플레이어 체력 자동 회복** (2026-10-17, 사용자 요청) — 플레이 확인 대기
  - 결정 (2026-10-17, 사용자 답): 1%는 **올림 · 최소 1**(최대 체력 20 → 10틱마다 1, 실제 5%) · **플레이어만** · 화상 · 중독 등 **상태 이상 중에도 회복**
  - 구현: `MagicConfig`에 `HpRegenIntervalTicks`(0 = 없음) · `HpRegenPercent` · `HpRegenAmount(maxHp)`(선택 인자라 기존 호출은 회복 없음 그대로). `HpRegenSystem` — ⑤ 단계에서 마나 회복 다음, (틱 + 1) % 10 == 0인 틱마다 살아 있는 플레이어만 회복, 최대치에서 멈춤 → `UnitHealedEvent`. HUD는 HP 막대만 갱신하고 로그는 남기지 않음(10틱마다 한 줄씩 쌓여 전투 로그가 묻힘). 틱 번호로만 정하므로 저장 상태 늘지 않음(스냅샷 v7 그대로). 에셋 `MagicConfig.asset` `_hpRegenIntervalTicks: 10` · `_hpRegenPercent: 1` (BALANCE 4번)
  - 테스트 4개 추가 (`ManaTests` — 10틱마다 1 · 최대치에서 멈춤 · 이벤트, 적은 회복 안 함, 1% 올림 · 최소 1 계산 · 주기 0이면 없음, 에셋 검산 · 잘못된 값 거부 보강 / `HudModelTests` — 회복은 HP만 갱신 · 로그 없음) — 전체 165개 통과
  - **확인 못 한 것:** 플레이 모드에서 HP 막대가 10턴마다 차는 모습
  - 확인 방법: `Main.unity` 플레이 → 적에게 맞아 HP를 줄인 뒤 제자리 대기(또는 이동) 10턴마다 HP 막대가 1씩 차는지. 로그에는 회복 줄이 나오지 않음

## Phase 3 — 반응 엔진과 물 (원본 개발 순서 3~5)
- [x] **3-1. 반응 규칙 엔진** (2026-10-17) — 플레이 확인 대기 — `상태/지형 × 속성 → 결과` 데이터 표, 안전장치(상한 · 전이 감쇠 · 대상 수), 발동 우선순위 (Q9 · Q10)
  - 입력: 사용자 반응 규칙 요청표(3차 수정) → `Docs/Source/반응규칙_요청표_1.md`(보존) + 구현 기준 `REACTIONS.md`
  - 결정 (2026-10-17, 사용자 답 — REACTIONS.md 1번 D1~D8): 2-5는 다른 PC에서 완료 → 3-1 바로 진행 / 불 단독 화염 지대 제거, 융합 생길 때까지 화염 지대 없음이 맞음 / 화상 · 둔화 · 중독 확률 70% / 지형이 거는 상태는 매 턴 100%(요청표 50%를 수정) / 감전 전이 100% · 피해 50% / 반응 피해 상한은 적용하되 폭발 상한을 올림 / 오염된 물 위 젖음 / 주문은 게임 중 획득, 시작 칸 4개는 빈 칸 + 디버그 창에서 칸마다 주문 고르기
  - 정한 것: 폭발 상한 = max(주문 피해, **5**)(D6의 "올림" 값 — 유독성 폭발 최대 5가 그대로 나오게), 일반 추가 피해 상한 = max(주문 피해, 3), 주문당 반응 4회. 우선순위 숫자 빙결 · 파쇄 · 해동 30 / 감전 · 폭발류 20 / 독화 · 증발 10 / 지형 변환 · 제거 0. 지형 반응은 "주문이 닿은 칸"(Bolt 멈춘 칸 · Beam 지나간 칸 · Burst 범위)의 지형에 대해. 주문 피해로 쓰러진 대상도 그 자리에서 폭발은 일어남(추가 피해는 살아 있을 때만). 반응 로그 "독화! 적 #3"
  - 범위: 지금 있는 속성 · 상태 · 지형만으로 되는 반응 5종 — 독화(중독 × 불, 독구름 × 불 한 그룹) · 유독성 폭발(중독 × 번개) · 가스 폭발(독구름 × 번개) · 소화(화염 지대 × 얼음). 젖음 · 빙결 · 물 계열 11종은 3-2 이후(REACTIONS.md 3번 표)
  - 구현 — Simulation: `Status/ReactionDef.cs`(ReactionTrigger · ReactionDef · ReactionConfig · ReactionBook — 같은 대상 · 속성 조합 중복 · 없는 상태면 예외), `Status/ReactionEngine.cs`(후보 수집 → 우선순위 정렬 → 그룹 · 대상당 1회 · 주문당 N회 → 추가 피해 · 소모 · 폭발 예약 → 폭발 대상당 가장 큰 것 1회 → 상태 확률, `ReactionTriggeredEvent`), `StatusEffects.Remove` · `TerrainEffects.Remove`, `CastSpellCommand` 순서를 **원래 피해 → 반응 → 새 상태 → 지형**으로, `SpellBook.Reactions` · `Ids`, `Magic/SpellSlots.Assign` + `SpellSlotsChangedEvent`
  - 구현 — Runtime: `Data/ReactionCatalogAsset`(`Data/Reactions/ReactionCatalog.asset`, 안전장치 값 · 반응 5종 · 표시 이름), `GameRoot._reactionCatalog`, `HudText.Reaction` · `HudModel` 반응 로그 · 장착 칸 변경 반영, 디버그 창 "[상태 변경] 주문 장착 칸"(칸마다 주문 고르기). Setup 메뉴: ReactionCatalog 생성 · 연결, 시작 주문 빈 칸, 불 지형 줄 삭제, 상태 확률 70%
  - 데이터 변경: `SpellCatalog.asset` 불 LeavesTerrain → 없음, 화상 · 둔화 · 중독 확률 100 → 70. `Player.asset` 시작 주문 4개 → 빈 칸
  - 테스트 16개 추가 (`ReactionTests` 14 — 독화 +2 · 소모 안 함, 유독성 폭발 남은 틱만큼 · 반지름 · 소모 안 함, 폭발이 시전자도 맞힘, 가스 폭발 구름 제거 · 겹쳐도 1회, 독화 상태 · 지형 한 그룹, 소화, 주문당 4회, 같은 우선순위면 상태 먼저, 연쇄 금지, 근접 공격은 반응 없음, 같은 주문이 새로 건 상태로는 반응 안 함(순서 4-1), 추가 피해 상한, 표 검사, BALANCE 8번 검산 / `HudModelTests` 2 — 반응 로그 이름, 장착 칸 변경). 기존 테스트 수정: 불 화염 지대 → "불 단독은 지형 없음", BALANCE 검산 70% · 빈 시작 칸 — 전체 161개 통과
  - 확인: Unity가 꺼져 있던 동안 Simulation · Runtime · Editor · 테스트 코드를 임시 프로젝트(scratchpad)로 빌드 — 오류 0. 사용자가 에디터를 연 뒤 Setup · 데이터 갱신 · 테스트 161개 통과. 시드 20261016 편집 모드: 시작 칸 4개 비어 있음 → 칸 1 · 3에 불 · 번개 넣으면 HUD 칸 즉시 반영 → 중독된 적에 번개 → 로그 "번개 줄기 시전 / 적 #2에게 피해 3 / 유독성 폭발! 적 #2 / 적 #2에게 피해 5 / 적 #2 쓰러짐". 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드에서 디버그 창 칸 고르기 UI · 반응 체감. 화염 지대는 지금 게임에서 생기지 않으므로 소화는 테스트로만 확인
  - 확인 방법: `Main.unity` 플레이 → 디버그 창 "[상태 변경] 주문 장착 칸"에서 칸 1 = poison_basic, 칸 2 = lightning_basic, 칸 3 = fire_basic → 1(독)으로 적 무리를 중독 · 독구름 → 2(번개)로 유독성 · 가스 폭발, 3(불)로 독화. 로그에 반응 이름이 나오는지
  - 피드백: Unity 에디터가 작업 중 꺼져 있었음(MCP "Unity not detected"가 계속됨 → `~/.unity/mcp/connections/`가 비어 있고 Unity.exe 프로세스 없음으로 확인). 에디터 없이도 Unity가 만든 csproj를 복사해 소스 목록만 glob으로 바꾸면 컴파일 오류는 미리 잡을 수 있음 — 새 스크립트의 `.meta` · 에셋 생성 · 테스트는 에디터 필요
- [x] **3-2. 물 타일 · 젖음** (2026-10-17) — 플레이 확인 대기 — 생성 · 확산 · 제거 (Q6)
  - 착수 전 확인: 3-1 · T-2 · 2-6 · 융합 결정은 main 병합 · 푸시됨(4b71944까지 사용자가 푸시 — origin/main과 같음)
  - 결정 (2026-10-17, 사용자 답): 3-2 범위 = **물 기본 + 제거 · 변환** — 물 주문 · 젖음 · 물 타일 · 오염된 물 + 증발(젖음 · 물 타일 · 오염된 물 × 불, 화염 지대 × 물) · 오염(물 × 독). 빙결 · 빙판 계열은 3-3 물+얼음, 감전 · 수면 감전은 3-3 물+번개 — 추천안
  - 정한 것 (Claude ✎): ① 젖음 지속 3 — 물 위에서는 매 턴 갱신, 떠나면 2턴 더 (원본 "떠난 뒤 3틱"을 기존 상태 규칙 "지속 N = ④ 처리 N번"에 맞춤). ② 지형만 바꾸는 반응(폭발 없음 · 빈 칸이거나 추가 피해 0)은 주문당 4회에 세지 않고 로그 한 줄 — 물 웅덩이 9칸에 독 → 9칸 모두 오염. ③ 같은 그룹이 이미 발동한 대상의 다른 줄은 피해 없이 소모 · 변환만 — 젖은 적이 물 위에서 불을 맞으면 +2 한 번, 젖음과 물 모두 제거. ④ 오염된 물 × 불도 증발(원본 "물 타일과 동일 ✎"). ⑤ 물 주문 표시 이름 "물보라". 색: 물 파랑 · 오염된 물 탁한 초록 · 젖음 틴트 파랑(틴트 우선순위 맨 뒤)
  - 구현: `StatusType.Wet` · `TerrainEffectType.Water` · `ContaminatedWater` (끝에 추가 — 스냅샷 v7 그대로). `TerrainDef`에 `DamagePerTick`(③에서 상태 다음에 피해) · `BlockedBy`(그 지형이 있는 칸에는 안 놓임 — 물은 화염 지대에 막힘). `TerrainEffects.Transform`(남은 틱 유지). `ReactionDef.TransformTerrainTo`(지형 반응 전용, 소모와 함께 못 씀). `ReactionEngine` 규칙 ② · ③. 피해 0 주문은 원래 피해 단계 건너뜀. 에셋: SpellCatalog `water_basic` · StatusCatalog 젖음 · TerrainCatalog 물 타일 · 오염된 물 · ReactionCatalog 5줄(Setup 메뉴로 추가). 표시: 지형 오버레이 색 2개, 젖음 틴트 · HUD 아이콘 · 한글 "젖음"
  - 테스트 9개 추가 (`WaterTests` — 물 주문 젖음 · 웅덩이 · 피해 없음, 물 위 젖음 유지 · 풀림, 오염된 물 피해 + 젖음, 증발 3종(젖은 대상 · 젖은 대상이 물 위 · 빈 물 칸), 오염 9칸 남은 틱 유지 · 로그 1줄, 화염 지대 위 물 = 증발 + 물 안 깔림, 잘못된 변환 · 막힘 거부). 에셋 검산 4개(주문 · 상태 · 지형 · 반응)에 새 값 포함 — 전체 174개 통과
  - **확인 못 한 것:** 플레이 모드에서 물 주문 조준 · 오버레이 색 · 젖음 틴트 · 로그. 오염된 물 위 매 턴 피해와 2-6 체력 회복이 겹칠 때의 체감
  - 확인 방법: `Main.unity` 플레이 → 디버그 창 "[상태 변경] 주문 장착 칸"에서 칸 1 = water_basic, 칸 2 = fire_basic, 칸 3 = poison_basic → 1로 적 무리에 물(파랑 웅덩이 · 적 파랑 틴트) → 2(불)로 "증발! 적 #N"과 +2 → 다시 1로 웅덩이를 만들고 3(독)으로 "오염!" · 웅덩이가 탁한 초록으로. 마나는 "[상태 변경] 마나" 버튼으로 채운다
- [x] **3-2a. 인벤토리 · 빠른 칸** (2026-10-17, 3-2b 착수 중 사용자 요청으로 먼저) — 플레이 확인 대기
  - 결정 (2026-10-17, 사용자 답): 융합은 "인벤토리에서 융합기를 고르면 인벤토리에서 주문 두 개를 고르는 방식" → **인벤토리 시스템 먼저**. 화면에 **빠른 칸 1~6**을 두고 주문 · 아이템(융합기 등)을 등록해 쓴다 — **주문 장착 칸 4개를 대신함**(추천안). 인벤토리는 **고정 칸 20칸**. 빠른 칸에 없는 주문도 **인벤토리에서 바로 사용**(추천안). 등록은 **항목을 누르면 "등록하기"가 나오고, 빈 빠른 칸을 누르면 인벤토리가 열려 등록할 항목을 고르게 유도**
  - 정한 것 (Claude ✎): ① 인벤토리 창은 I 키, 한 줄 5칸 × 4줄, 방향키 · 마우스로 고르고 Enter · 클릭 = 메뉴(사용 · 등록하기 · 등록 해제(등록돼 있을 때) · 닫기), 숫자 키 = 고른 항목을 그 빠른 칸에 바로 등록, Esc = 한 단계 뒤로, 창 밖 클릭 = 한 단계 뒤로. 열려 있는 동안 차례가 가지 않음. ② 같은 주문은 인벤토리에 한 번만, 아이템은 여러 개(겹치지 않음). ③ 한 항목은 빠른 칸 하나에만 — 다른 칸에 등록하면 원래 칸이 비워짐. 인벤토리에서 빼면 가리키던 빠른 칸도 비움(같은 것이 남아 있으면 그대로). ④ 빠른 칸 등록은 차례를 쓰지 않는다(틱 밖 변경 — 디버그 창과 같은 방식). ⑤ 융합기 "사용"은 3-2b 전까지 "아직 쓸 수 없다" 안내만. ⑥ 시작 주문이 있으면 인벤토리 · 빠른 칸 앞쪽부터(테스트용, 실제 게임은 없음)
  - 구현: `Simulation/Items/Inventory.cs` — `InventoryKind`(None · Spell · Item) · `InventoryRef`(종류 + ID) · `ItemDef`/`ItemBook`(`ItemUse.Fusion`) · `Inventory.TryAdd` · `RemoveAt` · `AssignQuickSlot` · `QuickSlotOf` · `InventoryChangedEvent` · `QuickSlotsChangedEvent`. `Unit.SpellSlots` → `InventorySlots` · `QuickSlots` · `QuickSlotSpell` · `HasSpell`. `SpellSlots.Assign` · `SpellSlotsChangedEvent` 삭제. `MagicConfig.SpellSlotCount` → `QuickSlotCount`(6) + `InventorySlotCount`(20). `SpellCasting.CheckSpell` · `TryCreateCastSpell`(인벤토리에 있는 주문 ID로), `CastBlock.NotASpell` · `NotOwned`. 스냅샷 **v8**(유닛마다 인벤토리 · 빠른 칸). 표시: `ItemCatalogAsset`(`Data/Items/ItemCatalog.asset` — 마법 융합기), `HudText` 아이템 이름 · 인벤토리 안내, `HudModel.QuickSlots`, `HudPresenter` 빠른 칸 상자 6개(실행 중 생성 · 클릭 판정 `QuickSlotAt` · 강조), `InventoryMenu`(창 상태 — 엔진 없이 테스트), `InventoryPresenter`(가운데 창 · 칸 · 메뉴, 클릭 판정). `PlayerInputController` — 빠른 칸 키 · 클릭, I 키, 조준을 주문 ID로. 디버그 창 "[상태 변경] 주문 장착 칸" → "[상태 변경] 인벤토리 · 빠른 칸"(주문 · 아이템 넣기 · 빼기, 빠른 칸마다 고르기). Setup: HUD `Slots` 글줄 → `QuickSlots` 줄 + `Inventory` 패널
  - 테스트 12개 추가 (`InventoryTests` — 시작 주문 배치, 넣기(앞 칸 · 주문 한 번 · 아이템 여러 개 · 꽉 참 · 모르는 ID), 빠른 칸 한 칸만 · 인벤토리에 있어야 함, 빼면 빠른 칸 비움, 스냅샷 v8 왕복, 인벤토리에서 바로 시전 · 아이템 칸은 NotASpell, 창: 격자 이동 · 주문 사용 · 등록하기 → 숫자 · 빈 칸 열기 → 바로 등록 · 아이템 사용 · Esc 단계, 아이템 에셋 검산). 기존 테스트는 빠른 칸 API로 옮김 — 전체 186개 통과
  - 확인: 편집 모드에서 판 시작 → 디버그 경로로 물 · 불 · 융합기를 넣고 칸 1 · 6에 등록 → HUD 빠른 칸 상자 6개("1 물보라 2MP" … "6 마법 융합기"), 인벤토리 창 20칸 · 메뉴 4줄이 그려짐
  - **확인 못 한 것:** 플레이 모드에서 키 · 마우스 조작 전부(I 키, 방향키 이동, 메뉴 클릭, 빈 빠른 칸 클릭 → 등록, 인벤토리에서 바로 사용 → 조준), 창 배치 · 글자 크기
  - 확인 방법: `Main.unity` 플레이 → 디버그 창 "[상태 변경] 인벤토리 · 빠른 칸"에서 water_basic · fire_basic · fusion_device를 "넣기" → 화면의 빈 빠른 칸 1을 클릭(또는 1 키) → 인벤토리가 열리면 물보라를 골라 Enter → 칸 1에 등록됨. I 키 → 불꽃 화살 칸에서 Enter → "사용" → 바로 조준. 융합기 → "등록하기" → 6 키 → 칸 6에 등록, 6을 누르면 "아직 쓸 수 없다" 안내
- [x] **3-2b. 마법 융합기 · 융합 3종** (2026-10-17) — 플레이 확인 대기 — 불+물 화염 지대 · 물+독 오염된 물 · 불+번개 폭발 (얼음+물 빙판은 3-3). 인벤토리에서 융합기 사용 → 주문 두 개 고르기 (REACTIONS.md 5번 F1~F5 · 3-2b 결정)
  - 결정: REACTIONS.md 5번 F1~F5 + 3-2b 착수 결정(얼음 + 물은 3-3 · 피해 = 두 주문 합 + 표의 효과만 · 사거리 = 모양을 정한 주문 것). 수치 BALANCE 10번
  - 정한 것 (Claude ✎): ① 모양이 같으면 첫 주문의 모양 · 사거리 · 속성. ② 융합 결과의 속성 = 모양을 정한 주문의 속성(반응을 안 일으켜 지금은 영향 없음 — 속성 저항 · 약점이 생기면 "융합 주문의 속성"을 따로 정할 것). ③ 표시 이름 용암 분출 · 오염 물결 · 폭발 마법. ④ 융합기 "사용"은 차례를 쓰지 않고 인벤토리를 "주문 두 개 고르기"로 다시 연다 — 주문 칸만 고를 수 있음(아이템 · 빈 칸은 어둡게), 첫째는 파랑, 같은 칸 다시 · Esc = 첫째 취소, 한 번 더 Esc = 닫기(3-2a "Esc = 한 단계 뒤로"에 맞춤). ⑤ 두 주문을 고르면 대상과 무관한 판정(융합기 · 같은 주문 · 가진 주문 · 조합 · 마나)을 먼저 보고 막히면 안내만, 통과하면 융합 조준(일반 주문과 같은 조준 · 미리보기 · Tab · Esc)
  - 구현 — Simulation (커밋 52eae7d WIP): `Magic/Fusion.cs` — `FusionDef`(순서 무관 `Matches`) · `FusionBook`(같은 속성 · 같은 ID · 같은 조합 · 없는 지형/상태 거부, `Build`) · `FusionCasting`(`HasFusionDevice` · `Check` · `TryCreateCast`, 플레이어만). `SpellDef.TerrainDurationTicks` · `TriggersReactions`, `CastBlock.NoFusionDevice` · `NoFusionRecipe` · `SameSpell`(끝에 추가), `SpellCasting.CheckDef`, `CastSpellCommand(SpellDef)` 생성자, `TerrainEffects.Place(…, durationTicks)`
  - 구현 — Runtime: `Data/FusionCatalogAsset`(`Data/Fusions/FusionCatalog.asset`, 상태 확률 0%면 OnValidate 경고), `GameRoot._fusionCatalog` → `FusionBook` · 융합 이름을 `HudText`에(`Spell()`이 융합 ID도 찾음 → 로그 "용암 분출 시전"), `PlayerInputController` — 융합기 사용 → `InventoryMenu.OpenForFusion` → `FuseSpells` → 융합 조준(`FusionCasting.Check` 미리보기 · `TryCreateCast` 시전), 막힘 이유 한글(`HudText.CastBlockReason` 3개 · `FusionBlocked`), 3-2a의 "아직 쓸 수 없다" 안내 삭제. `InventoryMenu.Cancel` 융합 단계, `InventoryPresenter` 융합 제목 · 첫 칸 강조. Setup 메뉴: FusionCatalog 생성(3종, 멱등) · GameRoot 연결
  - 테스트 11개 추가 (`FusionTests` — 표 검사(같은 속성 · 순서만 다른 조합 · 같은 ID · 없는 지형), `Build` 합산 · 큰 모양 · 순서 무관 · 없는 조합, 막힘 이유 순서(같은 주문 · 없는 주문 · 조합 없음 · 자기 칸 · 사거리 · 마나 · 융합기 없음), 불 + 물 화염 지대 6틱 · 물 타일 · 젖음 없음 · 행동 1회, 물 + 독 오염된 물 8틱 · 중독 없음, 불 + 번개 관통 8 · 중독된 적 · 독구름이어도 반응 없음, 지형 지속 재정의, 창: 주문만 고름 · 같은 칸 취소 · 둘째 → FuseSpells, Esc 단계, 융합 이름, BALANCE 10번 검산) — 전체 197개 통과
  - 확인: 이 PC(새로 clone)에서 Unity MCP로 컴파일 · Setup 메뉴 실행 · 테스트 197개 통과. `FusionCatalog.asset` 3줄 저장 확인. 씬 변경은 GameRoot `_fusionCatalog` 참조 한 줄. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 플레이 모드 전체 — 융합기 사용 → 인벤토리 고르기 화면(제목 · 파랑 칸 · 어두운 칸) · 융합 조준 미리보기 · 시전 · 로그 · 화염 지대 6턴. 3-2a 플레이 확인도 아직
  - 확인 방법: `Main.unity` 플레이 → 디버그 창 "[상태 변경] 인벤토리 · 빠른 칸"에서 fire_basic · water_basic · poison_basic · lightning_basic · fusion_device 넣기 → 융합기를 빠른 칸 6에 등록 → 6 키 → 인벤토리 제목 "마법 융합 — 주문 두 개 고르기" → 불꽃 화살 Enter(파랑) → 물보라 Enter → 노란 안내 "용암 분출 (불꽃 화살 + 물보라, 5 MP)" · 3×3 미리보기 → Space → 로그 "용암 분출 시전" · 3×3 주황 칸이 6턴. 불 + 얼음을 고르면 "불꽃 화살 + 얼음 화살: 융합할 수 없는 조합". 불 + 번개는 직선 미리보기 · 적마다 8. 마나는 "[상태 변경] 마나"로 채운다
  - 참고: 이 PC는 새로 clone한 상태였음 — 원격에 `phase3-2b-fusion`(a6f2469 "중간 저장" = .meta 2개 + 진행 상황 메모)이 있어 그 위에서 이어감
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

# CLAUDE.md

## 프로젝트 개요
턴제 탑다운 그리드 로그라이크. 7개 기본 속성(불 · 얼음 · 번개 · 독 · 대지 · 바람 · 물)과 희귀한 시간 마법을 마나로 사용하고,
**상태 × 속성 규칙**으로 마법끼리 연결되는 전투가 핵심이다. 기본 게임 구조는 Shattered Pixel Dungeon을 참고한다.

- 기획은 `GDD.md`, 작업 순서와 작업 일지는 `TASKS.md`, 수치는 `BALANCE.md`(첫 수치가 생길 때 만든다).
- 원본 기획서: `마법시스템_기획서_v0.3.md` (GDD.md로 옮겨 정리함. 이후 기준은 GDD.md)
- 방법론: `claude_all_type_genre.md` (장르 무관 협업 가이드)
- 수치는 BALANCE.md와 데이터 에셋 기준이며 코드에 임의로 넣지 않는다.

## 참조 문서
| 문서 | 역할 | 상태 |
|---|---|---|
| CLAUDE.md | AI 작업 규칙 (이 문서) | 작성 |
| GDD.md | 기획서 | 작성 (v0.3 기반) |
| TASKS.md | 작업 순서 + 작업 일지 | 작성 |
| BALANCE.md | 수치 · 검산 · 실험 | 첫 수치가 생길 때 |
| REACTIONS.md | 상태/지형 × 속성 규칙 표 상세 설계 | 사용자 속성표 받은 뒤 |
| TIME_MAGIC.md | 시간 마법 상세 설계 | Phase 8 착수 전 |

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
| 렌더 파이프라인 | **URP 17.6.0 + 2D Renderer** (`Assets/_Project/Settings/Rendering/URP2D.asset`) | 확정 2026-10-14, 변경 금지 |
| 그래픽 방식 | **2D 스프라이트 + 고각도 아트** (2.5D 느낌은 아트로, 픽셀 전환은 스프라이트 교체로) | 확정 2026-10-14, 변경 금지 |
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
- 그레이박스 타일 · 스프라이트는 `Assets/_Project/Art/Greybox/`. 메인 씬은 `Assets/_Project/Scenes/Main.unity` (빌드 씬 유일 항목).

## 코딩 규칙
- 네임스페이스: `ProjectM.Core`, `ProjectM.Magic`, `ProjectM.Status`, `ProjectM.Units`, `ProjectM.Dungeon`, `ProjectM.Presentation`, `ProjectM.UI`, `ProjectM.Editor`
- 이름: 클래스 · 메서드 · 프로퍼티 PascalCase, private 필드 `_camelCase`, 지역 변수 camelCase
- 게임 로직과 엔진 컴포넌트를 분리한다. 규칙은 순수 C#으로 쓰고, MonoBehaviour는 얇은 어댑터(이벤트 구독 · 표시 · 입력 전달)만 맡는다.
- 수치(피해 · 사거리 · 지속 틱 · 마나 · 확률)는 데이터 에셋에 둔다.
- `Update`에서 규칙을 돌리지 않는다. 시뮬레이션은 틱으로만 진행한다. `Find`/`GetComponent`는 캐시한다.
- 결합은 C# event/Action으로 낮추고 "바뀐 것만" 알린다.
- enum은 끝에만 추가한다 (에셋이 정수로 저장함).
- 바뀔 수 있는 판정(시야, 사거리, 전이 경로, 반응 규칙)은 인터페이스 뒤에 둔다.
- 마법서 · 아이템 효과는 한 조회 지점(예: `SpellModifiers`)에서 읽고, 시뮬레이션 · 미리보기 UI · 연출이 같은 값을 쓴다.
- 싱글턴 + `DontDestroyOnLoad` 남발을 피한다. 순수 로직은 생성자 주입, 어댑터는 씬 컴포지션 루트 하나에서 연결한다.

## 핵심 설계 규칙 (변경 금지)
바꾸고 싶어지면 영향 범위를 표로 정리하고 사용자 결정을 받은 뒤에만 바꾼다. 근거는 GDD 2~3장.
1. **시뮬레이션과 표현 분리.** 전투 · 상태 · 지형 · 마나 규칙은 Transform · 카메라 · 충돌체 · Tilemap을 모른다. 로직은 이벤트(`FireSpawned(3,4)`, `StatusApplied(Wet)`)를 발행하고 표현 레이어가 이를 3D 이펙트 또는 스프라이트로 바꾼다.
2. **2D 그리드 (x, y), 고도 없음.** 시야 · 사거리 · 물 흐름 · 전이 경로는 그리드 계산으로만 한다. 레이캐스트 · 물리를 쓰지 않는다. 고도가 필요해지면 타일의 선택 속성 하나로 분리한다.
3. **틱 파이프라인 하나.** 상태 지속 · 지형 지속 · 마나 회복이 모두 같은 틱 시스템을 따른다. 틱 순서는 고정하고 문서(GDD 3-3)에 적는다.
4. **시계 교체 방식.** 규칙은 "1틱 진행"만 받는다. 턴제는 플레이어 행동 확정 시, 제한해제는 1초마다 1틱. 모드별로 마법 · 상태 · AI를 따로 구현하지 않는다. deltaTime 누적이 아닌 고정 간격 틱 스케줄러 + 커맨드 큐.
5. **모든 지속시간은 틱 단위.** 모든 행동에 행동 비용(틱) 필드를 둔다 (초기값 전부 1).
6. **상태는 직렬화 가능한 순수 데이터.** 유닛 · 타일 · 상태 · 지형 · 마나를 스냅샷 한 번으로 저장 · 복원할 수 있어야 한다 (시간 마법 · 세이브 · 회귀 테스트 공용).
7. **시드 난수 주입.** `UnityEngine.Random`, `System.Random` 직접 생성 금지. 난수 인터페이스를 주입하고 용도별로 분리한다 (전투 ≠ 맵 생성 ≠ 연출).
8. **모든 엔티티에 고유 ID.** 참조는 ID로 한다.
9. **시너지는 규칙 표.** "A마법 + B마법"을 하드코딩하지 않는다. 마법은 속성과 상태/지형 태그를 부여하고, 반응은 `상태/지형 × 속성 → 결과` 데이터 표로 정의한다.
10. **조합 안전장치.** 조합 추가 피해 상한, 전이 피해 < 첫 대상 피해, 주문당 발동 횟수 · 대상 수 상한, 동일 상태 중첩은 "지속 갱신" 또는 "강도 증가" 중 하나만, 플레이어도 지형 · 상태 영향을 받는다. 조합으로 인한 추가 행동은 금지.
11. **속성은 7개.** 변형(관통 · 추적 · 연쇄 등)은 속성이 아니라 마법서 시스템이 맡는다. 비전 계열 없음.
12. **마법 자원은 마나.**

## 아트 · 그래픽 규칙
- 고각도 2.5D로 시작하되 2D 픽셀로 바꿀 수 있어야 한다. 데이터는 비주얼 에셋을 **ID로만** 참조하고, 표현 레이어 교체만으로 전환한다.
- 재미 검증(Phase 4) 전에는 그래픽 작업을 하지 않는다. 그레이박스 = 색만 다른 단색 타일 · 도형.
- 상태 · 지형 표시는 타일 오버레이와 상태 아이콘 데이터로 정의한다 (2.5D/2D 공용).
- 연출은 로직을 기다리지 않는다. 로직은 즉시 계산하고 애니메이션이 따라오며, 실시간 모드에서는 1초 안에 끝나도록 연출 시간을 줄이거나 건너뛸 수 있어야 한다.

## Git · 환경
- 한 항목 = 한 커밋. 메시지: `MM.DD/Phase-항목/PascalCase요약` (예: `10.14/1-2/TickScheduler`), 수정은 `…Fix`.
- 커밋 전 `git status`로 의도하지 않은 변경(줄바꿈만 바뀐 설정 파일, 플레이 중 저장된 설정)을 확인한다.
- 1인 개발. 여러 PC에서 작업할 때 pull 후 에디터에 열려 있던 씬은 디스크에서 다시 연다.
- 커밋 전 `git diff --cached --stat`으로 스테이징 내용을 확인한다 (미리 스테이징된 변경이 섞이지 않게).
- 에셋 이동 · 삭제는 Unity MCP(`AssetDatabase.MoveAsset/DeleteAsset`)가 확인 창 때문에 막힌다 → `git mv`/`git rm`으로 `.meta`와 함께 처리하고 Refresh.
- `Reference/`(Assets 밖)는 참고용 원본 코드 보관소. Unity가 컴파일하지 않는다.
- Unity MCP로 긴 작업이 시간 초과돼도 에디터에서는 계속 진행된다 → 콘솔 로그로 결과를 확인한다.

# TASKS.md — 작업 순서 + 작업 일지

> 한 번에 한 항목. 위에서부터 진행한다. 기획은 GDD.md, 규칙은 CLAUDE.md.
> 상태: `[ ]` 대기 / `[~]` 진행 중 / `[x] … — 플레이 확인 대기` / `[x] … — 플레이 확인 완료`
> 커밋: 한 항목 = 한 커밋, `MM.DD/Phase-항목/PascalCase요약`

## 진행 요약 (2026-10-23 기준 — 새 세션은 여기부터 읽는다)

**최신 (2026-10-23): Phase P — 그래픽 3D → 2D 픽셀(SPD식) 전환 중. 맵(바닥 · 얇은 벽 RuleTile · 흐릿한 안개) · 유닛(주인공 걷기 · 적 3종 · 보스) 2D 완료(플레이 확인 대기), 남은 것 = 바닥 아이템 · 이펙트 · 3D 정리. 작업 브랜치 `phaseP-2d`(원격 푸시, main 병합 전 — 갑옷 `phase3-armor` · `phaseP-pixel` 포함). 답 대기: 한 칸 기둥도 칸 안에만 그릴지. 인수인계 요약은 `Docs/진행상황.md`.**

**최신 (2026-10-20): 3-9 물약 · 주문서(SPD) — 3-9a 아트 · 3-9b/c 종류 · 식별 · 배치 · 효과 15종 · 던지기 구현 완료, 브랜치 `phase3-consumables`(main 병합 전) · 테스트 실행 · 플레이 확인 대기. 남은 SPD 효과(신속 · 투명 · 부양 · 정신 시야 / 저주 해제 · 분신 · 공포 · 자장가 · 변환)는 다음 항목.**

**현재 위치: Phase G (3D 그래픽) 일시중지 — G-1 · G-2a · G-2b · G-3 완료 · main 병합 (G-3 `240833e` · 플레이 확인 완료 2026-10-19). G-4 UI 아트는 에셋이 없어 보류 (결정 2026-10-19, 사용자).**
**진행 중: 3-7 장비(3-7a 무기 기본 · 3-7b 힘 · 강화 · 3-7c 기습 · 방어) — 구현 완료 · main 병합 (2026-10-19) · 테스트 실행(사용자) · 플레이 확인 대기.** 3-7a 무기 기본 — 구현 완료 · main 병합 (2026-10-19) · 테스트 실행(사용자) · 플레이 확인 대기. 그다음 3-7c 기습 · 방어.** 3-6 경험치 · 레벨업 · 명중 · 회피 (2026-10-19 사용자 요청, SPD 참고) — 구현 완료 · main 병합 (2026-10-19, 걷기 수정 · 빌드 마젠타 수정 · 무기 모델 6종 가져오기와 함께) · 테스트 실행(사용자) · 플레이 확인 대기.** 3-5 마법서는 main 병합 · 플레이 확인 대기. 장비 시스템 준비: 무기 데이터 `Docs/장비_데이터.md` · 에셋 `Docs/무기_에셋.md`(모델 6개 받음, 들고 있는 모습 = (가) 지팡이 숨김). 그다음 작업은 미정 — 새 세션은 사용자에게 먼저 묻는다. 후보 (추천 순): ① 밀린 확인 — EditMode 테스트 272개 실행(3-3a 이후 한 번도 안 돎, 사용자가 직접) ② **Phase 4 재미 검증**(4-1 30분 플레이 → 진단) ③ 이펙트 다듬기(ART_ASSETS 13번 "남은 것") ④ G-4 재개 — UI 에셋(ART_ASSETS 9번: 상태 아이콘 6 · 주문 아이콘 5 · 융합기 1 · 막대 · 칸 틀 · 로그 바탕 · 인벤토리 창 틀)을 받은 뒤. 인수인계 요약은 `Docs/진행상황.md`

**그래픽 결정 (2026-10-18, 사용자):** 그래픽 작업을 Phase 4 재미 검증보다 먼저 한다 · 픽셀 아트 대신 **Varco 3D 모델** · 화면에 올리는 방식 = **A. 실시간 3D**(추천안 — B는 3D 모델을 2.5D 스프라이트로 미리 렌더). CLAUDE.md 기술 스택(URP 2D → Universal 3D, 2D 스프라이트 → 3D 모델) · 아트 규칙을 바꿈. 에셋 목록 · 규격 `Docs/ART_ASSETS.md`

**테스트 실행 (결정 2026-10-18, 사용자):** 사용자가 다시 지시할 때까지 Claude는 테스트를 **실행하지 않는다** — 테스트 코드 · 검산은 작성하고, 실행 · 플레이 확인은 사용자가 직접 한다. 컴파일 오류 확인 · Setup 메뉴 실행은 한다.

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
| | 3-2a 인벤토리(20칸) · 빠른 칸 6개(주문 장착 칸을 대신함) · 마법 융합기 아이템 · 인벤토리에서 바로 시전 | 완료 (플레이 확인 대기) | 1153244 (main 병합) |
| | 3-2b 마법 융합기 · 융합 3종(용암 분출 · 오염 물결 · 폭발 마법) | 완료 (플레이 확인 대기) | 52eae7d ~ 992646f (main 병합) |
| | 3-3a 얼음 계열 — 빙결 · 결빙 · 빙판 · 해동 · 파쇄 + 얼음 + 물 융합(빙결 파도) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 2060d7a (main 병합) |
| | 3-3b 번개 계열 — 감전(사슬, 인원 제한 없음) · 수면 감전(8방향 8칸 · 4명) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 5ead4e0 (main 병합) |
| | 3-4a 층 이동 — 5층(5층 = 보스 층) · 내려가는 · 올라가는 계단 · 떠난 층 멈춤 보존 · 상태 전부 유지 · 적 재생성(30틱 · 5마리) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | f0b2c09 ~ 84b84bb (main 병합) |
| | 3-4b 결과 화면(승리 · 패배, 도달 층 · 처치 · 턴 · 시드 · 빠른 칸 · 장비 자리 · 사망 원인) · 5층 보스 방 + 임시 보스(60 · 5 · 10) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 792e0dd (main 병합) |
| | 3-5 마법서 — 한 판에 속성별 한 권씩 1~4층에 층당 1~2권(1층 ≥ 1) · 걸어 들어가면 줍기 · 사용 = 주문 배움 + 빈 빠른 칸 등록 · 스냅샷 v11 | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 71f2e08 (main 병합) |
| | 3-6 경험치 · 레벨업(SPD — 필요 5 + 5 × 레벨 · 최대 30 · 체력 +5 · 마나 +1 · 명중 · 회피 +1 · 적 최고 레벨 넘으면 0) · 근접 명중 · 회피(SPD 굴림, 주문은 항상 명중, 행동 불가 = 회피 0) · HUD 경험치 막대 · 스냅샷 v12 | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 4400aaf (main 병합) |
| | 3-7 장비 — 무기 6종(1/3 배율 · 굴림 · 창 2칸/2틱 · 손에 듦) · 힘(시작 10 · 요구 12) · 힘 물약 · 강화 주문서(무기 자체에, 요구 힘 첫 −1 뒤 2당 −1) · 기습(항상 명중 · 더크) · 방어(육척봉 0~2) · 스냅샷 v15 | 구현 완료 (테스트 실행 · 플레이 확인 대기) | a6ae0a6 ~ 8e35812 (main 병합) |
| | G-2c 플레이어 애니메이션 — Mixamo Idle · Walk를 양 마법사에 Humanoid 리타깃, 평소 Idle · 이동 중 Walk | 플레이 확인 완료 (테스트 실행 대기) | a880442 (main 병합) |
| G 3D 그래픽 | G-1 URP Universal(3D) 전환 · 3D 그레이박스(눕힌 바닥 Tilemap · 벽 블록 · 캡슐 유닛 · 고각도 카메라 · 화면 → 바닥 → 칸) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | main 병합 |
| | G-2a Varco 모델 연결 — 양 마법사 + 지팡이 · 근접 적 3변형(토끼 · 쥐 · 슬라임) · 슬라임 소녀 보스 · 바닥 · 벽 · 기둥(외톨이 벽) · 계단 2 | 구현 완료 (테스트 실행 · 플레이 확인 대기) | 763bb72 (main 병합) |
| | G-2b 코드 움직임 — 이동 보간 + 통통 · 공격 찌르기 · 피격 번쩍 + 흔들림 · 시전 통통 · 사망 가라앉기 · 대기 숨쉬기 · 카메라 부드럽게 따라가기 · 계단 위 서는 높이 · Grid 칸 두께 0 (G-1 버그) | 구현 완료 (테스트 실행 · 플레이 확인 대기) | d716ccf (main 병합) |
| | G-3 이펙트 — 가져온 팩(Hovl · Eric) URP 변환 · 지형 루프 · 유닛 상태 루프 · 주문(지팡이 빛 · 투사체 · 번개 선 · 맞는 효과) · 반응 · 감전 사슬 · 바닥 무늬 5 · 조준 데칼 4 · Bloom | 플레이 확인 완료 (테스트 실행 대기) | 240833e (main 병합) |

**숫자:** EditMode 테스트 **341개 전부 통과** (2026-10-20, 3-8 · 대각선 수정까지 — Claude가 사용자 지시로 실행) · 콘솔 프로젝트 오류 0 · 씬 1개(`Assets/_Project/Scenes/Main.unity`) · 스냅샷 형식 **v15** (3-7c: 무기 기습 · 방어)

**지금 플레이하면:** 매번 새 시드로 50×50 층 + 플레이어(초록) + 적 5마리(빨강). 방향키/WASD/넘패드로 한 칸씩(대각선 = W+D처럼 두 키 함께, 넘패드 7·9·1·3), 클릭하면 경로 따라 이동, 적에게 부딪히면 공격. **안 본 칸은 검정, 본 적 있는 칸은 어둡게, 지금 보이는 칸(반지름 8, 벽에 가려짐)만 밝게.** 보이지 않는 적은 안 그려진다. 적은 벽 너머를 못 보고(반지름 6), 플레이어를 못 봤으면 맵을 배회하고, 보면 쫓아오고(붙어 있으면 플레이어 발자국을 그대로 밟음), 놓치면 마지막으로 본 칸까지 가 본다. 왼쪽 위 HUD에 빨강 HP · 파랑 MP 막대(숫자 포함, MP는 3턴마다 1 회복, HP는 10턴마다 1 자동 회복) · 내 상태 아이콘(색 칸 + 남은 턴) · 주문 칸 4개(한글 이름 + 마나 비용, 모자라면 빨강) · 노란 안내 줄, 왼쪽 아래에 한글 전투 로그 8줄(같은 상태 갱신은 생략). 글꼴은 Galmuri11. **HP · MP 아래 빠른 칸 6개(주문 · 아이템). 처음엔 모두 비어 있음 — 주문 · 아이템은 디버그 창(메뉴 ProjectM/Debug/Debug Window) "[상태 변경] 인벤토리 · 빠른 칸"에서 넣는다. I 키 = 인벤토리(20칸, 방향키 · 마우스로 고르고 Enter/클릭 → 사용 · 등록하기 · 등록 해제), 빈 빠른 칸을 누르면 인벤토리가 열려 그 칸에 등록할 항목을 고른다.** 숫자 키 1~6 · 칸 클릭으로 주문 조준(가까운 적 자동 선택, Tab 다음 적, 방향키 · 마우스로 조준 칸 이동, 경로 노랑 · 맞는 칸 주황 미리보기) → Space/Enter/클릭으로 시전, Esc 취소. 불 = 단일 5 + 화상 70%(3턴 · 1), 얼음 = 단일 3 + 둔화 70%(3턴 · 행동 2턴), 번개 = 직선 관통 3 + 마비 30%(2턴 행동 불가), 독 = 3×3 폭발 2 + 중독 70%(5턴 · 1, 나도 맞음) + 독구름 3×3 4턴(그 위 유닛 매 턴 중독). 불 단독으로는 화염 지대가 생기지 않음(불 + 물 융합으로만, 3-2 이후). **반응:** 중독된 적에 불 = 독화(+2), 번개 = 유독성 폭발(남은 중독 턴만큼 주변 3×3, 나도 맞을 수 있음), 독구름에 불 = 독화, 번개 = 가스 폭발(구름 사라지고 폭발 2 + 중독 70%). 로그에 "유독성 폭발! 적 #2"처럼 나옴. 걸린 적은 색이 바뀜, 내 상태는 HUD 상태 아이콘. **물 (3-2):** 디버그 창에서 water_basic(물보라)을 넣으면 3×3에 피해 없이 젖음 + 물 타일(파랑, 12턴, 위에 있으면 계속 젖음 · 화염 지대 칸엔 안 깔림). 젖은 적이나 물 칸에 불 = 증발(+2, 젖음 · 물 제거), 물 칸에 독 = 오염(탁한 초록, 위에 있으면 매 턴 피해 1 + 젖음). 젖은 적은 파랑 틴트. **융합 (3-2b):** 디버그 창에서 fusion_device와 주문들을 넣고 융합기를 사용(빠른 칸 또는 인벤토리 "사용") → 인벤토리 제목이 "마법 융합 — 주문 두 개 고르기"로 바뀌고 주문 두 개를 고르면(첫째는 파랑 칸, 같은 칸 다시 · Esc = 첫째 취소) 융합 조준 → 시전. 불 + 물 = 용암 분출(3×3 피해 5 + 화염 지대 6턴), 물 + 독 = 오염 물결(3×3 피해 2 + 오염된 물 8턴), 불 + 번개 = 폭발 마법(직선 관통 8). 마나는 두 주문 합, 행동 1회, 반응 없음, 두 주문의 상태는 안 걸림. 조합이 없으면 "융합할 수 없는 조합" 안내. **얼음 (3-3a):** 젖은 적에 얼음 = 빙결(2턴 행동 불가, 흰 틴트), 물 칸에 얼음 = 결빙(물 → 빙판 8턴, 밝은 하늘색, 위에 있으면 매 턴 둔화 + 서 있던 유닛 빙결), 빙결된 적에 불 = 해동(+1), 번개 = 파쇄(+3), 빙판에 불 = 다시 물(12턴). 얼음 + 물 융합 = 빙결 파도(3×3 피해 3 + 맞은 적 빙결 + 빙판 8턴). 빙결 면역 없음. **번개 (3-3b):** 젖은 적에 번개 = 감전(+2, 3칸 안 젖은 유닛으로 사슬처럼 계속 번져 각 2 — 젖은 나도 맞음), 물 · 오염된 물 칸에 번개 = 수면 감전(이어진 물 칸 8칸까지, 그 위 유닛 4명에게 1). **층 (3-4a):** 파랑 칸(내려가는 계단)으로 걸어가면 안내 줄 "내려가는 계단 — Enter/이 칸 클릭으로 내려가기", 그 위에서 Enter 또는 자기 칸 클릭 = 다음 층(턴 안 씀, 로그 "2층으로 내려왔다"). 도착한 청록 칸(올라가는 계단) 위에서 Enter = 이전 층 — 떠났던 층은 적 · 지형 · 탐험 기록이 그대로. 체력 · 마나 · 인벤토리 · 상태 이상은 그대로 가져감. 5층까지. **5층 (3-4b):** 큰 방 하나, 반대쪽 끝에 자주색 보스(체력 60 · 피해 5)만 — 쓰러뜨리면 승리. 30턴마다 안 보이는 곳에 적 1마리가 새로 생김(층당 5마리까지, 5층 제외). **판이 끝나면(승리 · 사망) 결과 화면** — 도달 층 · 처치 · 턴 · 시드 · 빠른 칸 · 장비(없음) · 사망 원인, R로 새 판.

**융합 마법 결정 (2026-10-17, REACTIONS.md 5번 F1~F5):** 마도구 "마법 융합기"가 있어야 씀(아이템 `fusion_device`, 3-2a) · **인벤토리에서 융합기를 사용 → 인벤토리에서 주문 두 개 고르기**(변경 2026-10-17) · 마나 합 · 행동 1회(규칙 10 예외로 명시) · 모양은 두 주문 중 큰 쪽(Bolt < Beam < Burst) · 융합 결과는 반응 안 일으킴 · 적은 안 씀. 인벤토리 · 빠른 칸을 먼저(**3-2a**), 융합은 **3-2b**. 3-2b 결정: 얼음 + 물은 3-3 · 지형 융합 피해 = 두 주문 합 + 표의 효과만 · 사거리 = 모양을 정한 주문 것.

**3-3 범위 (결정 2026-10-18, 사용자 답):** **3-3a 얼음 계열**(빙결 · 결빙 · 빙판 · 해동 · 파쇄 · 오염된 물 결빙 · 얼음 + 물 융합) → **3-3b 번개 계열**(감전 전이 100% · 피해 50% · 수면 감전 · 오염된 물 × 번개). 불 + 독(독화)은 3-1에서 끝남. 빙결 면역은 **삭제**. 감전 전이는 **젖은 플레이어에게도 간다**(GDD Q7)

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
- [x] **3-3a. 얼음 계열 · 얼음 + 물 융합** (2026-10-18) — 테스트 실행 · 플레이 확인 대기 — 3-3 "대표 시너지 3종"(물+번개 · 물+얼음 · 불+독)을 나눔: 불+독은 3-1 독화로 끝, 물+얼음 = 3-3a, 물+번개 = 3-3b
  - 결정 (2026-10-18, 사용자 답): 3-3을 **3-3a 얼음 → 3-3b 번개**로 나눔 — 추천안. 빙결 면역(풀린 뒤 2틱) **삭제** — 사용자 선택(추천안은 면역을 효과 없는 상태로 두기였음). 감전 전이는 **젖은 플레이어에게도 간다**(GDD Q7) — 추천안, 3-3b에서 구현. 테스트 실행은 사용자가 직접(지시 전까지 Claude는 실행하지 않음)
  - 정한 것 (Claude ✎): ① 결빙(#13)의 우선순위는 원본 "0 (+빙결 30)"을 한 줄로 합쳐 30. ② 빙결 3줄(젖음 · 물 타일 · 오염된 물 × 얼음)은 한 그룹 — 물 위 젖은 적은 빙결 · 로그 한 번, 물은 빙판으로. 해동 2줄(빙결 · 빙판 × 불)도 한 그룹. ③ 오염된 물 × 얼음 = 물 타일과 같은 결빙(원본 "동일 ✎"). ④ 빙결 틴트는 흰빛 하늘색, 틴트 우선순위 맨 앞. 빙판 오버레이는 밝은 하늘색. ⑤ 얼음 + 물 융합 이름 "빙결 파도" — Burst 3×3 · 피해 3 · 마나 4 · 맞은 대상 빙결 100% · 빙판 8틱. ⑥ 빙결된 대상에 얼음을 쓰면 얼음 주문의 둔화 판정도 그대로 들어감(순서: 반응 → 새 상태)
  - 구현 — Simulation: `StatusType.Frozen`(6) · `TerrainEffectType.IcePatch`(5) — 끝에 추가, 스냅샷 v8 그대로. `ReactionDef.TargetStatus` · `TargetStatusChancePercent`(대상 · 그 칸 유닛에 상태) · `TransformDurationTicks`(변환 뒤 지속, 0 = 유지) + 검사(지속만 있고 변환 없음 · 확률 범위 · StatusBook에 없는 상태). `ReactionEngine` — 대상 상태 적용(추가 피해 다음), 대상 상태를 거는 지형 줄은 유닛이 있으면 "지형만 바꾸는 줄"이 아님. `TerrainEffects.Transform(…, durationTicks)`
  - 구현 — Runtime: `ReactionCatalogAsset` 항목 필드 3개, `HudText` "빙결", `EntityPresenter` 빙결 틴트, `TerrainOverlayPresenter` 빙판 색. Setup 메뉴: StatusCatalog 빙결 · TerrainCatalog 빙판 · ReactionCatalog 6줄 · FusionCatalog 얼음 + 물 (없을 때만 추가)
  - 테스트 (작성만, 실행 안 함): `IceTests` 10개 — 빙결 · 풀린 뒤 다시 빙결(면역 없음) · 해동 · 파쇄 · 물 위 젖은 적 결빙(그룹 1회 + 빙판 8틱) · 물 위 안 젖은 적 결빙 · 빈 물 · 오염된 물 칸 결빙 · 빙판 해동 → 물 12틱 · 빙판 둔화 · 잘못된 반응 거부. `FusionTests.IceWater_FreezesTargets_LeavesIcePatch` 1개. 검산 표 갱신: `SpellTests.BalanceStatuses`(빙결) · `BalanceTerrains`(빙판) · `ReactionTests.BalanceReactions`(6줄, 검산에 변환 지속 · 대상 상태 비교 추가) · `FusionTests.BalanceFusions`(4종) · Status/Terrain 검산 종류 목록
  - 확인: Unity 컴파일 오류 0 · Setup 메뉴 실행 → 에셋 4개 갱신(StatusCatalog · TerrainCatalog · ReactionCatalog · FusionCatalog) · 씬은 빙판 색 필드 한 줄. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드 전체(빙결 틴트 · 빙판 색 · 로그 "빙결! 적 #N" · 빙결된 적이 멈추는지)
  - 확인 방법: Test Runner EditMode 전체 실행(208개 예상). `Main.unity` 플레이 → 디버그 창에서 water_basic · ice_basic · fire_basic · lightning_basic · fusion_device 넣기 → 물보라로 적 무리를 적시고 웅덩이 → 얼음 화살로 적 → 로그 "빙결! 적 #N", 적이 흰빛으로 2턴 멈춤 · 그 칸 웅덩이가 밝은 하늘색 빙판 → 번개로 "파쇄!"(3 + 3) 또는 불로 "해동!"(5 + 1). 빈 웅덩이 칸에 얼음 → 빙판, 빙판에 불 → 다시 파랑 물. 융합기로 얼음 + 물 → "빙결 파도 시전", 3×3 빙판 · 맞은 적 빙결
- [x] **3-3b. 번개 계열** (2026-10-18) — 테스트 실행 · 플레이 확인 대기 — 감전 · 수면 감전 · 오염된 물 × 번개. 전이는 젖은 플레이어에게도 (Q7 결정 2026-10-18)
  - 결정 (2026-10-18, 사용자 답): 감전 전이 = **사슬, 인원 제한 없음**(맞은 적 3칸 안 젖은 유닛 모두 → 거기서 다시 3칸 …) — 사용자 선택(추천안은 사슬 · 최대 4명). 규칙 10 "대상 수 상한"의 예외로 CLAUDE.md에 적음. 전이 피해 = **첫 대상이 받은 피해 × 50% 내림** — 추천안. 수면 감전 물 연결 = **8방향** — 추천안
  - 정한 것 (Claude ✎): REACTIONS.md 3번 3-3b 아래 ①~⑤ — 감전 3줄 한 그룹(젖은 유닛이 물 위면 감전 하나) · 직접 맞은 유닛은 전이 피해 없음(사슬은 거쳐 감) · 전이는 대상당 1회(최댓값) · 같은 웅덩이는 한 번만 퍼짐 · 같은 지형만 따라감
  - 구현 — Simulation: `ReactionSpread`(None · StatusChain · TerrainFlood) + `ReactionDef.Spread` · `SpreadRange` · `SpreadMaxTargets` · `SpreadDamagePercent`(1 ~ 99, 사슬은 상태 반응 · 지형 전이는 지형 반응만, 소모와 함께 못 씀). `ReactionEngine` — 추가 피해 계산을 한 번만, 전이 대상 모으기(`ChainTargets` 너비 우선 · `FloodTargets` 8방향 너비 우선 · 지나간 칸 기록), 폭발 다음에 대상당 1회 적용. 전이가 있는 지형 줄은 "지형만 바꾸는 줄"이 아님
  - 구현 — Runtime: `ReactionCatalogAsset` 항목 필드 4개. Setup 메뉴: ReactionCatalog 감전 3줄(없을 때만)
  - 테스트 (작성만, 실행 안 함): `ShockTests` 9개 — 사슬 · 인원 제한 없음 · 마른 유닛 제외, 4칸 끊김, 젖은 시전자, 직접 맞은 적끼리 전이 없음, 수면 감전 4명 · 1, 8칸에서 멈춤, 오염된 물, 물 위 젖은 적은 감전만, 잘못된 전이 정의 거부. `ReactionTests.BalanceReactions` 3줄 + 검산에 전이 필드 비교
  - 확인: Unity 컴파일 오류 0 · Setup 메뉴 실행(ReactionCatalog 갱신). 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드(로그 "감전! 적 #N" · 사슬 피해 숫자 · 내가 젖었을 때 자기 피해). 전이 피해는 따로 로그 줄이 없고 피해 줄("적 #N에게 피해 2")로만 보임
  - 확인 방법: Test Runner EditMode 전체(217개 예상). `Main.unity` 플레이 → 디버그 창에서 water_basic · lightning_basic 넣기 → 물보라로 적 무리를 적심 → 번개 줄기로 한 마리 → 로그 "감전! 적 #N", 맞은 적 3 + 2, 3칸 안에 이어진 젖은 적들 2씩. 빈 물웅덩이에 번개 → "수면 감전!", 웅덩이와 이어진 물 위 적 4명까지 1씩. 내가 물 위(젖음)에 서서 3칸 안의 젖은 적에게 번개를 쓰면 나도 2
- [x] **3-4a. 층 이동 · 적 재생성** (2026-10-18) — 테스트 실행 · 플레이 확인 대기 — 3-4 "한 판 완성"을 나눔: 3-4a 층 이동 → 3-4b 결과 화면 · 임시 보스
  - 결정 (2026-10-18, 사용자 답): 런 = **5층 단위로 던전 테마가 바뀜, 지금은 5층까지** — 1~4층 일반, 5층 보스 맵(사용자 선택, 추천안은 3층). 계단 = **언제든, 계단 칸에 들어가는 행동으로 이동**(추천안) + **이전 층으로 돌아갈 수 있고 그 층은 그대로 남음**(사용자 추가). 다음 층으로 **전부 유지(상태 이상도)**(사용자 선택, 추천안은 상태 해제). 떠난 층의 시간 = **멈춤**(추천안). 보스 층 = **임시 보스 + 처치 = 승리**(추천안, 3-4b). 작업 = **3-4a 층 이동 → 3-4b 결과 화면 · 보스**(추천안). 결과 화면 = 도달 층 · 처치 수 · 턴 수 · 시드 · 빠른 칸 등록 요소 · 장착 장비(지금은 자리만 — 추천안) · 사망 원인(3-4b). **적 재생성 = 30틱마다 1마리**(사용자 선택, 추천안은 50) · **시야 밖 바닥 칸 무작위** · **층당 5마리까지**(추천안) · **보스 층 제외**(추천안)
  - 변경 (2026-10-18, 사용자 요청 — 첫 커밋 f0b2c09 뒤): **계단 칸으로는 걸어가기만 하고, 계단 위에서 한 번 더 눌러야 이동. 계단 이동은 틱을 쓰지 않음.** "한 번 더" = Enter(넘패드 Enter) 또는 서 있는 칸 클릭 ✎(Space · . · 넘패드 5는 그대로 대기). 계단 위에 서면 안내 줄에 "내려가는 계단 — Enter/이 칸 클릭으로 내려가기". 구현: `FloorTransitionSystem`(⑥ 틱 시스템) → `FloorTransition`(틱 밖 `TryUse` · `StairsDirection`, 파일 이름만 바꿔 GUID 유지), `MoveCommand`의 층 이동 요청 · `World.PendingFloorChange` 삭제, `PlayerInputController` Enter · 자기 칸 클릭 · 계단 안내, `HudText.StairsHint`. 테스트 2개 추가(걸어가기만으로는 층 안 바뀜 · 사용해도 틱 · 플레이어 차례 그대로, 계단 아닌 곳에서 사용 = 아무 일 없음) — FloorTests 12개
  - 정한 것 (Claude ✎): ① 도착 칸 = 내려가면 그 층의 올라가는 계단(= 그 층 시작 칸), 올라가면 내려가는 계단. 다른 유닛이 서 있으면 가장 가까운 빈 칸. 도착하면 계단 위이므로 바로 Enter로 되돌아갈 수 있음. ② (변경 전: 계단에 들어간 틱 ⑥ 끝에 처리) → 층 이동은 틱 사이에 즉시, 적은 그동안 행동하지 않음. ③ 떠난 층의 적 행동 순서는 "떠날 때 남은 틱"으로 저장해 돌아오면 이어서. ④ 처음 가는 층의 적 5마리 배치는 1층과 같은 규칙(시작 칸에서 8칸 이상). ⑤ 재생성 난수는 새 용도 `RngStream.Spawn` — 재생성이 다음 층 모양을 바꾸지 않게. ⑥ 올라가는 계단 색 = 청록(그레이박스, `Data/Visuals/StairsUpTile.asset`). ⑦ 마지막 층(5층)은 3-4b 전까지 일반 층 생성 + 내려가는 계단 없음 + 처음 적 5마리 + 재생성 없음 (임시)
  - 구현 — Simulation: `TileType.StairsUp`(4, 걸을 수 있음) · `GridMap.Find`. `DungeonGenerator.Generate(rng, stairsUp, stairsDown)`(계단은 난수 안 씀 — 1층 결과 그대로). `DungeonGenConfig.FloorCount` · `SpawnConfig.RespawnIntervalTicks` · `RespawnMaxEnemies`(선택 인자). `RngStream.Spawn`(4). `World` — `Map` · `Explored` · `Terrain`을 층마다 바꿈, `Floor` · `StoredFloors` · `PendingFloorChange`(틱 안에서만) · `StoreCurrentFloor` · `EnterNewFloor` · `RestoreStoredFloor` · `FindStoredFloor`. `UnitRegistry.Teleport`(발자국 지움). `MoveCommand` — 플레이어가 계단 칸에 들어가면 층 이동 요청. `Dungeon/FloorTransitionSystem`(⑥ 끝: 저장 → 생성 · 복원 → 도착 → MapLoaded · 지형 · 유닛 · `FloorChangedEvent` → 시야). `Units/EnemyRespawnSystem`(⑤). `FloorPopulator.PopulateEnemies`. 스냅샷 **v9** — `Floor` · `StoredFloorState[]`(층 · 맵 · 탐험 · 지형 · 유닛 · 행위자 남은 틱), 코덱은 공용 조각으로 나눔(v8 부분 바이트 순서 그대로)
  - 구현 — Runtime: `GameRoot`가 두 시스템 등록(층 수 · 재생성 수치 · 적 데이터 · 시야), 1층은 계단 옵션으로 생성. `HudModel` 층 이동 로그(`HudText.FloorChanged`). `PlayerInputController` — 층이 바뀌면 클릭 경로 · 조준 취소. 에셋: `DungeonGenConfig._floorCount` 5, `SpawnConfig._respawnIntervalTicks` 30 · `_respawnMaxEnemies` 5. Setup 메뉴: 올라가는 계단 타일 생성 · TileVisualSet 연결
  - 테스트 (작성만, 실행 안 함): `FloorTests` 10개 — 내려가기(새 층 · 올라가는 계단 도착 · 체력 · 마나 · 상태 유지 · 1층 적 저장), 올라갔다 다시(적 ID · 위치 · 지형 남은 틱 그대로, 2층 재생성 안 함), 같은 시드 같은 2층, 1층은 계단 옵션과 무관하게 같은 맵, 마지막 층 계단 없음, 스냅샷 v9 왕복(바이트 동일), 층 로그 문구, 가장 가까운 빈 칸, 재생성(주기 · 시야 밖 · 상한), 보스 층 재생성 없음 · 같은 시드 같은 자리. 검산 갱신: `DungeonGeneratorTests`(층 수) · `UnitCombatTests`(재생성 2개) · `InventoryTests`(스냅샷 v9)
  - 확인: Unity 컴파일 오류 0 · Setup 메뉴 실행(StairsUpTile · TileVisualSet) · 설정 에셋 2개에 새 필드 저장. 콘솔 프로젝트 오류 0. (저장 중 `Galmuri11 SDF.asset`에 동적 글자 데이터가 써져 되돌림)
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드 전체(계단 이동 · 화면 전환 · 카메라 · 안개 · 돌아왔을 때 적 틴트 · 재생성 체감). 디버그 창에는 지금 층 번호가 아직 없음
  - 확인 방법: Test Runner EditMode 전체(229개 예상). `Main.unity` 플레이 → 디버그 창 "맵 전체 밝히기"로 파랑 계단을 찾아 걸어가기 → 층은 그대로, 안내 줄 "내려가는 계단 — Enter/…" → Enter(또는 자기 칸 클릭) → 로그 "2층으로 내려왔다", 새 맵 · 발밑 청록 계단, 적이 움직이지 않음(턴 안 씀) → 그 자리에서 Enter → "1층으로 올라왔다", 떠날 때의 적 · 안개 기록이 그대로 → 다시 2층 → 같은 2층. 한 층에서 30턴쯤 기다리면 안 보이는 곳에서 적이 하나씩 생김(최대 5). 5층에는 파랑 계단이 없음
- [x] **3-4b. 결과 화면 · 임시 보스** (2026-10-18) — 테스트 실행 · 플레이 확인 대기 — 5층 보스 맵(큰 방 + 임시 보스, 처치 = 승리) · 사망 · 승리 결과 화면(도달 층 · 처치 수 · 턴 수 · 시드 · 빠른 칸 등록 요소 · 장착 장비(자리만) · 사망 원인) · R 새 판
  - 결정 (2026-10-18, 사용자 답): 임시 보스 **HP 60 · 피해 5 · 시야 10**(사용자 선택, 추천안은 40 · 4) · 보스 맵 = **큰 방 하나**(추천안) · 보스 층에는 **보스만**(추천안)
  - 정한 것 (Claude ✎): ① 보스 방 21 × 15(맵 가운데) · 기둥 4개(방을 가로 · 세로 4등분한 점) · 왼쪽 끝 가운데 올라가는 계단 · 오른쪽 끝 가운데 보스 — 난수 안 씀. ② 보스 AI = 근접 적과 같음(쫓아와 공격). 색 = 짙은 자주색 원. ③ 승패는 ⑥ 사망 판정에서 — 플레이어 사망 = 패배, 보스 사망 = 승리, 한 번만. 같은 틱에 둘 다면 패배(EntityId 순). ④ 사망 원인 = 플레이어가 **마지막으로 받은 피해**의 출처 — "적 #N의 공격" · "보스의 공격" · "내 불꽃 화살" · "유독성 폭발 (내 주문의 반응)" · "화상" · "오염된 물" 등. ⑤ 처치 수 = 죽은 적 전부(보스 포함, 폭발 · 지형으로 죽은 것도). 도달 층 = 가 본 가장 깊은 층, 턴 수 = 지금 틱. ⑥ 결과 화면 = 화면 전체 어두운 덮개 + 가운데 상자, 승리 금색 · 패배 빨강 제목, 판이 끝나면 입력 멈춤 · R로 새 판. 장비 줄은 "없음"(Phase 6)
  - 구현 — Simulation: `Core/RunState.cs` — `DamageKind` · `DamageCause`(공격 · 주문 · 반응 · 상태 · 지형) · `RunOutcome` · `RunState`(처치 수 · 결과 · 보스 ID · 마지막 피해). `World.Run` · `DeepestFloor`. `Combat.ApplyDamage(…, cause)` — 플레이어 피해면 기록, `UnitDamagedEvent.Cause`. 피해 지점 5곳에 출처(근접 · 주문 · 반응 추가 피해 · 폭발 · 전이 · 상태 · 지형). `DeathCheckSystem` — 처치 수 · 승패 → `RunEndedEvent`. `DungeonGenConfig.BossRoomWidth · Height` · `DungeonGenerator.GenerateBossRoom`. `FloorTransition(…, bossDef)` — 마지막 층 = 보스 방 + 보스(`Run.SetBoss`). 스냅샷 **v10**(한 판 기록)
  - 구현 — Runtime: `UI/ResultModel`(순수 — 승패 · 층 · 처치 · 턴 · 시드 · 빠른 칸 · 장비 · 사망 원인, 본문 문구) · `UI/ResultPresenter`(그리기만). `HudText` — 결과 제목 · 지형 이름 · `DeathCause`. `GameRoot._bossDef` · `_result`, `RunEndedEvent` → 입력 멈춤 · 결과 화면, R = 승리 · 패배 모두 새 판. 에셋 `Data/Units/Boss.asset`, `DungeonGenConfig` 보스 방 21 × 15. Setup 메뉴: Boss.asset · 보스 모습(자주색) · HUD `Result` 패널(덮개 · 상자 · 제목 · 본문) · 연결
  - 테스트 (작성만, 실행 안 함): `BossTests` 8개 — 보스 층(큰 방 21 × 15 − 기둥 4 · 올라가는 계단 · 보스만 · 수치), 보스 처치 = 승리 한 번 · 처치 수, 사망 = 패배 · 마지막 피해가 원인(공격 뒤 화상 → "화상"), 적 처치는 수만 셈, 사망 원인 문구 6종, 결과 본문(승리), 스냅샷 v10 왕복, 에셋 검산(Boss.asset · 보스 방). 버전 검사 9 → 10 (`InventoryTests` · `FloorTests`)
  - 확인: Unity 컴파일 오류 0 · Setup 메뉴 실행(Boss.asset · 보스 모습 · 결과 패널 — 씬에 Result 오브젝트 추가). 콘솔 프로젝트 오류 0. `Galmuri11 SDF.asset` 동적 글자 변경은 다시 되돌림 → CLAUDE.md Git 절에 적음
  - 피드백: Setup 메뉴 · SaveAssets 뒤 폰트 에셋(`Galmuri11 SDF.asset`)에 동적 글자 데이터가 써지는 일이 3-4a · 3-4b 두 번 — 원인: 동적 SDF 폰트가 에디터에서 글자를 추가하면 에셋이 바뀜. 해결: 커밋 전 되돌림. 검사: `git status`에 이 파일이 있으면 되돌린다 (CLAUDE.md에 추가)
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드 전체(보스전 체감 · 결과 화면 배치 · 글자 크기 · R 재시작). 보스 난이도는 재미 검증(4-1)에서 조정
  - 확인 방법: Test Runner EditMode 전체(237개 예상). `Main.unity` 플레이 → 디버그 창 "맵 전체 밝히기" + 계단으로 5층까지(각 층 파랑 계단 위에서 Enter) → 큰 방 · 오른쪽 끝 자주색 보스 → 이기면 금색 "승리 — 보스를 쓰러뜨렸다" + 기록, 지면 빨강 "패배 — 쓰러졌다" + "사망 원인: …" → R로 새 판(결과 화면 사라짐)

- [x] **3-5. 마법서** (2026-10-19) — 테스트 실행 · 플레이 확인 대기 — 맵에서 속성 마법서를 줍고 인벤토리에서 사용하면 그 주문을 배운다 (사용자 요청 2026-10-19)
  - 결정 (2026-10-19, 사용자 — 모두 추천안): ① 배치 = 한 판에 속성별 마법서 **한 권씩**(5권) 시드로 섞어 **1~4층**(보스 층 제외)에 층당 1~2권 ② 줍기 = **걸어 들어가면 자동**(차례 추가 소모 없음, 꽉 차면 바닥에 남고 안내) ③ 사용 = **책 소모 · 주문 배움 · 첫 빈 빠른 칸 자동 등록**, 차례 안 씀 ④ 시작 = **빈손 + 1층에 최소 1권**
  - 정한 것: 수치는 `LootConfig.asset`(층당 최대 2 · 시작 칸에서 3칸 이상, BALANCE 11번). 난수는 `RngStream.Loot`만 — 맵 · 적 배치(MapGen)는 그대로. 계획(아직 안 간 층 몫)은 `RunState.PendingFloorItems`, 바닥 아이템은 `World.FloorItems`(지금 층, 떠난 층에 함께 보존). 이미 아는 주문의 책은 사용해도 그대로 남음("이미 알고 있는 주문"). 책 3D 모델이 없어 그레이박스 납작한 상자(속성 색) + 반짝임 루프로 떠서 돎
  - 구현 — Simulation: `Items/Spellbooks.cs` — `LootConfig`, `FloorItem` · `FloorItemLayer`, `Loot`(`PlanSpellbooks` · `PlaceFloorItems`), `ItemPickupSystem`(⑤, 걸어 들어가면 줍기), `Spellbooks.TryLearn`(`LearnResult`), 이벤트 `FloorItemPlacedEvent` · `ItemPickedUpEvent` · `ItemPickupFailedEvent` · `SpellLearnedEvent`. `ItemUse.LearnSpell` + `ItemDef.SpellId`. `World.FloorItems` · `RunState.PendingFloorItems`. `FloorTransition`: 새 일반 층은 MapLoaded 뒤에 그 층 몫을 놓고, 돌아온 층은 남은 책을 다시 알림 (`loot` 인자, 없으면 놓지 않음)
  - 스냅샷 **v11** = 지금 층 바닥 아이템 · 떠난 층 바닥 아이템 · 배치 계획
  - 구현 — Runtime: `Data/LootConfigAsset`, `ItemCatalogAsset.Entry.SpellId`, `Presentation/FloorItemPresenter`(책 상자 · 반짝임, 보이는 칸만, 주우면 지움), `PlayerInputController.UseItem`(마법서 → `Spellbooks.TryLearn`, 틱 밖 변경), `HudModel` 로그("불꽃 화살 마법서를 주웠다" · "불꽃 화살을 배웠다 → [1]" · "인벤토리가 가득 차 … 줍지 못했다"는 같은 칸에서 한 번만), `HudText.Eul`(받침에 맞춘 을/를), `GameRoot`(`_lootConfig` · `_floorItems`, 판 시작에 계획 + 1층 배치, ItemPickupSystem 등록)
  - 구현 — Editor: Main Scene 메뉴 = ItemCatalog에 마법서 5 · `LootConfig.asset` · 씬 `FloorItems` 오브젝트 · GameRoot 연결, FX 메뉴 = `item.spellbook_*` 줄(책 색 · 반짝임) · FloorItemPresenter에 FxCatalog 연결
  - 테스트 13개 작성 · 기존 4곳 고침, **실행 안 함 (사용자가 직접)**: `SpellbookTests` — 배치 계획(시드 30개: 5권 · 속성별 한 권 · 1~4층 · 층당 1~2 · 1층 ≥ 1), 같은 시드 같은 배치 · 맵 불변, 바닥 칸 · 시작에서 3칸 이상 · 이벤트, 걸어 들어가면 줍기(1틱), 꽉 차면 남김 · 실패 알림 · 비면 그 자리에서 줍기, 사용 → 책 소모 · 주문 · 첫 빈 빠른 칸, 빠른 칸이 꽉 차도 배움, 이미 아는 주문 · 마법서 아님, `ItemDef` 검사, 새 층에 그 층 몫 · 떠난 층 책 보존, 스냅샷 v11 왕복 바이트 동일, 에셋 값(BALANCE 9 · 11번). 고침: 스냅샷 버전 10 → 11(BossTests · FloorTests · InventoryTests), 아이템 수 1 → 6(InventoryTests)
  - 확인: 컴파일 오류 0 · Main Scene → FX 메뉴 실행(아이템 6 · LootConfig · GameRoot · FloorItems 연결 확인). 콘솔 새 오류 없음
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드 전체(책이 보이는지 · 줍기 · 사용 · 로그 · 층 이동 뒤 책 유지)
  - 확인 방법: Test Runner EditMode 전체(272개 예상, 새 `SpellbookTests` 13개). `Main.unity` 플레이 → 1층 어딘가에 속성 색 책이 떠서 돎(디버그 창 "맵 전체 밝히기"로 위치 확인) → 그 칸으로 걸어가면 로그 "…마법서를 주웠다" → I 인벤토리 → 책 선택 → 사용 → 로그 "…을(를) 배웠다 → [1]", 빠른 칸 1에 주문 → 숫자 키로 바로 조준. 2~4층에도 1~2권씩, 5층(보스)에는 없음. 아래층에 갔다 와도 안 주운 책은 그대로
  - 피드백: 테스트에서 NUnit `Does.Not.Contain`은 문자열 전용이라 배열 검사는 `Has.No.Member`로 (컴파일 오류로 발견)

- [x] **3-6. 경험치 · 레벨업 · 명중 · 회피** (2026-10-19) — 테스트 실행 · 플레이 확인 대기 — Shattered Pixel Dungeon 방식을 참고 (사용자 요청 2026-10-19). SPD는 GPLv3라 코드는 옮기지 않고 규칙 · 수치만 참고해 새로 씀 (원본 `Hero.java` · `Char.java` · `Mob.java`에서 공식 확인)
  - 결정 (2026-10-19, 사용자): ① 레벨업 보상 = **최대 체력 +5 · 최대 마나 +1 · 명중 · 회피 +1** (현재 체력 · 마나도 같이 오름) ② 필요 경험치 = **SPD 공식 5 + 5 × 레벨**, 최대 레벨 30 ③ 반복 사냥 방지 = **적마다 최고 레벨(SPD maxLvl)** — 플레이어 레벨이 넘으면 경험치 0 ④ **명중 · 회피 도입** — 2026-10-16 "항상 명중"(근접)을 바꿈 ⑤ **주문은 항상 명중** (추천안 — SPD 완드도 안 빗나감, 반응 예측 유지) ⑥ **행동 불가(마비 · 빙결) 대상은 회피 0** (추천안, SPD 마비)
  - 정한 것: 판정 = SPD `Char.hit` — 명중 굴림(0~공격자 명중) ≥ 회피 굴림(0~대상 회피)이면 맞음, 난수 `RngStream.Combat`. 회피 0이면 굴리지 않음(테스트 · 기존 수치에서 난수 소비 불변). 경험치는 적이 죽으면(누가 죽였든, ⑥ 사망 판정) 살아 있는 플레이어에게 — SPD와 같음. 최대 레벨에서 남는 경험치는 버림(SPD는 축복 버프 — 없음). 수치는 SPD 영웅 · 쥐 · 구(Goo): 플레이어 명중 10 · 회피 5, 근접 적 8 · 2 · 경험치 1 · 최고 레벨 5, 보스 10 · 8 · 경험치 10 · 최고 레벨 30
  - 구현 — Simulation: `Units/Progression.cs` — `ProgressionConfig`(`ExpToNext`), `Progression`(`AwardKill` · `GainExp` · 레벨업), 이벤트 `ExpGainedEvent` · `LevelUpEvent`. `Combat.RollHit` + `AttackMissedEvent`, `AttackCommand`가 빗나가면 피해 없이 이벤트. `TickScheduler.CanAct`(행동 규칙 공개). `Unit` · `UnitDef` + 명중 · 회피 · 처치 경험치 · 경험치 최고 레벨 · 레벨 · 경험치 (`MaxHp` · `MaxMana`는 레벨업으로 바뀜). `DeathCheckSystem`(경험치), `World.InstallCoreSystems(…, progression)` — null이면 경험치 없음(테스트 기본)
  - 스냅샷 **v12** = 유닛 명중 · 회피 · 처치 경험치 · 최고 레벨 · 레벨 · 경험치
  - 구현 — Runtime: `Data/ProgressionConfigAsset`, `UnitDefAsset` 필드 4개, `GameRoot._progressionConfig`, HUD 경험치 막대(MP 아래 얇은 노란 막대 "Lv 2  EXP 2/15", 최대면 "Lv 30 (최대)") · 로그("적 #3에게 빗나감" · "적 #3의 공격: 빗나감" · "경험치 +1" · "레벨 2! 최대 HP 25 · MP 11") — 문구는 `HudText`. `EntityPresenter` 빗나가도 찌르기(피격 번쩍 없음)
  - 구현 — Editor: Main Scene 메뉴 = `Data/Units/ProgressionConfig.asset`(없을 때만) · 유닛 에셋 새 필드(0인 칸만 채움 `FillZeroInts`) · GameRoot · HUD `XpBar` 연결, 빠른 칸 · 안내 줄을 32 px 아래로
  - 테스트 작성, **실행 안 함 (사용자가 직접)**: `ProgressionTests` 13개 — SPD 공식, 레벨업 수치 · 현재 값, 여러 레벨 한 번에 · 남는 경험치, 최대 레벨, 적 사망 → 경험치, 최고 레벨 넘으면 0, 설정 없으면 0, 회피 0 = 난수 안 씀, 빗나감 이벤트 · 피해 없음, 마비 대상 회피 0, 영웅 vs 쥐 명중률(1000회 — 기대 90% · 69%), 스냅샷 v12 왕복, 에셋 값(BALANCE 3 · 12번). `HudModelTests` +2 (경험치 막대 · 레벨업 로그, 빗나감 로그). 고침: 스냅샷 버전 11 → 12(BossTests · FloorTests · InventoryTests)
  - 확인: 컴파일 오류 0 · Main Scene → Varco Models → FX 메뉴 실행, ProgressionConfig(30 · 10 · 5 · 1 · 1 · 1) · 유닛 에셋(플레이어 10/5 · 적 8/2/1/5 · 보스 10/8/10/30) · HUD XpBar 연결 확인
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드 전체(빗나감 빈도 체감 · 경험치 막대 · 레벨업 로그 · 층 이동 뒤 레벨 유지)
  - **바뀐 기존 결과:** 근접 공격이 이제 빗나간다 — BALANCE 3번 "적을 3대 · 10대"는 명중했을 때 기준. 기존 테스트는 유닛 회피 0(UnitDef 기본값)이라 그대로 항상 맞음
  - 확인 방법: Test Runner EditMode 전체(새 `ProgressionTests` 13 · `HudModelTests` +2). `Main.unity` 플레이 → 왼쪽 위 MP 아래 노란 막대 "Lv 1  EXP 0/10" → 적을 근접 공격하면 가끔 "빗나감" 로그(플레이어 약 90%, 적 약 69% 명중) → 적을 잡으면 "경험치 +1" · 막대 증가 → 10마리면 "레벨 2! 최대 HP 25 · MP 11", HP · MP 막대 최대치 증가. 마비 · 빙결된 적은 항상 맞음. 레벨 6부터는 근접 적이 경험치를 주지 않음

- [x] **3-7. 장비 — 근접 무기 6종** (2026-10-19 사용자 요청, SPD 2티어 참고) — 마법서처럼 맵에 배치. 데이터 · 결정 · 수치는 `Docs/장비_데이터.md`, 에셋은 `Docs/무기_에셋.md`(모델 6개 받음)
  - 결정 (2026-10-19, 사용자): ① 무기 피해 = 최소~최대 굴림(맨손 고정 3 · 주문 고정 그대로) ② 수치 = 사용자 값에 **1/3 배율**(비율 유지) ③ 창 공격 2틱 ④ 창 2칸 = 사이 칸이 비어 있을 때만 ⑤ 기습 = 적이 플레이어를 못 본 상태 → 항상 명중 + 더크 최소 피해 상승 ⑥ 방어 굴림 도입(근접만, 육척봉 0~2) ⑦ **강화 주문서 같이**(추천은 보류) ⑧ **힘 도입**(추천은 생략) — 시작 10 · 요구 12 · 부족 1당 명중 ÷ 1.5 · 남는 힘 1당 보너스 0~1 ⑨ 장착 = 인벤토리에서 사용, 차례 1, 장비 칸 1개, 맨손 시작 · 한 판에 무기 2 · 강화 주문서 3 · 힘 물약 2 (1~4층) · 들고 있는 모습 (가) 지팡이 숨김. ①~⑥ · ⑨는 추천안
  - 나눔: **3-7a** 무기 기본(데이터 · 장착 · 굴림 · 명중 배수 · 창 2칸/2틱 · 배치 · HUD · 바닥 · 손) → **3-7b** 힘 · 강화(힘 물약 · 강화 주문서) → **3-7c** 기습 · 방어 굴림
- [x] **3-7a. 무기 기본** (2026-10-19) — 테스트 실행 · 플레이 확인 대기
  - 정한 것 (Claude, 위 결정 안에서): 무기 전투 수치(최소 · 최대 · 명중 % · 비용 · 거리)는 **장착할 때 계산해 유닛에 둔다**(`Unit.Weapon` = `EquippedWeapon`, 스냅샷에 저장) — 공격 판정이 아이템 표를 몰라도 되고, 3-7b 강화는 다시 계산해 넣으면 됨. 사용자 값(SPD 크기)은 ItemCatalog에 그대로, 나누기 3 · 최소 기본 2는 카탈로그 공통 값. 반올림은 0.5 올림. 창 2칸은 직선 · 대각선만(사이 칸이 정해지는 칸). 클릭으로 보이는 적을 누르면 지금 자리에서 닿을 때 바로 공격(창 2칸 포함), 키보드는 지금처럼 옆 칸. 벗기 동작은 없음(다른 무기로 바꾸기만). 무기 배치는 책 계획 뒤에 붙여 책 · 맵 · 적 배치 불변. 손: 모두 칼처럼 앞으로 70° 눕혀 든다 (처음엔 한 손 무기만 기울이고 창 · 육척봉은 세웠으나 사용자 요청으로 모두 같게, 2026-10-19 — 긴 무기는 쥐는 곳 0.30)
  - 구현 — Simulation: `Units/EquippedWeapon`, `Items/Weapons.cs` — `WeaponDef` · `WeaponRules` · `Weapons`(`Resolve` · `Equip`) · `EquipWeaponCommand`(차례 1) · `WeaponEquippedEvent`. `ItemUse.EquipWeapon = 3` · `ItemDef.Weapon` · `ItemBook.WeaponRules`. `Units/Combat.cs` `MeleeWeapon`(`RollDamage` · `AccuracyPercentOf` · `AttackCost` · `CanReach`), `Combat.RollHit`에 명중 배수, `AttackCommand`(거리 · 굴림), `PlayerActions.TryAttackAt`(클릭) · 공격 비용 = 무기. `Loot.PlanWeapons`(`LootConfig.WeaponsPerRun`, `RngStream.Loot`) · `RunState.AddPendingFloorItems`
  - 스냅샷 **v13** = 유닛이 든 무기
  - 구현 — Runtime: `ItemCatalogAsset` 무기 칸(최대 피해 · 강화 효율 · 명중 · 비용 · 거리) + 공통(나누기 · 최소 기본), `LootConfigAsset._weaponsPerRun`, `GameRoot`(무기 계획), `PlayerInputController`(무기 사용 = 장착 명령 · 인벤토리 닫음, 보이는 적 클릭 = 그 자리 공격), HUD 무기 줄("무기: 소검 1~5" · "무기: 맨손 3", 경험치 막대 오른쪽) · 로그("소검을 들었다"), 결과 화면 장비 = 든 무기, `Presentation/ItemVisualCatalog`(아이템 → 모델), `FloorItemPresenter`(모델이 있으면 눕혀 떠 돎 + 반짝임), `EntityPresenter.ShowHeld`(무기를 들면 손의 지팡이를 숨기고 `Weapon_<ID>`만 켬 — 결정 (가))
  - 구현 — Editor: Main Scene 메뉴 = ItemCatalog 무기 6줄 · LootConfig 무기 2 · HUD `Weapon` 글상자 · FloorItems에 ItemVisualCatalog. Varco Models 메뉴 = 무기 프리팹 6(`Prefabs/Varco/Weapon_*`) · `Data/Visuals/ItemVisualCatalog.asset` · 플레이어 프리팹 오른손에 무기 6개(꺼 둠). FX 메뉴 = `item.<무기>` 은빛 반짝임 6줄
  - 테스트 작성, **실행 안 함 (사용자가 직접)**: `WeaponTests` 24개(TestCase 9 포함) — 1/3 표 값, 명중 · 비용 · 거리 전달, 장착 차례 1 · 인벤토리에서 빠짐 · 이벤트, 바꾸면 이전 무기 인벤토리로, 무기 아님 = 그대로, 굴림 범위 · 맨손 고정, 명중 배수, 창 직선 · 대각선 2칸 · (1, 2) 불가 · 사이 유닛 · 사이 벽, 짧은 무기 2칸 불가, 창 2틱 · 이동 1틱, 무기 계획(시드 30개: 2개 · 다른 종 · 1~4층 · 책 계획 그대로 · 같은 시드 같은 계획), 0개면 없음, 스냅샷 v13 왕복, HUD 줄 · 로그 · 결과 화면, 에셋 값(BALANCE 11 · 13번). 고침: 스냅샷 버전 12 → 13(BossTests · FloorTests · InventoryTests), 아이템 수 6 → 12(InventoryTests)
  - 확인: 컴파일 오류 0 · Main Scene → Varco Models → FX 실행: 아이템 12 · 나누기 3 · 최소 2 · 6종 수치(1~5 · 1~4 · 1~7 · 1~4 · 1~4 · 1~7, 명중 132 · 68, 창 2틱 · 2칸) · 무기 배치 2 · ItemVisualCatalog 6 · 플레이어 손 무기 6(꺼 둠) · 지팡이 켜짐 · HUD 무기 줄 연결. 손에 든 모습을 정면 · 위(55°)에서 렌더해 확인
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드 전체(바닥 무기가 보이는지 · 줍기 · 장착 · 지팡이 숨김 · 창 2칸 클릭 공격 · 층 이동 뒤 무기 유지). 더크(0.25 m)는 위에서 거의 안 보임 — 길이는 `VarcoModelSetup.WeaponSpecs`
  - 확인 방법 (3-7a): Test Runner EditMode 전체(새 `WeaponTests`). `Main.unity` 플레이 → 1~4층 어딘가에 은빛으로 반짝이며 도는 무기 2개(디버그 창 "맵 전체 밝히기") → 걸어 들어가 줍기 → I 인벤토리 → 무기 선택 → 사용 → 로그 "…을 들었다", HUD "무기: 소검 1~5", 손의 지팡이가 무기로 바뀜, 차례 1 지남 → 적을 치면 피해가 1~5 사이로 바뀜. 창: 직선 · 대각선으로 한 칸 떨어진 적을 클릭하면 그 자리에서 공격(2틱 — 적이 두 번 움직임). 다른 무기를 들면 이전 무기는 인벤토리로. 사망 · 승리 결과 화면 "장비: 소검"
  - 수정 (2026-10-19, 사용자 플레이 피드백 — "무기가 안 보인다"): 원인 = 무기 2개를 1~4층에 무작위로 놓아 시드 1~6 중 5개는 1층에 무기가 없었음(표시 · 모델은 정상 — 에디터에서 바닥에 놓아 확인). 해결 = 첫 무기는 반드시 1층, 둘째는 무작위 (`Loot.PlanWeapons`). 검사 = `WeaponTests.PlanWeapons_*`(시드 30개 모두 1층에 1개)
- [x] **3-7b. 힘 · 강화** (2026-10-19) — 테스트 실행 · 플레이 확인 대기
  - 결정 (2026-10-19, 사용자): ① 강화는 **무기 자체에** (추천안 — 내려놓아도 인벤토리 칸에 "소검+2"로 남음) ② 힘 물약 · 강화 주문서 = **차례 1** (추천안) ③ 요구 힘은 강화하면 줄어든다 — **첫 강화에 −1, 그 뒤 2단계마다 −1** (사용자 지정, 추천은 12 고정이었음). 앞서 정한 것: 시작 힘 10 · 요구 12 · 부족 1당 명중 ÷ 1.5 · 남는 1당 보너스 0~1 · 한 판에 강화 주문서 3 · 힘 물약 2
  - 정한 것 (Claude): 든 무기 없이 강화 주문서를 쓰면 차례를 쓰지 않고 안내("든 무기가 없다"). 힘 효과는 무기를 들었을 때만(맨손은 그대로 — SPD도 무기 굴림에만). 보너스 굴림은 나누기 3을 적용하지 않음. 물약 · 주문서는 모델이 없어 바닥에서 책 상자(빨강 · 금색) + 반짝임. 주문서 · 물약 배치는 무기 계획 뒤에 붙여 책 · 무기 · 맵 · 적 배치 불변
  - 구현 — Simulation: `InventoryRef.Upgrade`(칸의 강화 단계, 다르면 다른 항목), `ItemUse.StrengthPotion = 4` · `UpgradeScroll = 5` · `ItemDef.Amount`, `WeaponDef.StrengthRequirement` · `Weapons.StrengthRequirement`(12 − ⌈강화/2⌉), `EquippedWeapon.Upgrade` · `StrengthRequirement`, `Unit.Strength` · `UnitDef.Strength`, `MeleeWeapon`(`StrengthDeficit` · `ExcessStrength` · `StrengthAccuracyFactor`, `RollDamage` 보너스), `Combat.RollHit`에 힘 배수, `Consumables`(`Check` · `Use`) · `UseConsumableCommand`(차례 1) · `StrengthChangedEvent` · `WeaponUpgradedEvent`, 장착 · 교체가 강화 단계를 유지, `Loot.PlanConsumables`(`LootConfig.UpgradeScrollsPerRun` · `StrengthPotionsPerRun`)
  - 스냅샷 **v14** = 유닛 힘 · 무기 강화 · 요구 힘 · 인벤토리 칸 강화 단계
  - 구현 — Runtime: `ItemCatalogAsset`(요구 힘 · Amount, `FillWeaponStrength`), `LootConfigAsset`(주문서 3 · 물약 2), `UnitDefAsset._strength`, `GameRoot`(소모품 계획), `PlayerInputController`(물약 · 주문서 사용 = 명령, 무기 없으면 안내), HUD 무기 줄 "무기: 소검+1 1~6 · 힘 10/11" · 로그("강화: 소검+1 (1~6, 요구 힘 11)" · "힘이 올랐다 (힘 11)"), 인벤토리 이름 "손도끼+2", 결과 화면 "소검+1"
  - 구현 — Editor: Main Scene 메뉴 = 물약 · 주문서 줄 · 무기 요구 힘 12(기존 줄 채움) · LootConfig 3 · 2 · 플레이어 힘 10. FX 메뉴 = `item.potion_strength`(빨강) · `item.scroll_upgrade`(금색)
  - 테스트 작성, **실행 안 함 (사용자가 직접)**: `StrengthUpgradeTests` 18개(TestCase 6 포함) — 요구 힘 표(0~5), 부족 2 = 명중 ÷ 2.25 · 힘 12 = 1, 남는 힘 보너스(최대 5 + 2), 맨손 영향 없음, 힘 물약 +1 · 차례 1 · 소모 · 이벤트, 무기 없이 주문서 = 쓰지 않음, 주문서 → 소검+1(1~6 · 요구 11), 바꿔도 강화가 무기에 남음 · 다시 들면 +1, 배치(주문서 3 · 물약 2 · 1~4층 · 같은 시드 같은 계획), 스냅샷 v14 왕복, HUD 줄 · 로그 · 인벤토리 이름 · 결과 화면, 에셋 값. 고침: 스냅샷 버전 13 → 14(BossTests · FloorTests · InventoryTests · WeaponTests), 아이템 수 12 → 14(InventoryTests)
  - 확인: 컴파일 오류 0 · Main Scene → Varco Models → FX 실행: 아이템 14 · 요구 힘 12(+3 → 10) · 물약 · 주문서 Amount 1 · 배치 무기 2 · 주문서 3 · 물약 2 · 플레이어 힘 10
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드 전체(물약 · 주문서가 바닥에 보이는지 · 사용 · HUD · 명중 벌점 체감)
  - 확인 방법: Test Runner EditMode 전체(새 `StrengthUpgradeTests`). `Main.unity` 플레이 → 1~4층에 빨간 상자(힘 물약 2) · 금색 상자(강화 주문서 3) → 주워서 I → 무기를 든 뒤 주문서 사용 → 로그 "강화: 소검+1 (1~6, 요구 힘 11)", HUD "무기: 소검+1 1~6 · 힘 10/11", 차례 1 → 물약 사용 → "힘이 올랐다 (힘 11)" → HUD 힘 11/11. 무기 없이 주문서를 쓰면 "든 무기가 없다" 안내만(차례 안 씀). 다른 무기로 바꾸면 인벤토리에 "소검+1"
- [x] **3-7c. 기습 · 방어** (2026-10-19) — 테스트 실행 · 플레이 확인 대기
  - 결정 (앞서 2026-10-19): 기습 = 적이 플레이어를 못 본 상태 → 항상 명중 + 더크 최소 피해 67% 상승 · 방어 굴림 도입(근접만, 육척봉 0~2)
  - 정한 것 (Claude): "못 본 상태" = 적 AI 기억(`LastSeenTargetPos`)이 없음 — AI가 플레이어를 보면 매 차례 채우고 쫓다 놓치면 비우므로 시야를 다시 계산하지 않음(결정론 · 상태 없음). 기습 항상 명중은 맨손 · 모든 무기 · 창 2칸 공격 모두. 플레이어가 치는 쪽만(적이 플레이어를 기습하지 않음). 방어는 든 무기 값(육척봉) — 갑옷이 생기면 `DefenseOf`에 더함. 방어 0이면 난수를 쓰지 않음(기존 결과 불변)
  - 구현 — Simulation: `WeaponDef` · `EquippedWeapon`에 `SurpriseMinPercent` · `Defense`, `MeleeWeapon`(`IsSurprise` · `SurpriseMin` · `DefenseOf` · `ApplyDefense`, `RollDamage(…, surprise)`), `AttackCommand`(기습이면 명중 판정 생략 + `SurpriseAttackEvent` → 피해 굴림 → 방어 굴림), `DamageBlockedEvent`
  - 스냅샷 **v15** = 무기 기습 % · 방어
  - 구현 — Runtime: `ItemCatalogAsset` 칸(기습 % · 방어, `FillWeaponSpecials`), HUD 무기 줄 "· 방어 0~2" · 로그("기습! 적 #3" · "방어: 1 막음" — 플레이어가 막은 것만). Editor: Main Scene 메뉴 = 더크 67 · 육척봉 2(0인 칸만)
  - 테스트 작성, **실행 안 함 (사용자가 직접)**: `SurpriseDefenseTests` 9개 — 기습 판정(기억 없음 · 있음 · 적이 칠 때), 기습 항상 명중(맨손, 회피 10만), 더크 기습 최소(+0 3 · +2 4 · 다른 무기 그대로), 더크 굴림(기습 3~4 · 보통 1~4), 육척봉 방어(받은 + 막은 = 2, 0~2), 방어 0 = 난수 안 씀, 스냅샷 v15 왕복, HUD 줄 · 로그, 에셋 값. 고침: `ProgressionTests` 빗나감 테스트(대상이 플레이어를 본 상태로 — 아니면 기습이라 항상 맞음), 스냅샷 버전 14 → 15(BossTests · FloorTests · InventoryTests · StrengthUpgradeTests · WeaponTests)
  - 확인: 컴파일 오류 0 · Main Scene → Varco Models → FX 실행: 더크 기습 67%(최소 3) · 육척봉 방어 2 · 다른 무기 0
  - **바뀐 기존 결과:** 플레이어를 아직 못 본 적(배회 중)을 치면 이제 항상 맞는다 — 첫 공격의 빗나감이 없어짐
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드(기습 로그 · 더크 피해 · 육척봉 막기 체감)
  - 확인 방법: Test Runner EditMode 전체(새 `SurpriseDefenseTests`). `Main.unity` 플레이 → 플레이어를 못 보고 배회하는 적 뒤로 다가가 치면 로그 "기습! 적 #n" 뒤 피해(빗나가지 않음), 더크면 3~4. 적이 쫓아오기 시작한 뒤에는 기습 아님. 육척봉을 들면 HUD "· 방어 0~2", 적에게 맞을 때 가끔 "방어: n 막음"과 함께 받은 피해가 줄어듦

- [x] **3-8. 카메라 둘러보기 · 확대 축소** (2026-10-19 사용자 요청) — 테스트 실행 · 플레이 확인 대기
  - 요청: 마우스를 누른 채 일정 거리 이상 움직이면 끌기로 맵 여기저기를 보고, 이동이 시작되면 카메라가 다시 플레이어에게. 휠 내리면 축소 · 올리면 확대
  - 정한 것 (Claude): 끌기 판정 = 누른 곳에서 12 px(`PlayerInputController._dragThreshold`) 넘게 움직임. 그래서 **칸 클릭(이동 · 공격 · 주문 시전)은 누를 때가 아니라 놓을 때** 일어난다(끌었으면 클릭 아님). 빠른 칸 클릭은 그대로 누를 때. 끌기 = 잡은 바닥 점이 커서 밑에 그대로 있게 카메라를 옮김(맵 범위 안). 플레이어가 움직이면(걸음 · 층 이동) 따라가기로 부드럽게 돌아옴 — 공격 · 시전 · 대기만 하면 둘러보던 곳에 머묾. 휠 한 칸 = 2 m, 거리 8 ~ 32 m(지금 16). 인벤토리가 열려 있으면 끌기 · 휠 없음. 클릭 이동 중에 끌기 시작하면 그 경로는 멈춤
  - 구현 — Runtime: `CameraController`(`SetPanBounds` · `BeginDrag` · `DragTo` · `PanBy` · `Zoom`, `IsFreeLook`, `FollowTarget`이 둘러보기를 끝냄, 인스펙터 `_minDistance` · `_maxDistance` · `_zoomStep`), `PlayerInputController`(`_cameraRig` · `_dragThreshold`, 누름 → 끌기 / 놓을 때 클릭, 휠), `GameRoot`(새 판 · 층 이동 때 둘러보기 범위 = 맵 크기). Editor: Main Scene 메뉴가 `_cameraRig` 연결
  - 테스트 작성, **실행 안 함 (사용자가 직접)**: `PresenterTests` +3 — 둘러보기 이동 · 맵 범위 · 같은 거리, 따라가기가 둘러보기를 끝냄, 확대 · 축소 방향 · 최대 멈춤 · 보는 곳 그대로
  - 확인: 컴파일 오류 0 · Main Scene 메뉴 실행 후 `_cameraRig` = Main Camera
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드(끌기 느낌 · 휠 · 놓을 때 클릭으로 바뀐 조작감)
  - 확인 방법: `Main.unity` 플레이 → 왼쪽 버튼을 누른 채 끌면 맵이 손에 붙어 움직임 → 놓아도 이동하지 않음 → 방향키 · 클릭으로 한 걸음 움직이면 카메라가 플레이어에게 돌아감. 짧게 클릭하면 지금처럼 이동 · 공격(놓을 때). 휠 올리면 가까이, 내리면 멀리. 주문 조준 중 끌어도 시전되지 않음

- [x] **버그 — 직선 이동 중 대각선으로 꺾으면 순간 이동** (2026-10-19 사용자 플레이 피드백) — 테스트 실행 · 플레이 확인 대기
  - 원인: `EntityPresenter.OnMoved`가 "1.6칸보다 먼 이동은 바로 옮김"을 **보이는 위치 → 새 칸** 거리로 쟀다. 꾹 누른 직선 이동은 보간이 뒤처져 보이는 위치가 앞 칸보다 뒤에 있으므로, 그 자리에서 대각선 칸까지는 1.6칸을 넘어 순간 이동으로 판정됨 (뒤처진 0.5칸 + 대각선 → 약 1.8칸)
  - 해결: 순간 이동 판정을 **칸 사이 거리(From → To)**로 — 대각선 한 걸음 = 1.41칸 < 1.6. 보간은 지금처럼 보이는 위치에서 이어 출발
  - 검사: `PresenterTests.Motion_DiagonalStepWhileLagging_DoesNotSnap` (작성, 실행 안 함). 확인 방법: `Main.unity` 플레이 → 방향키 하나를 꾹 누르다 다른 키를 함께 눌러 대각선으로 꺾으면 끊김 없이 이어서 걸음

- [x] **테스트 전체 실행** (2026-10-20, 사용자 지시) — 3-3a 이후 처음. 341개 중 3개 실패 → 고친 뒤 **341개 전부 통과**
  - `ProgressionTests.EnemyDeath_NoExpAboveEnemyMaxLevel` (3-6에서 쓴 테스트 버그): 레벨업 경험치 이벤트가 버스에 쌓인 채 `s.Events.Clear()` → 다음 `Flush`에 들어와 "경험치 없음" 검사가 틀림. 비우기 전에 `Flush`
  - `PresenterTests.ScreenToGround_ThroughRotatedGrid_GivesCell` · `Motion_AttackLungesAttacker_FlashesTarget` (G-1 · G-2b 이후 실행 안 하던 테스트): 테스트 Grid가 기본 칸 두께 1이라 칸 중앙이 바닥 0.5 m 아래 — 실제 씬처럼 `cellSize z = 0`. 위치를 정확히 같은지(`Is.EqualTo(Vector)`)로 비교해 부동소수 오차로 실패 → 거리 · `Within` 허용 오차로
  - 피드백: ① 이벤트를 모아 검사하는 테스트는 `Clear()` 전에 `Flush()` ② 테스트용 Grid는 실제 씬 설정(칸 두께 0)과 같게 ③ 위치 · 벡터 비교는 항상 허용 오차로. 오래 안 돌린 테스트는 이런 차이가 쌓이므로 항목마다 실행하는 편이 낫다

- [ ] **3-9. 물약 · 주문서 — SPD 방식** (2026-10-20 사용자 요청) — 물약 12색 · 주문서 12룬 아트(`Docs/소모품_에셋.md`)에 SPD와 같은 효과를 붙인다
  - 결정 (2026-10-20, 사용자 — 모두 추천안): ① 아트 = **A** — 물약은 모델 하나(jade) + 액체만 바꾼 12색 텍스처(에디터 메뉴가 만듦), 주문서는 빈 종이 모델 하나 + 룬은 게임에서 겹쳐 그림 ② **식별 = SPD 그대로** — 판마다 색 · 룬을 효과에 무작위 배정(시드), 처음엔 "빨간 물약"처럼 보이고 쓰면 이름이 밝혀짐 ③ 범위 = **쉽게 되는 것부터** — 물약: 힘 · 회복 · 경험 · 정화 · 독가스 · 화염 · 냉기 · 마비가스 / 주문서: 강화 · 식별 · 지도 · 순간이동 · 충전(마나) · 분노 · 응징. 나머지(신속 · 투명 · 부양 · 정신 시야 / 저주 해제 · 분신 · 공포 · 자장가 · 변환)는 다음 항목 ④ 해로운 물약 = **던지기 도입**(조준해 던지면 그 칸에서 터짐, 마시면 발밑에서) ⑤ 배치 = **보장(힘 2 · 강화 3) + 층마다 무작위 물약 2 · 주문서 2**(SPD 비율)
  - 나눔: **3-9a** 아트(모델 2 · 12색 텍스처 · 12룬) → **3-9b** 종류 · 식별 · 배치 → **3-9c** 효과 · 던지기
- [x] **3-9a. 물약 · 주문서 아트** (2026-10-20) — 플레이 확인 대기 (게임 연결은 3-9b)
  - 구현: `Art/Varco3D/Prop_Potion.fbx`(jade) · `Prop_ScrollBlank.fbx`(빈 종이) + 텍스처(GLB에서 꺼냄). 메뉴 **ProjectM/Setup/Consumables** (`Editor/ConsumableArtSetup.cs`, 멱등 — Varco Models 다음, FX 앞): 물약 = 초록~청록 · 채도 있는 픽셀만 골라 12색으로 바꾼 텍스처(그늘 유지) · 재질 · 프리팹 `Potion_<색>`, 주문서 = 빈 종이 + 종이 위에 눕힌 룬 판(룬 12개를 획으로 그린 금색 텍스처, 알파 잘라내기 + 발광) · 프리팹 `Scroll_<룬>`. `ItemVisualCatalog`에 `potion_<색>` · `scroll_<룬>` + `Upright`(눕히지 않음) 칸, `FloorItemPresenter`가 Upright면 그대로 세움
  - 정한 것 (Claude): 크기 물약 0.32 m · 주문서 0.42 m, 룬 판 = 종이 짧은 쪽 × 0.45. Oethel은 게임에서 그리므로 차트대로 바른 모양(이미지는 rot C였음). 룬 위쪽 = 화면 위(+Z)
  - 확인: 컴파일 0 · 메뉴 실행 · 물약 12 · 주문서 12 프리팹을 Unity에서 렌더해 색 구분 · 룬 모양 확인. **확인 못 한 것:** 게임 화면(아직 아이템과 연결 안 됨 — 3-9b)
- [x] **3-9b · 3-9c. 물약 · 주문서 종류 · 식별 · 배치 · 효과 · 던지기** (2026-10-20) — 테스트 작성, 실행 안 함 · 플레이 확인 대기. 두 항목을 한 커밋으로 (효과 없이는 식별 · 배치를 확인할 수 없어서)
  - 종류: `ItemUse.StrengthPotion/UpgradeScroll` → `Potion/Scroll`(값 4 · 5 그대로 — 에셋 호환) + `ConsumableEffect`(15종) · `Weight`(SPD 무게). 카탈로그에 물약 7 · 주문서 6 추가 (BALANCE 14번), 3-7b 두 줄은 Setup이 효과를 채움
  - 식별 (`Identification`): 판 시작에 물약 · 주문서 종류마다 겉모습(12색 · 12룬)을 섞어 배정(`RngStream.Loot`, 배치 계획 모두 뒤 — 책 · 무기 · 보장 소모품 계획은 그대로). `RunState.Looks` · `Identified` (**스냅샷 v16**). 마시기 · 읽기 = 식별, 던진 범위 물약 = 식별, 식별 주문서 = 인벤토리 앞 칸부터 모르는 한 종류. `FloorItemPlacedEvent.VisualId` = 겉모습 → 바닥 모델 · 반짝임(`item.potion` · `item.scroll` 공통 줄)도 겉모습으로 (정체가 드러나지 않게)
  - 이름: `HudText.Run`이 있으면 모르는 아이템은 "진홍 물약" · "티르 룬 주문서", 로그 "진홍 물약을 마셨다 — 치유 물약이었다" + 효과 한 줄 (`ConsumableUsedEvent` · `ItemIdentifiedEvent` → `HudModel`)
  - 배치: `Loot.PlanRandomConsumables` — 1~4층마다 무작위 물약 2 · 주문서 2 (무게, 무게 0인 힘 · 강화는 보장 수로만), `PlanConsumables` 뒤
  - 효과 (`Consumables.Apply`, 효과 종류 하나 = case 하나): 힘 · 강화(그대로) · 치유 · 경험 · 정화 · 독가스(독구름) · 액체 화염(화염 지대) · 냉기(빙결 · 물 → 빙판 · 불 끔) · 마비 가스(**새 지형 `ParalyticGas`=6**, 마비 4틱) / 식별 · 지도 · 순간이동 · 충전(마나) · 격노 · 응징(**새 피해 출처 `DamageKind.Item`**=6). 범위는 `Consumables.Area`(벽을 돌아 걸음 수)
  - 던지기: 인벤토리 메뉴 "던지기"(물약만) → 주문 조준과 같은 조작(마우스 · 방향키 · Tab · Space/클릭 · Esc) → `ThrowItemCommand`(차례 1). 판정 · 미리보기는 `Throwing.Check` 하나 (사거리 8 · 보이는 칸 · 벽 앞/유닛 칸에서 멈춤, 미리보기는 경로 + 떨어지는 칸만 — 모르는 물약의 범위를 보여 주지 않음)
  - 정한 것 (Claude, 질문 없이 — 사용자 결정의 세부): 치유는 즉시 가득(SPD는 서서히) · 식별 주문서는 고르기 대신 앞 칸부터(고르는 창은 나중에) · 순간이동 난수는 Combat · 격노는 "모든 적이 읽은 칸 기억"만(SPD의 보이는 적 광란은 뺌) · 응징의 실명은 뺌 · 가스는 번지지 않고 반경 2에 한 번에 깔림(SPD는 가스가 번짐) · 주문서는 못 던짐 · 마비 가스 지속 4틱 · 겉모습 반짝임은 공통 색
  - 확인: 컴파일 0 · Setup(Main Scene · FX) 실행 — 카탈로그 27줄 `ToBook` 통과, 겉모습 24개 모두 모델 있음. 테스트 `ConsumableTests` 27개 작성 + 기존 검산 3개 갱신(`InventoryTests` 27개 · 지형 표 · `StrengthUpgradeTests`) — **실행 안 함**
  - **확인 못 한 것:** 테스트 실행 · 게임 화면 전부 (바닥 겉모습 · 이름 · 로그 · 던지기 조준 · 가스 오버레이 · 마비 가스 색)
  - 확인 방법: `Main.unity` 플레이 → 1층에서 물약 · 주문서를 주우면 "○○ 물약을 주웠다"(색 이름) → I → 물약 고르고 Enter → "사용"이면 마심 · "던지기"면 조준(마우스로 칸 · 클릭) → 로그에 "…을 마셨다 — 치유 물약이었다"와 효과 한 줄, 같은 색 물약은 그 뒤 진짜 이름으로 보임. 독가스 · 화염 · 냉기 · 마비 가스는 던진 칸 둘레 5×5에 지형 색. 새 판(R)마다 색 ↔ 효과가 바뀜
- [x] **체력 회복 확인 · 연출 끝날 때까지 다음 틱 대기** (2026-10-20 사용자 요청) — 테스트 작성, 실행 안 함 · 플레이 확인 대기
  - 체력 회복: 처음엔 소수점 누적(정확히 1% → 최대 20이면 50틱마다 1)으로 바꿨으나 사용자가 되돌림. **결정 (2026-10-20): 10틱마다 최대 체력 1%, 소수점은 올림 — 2-6의 원래 규칙 그대로** (최대 20이면 10틱마다 1). 코드 변경 없음 (`MagicConfig.HpRegenAmount`). BALANCE 4번
  - 연출 대기: `PlayerInputController.PresentationBusy`(GameRoot = `EntityPresenter.IsAnimating` 이동 · 공격 · 맞음 · 시전 · 죽음 ‖ `FxPresenter.IsPlaying` 투사체 · 광선 · 맞을 때까지 기다리는 효과)가 true인 동안 행동 입력을 받지 않음 → 턴제에서는 다음 틱이 연출 뒤에 진행. 규칙은 그대로 즉시 계산(아트 규칙 "연출은 로직을 기다리지 않는다" 유지 — 기다리는 것은 입력). 확대 축소는 대기 중에도 됨
  - 정한 것 (Claude): 터진 뒤 남는 연기 같은 한 번짜리 효과 · 지형/상태 반복 효과 · 걷기 유지 · 숨쉬기는 기다리지 않음. 대기 중 **짧게 누른 키 · 클릭은 버려짐**(누르고 있는 키는 끝난 뒤 이어서 읽힘) — 불편하면 입력 하나를 기억해 두는 방식으로 바꿀 수 있음. 한 번의 입력에 플레이어와 적 행동이 같이 처리되므로 같은 차례 안의 연출(내 이동 + 적 공격)은 지금처럼 동시에 재생됨
  - 확인: 컴파일 0. 테스트 `ManaTests` 회복 2개 수정 · `PresenterTests.Motion_IsAnimatingUntilStepFinishes` 추가 — **실행 안 함**. **확인 못 한 것:** 플레이 (체감 속도 · 키 반복)
  - 확인 방법: `Main.unity` 플레이 → 방향키를 꾹 누르면 한 칸 이동 연출이 끝난 뒤 다음 칸으로 감(미끄러지거나 순간이동 없음), 적을 치거나 주문을 쓰면 투사체가 닿고 맞는 연출이 끝나야 다음 행동이 됨. HUD HP가 20이면 50틱(약 50걸음)마다 1 오름
- [x] **테스트 전체 실행 (2차)** (2026-10-20, 사용자 지시 — 3-5 이후 항목) — 369개 중 6개 실패 → 고친 뒤 **369개 전부 통과**
  - 실패 6개 모두 같은 원인: 각 기능 테스트가 스냅샷 버전을 `Is.EqualTo(15)`로 검사 → 3-9에서 v16으로 올리자 함께 깨짐 (Boss · Floor · Inventory · StrengthUpgrade · SurpriseDefense · Weapon)
  - 해결: "그 기능이 들어간 버전 이상"(`Is.GreaterThanOrEqualTo(n)`)으로. 정확한 현재 버전은 그 버전을 올린 항목의 테스트 하나(`ConsumableTests` — 16)만 검사
  - 피드백: 버전 · 개수처럼 다른 항목이 올리는 값은 정확히 같은지 검사하지 말고 하한으로. 버전을 올릴 때는 `grep "CurrentFormatVersion, Is.EqualTo"`로 남은 곳을 확인
- [ ] **3-10. 장비 · 인벤토리 — SPD 방식** (2026-10-20 사용자 요청: SPD 배낭 화면처럼, 장비도 SPD처럼, F 키로도 열기)
  - 결정 (2026-10-20, 사용자): ① 장비 칸 = **5칸(무기 · 갑옷 · 유물 · 기타 · 반지) + 갑옷까지 구현** (유물 · 반지는 틀만) ② 아이콘 = **3D 모델 렌더**(에디터 메뉴) ③ 주문 = **처음엔 배낭을 같이 쓰고, 나중에 "주문 보관함"을 얻으면 분리** ④ 겹치기 = **같은 종류는 한 칸에 겹침**(개수 표시) ⑤ 갑옷 = **SPD 5종 전부** ⑥ 방어 = **무기처럼 ÷3** ⑦ 갑옷 모델 = **Varco로 생성**(확인 후 가져옴, 그 전엔 회색 상자) ⑧ 순서 = **3-10a 갑옷 → 3-10b 배낭 UI**
  - 참고: 질문에서 요구 힘을 "10~14"로 잘못 적었음 — SPD 실제 값 10 · 12 · 14 · 16 · 18로 구현
- [x] **3-10a. 갑옷** (2026-10-20) — 테스트 작성, 실행 안 함 · 플레이 확인 대기
  - 구현: `ItemUse.EquipArmor`=6 · `ArmorDef`(티어 · 요구 힘) · `ArmorRules`(나누기 3 · 벌점 2) · `Armors`(Resolve · Equip · Wear · Upgrade · RollDefense · EvasionFactor) · `Unit.Armor`(`EquippedArmor`, **스냅샷 v17**). 입기는 무기와 같은 `EquipWeaponCommand`(차례 1, `Weapons.Equip`이 갑옷이면 `Armors.Equip`). 방어는 `MeleeWeapon.ApplyDefense`(무기 방어 + 갑옷 굴림, 근접만), 회피 벌점은 `Combat.RollHit`
  - 강화 주문서: `UpgradeTarget`(Auto = 무기 먼저, 없으면 갑옷 · Weapon · Armor) — `UseConsumableCommand` 인자. 고르는 창은 3-10b
  - 데이터: 갑옷 5줄 · 공통 2값 (BALANCE 15번), `Player.asset` `_startingArmor` = 천 갑옷, `LootConfig` `_armorsPerRun` 2 (`Loot.PlanArmors`, 무작위 소모품 계획 뒤)
  - 화면: HUD에 갑옷 줄(마나 막대 오른쪽 "갑옷: 천 갑옷 방어 0~1 · 힘 10/10"), 로그 "가죽 갑옷을 입었다" · "강화: …", 결과 화면 장비에 갑옷. 바닥 모습은 아직 회색 상자(Varco 모델 전)
  - 정한 것 (Claude): 방어 굴림은 고른 분포(SPD는 정규분포 — 무기와 같게), 힘 모자람의 이동 속도 벌점(SPD)은 뺌, 갑옷 배치는 2티어 이상에서 겹치지 않게, 판 시작 천 갑옷은 로그 없음
  - 확인: 컴파일 0 · Setup 실행 — 카탈로그 32줄, 갑옷 5종 수치가 BALANCE 표와 같음(에디터에서 계산). 테스트 `ArmorTests` 작성 + `InventoryTests` 개수 32 · 스냅샷 정확한 버전 검사를 ArmorTests로 옮김 — **실행 안 함**
  - **확인 못 한 것:** 테스트 실행 · 플레이 (HUD 갑옷 줄 · 입기 · 방어 체감)
  - 확인 방법: `Main.unity` 플레이 → HUD 마나 막대 오른쪽에 "갑옷: 천 갑옷 방어 0~1 · 힘 10/10" → 바닥 갑옷(회색 상자)을 주워 I → 사용 → "…을 입었다", 갑옷 줄이 바뀜, 천 갑옷은 인벤토리로. 적에게 맞으면 가끔 "방어: n 막음"
## Phase P — 픽셀 그래픽 전환 (2026-10-20 사용자 요청: "전체적인 모든 그래픽을 픽셀 스타일로")
- 결정 (2026-10-20, 사용자): ① 방식 = **AI 픽셀 이미지(Varco 이미지 생성 · 기존 이미지를 픽셀로 다시 그림) + 2D 게임 화면** (추천안 "3D 모델을 픽셀 스프라이트로 렌더" 대신) ② 시점 = **위에서 비스듬히(SPD식 — 바닥은 위에서, 벽은 윗면 + 앞면, 캐릭터는 정면 · 옆모습)** ③ 타일 = **32×32** ④ 순서 = **시험 먼저**(플레이어 · 적 1 · 바닥 · 벽 · 아이템 3 → 한 화면 시험 → 보고 결정한 뒤 전체 전환)
  - 이 결정은 CLAUDE.md 기술 스택의 "그래픽 방식: 실시간 3D (G-1, 변경 금지)"를 되돌린다 — 사용자가 영향 범위 표(규칙 · 테스트 그대로, 렌더 · 맵 표시 · 캐릭터 애니메이션 · 아이템 · 이펙트 · 문서 교체)를 보고 결정. CLAUDE.md는 시험 결과를 보고 전체 전환을 확정할 때 고친다
  - 알려 둔 위험: AI는 같은 캐릭터를 여러 프레임에 일관되게 그리기 어렵다 → 처음엔 한 장 + 코드 흔들림(통통 튐 · 기울기)으로 동작을 대신
  - 3-10a 다음에 하려던 **갑옷 3D 생성은 하지 않음**(픽셀로 바뀌므로). 3-10b 배낭 UI 아이콘은 픽셀 아이콘으로 — 전환 뒤에
- [~] **P-0. 픽셀 시험** (2026-10-20) — 결과 보고, 해상도 결정 대기
  - 만든 것: Varco EditImage(nano-banana-pro, 같은 지시문 — SPD 스타일 · 색 약 16 · 1px 외곽선 · 초록 배경)로 기존 이미지 7장을 픽셀로 다시 그림 (양 마법사 · 쥐 · 소검 · 비취 물약 · 천 갑옷 · 바닥 · 벽). 후처리(초록 제거 · 잘라내기 · 축소 · 색 수 제한) → 12×8칸 모의 화면 (게임 엔진이 아닌 이미지 합성). 결과 `Docs/PixelTest/`
  - 본 것: 캐릭터 · 쥐 · 물약 · 소검은 도트 느낌이 좋음. 천 갑옷은 그림 느낌이 남음, 바닥은 무늬가 잘고 반복이 눈에 띔, 벽은 괜찮음. AI 이미지의 실제 도트 굵기는 1024px에 약 10px(≈ 100px 해상도)라 **32px로 줄이면 양털 · 뿔 같은 세부가 뭉개짐**, 64px은 대부분 살아남음
  - 해상도: 사용자 "64px로 시험삼아" (2026-10-20) → **결정 (2026-10-20): 64px 확정** — 사용자가 참고로 든 Dungeon Slasher(캐릭터 키 약 48~60px 추정)와 같은 대역, AI 이미지 디테일이 살아남음
  - 플레이어 다시 만들기 (사용자 요청 2026-10-20): "완전히 마법사 느낌이 나게 양 수인으로" — 양털 · 말린 뿔 · 고깔모자 · 긴 로브 · 지팡이, 4가지 시안에서 고름
  - 플레이어를 새로 (사용자 2026-10-21): 양 수인 시안은 쓰지 않고, 사용자 레퍼런스 3장(웹툰 · 게임 남자 캐릭터)의 **느낌**으로 오리지널 남자 캐릭터 — 레퍼런스 이미지는 업로드하지 않고 분위기만 글로(검은 흐트러진 머리 · 한쪽 눈 가린 앞머리 · 차가운 표정). 사용자 지정: **빈손 · 판타지풍 먼저**(검은 롱코트 · 높은 깃 · 은 버클 · 장갑 · 벨트 파우치) → 시안 4장 `Docs/PixelTest/8_*` · `9_*`
    - 본 것: 원본은 분위기가 잘 맞음. 그러나 **실제 사람 비율(약 7등신)이라 64px로 줄이면 얼굴이 약 8px**이 되어 표정 · 머리 모양이 거의 안 보이고, 옷이 모두 검정이라 바닥 위에서 한 덩어리로 보임 → 비율(3~4등신) · 강조색을 다시 정해야 함
    - 사용자 "3~4등신 + 강조색"(2026-10-21) → 진홍 2 · 사파이어 2 (nano-banana-pro) `10_*` · `11_*`: 64px에서도 얼굴 · 앞머리가 보임
  - 금발 남캐 (사용자 레퍼런스 1장 — 다른 게임 캐릭터, 이미지 업로드 없이 느낌만: 한쪽 눈 가린 금발 단발 · 높은 깃 롱코트 · 은색 문양 · 한쪽 가시 어깨 갑옷): **옷 파랑 · 검정 · 빨강 × 눈 초록 · 빨강 · 파랑 = 9조합**. 사용자 지정: **이미지 생성 모델 GPT 2.5**(`gpt-image-2.5-flare`) → `12_*` · `13_*`
    - 본 것: 9장 모두 디자인이 일관되고 조합대로 나옴. 다만 GPT 2.5는 3.5등신 지시보다 **머리가 작은 약 5등신**으로 그려, 64px에서는 **눈 색이 거의 구분되지 않음**(옷 색은 잘 보임)
  - 금발 남캐 수정 (사용자 2026-10-21): 머리를 레퍼런스(다른 애니 캐릭터 — 업로드 없이 느낌만)처럼 **짧고 부드러운 금발 · 눈 사이로 내려오는 앞머리**로, **어깨 장식 뺌**, **눈 빨강 · 파랑** × 옷 3색 = 6장, GPT 2.5 + 3.5등신을 강하게 지시 → `14_*` · `15_*`
    - 본 것: 이번엔 3.5등신으로 나와 64px에서도 얼굴이 크고 눈 색(빨강 · 파랑)이 보임. 6장 모두 같은 얼굴 · 머리 · 코트로 일관됨
  - 6조합을 엔진 64px 시험 씬에 2배율로 렌더 → `16_heroes_engine_x2.png` (스프라이트 `Art/Pixel/Test/Heroes/`)
  - **결정 (2026-10-21, 사용자): 플레이어 = 짧은 금발 · 검은 코트 · 빨간 눈** (`hero_black_red`) — 시험 씬 플레이어도 이것으로. 참고: 검은 코트는 바닥과 대비가 가장 약함 → 전체 전환 때 바닥 색 · 외곽선으로 보완
  - 기존 애니 캐릭터(모노가타리 아라라기 카렌)를 그대로 만들자는 요청은 저작권 문제로 진행하지 않고 오리지널 안을 제안 → **사용자 수락 (2026-10-21): 오리지널 무도가 여캐, 옷 색만 "하얀색에 가까운 하늘색", 나머지는 제안대로**(검은 포니테일 · 솟은 머리 한 가닥 · 자신만만한 웃음 · 짧은 재킷 · 붕대 손 · 반바지 · 긴 양말 · 은색 별 핀, 빈손). GPT 2.5로 7등신 2장(T1 · T2) + 3.5등신 2장(C1 · C2) → `19_*` · `20_*` — 선택 캐릭터가 7등신이라 둘 다 뽑아 비교
    - 사용자 수정 (2026-10-21): 머리를 **검은 단발**(턱선 · 일자에 가까운 앞머리, 레퍼런스는 같은 작품의 다른 캐릭터 — 업로드 없이 머리 모양만)로 → 포니테일 · 솟은 머리 뺌, 별 핀 유지. 다시 7등신 2 + 3.5등신 2 → `21_*` · `22_*`
- [~] **P-2. 원본 일러스트 / 게임 스프라이트 나누기** — **결정 (2026-10-21, 사용자): 7등신 그림 = 캐릭터 원본 일러스트**(선택 창 초상 등), **게임 안 = 같은 디자인을 AI에게 64×64 격자(칸 = 16×16 블록)에 맞춰 다시 그리게 한 스프라이트**
  - 원인 (사용자 질문 "게임에 들어가면 도트가 깨져 보임"): AI 도트는 칸 크기가 제각각(1024px에 약 8~11px)이라 64px로 평균 축소하면 칸 경계가 어긋나 중간색이 생기고, 색 수 제한에서 얼룩 · 끊긴 외곽선이 됨 + 7등신은 얼굴 약 8px라 형태가 무너짐 + 채팅 미리보기가 한 번 더 축소됨
  - 시험: 금발 B1을 EditImage(GPT 2.5)로 격자 맞춤 — 7등신 그대로 2장 · 게임용 4등신 2장. 축소는 칸 크기 · 시작 위치를 찾아 칸 가운데의 가장 흔한 색을 집음(평균 없음)
    - 결과 (`23_*` · `24_*` · `25_grid_zoom_x6.png`): 새 방식은 색이 또렷하고 중간색 · 얼룩이 없음 — 기존 방식에선 금발이 회색 피부색으로 뭉개지던 것이 노란 머리 · 빨간 눈까지 살아남. 다만 AI가 칸을 지시(16px)보다 굵게(약 19px) 그린 3장은 게임 속 키가 약 50px로 작아짐, G4b(4등신, 칸 약 15px)만 키 62px로 지시대로. 축소 스크립트는 `Tools/grid64.py`(Assets 밖 — Unity가 컴파일 안 함)
  - **결정 (2026-10-21, 사용자): 게임 안 스프라이트 = 4등신**(격자 맞춤, 금발은 G4b) · **원본 일러스트 = 7등신 고해상도 도트 일러스트**(사용자 레퍼런스 2장 — 다른 게임 캐릭터 일러스트, 업로드 없이 세로 캐릭터 카드 구도 · 손에 마법 빛 · 어두운 배경 극적 조명 연출만). 대상은 선택 창 두 명(금발 B1 · 흑발 G), 무도가 여캐는 시안 선택 뒤
    - 결과: 7등신 일러스트 금발 2 · 흑발 2 (`26_illust_7head.png`, 9:16, 손에 푸른 마법 빛) · 게임용 흑발 4등신 K4a · K4b 격자 맞춤(칸 약 15px → 키 61~62px, `27_*` · `28_*`). 아직 프로젝트(선택 창 초상 · 시험 씬)에는 넣지 않음 — 사용자 선택 대기
    - 사용자 수정 (2026-10-21): 손에 든 빛을 **노란색**으로 → 4장 모두 빛 · 비추는 가장자리 조명 · 입자만 금빛 노랑으로 바꿈(EditImage, GPT 2.5) `29_illust_yellow_light.png`
  - **사용자 선택 (2026-10-21): 금발만 넣음, 흑발은 나중에 다시.** 금발 일러스트는 사용자가 번호를 말하지 않아 노란 빛 **금발 1**로(바꾸기 쉬움) → `Art/Pixel/Characters/hero_blond_illust.png`(864×1536 — 칸이 일정한 격자가 아닌 도트풍 그림이라 부드러운 필터로 가져옴), 게임 안 `hero_blond.png` = 격자 맞춤 4등신 **G4b**
  - 선택 창: `CharacterCatalogAsset.Entry.Illustration` 추가 — 있으면 카드(330×620)에 비율 유지로 일러스트, 없으면 64px 초상 4배(흑발). Setup이 `<id>_illust.png`가 있으면 가져와 채움
  - (2026-10-22) Unity 재시작 후 컴파일 0 · 플레이 모드를 멈추고(사용자 허락) Setup(Main Scene) 실행 → 금발 일러스트 연결(864×1536, 부드러운 필터), 흑발은 일러스트 없음. **확인 못 한 것: 선택 창 실제 화면** — 확인 방법은 아래 그대로. ~~컴파일 · Setup 실행 · 화면 — 작업 중 Unity 에디터가 종료됨~~
  - (기록) 작업 중 Unity 에디터가 종료됨(MCP 연결 끊김). 확인 방법: Unity를 다시 열고 컴파일 오류 없음 → 메뉴 ProjectM/Setup/Main Scene → `Main.unity` 플레이 → 선택 창 왼쪽 카드에 금발 일러스트, 오른쪽 카드에 흑발 픽셀 초상
- [x] **P-1. 캐릭터 선택** (2026-10-21 사용자 요청: "캐릭터를 여러 명 중에서 고르게, 일단 두 캐릭터") — 테스트 작성, 실행 안 함 · 플레이 확인 대기
  - 결정 (2026-10-21, 사용자): ① 차이 = **우선 외형만**(수치 · 시작 장비 같음) ② 위치 = **본 게임(Main) 판 시작 화면**(처음 · R 새 판마다) ③ 비율 = **둘 다 7등신** ④ 이름 = **임시**(금발의 마법사 · 흑발의 마법사)
  - 캐릭터: ① 금발 · 검은 코트 · 빨간 눈 — 7등신으로 다시 생성(GPT 2.5, 3장 중 **B1 — 사용자 확정 2026-10-21** · `17_*` · `18_*`) ② 흑발 · 검은 롱코트 — 사용자가 보낸 그림 = Varco 시안 G(`0b50ce23…`). 64px 초상 `Art/Pixel/Characters/hero_blond.png` · `hero_black.png`
  - 구현: `CharacterCatalogAsset`(`Data/Units/CharacterCatalog.asset` — Id · 이름 · 모습 ID · 초상) · `CharacterSelectModel`(순수 — 고르기 · 확정, 지난 선택 기억) · `CharacterSelectPresenter`(화면을 덮는 판 + 카드, ← → · A D · 마우스 = 고르기, Enter · Space · 클릭 = 시작) · `GameRoot.BeginCharacterSelect` → `StartNewRun(모습 ID)` → `UnitDef.WithVisualId`. 문구 `HudText.CharacterSelectTitle/Hint`
  - 3D Main에서는 픽셀 전환 전까지 두 캐릭터 모두 지금 플레이어 3D 모델 (`EntityVisualCatalog`의 `hero_blond` · `hero_black` = 양 마법사 프리팹, Varco Models 메뉴가 넣음). 판의 캐릭터는 플레이어 유닛 모습 ID에 남아 저장됨(스냅샷 변경 없음)
  - 확인: 컴파일 0 · Setup(Main Scene · Varco Models) 실행 — 목록 2명, 초상 · 모델 연결, GameRoot 참조 확인. 테스트 `CharacterSelectTests` 4개 작성 — **실행 안 함**
  - **확인 못 한 것:** 플레이(선택 창 모양 · 키/마우스 조작 · R 새 판 때 다시 뜨는지). 확인 방법: `Main.unity` 플레이 → "캐릭터 선택" 창에 두 캐릭터 카드(초상 · 이름) → ← →로 노란 테두리 이동 → Enter(또는 클릭)로 시작 → 쓰러진 뒤 R → 다시 선택 창(지난번 고른 캐릭터에 커서)
  - **엔진 안 시험** (64px): 메뉴 **ProjectM/Setup/Pixel Test Scene** (`Editor/PixelTestSetup.cs`, 멱등 — 저장 안 한 씬이 있으면 멈춤) → 이미지를 픽셀 설정(점 필터 · 압축 없음 · 64 PPU)으로 가져오고 `Scenes/PixelTest.unity`(정사영 카메라 + Tilemap + `PixelTestView`)를 만듦. Main 씬 · 빌드 목록은 그대로
    - `PixelTestView`: 실제 던전 생성기 맵을 바닥 · 벽 앞면(아래가 바닥인 벽) · 벽 윗면 타일로, 적 8 · 아이템 10을 무작위로. 방향키 · WASD 이동(벽만 막힘, 규칙과 연결 안 함) · 휠 배율 · R 다른 시드. 1 텍셀 = 정수 화면 픽셀, 카메라 위치도 화면 픽셀에 맞춤
    - 렌더 결과 `Docs/PixelTest/5_engine_64px.png`(1280×768, 1배율): 픽셀이 번지지 않고 또렷함. 보인 문제 — 바닥 무늬가 잘고 반복이 심함, 벽 윗면이 민무늬 회색 + 64px마다 이음매, 방이 커서 화면에 약 20×12칸
  - **확인 못 한 것:** 플레이 모드(방향키 이동 · 휠)는 실행 안 함 — 에디터에서 렌더만. 확인 방법: `Scenes/PixelTest.unity`를 열고 플레이 → 방향키로 양 마법사가 한 칸씩, 휠로 배율, R로 다른 맵

- [~] **P-3. 2D 픽셀 전환** (2026-10-22 사용자 요청 "이제 2D 픽셀 그래픽으로 전환")
  - 결정 (2026-10-22, 사용자 — 모두 추천안): ① **2D를 옆에 새로 만들고 Main 씬을 교체**(3D 표현 코드는 다 옮긴 뒤 지움) ② 순서 **ⓐ 맵 → ⓑ 유닛 → ⓒ 바닥 아이템 → ⓓ 이펙트 → ⓔ 3D 정리**, 단계마다 커밋 · 플레이 확인 ③ 렌더러 **Universal 그대로**(조명 없는 스프라이트 재질) ④ **든 무기는 스프라이트에 안 보임**(SPD처럼)
- [x] **P-3a. 2D 맵** (2026-10-22) — 플레이 확인 대기
  - 구현: `CameraController` 2D 모드(정사영 · 정수 배율 자동 = 화면 높이 ÷ (64 × 14칸) · 휠 1~4배 · 위치 픽셀 맞춤 · z = 0 평면 클릭 · 끌기 범위 XY) · `PixelMapPresenter` + `PixelTileSet`(벽은 아래 칸이 걸을 수 있으면 앞면) · GameRoot `_pixelMap`이 있으면 3D `TilemapPresenter` 대신 묶음 · MainSceneSetup `Pixel2D = true`(Grid 눕히지 않음, 카메라 2D, 어두운 바탕, `PixelMap` 타일맵 맨 아래 정렬 · 조명 없는 재질). 안개 · 지형 효과 · 조준 타일맵은 그대로(평면만 바뀜)
  - 타일 (2026-10-22, 사용자 "타일 새로 만들어줘"): GPT 2.5 격자 맞춤으로 6종 × 2~4장 생성(`Docs/PixelTest/31_tiles_raw.png`) → `Tools/tile64.py`(정확히 64×64 칸 가운데 색) → 고른 것: 바닥 A · C(회색 석판 2변형) · 벽 앞면 F2(F1은 아래에 바닥이 그려져 제외) · 벽 윗면 T1 · 계단 down2 · up2. 모의 맵 `32_*`, 게임 렌더 `33_main_2d_tiles.png` — 이음매 거의 안 보임. ~~임시 타일~~
  - 확인: 컴파일 0 · Setup 실행 · 플레이 모드에서 캐릭터 선택 확정 후 카메라 렌더 `Docs/PixelTest/30_main_2d_map.png` — 바닥 · 벽 앞면 · 윗면 · 안개 정상. **유닛은 아직 3D 모델(정면으로 서 있음)이고 이펙트도 3D** — ⓑ · ⓓ에서 바꿈. 테스트 추가 없음(카메라 3D 테스트는 기본값이 3D라 그대로)
  - **확인 못 한 것:** 직접 조작(클릭 이동 · 끌기 · 휠 배율 · 계단 표시) — 확인 방법: `Main.unity` 플레이 → 캐릭터 고르기 → 위에서 본 픽셀 바닥 · 벽, 안 본 곳 검정 · 본 곳 어둡게, 휠로 배율이 1~4배로 딱딱 바뀌는지, 끌어서 둘러보기, 바닥 클릭 이동
- [~] **P-3b 준비: SD 캐릭터** (2026-10-22 사용자 요청 "캐릭터를 이런 SD 느낌으로 도트로" — 레퍼런스 2장은 다른 게임 SD 캐릭터, 업로드 없이 비율 · 분위기만): 타일 수정은 잠시 미룸(사용자 "타일 먼저 조금 바꾸고 싶어" → 이어서 SD 요청)
  - 금발 B1을 EditImage(GPT 2.5)로 **약 2.5등신 SD** · 64×64 격자 → 4장(`34_sd_raw.png`), `grid64.py`로 키 61~62px. 새 바닥 위 5배 비교 `35_sd_64px_x5.png` — 4등신보다 얼굴 · 빨간 눈이 훨씬 잘 보임. 발밑 초록 배경 찌꺼기 몇 픽셀 남음(배경 제거 기준을 넣을 때 정리)
  - SD1 + SD3 섞기 (사용자 "SD1과 SD3의 느낌을 섞어줘"): EditImage(원본 SD1 · 참고 SD3, GPT 2.5) 3장 `36_sd_mix_raw.png`, 바닥 위 5배 `37_sd_mix_64px_x5.png`. MIX1 · MIX2는 AI 도트 칸이 굵어(19px) 64px로 줄이면 키 50~52px로 작아짐, MIX3은 칸 15px · 키 62px. `grid64.py` 초록 기준 완화 + 초록 낀 외곽선 → 외곽선 색 (발밑 찌꺼기 정리)
  - 결정 (2026-10-22, 사용자 "SD3으로"): 금발 캐릭터의 게임 안 스프라이트 = **SD3** (약 2.5등신 SD — 섞은 그림 대신 SD3 그대로). `Art/Pixel/Characters/hero_blond.png`를 SD3 64×64로 교체(가져오기 설정 그대로: 점 필터 · 64 PPU · 비압축). 지금 쓰이는 곳은 캐릭터 선택 초상(일러스트가 없을 때) — 게임 속 유닛 표시는 P-3 ⓑ에서 이 스프라이트로
- [x] **P-3a2. 이어지는 바닥 · 벽 타일** (2026-10-22 사용자 요청 "바닥 · 벽 타일은 이런 느낌(SPD 스크린샷)으로, 타일이 연속되어 이어지게" — 참고 그림은 업로드 안 함) — 플레이 확인 대기
  - 결정 (2026-10-22, 사용자): ① **AI(Varco GPT 2.5)로 큰 그림** (코드로 그리기 추천안 대신) ② **큰 그림 4×4로 나눠 칸 위치로 배치** (추천) ③ **벽 윗면 밝은 테두리 넣기** (추천)
  - 그림: 이어지는 텍스처 3종 × 2장 생성 → 바닥 F1(회색 판석) · 벽 윗면 T2(밝은 회녹색 작은 벽돌) · 벽 앞면 W1(회색 벽돌) 고름. `Tools/tile64.py … 256`으로 256px로 줄이고 `Tools/tiles_seamless.py`가 floor_00~15 · walltop_00~15 · wallfront_0~3(밑동 그림자 2줄) · rim_MMM 46장을 만듦. 감기 이음새(4칸마다)는 확대로 확인 — 눈에 띄는 끊김 없음. 이전 floor_0/1 · wall_front · wall_top 삭제
  - 코드: `PixelTileSet`(span 4 · Floor/WallTop(x, y) · WallFront(x) · Rim(mask 256)) · `PixelMapPresenter`(테두리 덮개 `_rims`, `RimAt` — 벽 윗면에서 열린 쪽 = 바닥 또는 벽 앞면, 안쪽 모서리는 양옆 변이 닫혔을 때만, 칸이 바뀌면 둘레 3×4칸 다시 칠함) · MainSceneSetup `PixelRims` Tilemap(정렬 -9, 안개 아래)
  - 확인: 컴파일 0 · Setup 실행(타일 누락 경고 없음, 테두리 46장 연결) · 미리보기 `39_seamless_mock.png` · 플레이 모드 카메라 렌더 `38_seamless_tiles.png`. 테스트 추가 없음(실행 안 함)
  - **확인 못 한 것 · 남은 것:** 직접 조작으로 둘러보기. 계단 타일은 예전 바닥이 그려진 그림이라 새 바닥과 무늬가 다름 → 새로 만들어야 함. 문 · 바닥 그림자(벽 밑) 없음
- [x] **P-3a3. 얇은 벽 RuleTile** (2026-10-22 사용자 요청 "벽을 RuleTile로 만들어서 이렇게(SPD 스크린샷) 얇게") — 플레이 확인 대기
  - 구현: `PixelWallTile : RuleTile<Neighbor>`(이웃 Floor · NotFloor · Shown(바닥 또는 아래가 바닥인 벽) · Hidden, 회전 없음, 스프라이트 여러 장 규칙은 x % 장 수) — 규칙 37개(앞면 4 = 좌우가 바닥인지 · 윗면 33 = 위 · 오른쪽 · 왼쪽 변 + 안쪽 모서리 4)를 MainSceneSetup `EnsurePixelWallTile`이 `Tiles/Wall` 그림으로 매번 새로 만듦. 그림 `Tools/tiles_wall.py`: 벽 속 투명, 보이는 쪽 16px 돌 블록(16×16) 테두리, 앞면 = 위 16px 테두리 + 아래 48px 벽돌(W1 텍스처 x % 4) + 밑동 그림자. `PixelTileSet`은 바닥 · 벽 RuleTile · 계단만, `PixelMapPresenter`는 바닥이 바뀌면 둘레 벽 `RefreshTile`. 직전의 벽 윗면 텍스처 · `PixelRims` 덮개 · rim 46장은 지움(Setup이 PixelRims 오브젝트 정리). 패키지 `com.unity.2d.tilemap.extras` 9.0.0을 manifest에 명시(전에는 간접 의존), Runtime · Editor asmdef가 참조. 카메라 배경 = 검정(벽 속이 안개와 같은 색 — Unity MCP가 멈춰 사용자가 Setup을 직접 실행, 씬 반영 확인)
  - 확인: 컴파일 0 · Setup 실행(규칙 37 · 빠진 그림 0) · 미리보기 `40_thin_wall_mock.png` · 플레이 모드 렌더 `41_thin_wall_game.png` — 얇은 테두리 · 앞면 벽돌 정상. 테스트 추가 없음(실행 안 함)
  - **확인 못 한 것:** 직접 조작 확인 · 문 타일 · 계단 새 그림 없음
  - 수정 (2026-10-22 사용자 피드백 "벽 모서리 부분이 어색" — 스크린샷): 벽 끝(앞면 옆이 바닥)에 세우던 16px 세로 테두리 블록이 1칸짜리 상자 · 겹친 두 줄로 보임 → 블록 대신 얇은 밝은 모서리 2px(밝은 1 + 그림자 1)로. 규칙 · 코드는 그대로, `Tools/tiles_wall.py` 앞면 그림 16장만 다시 만듦. 미리보기 `45_wall_end_mock.png`(벽 끝 · 기둥 · 얇은 벽 경우 모음). 게임 화면 렌더는 안 함
  - 추가 (2026-10-22 사용자 요청 "벽이 한 칸일 때는 이렇게(SPD 기둥)"): 위 · 아래 칸이 모두 바닥인 벽(한 칸 기둥 · 얇은 가로벽) = 위 절반 윗면 고리(8px 블록, 가운데 어둠) + 아래 절반 벽돌. 옆 칸 종류(바닥 f · 벽 w · 빈 칸 o)별 9종 × x % 4 — 옆이 벽이면 고리를 열어 이어 붙임. `PixelWallTile.Neighbor.Empty` 추가, 규칙 46개(홀로 선 벽 9 → 앞면 4 → 윗면 33), 앞면 그림 이름 `wall_front_nS_X` · `wall_front_cEW_X`. 확인: 컴파일 0 · Setup(규칙 46 · 빠진 그림 0) · 미리보기 `46_wall_pillar_mock.png`. 게임 화면 렌더 · 테스트 실행 안 함
  - 수정 (2026-10-22 사용자 피드백 "벽 두께가 다르다, 벽 픽셀이 일정하게"): 테두리 16px · 고리 8px가 섞여 있던 것을 **모든 벽 8px 두께 · 8×8 블록**으로 통일(앞면 = 위 8줄 테두리 + 56줄 벽돌, 홀로 선 벽 고리도 같은 블록). 그림만 다시 만듦(이름 · 규칙 그대로). 미리보기 `47_wall_8px_mock.png`
  - 다시 수정 (2026-10-22 사용자: "얇은 것 말고 두꺼운 것으로 맞춰" + "벽 높이가 다르다, 높이도 맞춰야지"): **모든 벽 16px · 16×16 블록**으로 되돌림. 홀로 선 벽의 앞면은 다른 앞면과 같은 그림(테두리 16 + 벽돌 48)을 쓰고, 솟은 윗면(고리 — 윗줄 · 닫힌 옆 블록 · 가운데 어둠)은 **위 칸에 덮개**로 그림(SPD 기둥처럼 1.5칸 높이) — `PixelWallCaps` Tilemap(정렬 -9) · `PixelTileSet.WallCap(mask)` 4장(`wall_cap_M`, 비트 1 오른쪽 · 2 왼쪽 닫힘 = 옆 칸이 벽 아님) · `PixelMapPresenter.CapAt`(바로 아래가 벽이고 그 위 · 아래가 바닥). RuleTile은 규칙 37개로 돌아감(홀로 선 벽 규칙 · `Neighbor.Empty` 삭제). 확인: 컴파일 0 · Setup(규칙 37 · 빠진 그림 0 · 덮개 4장 · PixelWallCaps 생성) · 미리보기 `48_wall_16px_raised_mock.png`. 게임 화면 렌더 · 테스트 실행 안 함. 덮개는 위 칸 유닛 · 아이템보다 아래에 그려짐(유닛이 고리를 가림)
  - 다시 수정 (2026-10-22 사용자 — SPD 스크린샷 3장: "캐릭터 머리는 한 칸보다 더 올라오게, 벽은 이런 구조로 한 칸 안에 다 들어가게"): 솟은 윗면 덮개(`PixelWallCaps` · `CapAt` · `WallCap`)를 없애고 **홀로 선 벽은 한 칸 안에** = 윗줄 블록 16 + 가운데 띠 8(닫힌 옆만 16×8 블록, 나머지 어둠) + 앞면 테두리 16 + 벽돌 24. 테두리 두께는 모든 벽 16px 그대로. RuleTile 규칙 46개(홀로 선 벽 9 → 앞면 4 → 윗면 33, `Neighbor.Empty`) — 63a1b75의 코드로 되돌리고 Setup이 남은 PixelWallCaps 오브젝트를 지움. 기둥 · 얇은 벽의 벽돌(24줄)은 보통 앞면(48줄)보다 낮음(한 칸 안에 넣으려면 피할 수 없음). 확인: 컴파일 0 · Setup(규칙 46 · 빠진 그림 0 · PixelWallCaps 없음) · 미리보기 `49_wall_pillar_in_cell_mock.png`. 게임 화면 렌더 · 테스트 실행 안 함
  - 다시 수정 (2026-10-23 사용자 "벽의 높이를 다 맞춰줘"): 기둥(벽돌 24줄)과 보통 앞면(48줄) 높이가 달랐던 것 → **모든 앞면을 같은 구조로**: 한 칸 안에 윗면 40줄(위 칸이 바닥이면 윗줄 블록 16 · 가운데 띠 8 · 앞면 테두리 16, 아니면 윗줄 없이 열림) + 벽돌 24줄. 옆 칸이 바닥 · 빈 칸이면 윗면 옆을 16px 블록으로 닫고, 벽이면 열어 이음. 앞면 그림 `wall_front_KEW_X`(K = c · n, E · W = f · w · o) 72장, RuleTile 규칙 51개(앞면 18 → 윗면 33). 확인: Setup(규칙 51 · 빠진 그림 0) · 미리보기 `50_wall_same_height_mock.png`. 게임 화면 렌더 · 테스트 실행 안 함
- [x] **P-3 ⓑ 유닛 2D (1단계 — 표시 · 걷기)** (2026-10-23 사용자 "유닛 2D 전환 시작, 머리는 칸 위로 올라오게") — 플레이 확인 대기
  - 구현: `EntityPresenter` 2D 모드(`_pixel2D` · `ConfigurePixel2D`) — 프리팹 · 캡슐 대신 `SpriteRenderer` 하나(재질 SpriteUnlit, 정렬 10 = 지형 효과 5 위 · 안개 20 아래). 발밑 = 칸 아래 끝 + 14px → 64px 그림의 머리가 위 칸으로 올라옴. z = −0.5 + y × 0.001(아래 줄 유닛이 앞). 회전 대신 `PixelFacing`(가로가 크면 옆 — 왼쪽은 flipX, 아니면 위 = 뒤 · 아래 = 앞), 이동 · 공격 · 시전 방향으로 바뀜. 걷기 = 걷는 동안(`Walking`) 그 방향 4장을 초당 10장, 대기 = 2번째 장(지나가는 자세). 그림이 있는 유닛은 통통 튐 · 숨쉬기 끔. 피격 = 붉게(스프라이트 색은 곱하기라 밝게 못 함). 방향 계산은 2D에서 z를 버림(`Flat`), 계단 서는 높이 안 씀
  - 데이터: `EntityVisualCatalog.Entry`에 `PixelIdle` · `WalkFront/Back/Side` + `SetPixel`. Setup이 캐릭터마다 `Characters/{id}_walk_{front,back,side}_0~3.png`를 찾아 넣음 — 지금은 hero_blond만(흑발 = 정지 그림 hero_black.png 하나, 걷기 없음). 캐릭터 색은 흰색으로(전에는 3D 캡슐용 초록). 적 · 보스는 아직 그림이 없어 그레이박스 원(카탈로그 Sprite · Tint)
  - 확인: 컴파일 0 · Setup(hero_blond 걷기 4 · 4 · 4, 2D 켜짐) · 플레이 모드 렌더 `51_units_2d_game.png` — 바닥 위에 금발 SD 스프라이트, 유닛 6개 모두 스프라이트. **확인 못 한 것:** 걷기 프레임 · 방향 바꾸기 · 피격 색 · 사망 연출은 직접 움직여 보지 않음, 테스트 추가 · 실행 안 함(기존 EntityPresenter 테스트는 3D 기본값이라 그대로). 이펙트(FxPresenter)는 아직 3D 기준 위치(ⓓ)
  - 남은 것(ⓑ 다음 단계): 적 · 보스 픽셀 그림(Varco), 공격 · 시전 · 피격 모션 그림(지금은 코드 찌르기 · 통통), 흑발 캐릭터 걷기
- [x] **P-3 ⓑ 적 2D 그림** (2026-10-23 사용자 "적과 보스 픽셀 그림을 Varco로") — 플레이 확인 대기
  - 과정: SD3 그림을 화풍 원본으로 EditImage(GPT 2.5). 1차(서 있는 몬스터 토끼 · 쥐 · 물방울 슬라임 · 분홍 슬라임 소녀) → 사용자 피드백: 쥐는 보내 준 그림(SPD 쥐, 업로드 안 함 — 분위기만 글로)처럼 **네 발로 기는 단순한** 쥐, 토끼는 **하얀 보통 토끼**, 슬라임은 **초록 · 흐름 없음 · 눈코입 없음**, 보스는 **원래 3D 모델의 하늘색 슬라임 소녀 이미지**를 바탕으로 → 2차(옆모습) → 사용자 "쥐랑 토끼도 플레이어처럼 3/4 각도" → 3차
  - 결정 (2026-10-23, 사용자): 쥐 = R3 · 토끼 = B1 · 슬라임 = S1 (3/4 각도, `Art/Pixel/Enemies/enemy_mouse · enemy_rabbit · enemy_slime.png`, 64px 캔버스 · 아래 가운데 피벗). 보스는 잠시 보류했다가(1차 큰 그림 `enemy_src/boss_slime_girl_hold.png`) 아래에서 플레이어 규격으로 다시 만듦
  - 구현: `EntityVisualCatalog.Entry.PixelVariants` + `PickPixelStill`(엔티티 ID로 고름 = 3D `PickPrefab`과 같은 식 · 같은 순서 토끼 · 쥐 · 슬라임 → 같은 적은 2D/3D 같은 동물) · `SetPixelVariants`. `EntityPresenter` 2D: 걷기 그림이 없고 정지 그림이 있으면 그것(흰색), 왼쪽으로 가면 좌우 뒤집기, 통통 튐 · 숨쉬기는 코드. Setup이 `Art/Pixel/Enemies`를 가져와(아래 가운데 피벗) 넣음. 도구 `Tools/sprite_fit.py`(키를 맞추되 가로가 넘치면 가로에 — 꼬리 잘림 방지, 마젠타 배경 지원 — 초록 슬라임용). 원본 AI 그림 `Docs/PixelTest/enemy_src`, 후보 비교 `52_enemy_drafts_x3.png` · `53_enemy_drafts2_x3.png`
  - 보스 (2026-10-23 사용자 "슬라임 보스도 플레이어와 비슷하게 규격을 맞춰서"): 원래 3D 모델의 하늘색 슬라임 소녀 이미지(원본) + SD3(규격 · 화풍 참고)로 EditImage — **2.5등신 SD · 플레이어와 같은 키(64px 캔버스 · 키 61) · 3/4 각도**. 후보 `57_boss_chibi_drafts_x4.png`, 결정 (2026-10-23, 사용자): **C2** → `Art/Pixel/Enemies/boss_slime_girl.png`(원본 `Docs/PixelTest/enemy_src/boss_slime_girl.png`). Setup이 보스 정지 그림으로 넣음(확인: 보스 변형 1장 · 아래 가운데 피벗). 판정은 그대로 1칸. 배치 (2026-10-23 사용자 "보스도 캐릭터와 똑같이 중앙에"): 보스도 같은 EntityPresenter 규칙(발 = 칸 아래 + 26px)이고 그림도 같은 규격(64px 캔버스 · 발끝 아래 1줄 · 가로 가운데)이라 코드 변경 없음 — 플레이 모드에서 주인공 옆 칸에 보스 그림을 같은 규칙으로 세워 렌더해 확인 `58_boss_center.png`(빨간 네모 = 칸)
  - 수정 (2026-10-23 사용자 — SPD 스크린샷: "플레이어는 네모(칸) 중앙에 배치하고 머리는 칸을 넘어도 나오게"): 유닛 발 높이 칸 아래 끝 + 14px → **26px**(칸 가운데쯤 — 스크린샷의 SPD 발 높이 ≈ 칸의 0.43). 몸이 칸 가운데에 서고 머리는 위 칸으로 약 23px 넘음. `EntityPresenter.ConfigurePixel2D(…, footPixels)` + Setup 상수 `UnitFootPixels = 26`(적도 같음). 확인: Setup(_footPixels 26) · 플레이 모드 렌더 `56_unit_center.png`(빨간 네모 = 칸)
  - 확인: 컴파일 0 · Setup(근접 적 변형 3장 · 피벗 아래 가운데, 보스 0장) · 플레이 모드 렌더 `55_enemy_game.png`(슬라임 · 토끼 · 쥐가 바닥 위에). 테스트 추가 · 실행 안 함. 적 걷기 · 공격 모션 그림은 없음(정지 그림 + 코드 움직임)
  - 다시 수정 (2026-10-23 사용자 "전체적으로 벽이 낮은 느낌 — 수치 비교" + 우리 · SPD 스크린샷): 비교 — 우리 벽 앞면(테두리 + 벽돌) 40px = 캐릭터 키(61)의 **0.6배**, SPD는 앞면 = 칸 하나(16) = 캐릭터(15)의 **1.05배**. 결정 (2026-10-23, 사용자 — 추천안): **SPD와 같게** — 모든 앞면 = 칸 전체(테두리 16 + 벽돌 48, 앞면 규칙 4종 `wall_front_nS_X`), 기둥 · 얇은 가로벽은 솟은 윗면을 위 칸 덮개로(`PixelWallCaps` · `CapAt` · `WallCap` · `wall_cap_M` — 8f38650의 방식을 되살림, 기둥 1.5칸 = SPD와 같음). RuleTile 규칙 37개, `Neighbor.Empty` 삭제. 확인: 컴파일 0 · Setup(규칙 37 · 빠진 그림 0 · 덮개 4장 · PixelWallCaps) · 미리보기 `59_wall_full_height_mock.png`(캐릭터와 키 비교) · 게임 렌더 `60_wall_full_height_game.png`. 덮개는 위 칸 유닛보다 아래에 그려짐
- [x] **P-3 ⓒ 아이템 픽셀 그림** (2026-10-26 사용자 "구현된 장비 · 아이템 모두 픽셀 그래픽으로 생성 · 적용") — 플레이 확인 대기
  - 그림 (Varco GPT 2.5, SD3를 화풍 원본으로 EditImage 시트 4장 — 칸 격자, 1번 시트 채택): 무기 6(단검 · 손도끼 · 창 · 육척봉 · 더크 · 낫) · 갑옷 5(천 · 가죽 · 사슬 · 비늘 · 판금) + 마법 융합기 · 물약 12색(진홍 · 호박 · 금 · 비취 · 파랑 · 하늘 · 남 · 분홍 · 갈 · 숯 · 은 · 상아) · 마법서 5(불 · 얼음 · 번개 · 독 · 물) + 빈 주문서. 주문서 12룬은 빈 주문서에 룬 획을 코드로(AI가 룬을 정확히 못 그림 — 3D 때 결정과 같음). 총 41장 `Art/Pixel/Items`(64px 캔버스 · 시트마다 같은 배율, 가장 큰 아이템 긴 변 40px · 가운데 피벗). 원본 시트 `Docs/PixelTest/item_src`, 모아 보기 `65_items_all_x3.png`, 도구 `Tools/items_cut.py`
  - 적용: `ItemVisualCatalog.Entry.Icon` + `TryGetIcon` · `SetIcon`(Setup `EnsureItemIcons`가 파일 이름으로 채움 — 아이템 ID 또는 겉모습 키). `FloorItemPresenter` 2D 모드(`ConfigurePixel2D`) — 겉모습 키 → 없으면 아이템 ID 그림을 칸 가운데에, 위아래 2px 흔들림(픽셀 단위) · 도는 것 없음 · 정렬 9(유닛 아래). 인벤토리 칸 왼쪽 · 빠른 칸 오른쪽에 같은 그림(아이템만, 주문은 글자만) — `HudText.VisualOf`(물약 · 주문서는 식별돼도 겉모습 그림). 반짝임 효과는 이펙트 2D(ⓓ)에서
  - 확인: 컴파일 0 · Setup(아이콘 8/8 표본 · 바닥 2D 켜짐 · 인벤토리 · HUD 연결) · 플레이 모드 렌더 `66_item_floor_game.png`(불 마법서 · 번개 마법서 · 단검 · 분홍 물약이 바닥에). **확인 못 한 것:** 인벤토리 · 빠른 칸 아이콘은 화면 UI라 렌더로 못 봄(직접 I 키 · 빠른 칸 확인 필요), 테스트 추가 · 실행 안 함
- [x] **P-3a4. 흐릿한 시야 경계** (2026-10-23 사용자 "시야 경계를 SPD처럼 흐릿하게") — 플레이 확인 대기
  - 구현: `FogPresenter._soft`(SpriteRenderer)가 있으면 Tilemap 대신 칸 하나 = 텍셀 하나인 텍스처(맵 + 둘레 한 줄, RGBA32, **Bilinear**)를 스프라이트(PPU 1, 왼쪽 아래 = 칸 (−1, −1))로 늘려 그림 — 텍셀 가운데 = 칸 가운데라 이웃 칸 사이에서 안개 농도가 서서히 바뀜(SPD 방식). 색은 그대로(안 본 칸 검정 · 본 적 있는 칸 검정 60% · 보이는 칸 투명), 바뀐 칸만 픽셀 갱신 후 한 번 Apply. Setup이 2D에서 `Fog/FogSoft`(SpriteUnlit · 정렬 20)를 만들어 연결(3D는 Tilemap 그대로). 디버그 맵 밝히기는 둘 다 끔
  - 수정 (2026-10-23 사용자 "아직 안 본 부분은 검은색으로" — 스크린샷: 번진 빛에 안 본 벽 테두리가 비침): 안 본 칸은 **번짐 없이 완전히 검정** — Fog Tilemap이 안 본 칸만 검정으로 칠하고 부드러운 안개 위(정렬 21)에 그림. 그러데이션은 보이는 · 본 칸 안쪽에서만(보이는 칸 → 안 본 칸 경계는 보이는 칸 쪽에서 반 칸 어두워지다 칸 경계에서 검정으로 끊김). 확인: 플레이 모드 렌더 `62_fog_unexplored_black.png`
  - 확인: 컴파일 0 · Setup(_soft 연결) · 플레이 모드 렌더 `61_soft_fog_game.png`(보이는 곳 가장자리가 칸 하나에 걸쳐 어두워짐). 테스트 추가 · 실행 안 함(기존 안개 테스트는 _soft 없이 Tilemap 경로)
  - 수정 (2026-10-23 사용자 "한 줄 벽이 옆 벽보다 반 칸 튀어나와 있다" — 위 · 아래가 바닥인 가로벽): 솟은 윗면 덮개는 **한 칸 기둥(양옆도 벽 아님)에만** — 옆으로 이어진 한 줄 벽은 보통 앞면만(옆 벽과 같은 높이). `PixelMapPresenter.CapAt`: 아래 벽의 왼쪽 · 오른쪽이 벽이면 null, 기둥은 양옆 닫힌 고리(`WallCap(3)`). 미리보기 `63_wall_row_flush_mock.png`. 컴파일 0, 게임 렌더 · 테스트 실행 안 함
  - 결정 (2026-10-22, 사용자): **유닛 스프라이트는 머리가 칸 위로 올라오게** (SPD처럼 위 칸에 겹침) — 지금 게임 유닛은 아직 3D라 P-3 ⓑ(유닛 2D)에서 적용: 발을 칸 아래쪽에 두고 스프라이트를 칸보다 크게(또는 위로 올려) 그림, 유닛 정렬은 맵 · 벽 위
- [~] **P-3b 걷기 모션 (금발 마법사)** (2026-10-22 사용자 요청 "걷는 모션 4개 이상씩 Varco로")
  - 결정 (2026-10-22, 사용자): 방향 = **앞 · 뒤 · 옆 3방향**(옆은 좌우 뒤집기로 왼쪽 · 오른쪽 공용) — 추천안
  - SD3을 원본으로 EditImage(GPT 2.5) — 방향마다 2×2칸 4프레임(왼발 디딤 · 지나감 · 오른발 디딤 · 지나감) 시트 2장. 자르기: 시트 하나는 같은 배율(가장 큰 프레임 키 → 61px), 발끝 62줄, 칸 가운데 x = 32, 칸 가운데 색(평균 안 함), 칸 경계선은 안쪽 16px 잘라 뺌 — grid64 자동 칸 크기는 프레임마다 크기가 달라져서 안 씀. 후보 6벌 `42_walk_frames_x4.png` · `43_walk_all.gif`(줄 1 = 앞1 · 뒤1 · 옆1, 줄 2 = 앞2 · 뒤2 · 옆2). 옆은 완전한 옆모습이 아니라 3/4 옆. 프로젝트 에셋에 넣음
  - 결정 (2026-10-22, 사용자): **앞2 · 뒤1 · 옆2**, 옆은 지금 3/4 옆모습 그대로 좋음("옆모습 지금이 딱 좋아") — 왼쪽은 뒤집어 씀
  - 에셋: `Art/Pixel/Characters/hero_blond_walk_{front,back,side}_{0~3}.png`(hero_blond.png와 같은 가져오기 설정 — 점 필터 · 64 PPU · 아래 가운데 피벗). 원본 시트 `Docs/PixelTest/walk_src`, 자르기 도구 `Tools/walk_frames.py`, 미리보기 `44_walk_chosen.gif`(앞 · 뒤 · 오른쪽 · 왼쪽). **게임에서는 아직 안 씀** — 유닛 2D 표시(P-3 ⓑ)에서 이동 방향 → 프레임으로 연결
## Phase G — 3D 그래픽 (결정 2026-10-18: 재미 검증보다 먼저, Varco 3D 모델, 실시간 3D)
목표: 규칙(Simulation)은 그대로 두고 표현 레이어만 3D로. 모델이 오기 전에 3D 틀(그레이박스)을 먼저 만들고, 모델 · 이펙트는 ID에 끼우기만 한다. 목록 · 규격 `Docs/ART_ASSETS.md`.

- [x] **G-1. 3D 렌더러 · 3D 그레이박스** (2026-10-18) — 테스트 실행 · 플레이 확인 대기
  - 결정 (2026-10-18, 사용자 답): 그래픽 먼저 · Varco 3D 모델 · A. 실시간 3D (추천안). ART_ASSETS.md 0-1 공통 규격은 추천값으로 시작 (1칸 = 1 m · 카메라 55° · 벽 1.1 m · 유닛 0.8~0.9 m · 보스 1.8 m · 원점 = 발밑 중앙)
  - 정한 것 (Claude ✎): ① Grid를 X축 90° 돌려 바닥에 눕힘 — 칸 (x, y) = 월드 (x + 0.5, 0, y + 0.5), 규칙 좌표 그대로. ② 바닥 · 계단 · 지형 효과 · 안개 · 조준은 지금 Tilemap을 바닥에 눕혀 그대로 씀(1 · 2 · 3 cm씩 띄움), 벽만 3D 블록 — 바닥과 닿은 벽만 만들고(안쪽 벽 생략), 벽은 안개 위로 솟으므로 시야를 직접 따름(안 본 벽 숨김 · 본 적 있는 벽 35% 어둡게). ③ 카메라 = 원근 35° · 아래로 55° · 거리 16 m (세로 약 11~12칸), 인스펙터 값. ④ 화면 → 칸 = 카메라 광선과 바닥 평면 교점(수학, 물리 안 씀 — 규칙 2). ⑤ 유닛 = VisualId 프리팹이 있으면 그것, 없으면 캡슐(플레이어 초록 0.9 m · 적 빨강 0.8 m · 보스 자주 1.8 m), 이동 방향을 바라봄, 충돌체 없음. 상태 틴트는 재질 복제 없이 MaterialPropertyBlock. ⑥ 해 1개(Directional, 부드러운 그림자). ⑦ 이전 2D 파이프라인 에셋(URP2D · Renderer2D)은 파일만 남김
  - 구현 — Runtime: `EntityPresenter`(3D — `EntityView` 루트 + 렌더러, 프리팹 · 캡슐, 바라보기, 시야, 틴트), `EntityVisualCatalog`(+ `Prefab` · `Height`, `SetPrefab`), `TilemapPresenter`(+ 3D 벽 `_walls3D` · `_wallPrefab` · `_wallMaterial` · `_wallHeight`, 시야 · `RevealAll`), `CameraController`(고각도 · 원근 · `ScreenToGround`), `PlayerInputController.ScreenToCell`(바닥 교점), `GameRoot.DebugRevealAll`에 벽 포함
  - 구현 — Editor: Setup 메뉴 — URP Universal 파이프라인 에셋(`Settings/Rendering/URP3D.asset` · `Renderer3D.asset`) 생성 · 기본 지정, 해(`Sun`), Grid 90° · 덮개 높이, 3D 벽 켜기 · 벽 재질(`Data/Visuals/Materials/WallGreybox.mat`), 카메라 원근, 유닛 색 · 키. Editor asmdef에 URP 참조 2개
  - 테스트 (작성만, 실행 안 함): `PresenterTests` 수정 2(3D 모습 · 충돌체 없음 · 시야) + 추가 3(3D 벽 — 바닥과 닿은 벽만 · Tilemap에 안 그림 · 시야 따름 · 밝히기, 이동 방향 바라보기 · 상태 틴트, 화면 → 바닥 → 칸) — 전체 240개 예상
  - 확인: Unity 컴파일 오류 · 경고 0 · Setup 메뉴 실행 → 파이프라인 URP3D. 편집 모드에서 시드 20261018로 판을 시작해 카메라 렌더(1280×720) — 눕힌 바닥 · 3D 벽 · 초록 캡슐 · 고각도 시야 확인, 시야 밖 벽 숨김. 씬은 디스크 상태로 다시 열어 저장 안 함. 콘솔 프로젝트 오류 0
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드(클릭 이동이 3D에서 맞는 칸을 고르는지 · 조준 미리보기 · 안개 어두운 칸 · 층 이동 때 벽 다시 만들기 · 성능)
  - 확인 방법: Test Runner EditMode 전체(240개 예상). `Main.unity` 플레이 → 기울어진 3D 화면, 회색 벽 블록, 초록 캡슐(나) · 빨강 캡슐(적). 바닥 칸 클릭 → 그 칸으로 이동, 주문 조준 칸이 마우스 아래 칸과 맞는지, 본 적 있는 곳의 벽이 어둡게 · 안 본 곳은 검정
- G-2 범위 결정 (2026-10-18, 사용자 답): **G-2a 모델 연결 / G-2b 움직임으로 나눔** (추천안) · 근접 적 모델 = **토끼 · 쥐 · 슬라임 중 엔티티 ID로 고정 무작위 (표시만)** · 애니메이션 = **코드 움직임 먼저** (추천안 — 클립이 없음: 양 · 토끼만 뼈대, 나머지는 고정 모델) · 기둥 = **외톨이 벽(상하좌우에 벽 없음) = 기둥** (추천안)
- [x] **G-2a. Varco 모델 연결** (2026-10-18) — 테스트 실행 · 플레이 확인 대기
  - 에셋: `Art/Varco3D/` FBX 11개(Varco 플로우에서 받아 이름 변경 — 토끼는 사용자가 리깅된 모델로 교체) · 텍스처 `Textures/<이름>_base_color · _normal`(FBX에 묻힌 텍스처를 꺼냄 — 모든 모델이 같은 이름이라 섞임 방지) · 재질 `Materials/<이름>_Mat`(URP Lit, FBX `material_0`을 재질로 다시 연결). 블렌더 원본은 `Source~/`
  - 구현 — Editor: 메뉴 **ProjectM/Setup/Varco Models** (`VarcoModelSetup`, 멱등) — 프리팹 `Prefabs/Varco/`(루트 = 발밑 중앙 · 앞 +Z, 아래 모델에 크기 · 방향 보정, FBX 루트의 축 회전은 유지), 양 오른손 뼈에 지팡이, 바닥 판은 윗면 = y 0으로 내림, FBX 조명 · 카메라 가져오기 끔, 재질 GPU 인스턴싱, 카탈로그 · 타일 세트 연결. 크기 · 방향 표는 ART_ASSETS.md 12번
  - 구현 — Runtime: `EntityVisualCatalog`(+ `Variants` · `PickPrefab`(ID % 개수) · `SetVariants`, 프리팹엔 카탈로그 색을 안 곱함), `TileVisualSet`(+ 타입별 `Module` · `RandomRotation`, `PillarModule`), `TilemapPresenter`(칸마다 3D 모듈 — 모듈 있는 타입은 Tilemap에 안 그림, 외톨이 벽 = 기둥 + 바닥, 바닥 판은 위치로 90° 돌림, 타일이 바뀌면 자기 칸 + 8이웃 다시 세움, 높은 모듈(> 0.15 m: 벽 · 기둥 · 계단)만 시야를 따르고 낮은 바닥 판은 안개 덮개가 가림, 바뀐 칸만 다시 칠함)
  - 테스트 (작성, 실행 안 함): PresenterTests +4 — 변형 ID 고정 · 프리팹 색 안 곱함 · 바닥 · 계단 · 기둥 모듈과 시야 · 타일 변경 시 이웃 다시 세움
  - 확인: 컴파일 오류 0 · Varco Models 메뉴 실행 · 편집 모드에서 시드 20261018로 판을 시작해 카메라 렌더 — Varco 바닥 · 벽, 양 마법사(지팡이), 슬라임 적 확인. 보스 방 맵을 그려 기둥 4 · 올라가는 계단 · 1.8 m 보스 확인. 씬은 디스크 상태로 다시 열어 저장 안 함
  - **확인 못 한 것:** 테스트 실행(사용자가 직접) · 플레이 모드(성능 — 바닥 칸 1,000개 이상 × 1,000 삼각형, 층 이동 때 다시 세우기, 시야 · 안개와 모듈이 맞는지) · 토끼 · 쥐가 화면에 나오는 모습 · 계단 위에 선 유닛(계단 모듈 0.4~0.57 m에 발이 묻힘)
  - 확인 방법: Test Runner EditMode 전체(244개 예상). `Main.unity` 플레이 → 돌 바닥 · 벽돌 벽, 지팡이 든 양 마법사, 적은 토끼 · 쥐 · 슬라임 중 하나(같은 적은 늘 같은 모습). 안 본 곳 검정 · 본 곳 어둡게. 디버그 창 맵 전체 밝히기 + 계단으로 5층 → 기둥 4개 · 파란 슬라임 소녀 보스(플레이어 두 배)
  - 피드백: 블렌더에서 내보낸 FBX에 기본 씬 점광원(1000 W)이 섞여 플레이어 주변이 하얗게 날아갔다 → 원인: FBX 조명 가져오기 기본값 켜짐 · 해결: Varco Models 메뉴가 Varco3D FBX의 조명 · 카메라 가져오기를 끔 · 검사: 씬 Light가 Sun 하나뿐인지. / FBX 루트(Z-up → Y-up 축 회전 270°)를 덮어쓰면 모델이 눕는다 → 보정 회전은 곱한다
- [x] **G-2b. 코드 움직임** (2026-10-18) — 테스트 실행 · 플레이 확인 대기
  - 구현 — `EntityPresenter`: 이벤트는 목표만 바꾸고 프레임마다 `Advance(dt)`가 그린다 (규칙은 안 돌림 · 로직은 기다리지 않음, 모두 0.4초 안 — 실시간 모드 1초 규칙). 이동 = 보이는 위치에서 새 칸까지 0.12초 보간 + 0.08 m 통통 (제자리 이동 = 층 이동 · 1.6칸보다 먼 이동은 바로) · 근접 공격(UnitDamagedEvent, 원인 Attack) = 공격자가 대상 쪽으로 0.3 m 찌르고 바라봄 · 피격 = 0.22초 흰 번쩍(밝기 2.2배 → 1) + 흔들림 · 시전(SpellCastEvent) = 대상 칸을 보고 0.12 m 통통 · 사망(UnitDiedEvent 뒤 EntityRemovedEvent) = 보이던 것만 0.4초 동안 0.3 m 가라앉으며 작아진 뒤 지움 · 대기 = 엔티티마다 박자가 다른 숨쉬기(세로 3%). 수치는 인스펙터(표시 전용). `CameraController.FollowTarget`(SmoothDamp 0.15초) — 걸음은 따라가고 층 이동 · 새 판은 `SnapToTarget`. `TileVisualSet.StandHeight` + `TilemapPresenter.StandHeight` → 올라가는 계단 위 0.3 m (`EntityPresenter._ground`, Setup이 연결)
  - **G-1 버그 수정:** 씬 Grid의 cellSize z가 1이라 칸 중앙이 y −0.5 → 유닛 · 3D 모듈이 바닥 평면(y = 0: 클릭 판정 · 지형 효과 · 안개 · 조준 덮개)보다 0.5 m 아래에 있었다. Setup이 cellSize = (1, 1, 0)으로 고정. 테스트는 기본 Grid(z = 0)라 못 잡았다
  - 테스트 (작성, 실행 안 함): PresenterTests +4 (이동 보간 · 제자리 이동 즉시, 찌르기 · 번쩍, 사망 연출 뒤 지움, 계단 서는 높이) · 기존 이동 테스트에 `FinishMotions()` 추가
  - 확인: 컴파일 오류 0 · Main Scene · Varco Models 메뉴 실행 · 편집 모드에서 판을 시작해 이동 + 시전 이벤트를 흘려 중간 프레임(통통 · 숨쉬기) → 끝 위치 확인, 칸 중앙 y = 0 확인 · 렌더. 씬은 Setup이 저장(Grid 칸 두께 · `_ground` 연결)
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드(실제 프레임에서 움직임 체감 · 카메라 따라가기 · 클릭 경로 이동(0.08초 간격)과 0.12초 보간이 겹칠 때 · 사망 연출 · 클릭 판정이 이제 맞는지)
  - 확인 방법: Test Runner EditMode 전체(248개 예상). `Main.unity` 플레이 → 걸으면 칸 사이를 통통 튀며 미끄러지고 카메라가 부드럽게 따라옴, 적을 치면 앞으로 찌름 · 맞은 적이 하얗게 번쩍, 주문을 쓰면 대상 쪽을 보고 통통, 적이 죽으면 가라앉으며 사라짐, 가만히 있으면 숨쉬기. 칸을 클릭하면 마우스 아래 칸으로 감(이전엔 0.35 m쯤 어긋났을 수 있음). 계단으로 내려가면 카메라가 바로 옮겨지고 도착한 올라가는 계단 위에 서 있음
  - 피드백: Grid를 눕힐 때 cellSize z(칸 두께)가 0이 아니면 GetCellCenterWorld가 바닥 아래를 준다 → 검사: Setup 뒤 `grid.GetCellCenterWorld(0,0).y == 0`
- [x] **G-3. 이펙트** (2026-10-19) — 플레이 확인 완료 (2026-10-19, 사용자) · 테스트 실행 대기 — 지형 효과 5 · 상태 효과 6 · 주문 5 + 융합 4 · 반응 19 · 조준 데칼 4
  - 결정 (2026-10-19, 사용자): 에셋 = 사용자가 가져온 **Hovl Studio Magic effects pack + Eric VFX Studio Free Game VFX** (`Assets/_Project/FX`, Shuriken 프리팹). Hovl 재질(Built-in 셰이더 34개) = **Setup 메뉴로 URP 변환** — 추천안
  - 결정 (2026-10-19, 사용자): 범위 = **한 번에 전부** (추천안은 G-3a 지형 · 상태 / 3b 주문 · 반응 / 3c 조준으로 나누기였음)
  - 결정 (2026-10-19, 사용자): **Bloom 약하게** 켬 (Global Volume, threshold 1.1 · intensity 0.6 · scatter 0.6, 카메라 후처리 켬) — 추천안
  - 결정 (2026-10-19, 사용자): 팩의 **데모 씬만 지움** (Hovl 데모 .unity 2 · 라이팅 · 라이트맵 · 반사 프로브 · PP low 프로필, Eric 데모 씬) — 프리팹 · 재질 · 텍스처는 그대로 — 추천안
  - 정한 것: 효과 선택 · 크기는 팩 프리팹을 시간대별(0.1 · 0.25 · 0.5 · 0.9초)로 렌더해 고름 — 표는 `FxSetup.Table` = ART_ASSETS.md 13번. 팩 효과는 모두 반복(데모) 설정이라 한 번 재생 효과는 복제본의 반복을 끄고 `Lifetime` 뒤 지운다, 크기는 `scalingMode = Hierarchy` + 배율(원본이 3~12 m), 색은 시작 색에 속성 색 곱(밝기는 원래 색에서). 루프(지형 · 상태 · 투사체)는 0.8초 미리 돌려 놓인 순간부터 차 보이게. 물 · 빙판은 파티클 없이 바닥 무늬만, 번개 줄기 · 감전 사슬은 LineRenderer(꺾임은 위치 · 시간으로 정한 값 — 난수 아님)
  - 구현 — Runtime: `Presentation/FxCatalog`(키 → 프리팹 · 크기 · 색 · 속도 · 길이 · 높이, 키 규칙 terrain · status · cast · projectile · impact · burst · fusion · reaction · line), `Presentation/FxPresenter`(이벤트만 구독: 지형 = 칸 루프 · 시야 밖 숨김, 상태 = 유닛 모습에 붙는 루프 · 유닛이 안 보이면 숨김 · 지워지면 같이, 주문 = 지팡이 빛 → Bolt · Burst 투사체(최대 0.35초) / Beam 번개 선 → 도착 시 맞는 효과(융합은 fusion 키 우선), 반응 = 같은 순간의 주문이 있으면 투사체 도착까지 늦춤 · 감전 반응끼리 번개 선으로 이음, MapLoaded(새 판 · 층 이동) = 전부 지움, `Advance(dt)` · `FinishTransients()`). `TerrainOverlayPresenter` 종류별 바닥 무늬 타일, `AimPreviewPresenter` 경로 점 · 맞는 칸 테두리 · 조준 괄호 · 막힘 X 타일, `GameRoot._fx`(엔티티 모습 다음에 Bind · 디버그 밝히기 포함)
  - 구현 — Editor: 메뉴 **ProjectM/Setup/FX** (`FxSetup`, 멱등 — Main Scene · Varco Models 다음에 실행): ① FX 재질 Built-in → URP(`ParticleUpgrader` · `StandardUpgrader`) + 소프트 파티클 끔 ② 바닥 무늬 5 · 데칼 4 · 번개 선 텍스처를 코드로 그려 PNG(`Art/FX/Generated`) + 타일(`Data/Visuals/FxTiles`) + 번개 선 재질(가산) ③ `Data/Visuals/FxCatalog.asset` 53줄 ④ 씬 `Fx` 오브젝트 · 덮개 · 조준 · GameRoot 연결, `PostFX` 볼륨(`Settings/Rendering/PostFX.asset`). Editor asmdef에 `Unity.RenderPipelines.Core.Editor` · `Universal.Editor` 참조 추가
  - 테스트 11개 작성, **실행 안 함** (`FxPresenterTests` — 지형 루프 놓기 · 갱신 시 중복 없음 · 사라지면 끔, 표에 없는 키는 아무것도 안 함, 시야 밖 지형 숨김 · 디버그 밝히기, 상태 효과가 유닛에 붙고 유닛과 함께 지워짐, 상태 갱신 중복 없음 · 풀리면 끔, Bolt 투사체 → 도착 후 맞는 효과 · 모두 정리, 같은 순간 반응은 투사체 도착까지 대기, Burst는 burst 키 · 경로 끝 칸 중심, 시야 밖 주문은 아무것도 안 띄움, MapLoaded 전부 지움, FxCatalog Set · RemoveExcept)
  - 확인: 컴파일 오류 0 · Setup FX 실행(재질 변환 34 · 소프트 파티클 10 · 표 53줄 · 빠진 프리팹 0) · 편집 모드에서 판을 시작해 지형 5 · 상태 · 반응 · 주문 이벤트를 흘려 1600×900 렌더 3장 — 바닥 무늬(물결 · 얼음 금 · 독 · 그을음), 조준 괄호 · 점, 독구름 연기, 3×3 독 폭발 · 빙결 · 증기 · 가스 폭발, 번개 줄기 · 감전 사슬 선, 유닛 위 마비 · 화상 · 빙결. 렌더 뒤 씬은 디스크 상태로 다시 엶
  - **확인 못 한 것 / 아쉬운 점:** 테스트 실행 · 플레이 모드 전체(실제 프레임 재생 · Bloom 세기 · 성능 — 지형 칸이 많을 때 파티클 수). 물 칸은 정지된 물결 무늬(움직이는 물 셰이더 아님) · 빙판 반사 없음. 화염 지대 불씨가 작음, 얼음 · 독 · 물 투사체는 불꽃 튐(Sparks) 흔적이라 몸체가 약함. Eric 팩 FBX 3개의 .meta가 옛 형식이라 가져올 때 경고(동작엔 문제없음). 크기 · 색은 플레이 후 FxCatalog(또는 Setup 표)에서 조정
  - 확인 방법: Test Runner EditMode 전체(259개 예상). `Main.unity` 플레이 → 불 = 지팡이 주황 빛 → 불덩이가 날아가 터짐 · 화염 지대 칸에 불씨, 얼음 = 하늘색 터짐, 번개 = 꺾인 노란 선 · 끝에 전기, 독 = 3×3 보라 돔 터짐 · 독구름 칸에 보라 연기, 물 = 파란 물결 칸. 젖은 적 여럿에 번개 → 적에서 적으로 번개 선. 조준 = 경로 점 · 맞는 칸 테두리 · 흰 괄호 / 못 쓰면 빨간 X. 시야 밖 효과는 안 보임 · 층 이동하면 이전 층 효과가 사라짐
  - 피드백: ① Hovl 연기 · 불꽃 재질이 URP 변환 뒤 통째로 안 보임 → 원인: 소프트 파티클이 카메라 깊이 텍스처를 쓰는데 렌더러에서 꺼져 있음 → 해결: Setup이 FX 재질의 소프트 파티클을 끔 → 검사: Setup 로그 · 연기 효과 렌더. ② 가져온 팩 효과는 대부분 반복 · 크기 수 m의 데모 설정 → 표에서 Lifetime · Scale로 맞춤 (새 효과를 넣을 때도 시간대별 렌더로 확인)
- [x] **G-2c. 플레이어 애니메이션** (2026-10-19) — 플레이 확인 완료 (2026-10-19, 사용자) · 테스트 실행 대기 — 사용자가 넣은 `Assets/_Project/Animation/Idle.fbx` · `Walk.fbx`(Mixamo)를 플레이어에 적용: 평소 Idle, 이동할 때 Walk (사용자 요청 2026-10-19)
  - 정한 것 (기술상 필요 — 따로 묻지 않음): 클립 뼈대(`mixamorig:*`, 55뼈 + 다른 캐릭터의 검집 뼈)와 양 마법사 뼈대(`SheepRig/Hips/LeftUpperLeg…`, 21뼈)가 달라 Generic으로는 붙지 않는다 → **양 · Idle · Walk 모두 Humanoid(이 모델로 아바타)로 가져와 리타깃** (G-2a 메모 "SheepMage Generic — 클립을 붙일 때 정한다"의 답). 클립은 반복 · 루트 회전 · 높이 · 이동을 포즈에 굽고(제자리), 위치 · 방향은 지금처럼 `EntityPresenter`가 맡는다(`applyRootMotion` 끔)
  - 정한 것: 컨트롤러 `Animation/PlayerAnimator.controller` = Idle(기본) ↔ Walk, bool `Moving`, 전환 0.12초 · 종료 시간 없음, Walk 1.5배속(한 칸 0.12초 이동이라 걸음을 빠르게). 걸어서 옮긴 칸이면 Walk, 이동 보간 + 0.2초(`_walkHold`) 뒤 Idle — 키를 누르고 있거나 클릭 경로로 이어 걸으면 Walk 유지. 층 이동(제자리 이동)은 걷지 않음. 애니메이션 모델은 코드 통통 튐 · 숨쉬기를 끄고(클립이 맡음) 찌르기 · 피격 번쩍 · 시전 통통 · 사망 가라앉기는 그대로. 지팡이는 Idle 첫 자세에서 세워 오른손에 붙임(걸으면 팔과 함께 흔들림)
  - 구현 — Editor: `VarcoModelSetup` — `EnsureHumanoid`(Humanoid · 클립 이름 Idle/Walk · 반복 · 루트 굽기, 멱등), `EnsurePlayerController`(상태 · 전이 · 파라미터, 있으면 GUID 유지하고 다시 맞춤), `SetupAnimator`(플레이어 모델 Animator에 컨트롤러 · 루트 이동 끔 · Idle 첫 자세) → `AttachStaff`. 클립이 없으면 경고만 하고 코드 움직임으로
  - 구현 — Runtime: `EntityPresenter` — `EntityView.Animator`(컨트롤러가 붙은 Animator, 없으면 null) · `Walking`, 이동 이벤트에 `WalkUntil`, `Advance`가 `Moving`을 켜고 끔. `MovingParameter = "Moving"`
  - 테스트 1개 작성, **실행 안 함**: `PresenterTests.Motion_WalkWhileMoving_IdleAfterHold_TeleportDoesNotWalk` (걸으면 Walking · 유지 시간 뒤 Idle · 층 이동은 안 걸음 · 캡슐은 Animator 없음)
  - 확인: 컴파일 오류 0 · Varco Models 메뉴 실행(플레이어 프리팹 Animator = PlayerAnimator · SheepMageAvatar · 루트 이동 끔, 상태 Idle 기본 · Walk 1.5배) · 리타깃 렌더 — Idle 4프레임(지팡이 세워 쥠) · Walk 4프레임(다리 내딛기 · 팔 흔들기)
  - **확인 못 한 것:** 테스트 실행 · 플레이 모드(실제 Walk ↔ Idle 전환 타이밍 · 발 미끄러짐 체감 · 키 반복 · 클릭 경로 중 깜빡임 여부)
  - 확인 방법: Test Runner EditMode 전체(273개 예상). `Main.unity` 플레이 → 가만히 있으면 양 마법사가 숨 쉬듯 Idle, 방향키를 누르고 있거나 칸을 클릭해 걸으면 Walk, 멈추면 0.2초쯤 뒤 Idle. 걸음이 너무 빠르거나 느리면 컨트롤러 Walk 상태 Speed(지금 1.5), 깜빡이면 `EntityPresenter._walkHold`(지금 0.2초)를 조정
  - 수정 (2026-10-19, 사용자 플레이 피드백 — "걸을 때 목표 지점보다 앞으로 이동"): 원인 = 클립의 앞뒤 루트 이동(XZ)을 포즈에 구워(`lockRootPositionXZ`) Walk 한 주기 동안 몸(엉덩이)이 루트보다 **0.74 m 앞으로** 밀려 나갔다가 튀어 돌아옴. 해결 = XZ는 굽지 않고 루트로 빼서 버림(`applyRootMotion = false`) — 회전 · 높이만 굽기. 다시 잰 값: 한 주기 내내 엉덩이 앞뒤 0.00 m (클립 본래 속도 1.63 m/s는 버려지고 칸 이동은 코드만). 검사 = Varco Models 실행 뒤 Walk 시간대별 엉덩이 위치
  - 피드백: ① 클립 FBX(Mixamo)와 Varco 모델의 뼈 이름이 다르면 Generic 클립은 움직이지 않는다 → 둘 다 Humanoid로 리타깃 (검사: 아바타 `isHuman` · `isValid`, Varco Models 로그). ② 편집 모드에서 애니메이션 미리보기는 Animator 컬링 때문에 포즈가 안 바뀜 → 미리보기 동안 `AlwaysAnimate` + `Rebind`. ③ Idle.fbx · Walk.fbx에 함께 든 검 메시(법선 없음)가 가져올 때 탄젠트 경고를 낸다 — 동작엔 문제없음
  - 수정 (2026-10-19, 사용자 피드백 — "빌드만 하면 책이 마젠타"): 원인 = `GameObject.CreatePrimitive`가 붙이는 기본 재질(Built-in Standard 셰이더)은 에디터에서는 보이지만 URP 빌드에 셰이더가 들어가지 않아 마젠타. 코드로 만드는 바닥 책(`FloorItemPresenter`)과 그레이박스 캡슐(`EntityPresenter`)이 해당 (벽은 이미 `_wallMaterial`). 해결 = 흰 URP Lit `Data/Visuals/Materials/ShapeGreybox.mat`(Main Scene Setup이 없을 때만 만듦)을 씬이 참조(`_bookMaterial` · `_greyboxMaterial`)해 빌드에 포함, 색은 지금처럼 PropertyBlock. 확인: Setup 뒤 책 셰이더 = Universal Render Pipeline/Lit
  - 피드백: 런타임에 `CreatePrimitive` · `new Material(Shader.Find(...))`를 쓰면 빌드에서 셰이더가 빠질 수 있다 → 재질은 씬 · 프리팹이 참조하는 URP 에셋으로 넘긴다. 검사: `Scripts/Runtime`에서 `CreatePrimitive` 검색 → 모두 재질 필드로 바꿔 끼우는지
- [ ] **G-4. UI 아트** — 아이콘 12(모델 렌더) · 막대 · 빠른 칸 · 인벤토리 · 로그 바탕 · 결과 화면 · (폰트 결정) — **일시중지 (2026-10-19, 사용자): 아트 에셋이 없음.** 재개 조건: ART_ASSETS.md 9번 목록의 1차 에셋(아이콘 · 틀)을 받거나, Varco 모델 렌더로 아이콘을 만들기로 정할 때. 재개 시 먼저 물을 것: 아이콘 출처(직접 렌더 / 에셋) · 폰트(Galmuri11 유지 / 한글 일반 폰트) · 범위

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

1. **원래 PC에서:** 작업 브랜치를 main에 병합 · 푸시했는지 확인 (`git status` 깨끗, `git log origin/main`에 최신 커밋). 푸시는 사용자가 직접 (`git push origin main`) — G 단계부터 에셋(Varco · FX 팩)이 커서 푸시가 오래 걸린다 (Git LFS 안 씀, 파일당 100 MB 한도 안)
2. **새 PC에서 받기:** `git clone https://github.com/Shaun1667/Project_M.git` (이미 있으면 `git pull`). 작성자 이메일을 같은 값으로 맞춘다 (`git config user.email`)
3. **Unity 6.6 (6000.6.0f1)로 열기** — Unity Hub에 해당 버전 설치 필요. 첫 임포트는 오래 걸린다
4. **콘솔 오류 0 확인** — `Assets/_Project/Scenes/Main.unity` 열기. 씬 참조가 비었거나 모델 · 이펙트가 이상하면 메뉴를 **이 순서로** 실행(모두 몇 번 실행해도 같은 결과): `ProjectM/Setup/Main Scene` → `ProjectM/Setup/Varco Models` → `ProjectM/Setup/FX`. 실행 뒤 `Galmuri11 SDF.asset`이 바뀌면 `git checkout -- "Assets/_Project/Art/Fonts/Galmuri11 SDF.asset"`. Eric 팩 FBX .meta 옛 형식 경고 3개는 무시해도 됨
5. **테스트:** `Window > General > Test Runner > EditMode` → `ProjectM.Tests.EditMode` 실행 → 전부 초록 (개수는 진행 요약 "숫자" 참고)
6. **플레이 확인:** Main 씬 플레이 → 돌 바닥 · 벽돌 벽 · 양 마법사, 콘솔에 시드. 주문 · 아이템은 디버그 창(ProjectM/Debug/Debug Window)으로 넣어 이펙트 확인
7. **Claude Code 연결 (선택):** CLAUDE.md "새 PC 환경 준비"대로 Unity MCP 등록 → 새 세션 → Unity에서 연결 승인. 세션은 프로젝트 폴더에서 연다
8. **새 세션 첫 요청 예:** "CLAUDE.md와 TASKS.md 진행 요약, Docs/진행상황.md 읽고 이어서 진행해줘" (다음 작업이 미정이면 Claude가 후보를 묻는다)

주의: 이 PC의 Claude 메모리(`~/.claude/projects/…`)는 옮겨지지 않는다. 필요한 내용은 모두 이 저장소의 문서(CLAUDE · TASKS · GDD · BALANCE)에 있다.

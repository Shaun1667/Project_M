# itch.io 페이지 꾸미기 (NaruDungeon) — 2026-10-26

페이지: https://namuletta.itch.io/narudungeon — 수정은 itch.io에 로그인한 뒤 **Edit game** 화면에서.

## 1. 그림 (이 폴더)

| 파일 | 넣는 곳 | 크기 |
|---|---|---|
| `cover_630x500.png` | Edit game → **Cover image** (목록 썸네일) | 630 × 500 (itch 권장) |
| `banner_960x400.png` | Edit theme → **Banner** | 960 × 400 |
| `background_tile_256.png` | Edit theme → **Background** → 이미지, *Repeat* 켜기 | 256 × 256 반복 |
| `screenshot_1~4_*.png` | Edit game → **Screenshots** | 게임 렌더 (1번 = 탐험, 2 = 던전, 3 = 적, 4 = 아이템 모음) |

그림은 `Tools/itch_assets.py`가 게임 픽셀 그림으로 다시 만든다 (`python Tools/itch_assets.py`).

## 2. 테마 색 (Edit theme)

| 항목 | 값 | 비고 |
|---|---|---|
| BG color (바깥 바탕) | `#0B0C10` | 배경 이미지 아래 색 |
| BG2 color (본문 상자) | `#16181F` | 어두운 남색 |
| Text color | `#E6E8EE` | |
| Link color | `#FFCE5C` | 제목 글자 금색 |
| Button color | `#EE8C29` | 주황 (발표 자료와 같은 색) |
| Font | 제목 · 본문 모두 **Lato**(기본) 또는 픽셀 느낌이면 목록의 *Silkscreen* 계열 | itch가 주는 글꼴 목록 안에서 |
| Layout | 본문 왼쪽 + 스크린샷 오른쪽 칸 | 기본 레이아웃 |

## 3. 설명 (Edit game → Description)

`description.html` 내용을 그대로 붙여 넣는다 (itch 편집기의 HTML 보기, 또는 그대로 복사 · 붙여넣기).

## 4. 그 밖에 맞춰 둘 것

- **Genre:** Role Playing / Strategy · **Tags:** roguelike, turn-based, pixel-art, magic, dungeon-crawler, singleplayer
- **AI generation disclosure:** 그래픽에 AI 이미지 생성을 썼으므로 *Yes — Graphics*로 표시 (itch 정책)
- **Short description:** 속성과 상태가 맞물리는 턴제 마법 로그라이크
- 다운로드 파일(NaruDungeon.zip)이 3D 시기 빌드라면 2D 빌드로 바꾸거나 설명의 "개발 상태"를 맞춘다

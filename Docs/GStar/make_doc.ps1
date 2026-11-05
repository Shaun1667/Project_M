# 지스타 출품 양식 → Word(.docx) + PDF (2026-11-05). 실행: powershell -ExecutionPolicy Bypass -File Docs/GStar/make_doc.ps1
$ErrorActionPreference = 'Stop'
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$docx = Join-Path $dir 'MoaDungeon_지스타_출품서.docx'
$pdf = Join-Path $dir 'MoaDungeon_지스타_출품서.pdf'

$fields = @(
    @('팀명(또는 제작자 이름)', '남귀훈'),
    @('게임 타이틀(국문/영문)', '모아던전/MoaDungeon'),
    @('게임 장르', '턴제 로그라이크 (탑다운 그리드 · 마법 조합 전투)'),
    @('게임 소개', "모아던전은 불 · 얼음 · 번개 · 독 · 대지 · 바람 · 물, 7가지 속성 마법을 엮어 싸우는 턴제 로그라이크입니다.`n물을 뿌린 뒤 번개로 감전시키고, 젖은 적을 얼려 결빙시키는 것처럼 상태와 지형이 서로 반응하는 '상태 × 속성' 규칙이 전투의 핵심입니다.`n숲 속 동굴에서 사막 피라미드, 얼음 동굴, 용암 동굴을 지나 시간 신전 폐허까지, 26층 아래에 잠든 '시간의 모래시계'를 찾아 내려갑니다.`n매번 바뀌는 던전에서 마법서 · 유물 · 마법 융합으로 나만의 빌드를 만들고, 턴제 또는 실시간(1초 = 1턴) 모드로 즐길 수 있습니다."),
    @('플레이 타임(1회 플레이 시 소요 시간)', '1회 클리어(26층) 약 90 ~ 120분, 도중 사망 시 약 20 ~ 40분 (개발 중 예상치)')
)
$shots = @(
    @('screenshot_1_floor1_combo.png', '1층 숲 속 동굴 — 물보라로 적시고 번개 줄기로 감전 반응'),
    @('screenshot_2_floor5_boss.png', '5층 보스 슬라임 소녀 — 붉은 예고 칸을 피하며 얼음 화살로 결빙'),
    @('screenshot_3_floor7_desert.png', '7층 사막 피라미드 — 자칼 무리와의 전투')
)

$word = New-Object -ComObject Word.Application
$word.Visible = $false
try {
    $doc = $word.Documents.Add()
    $doc.PageSetup.TopMargin = 54; $doc.PageSetup.BottomMargin = 54
    $doc.PageSetup.LeftMargin = 60; $doc.PageSetup.RightMargin = 60
    $sel = $word.Selection
    $sel.Font.Name = '맑은 고딕'

    $sel.Font.Size = 20; $sel.Font.Bold = 1; $sel.ParagraphFormat.Alignment = 1
    $sel.TypeText('지스타 출품 신청서 — MoaDungeon'); $sel.TypeParagraph()
    $sel.Font.Size = 10; $sel.Font.Bold = 0
    $sel.TypeText('작성일 2026-11-05'); $sel.TypeParagraph(); $sel.TypeParagraph()
    $sel.ParagraphFormat.Alignment = 0

    foreach ($f in $fields) {
        $sel.Font.Size = 12; $sel.Font.Bold = 1
        $sel.TypeText('• ' + $f[0]); $sel.TypeParagraph()
        $sel.Font.Size = 11; $sel.Font.Bold = 0
        foreach ($line in ($f[1] -split "`n")) { $sel.TypeText($line); $sel.TypeParagraph() }
        $sel.TypeParagraph()
    }

    $sel.Font.Size = 12; $sel.Font.Bold = 1
    $sel.TypeText('• 대표 스크린샷 (1 ~ 10층 실제 플레이 화면)'); $sel.TypeParagraph()
    $sel.Font.Bold = 0
    $width = $doc.PageSetup.PageWidth - $doc.PageSetup.LeftMargin - $doc.PageSetup.RightMargin
    $i = 1
    foreach ($s in $shots) {
        $sel.ParagraphFormat.Alignment = 1
        $pic = $sel.InlineShapes.AddPicture((Join-Path $dir $s[0]))
        $ratio = $pic.Height / $pic.Width
        $pic.Width = $width; $pic.Height = $width * $ratio
        $sel.TypeParagraph()
        $sel.Font.Size = 10
        $sel.TypeText("[$i] " + $s[1]); $sel.TypeParagraph(); $sel.TypeParagraph()
        $i++
    }

    $doc.SaveAs2([string]$docx, 16)
    $doc.SaveAs2([string]$pdf, 17)
    $doc.Close(0)
    "saved: $docx"
    "saved: $pdf"
}
finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}

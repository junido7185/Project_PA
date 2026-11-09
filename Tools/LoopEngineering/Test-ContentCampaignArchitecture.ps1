param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path)

$ErrorActionPreference = 'Stop'
$campaign = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'CONTENT_CAMPAIGN_DAY1_30.md'))
$backlog = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'CONTENT_IMPLEMENTATION_BACKLOG.md'))
$sections = @{}
foreach ($match in [regex]::Matches($campaign, '(?ms)^## ([0-9]+)\. ([^\r\n]+).*?(?=^## |\z)')) {
    $number = [int]$match.Groups[1].Value
    if ($sections.ContainsKey($number)) { throw "Duplicate campaign section: $number" }
    $sections[$number] = $match.Value
}
function Require([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw "[CONTENT-000B] FAIL $Message" }
}
Require ($sections.Count -eq 20) 'Campaign has exactly 20 requested sections.'
$titles = @('Campaign Fantasy', 'Month-One Arc', 'Chapter Structure', 'Day 1–7 Detailed Script',
    'Day 8–14', 'Day 15–21', 'Day 22–30', 'Main Anchor Events', 'Character Appearance Timeline',
    'Relationship Event Timeline', 'Economy Progression', 'Unlock Timeline', 'Village Change Timeline',
    'Quest/Request Matrix', 'Failure & Recovery Rules', 'Save-state Requirements', 'Canon Errata',
    'Content Budget', 'Player Freedom Analysis', 'Day-30 End State')
for ($index = 0; $index -lt $titles.Count; $index++) {
    Require ($sections[$index + 1].StartsWith("## $($index + 1). $($titles[$index])")) "Section title $($index + 1)"
}
$days = @([regex]::Matches($sections[3], '(?m)^\| ([0-9]+) \| (ANCHOR DAY|SUPPORT DAY|FREE DAY|RECOVERY DAY) \|'))
Require ($days.Count -eq 30) 'Exactly 30 classified days.'
for ($day = 1; $day -le 30; $day++) {
    Require (@($days | Where-Object { [int]$_.Groups[1].Value -eq $day }).Count -eq 1) "Day $day occurs once."
}
$dayCounts = @{}
foreach ($day in $days) {
    $kind = $day.Groups[2].Value
    if (-not $dayCounts.ContainsKey($kind)) { $dayCounts[$kind] = 0 }
    $dayCounts[$kind]++
}
Require ($dayCounts['FREE DAY'] -ge 5 -and $dayCounts['RECOVERY DAY'] -ge 5) 'Free and recovery time exists.'
$anchorRows = @([regex]::Matches($sections[8], '(?m)^\| (A[0-9]+) \| ([0-9]+) \|'))
$anchorIds = @($anchorRows | ForEach-Object { $_.Groups[1].Value })
Require ($anchorIds.Count -ge 12 -and $anchorIds.Count -le 16) 'Main anchor budget 12..16.'
Require (@($anchorIds | Sort-Object -Unique).Count -eq $anchorIds.Count) 'No duplicate main anchors.'
$week = @([regex]::Matches($sections[4], '(?ms)^### (A[0-9]+) [^\r\n]+.*?(?=^### |\z)'))
Require ($week.Count -eq 7) 'Seven first-week anchors.'
$fields = @('시작 상태', '시간대', '장소', '등장 캐릭터', 'NPC 감정/목적', '첫 인상', '실제 목표',
    '필요한 시스템', '구체 행동', '대사 beat', '선택', '실패/미완료', '경제 변화', '친밀도 변화',
    '월드 변화', '저장할 상태', '다음 사건 이유')
foreach ($anchor in $week) {
    foreach ($field in $fields) {
        Require ($anchor.Value.Contains("| $field |")) "$($anchor.Groups[1].Value): $field"
    }
}
$features = @('Movement', 'Gathering', 'Inventory/Hotbar', 'Crafting', 'First Shop', 'Pricing',
    'Customer Preference', 'Settlement', 'Farming', 'Mining', 'Fishing', 'Producer Buy-in', 'Phone',
    'Hiring', 'Friendship', 'Resident Request', 'Specialist', 'Processing', 'Feed', 'Village Change',
    'Shop Growth', 'Tier', 'Audit', 'Building/Decoration', 'Terraform')
foreach ($feature in $features) {
    Require ($sections[12] -match ('(?m)^\| ' + [regex]::Escape($feature) + ' \| [0-9]+ \|')) "Feature: $feature"
}
$cast = @('MIRA', 'LOGAN', 'ARLO', 'TARA', 'NOA', 'FELIX', 'JUN', 'LUKA')
$roles = @('Farmer', 'Miner', 'Lumberjack', 'Fisher', 'Chef', 'Blacksmith', 'Tailor', 'Carpenter')
foreach ($role in $roles) {
    Require ($sections[9].Contains("Candidate_$role")) "Introduction identity: $role"
    Require (Test-Path -LiteralPath (Join-Path $ProjectRoot "Assets/Resources/NPCs/Profile_$role.asset")) "Existing profile: $role"
    Require (Test-Path -LiteralPath (Join-Path $ProjectRoot "Assets/Resources/Candidates/Candidate_$role.asset")) "Existing candidate: $role"
}
foreach ($resident in $cast) {
    Require ([regex]::Matches($sections[10], "(?m)^\| REL-$resident-[123] \|").Count -eq 3) "$resident three relationship events."
    Require ([regex]::Matches($sections[14], "(?m)^\| RQ-$resident-[12] \|").Count -eq 2) "$resident two personal requests."
}
for ($ticketNumber = 1; $ticketNumber -le 10; $ticketNumber++) {
    $ticket = 'CONTENT-{0:D3}' -f $ticketNumber
    Require ($backlog -match ('(?m)^## ' + $ticket + ' — ')) "$ticket implementation scope exists."
}
foreach ($target in [regex]::Matches($campaign + $backlog, '\[[^\]]+\]\(([^)]+)\)')) {
    Require (Test-Path -LiteralPath (Join-Path $ProjectRoot $target.Groups[1].Value)) "Document link: $($target.Groups[1].Value)"
}
Require (-not [regex]::IsMatch($campaign + $backlog, '(?m)[ \t]+$')) 'No trailing whitespace.'
Require (-not ($campaign + $backlog).Contains([string][char]0xFFFD)) 'UTF-8 replacement characters absent.'
$state = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Automation/LoopEngineering/State/loop-state.json') -Raw -Encoding UTF8 | ConvertFrom-Json
Require ($state.contentContinuationApproval.humanPreapproved -eq $true) 'Human continuation approval recorded.'
Require ($state.contentContinuationApproval.preapprovedThrough -eq 'CONTENT-010') 'Approval ends at CONTENT-010.'
Require ($state.contentContinuationApproval.localTicketCommitsAllowed -eq $true) 'Local commit authorization recorded.'

[PSCustomObject]@{
    ticket = 'CONTENT-000B'
    result = 'PASS'
    scope = 'Document coverage and reference validation; semantic source audit is recorded in the campaign. Not a Unity runtime test.'
    sections = $sections.Count
    classifiedDays = $days.Count
    dayTypes = $dayCounts
    mainAnchors = $anchorIds.Count
    firstWeekAnchors = $week.Count
    fieldsPerFirstWeekAnchor = $fields.Count
    exposedFeatures = $features.Count
    coreIntroductions = $roles.Count
    coreRelationshipEvents = 24
    personalRequests = 16
    implementationTickets = 10
    unityRuntime = 'NOT_RUN_DESIGN_ONLY'
} | ConvertTo-Json -Depth 4

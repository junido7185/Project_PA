# 연구 항목별 완료 대조 (2026-11-09)

아이템 생태 연구(ITEMS_AND_COLLECTION_DESIGN_RESEARCH_2026-11-03)의 항목 ID 하나하나를 실제 게임 데이터(`Assets/Resources` 아이템 id·레시피 자산), 도감(`DiscoveryLog.Sections`), 회귀 플랜 결과(reg239, 펜던트는 r241)에 스크립트로 대조했다(scratchpad `coverage.py`). 빠진 항목 0 — 이 대조에서 R05 해변 유리 펜던트가 미구현으로 처음 드러나 구현했다.

| 연구 항목 | 게임 데이터 | 도감 | 회귀 플랜 |
| --- | --- | --- | --- |
| F01 참치 | Item_Fish.asset (id 8) | 있음 | feedback: ok |
| F02 도미 | Item_Fish_RedSnapper.asset (id 3001) | 있음 | feedback: ok |
| F03 옐로탱 | Item_Fish_YellowTang.asset (id 3002) | 있음 | feedback: ok |
| F04 정어리 | Item_Fish_Sardine.asset (id 3003) | 있음 | sardine: ok |
| F05 고등어 | Item_Fish_Mackerel.asset (id 3004) | 있음 | mackerel: ok |
| F06 넙치 | Item_Fish_Flounder.asset (id 3005) | 있음 | flounder: ok |
| F07 숭어 | Item_Fish_Mullet.asset (id 3006) | 있음 | mullet: ok |
| F08 복어 | Item_Fish_Puffer.asset (id 3007) | 있음 | puffer: ok |
| F09 장어 | Item_Fish_Eel.asset (id 3009) | 있음 | eel: ok |
| F10 붕어 | Item_Fish_Crucian.asset (id 3010) | 있음 | crucian: ok |
| F11 잉어 | 넣지 않음(연구 조건) | — | — |
| F12 송어 | Item_Fish_Trout.asset (id 3008) | 있음 | trout: ok |
| B01 나비 | Item_Butterfly.asset (id 2004) | 있음 | feedback: ok |
| B02 숲 딱정벌레 | Item_Bug_Beetle.asset (id 3101) | 있음 | beetle: ok |
| B03 무당벌레 | Item_Bug_Ladybug.asset (id 3103) | 있음 | ladybug: ok |
| B04 메뚜기 | Item_Bug_Grasshopper.asset (id 3106) | 있음 | grasshopper: ok |
| B05 벌 | Item_Bug_Bumblebee.asset (id 3105) | 있음 | bumblebee: ok |
| B06 잠자리 | Item_Bug_Dragonfly.asset (id 3104) | 있음 | dragonfly: ok |
| B07 귀뚜라미 | Item_Bug_Cricket.asset (id 3109) | 있음 | cricket: ok |
| B08 사슴벌레 | Item_Bug_StagBeetle.asset (id 3107) | 있음 | stagbeetle: ok |
| B09 하늘소 | Item_Bug_Longhorn.asset (id 3108) | 있음 | longhorn: ok |
| B10 사마귀 | Item_Bug_Mantis.asset (id 3110) | 있음 | mantis: ok |
| B11 등불 나방 | Item_Bug_Moth.asset (id 3102) | 있음 | moth: ok |
| B12 반딧불이 | Item_Bug_Firefly.asset (id 3111) | 있음 | firefly: ok |
| 해변 가리비 | Item_ScallopShell.asset (id 2034) | 있음 | beach: ok |
| 해변 소라 | Item_ConchShell.asset (id 2035) | 있음 | beach: ok |
| 해변 불가사리 | Item_Starfish.asset (id 2036) | 있음 | beach: ok |
| 해변 해초 | Item_Seaweed.asset (id 2045) | 있음 | beach: ok |
| 해변 유리 | Item_BeachGlass.asset (id 2047) | 있음 | beach: ok |
| 해변 산호 | Item_CoralPiece.asset (id 2054) | 있음 | beach: ok |
| D01 점토 | Item_Clay.asset (id 2039) | 있음 | dig: ok |
| D02 옛 동전 | Item_OldCoin.asset (id 2040) | 있음 | dig: ok |
| D03 화석 | Item_FossilShard.asset (id 2041) | 있음 | dig: ok |
| D04 토기 조각 | Item_PotteryShard.asset (id 2049) | 있음 | dig: ok |
| D05 수정 원석 | Item_Crystal.asset (id 2052) | 있음 | dig: ok |
| D06 호박 | Item_AmberPiece.asset (id 2055) | 있음 | dig: ok |
| R01 정어리 한 접시 | Recipe_SardinePlate | — | sardine: ok |
| R02 해초 생선말이 | Recipe_SeaweedRoll | — | sardine: ok |
| R03 훈제 고등어 | Recipe_SmokedMackerel | — | mackerel: ok |
| R04 구이 넙치 | Recipe_GrilledFlounder | — | flounder: ok |
| R05 해변 유리 펜던트 | Recipe_GlassPendant | — | pendant: ok(r241) |
| R06 유리 모빌 | Recipe_GlassMobile | — | beach: ok |
| R07 토기 모자이크 | Recipe_PotteryMosaic | — | dig: ok |
| R08 광물 받침 | Recipe_CrystalStand | — | dig: ok |
| R09 곤충 안내판 | Recipe_InsectBoard | — | insectboard: ok |

## 너굴 리스트 연구 H1–H5

| 가설 | 상태 | 회귀 플랜 |
| --- | --- | --- |
| H1 소품 모델 품질 | 가게 방·소유 의자 | beach·decor·settlement ok |
| H2 테마 세트 | 2세트 + 단독 꾸밈 반응 | decorset·insectboard ok |
| H3 작은 움직임 | 유리 모빌 흔들림·광물 받침 반짝임 | decorset·dig ok |
| H4 색 변형 | 의자 2색 | chairpaint ok |
| H5 놓는 면 | 러그·뒷벽·책상 | rug·wallhang·desk ok |

## 그 밖의 승인 큐(연구 표 밖)

| 출처 | 항목 | 상태 | 근거 |
| --- | --- | --- | --- |
| 곤충 계획(Content-insects-plan-20261026) C.1 | 하루 안 반복 잡기 | 완료 | FOREST_BEETLE.md §하루 안 반복, beetle 플랜 |
| 〃 C.2 | 곤충 표본(공예) | 완료 | 장수풍뎅이 표본 상자 2056·나비 표본 액자 2025 |
| 〃 C.3 | 시간대 곤충(나방·반딧불이) | 완료 | 흰불나방 3102(moth)·애반딧불이 3111(firefly) |
| 〃 D | 검사기 순서·이름 의존, 곤충 이름 고정 "나비" | 완료 | BugCritter.Noun(종별), 회귀 플랜 이름 정렬 |
| 제작 계획(Content-crafting-plan-20261026) C.1 | 나비 표본 액자 | 완료 | 2025, crafting 플랜 |
| 〃 C.2 | 곤충 다양화 | 완료 | 곤충 12종 |
| 〃 C.3 | 벌목 업그레이드(좋은 도끼 → 벌목꾼 생산) | **사용자 결정: 전시 뒤(2026-11-09)** | Canon v2 §13.3 "선택 Root의 다음 1단계 Preview만, 실제 미래 기능은 구현하지 않는다" — 광부 한 갈래 시연만 승인. 플레이어 도구 단계(구리·철 도끼)는 TOOL_PROGRESSION.md로 완료 |
| 〃 D | 같은 이름 카드·데모 밖 레시피 숨김 | 완료 | 대체 레시피 카드 "생선구이 · 숭어" 식 이름, CraftingUI 데모 밖 레시피 숨김(365행) |
| NPC 생활 연구 작은 구현 순서 1–4 | 현재 행동 완성·개입/복귀·공동 공간·밤 고객 역할 | 완료(표현·애니메이션 다듬기는 보류된 모션 작업) | npc·feedback·market 플랜(걷기·쳐다봄·벌목꾼 살핌·저녁 모임·아침/밤 대화·손님 동선) |
| 지식 탐색 시범(사용자 지시) | 수집·분석·검증·저장·검색 | 완료(음성 분석만 설치 권한 거절) | Docs/DevKnowledge, devk.py |

## 재대조 — 연못 턱·정어리 정리 뒤 최신 결과(reg244 + r245)

같은 스크립트를 reg244 전체 회귀와 r245 정어리 재확인 결과로 다시 돌렸다. 빠진 항목 0(45행 중 44 ok, F11 잉어만 연구 조건으로 제외).

| 연구 항목 | 게임 데이터 | 도감 | 회귀 플랜 |
| --- | --- | --- | --- |
| F01 참치 | Item_Fish.asset (id 8) | 있음 | feedback: ok |
| F02 도미 | Item_Fish_RedSnapper.asset (id 3001) | 있음 | feedback: ok |
| F03 옐로탱 | Item_Fish_YellowTang.asset (id 3002) | 있음 | feedback: ok |
| F04 정어리 | Item_Fish_Sardine.asset (id 3003) | 있음 | sardine: ok(r245) |
| F05 고등어 | Item_Fish_Mackerel.asset (id 3004) | 있음 | mackerel: ok |
| F06 넙치 | Item_Fish_Flounder.asset (id 3005) | 있음 | flounder: ok |
| F07 숭어 | Item_Fish_Mullet.asset (id 3006) | 있음 | mullet: ok |
| F08 복어 | Item_Fish_Puffer.asset (id 3007) | 있음 | puffer: ok |
| F09 장어 | Item_Fish_Eel.asset (id 3009) | 있음 | eel: ok |
| F10 붕어 | Item_Fish_Crucian.asset (id 3010) | 있음 | crucian: ok |
| F11 잉어 | 넣지 않음(연구 조건) | — | — |
| F12 송어 | Item_Fish_Trout.asset (id 3008) | 있음 | trout: ok |
| B01 나비 | Item_Butterfly.asset (id 2004) | 있음 | feedback: ok |
| B02 숲 딱정벌레 | Item_Bug_Beetle.asset (id 3101) | 있음 | beetle: ok |
| B03 무당벌레 | Item_Bug_Ladybug.asset (id 3103) | 있음 | ladybug: ok |
| B04 메뚜기 | Item_Bug_Grasshopper.asset (id 3106) | 있음 | grasshopper: ok |
| B05 벌 | Item_Bug_Bumblebee.asset (id 3105) | 있음 | bumblebee: ok |
| B06 잠자리 | Item_Bug_Dragonfly.asset (id 3104) | 있음 | dragonfly: ok |
| B07 귀뚜라미 | Item_Bug_Cricket.asset (id 3109) | 있음 | cricket: ok |
| B08 사슴벌레 | Item_Bug_StagBeetle.asset (id 3107) | 있음 | stagbeetle: ok |
| B09 하늘소 | Item_Bug_Longhorn.asset (id 3108) | 있음 | longhorn: ok |
| B10 사마귀 | Item_Bug_Mantis.asset (id 3110) | 있음 | mantis: ok |
| B11 등불 나방 | Item_Bug_Moth.asset (id 3102) | 있음 | moth: ok |
| B12 반딧불이 | Item_Bug_Firefly.asset (id 3111) | 있음 | firefly: ok |
| 해변 가리비 | Item_ScallopShell.asset (id 2034) | 있음 | beach: ok |
| 해변 소라 | Item_ConchShell.asset (id 2035) | 있음 | beach: ok |
| 해변 불가사리 | Item_Starfish.asset (id 2036) | 있음 | beach: ok |
| 해변 해초 | Item_Seaweed.asset (id 2045) | 있음 | beach: ok |
| 해변 유리 | Item_BeachGlass.asset (id 2047) | 있음 | beach: ok |
| 해변 산호 | Item_CoralPiece.asset (id 2054) | 있음 | beach: ok |
| D01 점토 | Item_Clay.asset (id 2039) | 있음 | dig: ok |
| D02 옛 동전 | Item_OldCoin.asset (id 2040) | 있음 | dig: ok |
| D03 화석 | Item_FossilShard.asset (id 2041) | 있음 | dig: ok |
| D04 토기 조각 | Item_PotteryShard.asset (id 2049) | 있음 | dig: ok |
| D05 수정 원석 | Item_Crystal.asset (id 2052) | 있음 | dig: ok |
| D06 호박 | Item_AmberPiece.asset (id 2055) | 있음 | dig: ok |
| R01 정어리 한 접시 | Recipe_SardinePlate | — | sardine: ok(r245) |
| R02 해초 생선말이 | Recipe_SeaweedRoll | — | sardine: ok(r245) |
| R03 훈제 고등어 | Recipe_SmokedMackerel | — | mackerel: ok |
| R04 구이 넙치 | Recipe_GrilledFlounder | — | flounder: ok |
| R05 해변 유리 펜던트 | Recipe_GlassPendant | — | pendant: ok |
| R06 유리 모빌 | Recipe_GlassMobile | — | beach: ok |
| R07 토기 모자이크 | Recipe_PotteryMosaic | — | dig: ok |
| R08 광물 받침 | Recipe_CrystalStand | — | dig: ok |
| R09 곤충 안내판 | Recipe_InsectBoard | — | insectboard: ok |

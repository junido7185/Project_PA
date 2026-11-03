# 출처 원장

조회일: 2026-10-11. 아래 ID는 전 문서에서 공유한다. 영상의 빌드 버전이 표시되지 않았으면 ‘불명’으로 유지한다. 판매 페이지의 출시일은 관찰 영상의 빌드 버전이 아니다.

## P.A. 기준과 구현

고정 commit: `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`. [조회 당시 저장소](https://github.com/junido7185/Project_PA/tree/b121140ae6f5ca0f92078db5c2f75512dc8fc95a).

| ID | 출처 | 사용 범위 |
|---|---|---|
| PA-C | [Opening Demo Canon v2](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md) | 확정 데모. blob `b4d5260cd96940a4f4b90a3e5665970564c58c8e` |
| PA-I | [IDENTITY](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/01_GAME_DESIGN/Canon/PROJECT_PA_IDENTITY.md) | 정체성. blob `203f6b2bb8721836a4c4b5c19f2bbdc96a5b2627` |
| PA-R1 | [CURRENT_STATE](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/00_CURRENT/CURRENT_STATE.md) | 최신 상단 기록, 과거 기록과 날짜 분리 |
| PA-R2 | [CAPABILITY_REGISTRY](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/00_CURRENT/CAPABILITY_REGISTRY.md) | 권위·검증 범위. 최신 보정 우선 |
| PA-R3 | [GAME_LOOP_MAP](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/00_CURRENT/GAME_LOOP_MAP.md) | 실제 연결·fixture·자연 입력 경계 |
| PA-R4 | [CODEX_HANDOFF](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/00_CURRENT/CODEX_HANDOFF.md), [INTEGRATION_QUEUE](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/00_CURRENT/INTEGRATION_QUEUE.md) | 남은 사람 검수·완주 작업 |
| PA-R5 | [CODE_REUSE_MAP v1](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/02_IMPLEMENTATION/PROJECT_PA_CODE_REUSE_MAP_v1.md) | 기존 권위 찾기만 사용. Phone Item·Tent3·별도 Hub 등 낡은 설계는 Canon 우선 |
| PA-CODE | 같은 commit의 아래 10개 파일 | 정적 직접 읽기. 실행·전체 호출 그래프 검증 아님 |

읽은 코드: `PlayerInteraction.cs`, `EquipmentSystem.cs`, `CameraController.cs`, `ShopSlot.cs`, `PurchaseEvaluator.cs`, `UI/ShopPriceUI.cs`, `FishingSpot.cs`, `World/WorldBuildingPlacementService.cs`, `NpcController.cs`, `InteriorCustomerController.cs` (모두 `Assets/Scripts/` 아래). 마지막 파일은 legacy 경로를 구분하는 참고이며 Demo256 현행 경로로 취급하지 않았다. 파일별 대응은 [통합 자료](../02_design_knowledge/06_INTEGRATION_AND_WORK_PACKETS.md)에 있다.

## 원작 문헌

| ID | 출처·작성 주체 | 버전/시기 | 직접 지지하는 사실과 한계 |
|---|---|---|---|
| DK-D1 | [KRAFTON 낚시 도움말](https://support.krafton.com/hc/en-us/articles/53061744418969-How-do-I-fish) | 2025-12-05 업데이트 | 캐스팅, 입질 구별, 저항 중 릴 중지, breakout meter. 영상 타이밍은 없음 |
| DK-D2 | [James Bendon 인터뷰, Vooks](https://www.vooks.net/interview-chatting-with-dinkum-developer-james-bendon/) | 2026-01-28 | 화면의 물고기와 잡는 물고기의 연결, 낚시 난도/좌절에 대한 제작자 발언. 특정 최신 수치 아님 |
| DK-D3 | [공식 Dinkum 사이트](https://dinkum.krafton.com/en), [Steam](https://store.steampowered.com/app/1062520/Dinkum/) | 현재 소개, 관찰 빌드 불명 | 탐험·채집·마을 성장의 활동 구성. 농업·전투는 P.A. 데모 범위 밖 |
| AC-D1 | [Nintendo 시작 팁](https://www.nintendo.com/en-gb/News/2020/March/Start-deserted-island-life-right-with-these-Animal-Crossing-New-Horizons-tips--1751422.html) | 2020-03-30 출시기 규칙 | 손 도구, 나무/바위 자원, 조리법·가방·Phone, 낚시·벌레 조작. 현재 모든 업데이트의 규칙으로 일반화 금지 |
| AC-D2 | [Nintendo ‘Ichikara’ GameWith 가이드](https://www.nintendo.com/jp/ichikara/acbaa/index_en.html) | 출시기 입문 가이드 | 텐트 입지와 낚시의 보이는/들리는 입질 설명. Nintendo 호스팅, 필자는 GameWith이며 개발자 인터뷰 아님 |
| AC-D3 | [Nintendo 게임 소개](https://www.nintendo.com/en-gb/Games/Nintendo-Switch-games/Animal-Crossing-New-Horizons-1438623.html) | 기본 경험 설명 | 재료 → DIY → 안팎 꾸미기. 배치 충돌 알고리즘 근거는 없음 |
| ML-D1 | [Moonlighter 공식 Steam](https://store.steampowered.com/app/606150/Moonlighter/) | 1편 기본 소개, 영상 빌드 불명 | 직접 상점·가격 책정. 정확한 반응 구간/가격 공식은 확보하지 않음 |
| ML2-D1 | [DigitalMallen 개발자 ‘Let’s talk about the shop’](https://steamcommunity.com/app/2350790/discussions/0/596277387327440160/) | 2025-06 Next Fest 논의 | 가격 추측의 장기 반복 문제, 당시 2편의 상점 의도·피드백·검토 방향. 출시판 규칙 아님 |
| ML2-D2 | [Digital Sun/11 bit 공식 뉴스 목록](https://steamcommunity.com/app/2350790/allnews/) | 2026-05 업데이트, 2026-09 1.0 공지 | 2026-05-20 이후 shop behavior 변경, 이후 1.0 출시 공지 확인. 개별 현재 가격 지표 구현은 미검증 |
| DV-D1 | [MINTROCKET 공식 press](https://pressmintrocket.oopy.io/dave_the_diver) | 기본 게임 소개 | 낮 확보·밤 운영·업그레이드 순환. 순수 소개가 내부 구현을 설명하지 않음 |
| DV-D2 | [Hwang/Cha 개발자 인터뷰, Software Engineering Daily](https://softwareengineeringdaily.com/articles/developing-dave-the-diver-with-jaeho-hwang-and-minsuk-cha/) | 제작 회고 | 작살 촉감 우선, 자동 식당을 직접 조작으로 수정, 캐릭터로 시스템 연결. Cinemachine/Timeline 사용 발언은 있으나 P.A. 패키지 도입 근거 아님 |
| DV-D3 | [공식 Steam](https://store.steampowered.com/app/1868140/DAVE_THE_DIVER/) | 기본 게임, 클립 빌드 불명 | 아래 공식 미디어의 제공 페이지. DLC 경험과 섞지 않음 |

## 직접 관찰한 공식 미디어

파일 시간은 아래 URL의 해당 클립 기준이다. Steam 페이지의 상단 예고편 시간과 다르다. `?t=`는 CDN 갱신 파라미터이며 버전 번호가 아니다. 링크가 갱신되면 같은 페이지의 About This Game 미디어와 대조한다.

| ID | 내용 | 길이·원본 크기·파일 프레임률 | 원본 링크 |
|---|---|---|---|
| DK-V1 | Life 몽타주 | 6.00초, 1000×290, 10 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1062520/extras/c653b3d8fefa8c0b06e35bcc00395df0.webm?t=1767607973) |
| DK-V2 | Explore 몽타주 | 6.20초, 1000×290, 10 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1062520/extras/b567b5c6078a6217dc42d4646f7ad8ce.webm?t=1767607973) |
| DK-V3 | Build 몽타주 | 6.40초, 1000×290, 10 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1062520/extras/803b99cee6df8ff0ab91369adaebef25.webm?t=1767607973) |
| DK-V4 | Town 성장 몽타주 | 6.20초, 1000×290, 10 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1062520/extras/2b201f348e7877314beea4704a3022a7.webm?t=1767607973) |
| ML-V1 | 가판대·고객·계산대 | 11.94초, 780×214, 약 33.33 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/606150/extras/041281f4bfe82aba9d00fa1314716dd3.webm?t=1790860598) |
| DV-V1 | 낮 바다 → 밤 식당 | 6.75초, 640×360, 약 33.33 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1868140/extras/d5f3fed41b468e192d871dd1a8121cad.webm?t=1791478061) |
| DV-V2 | 큰 해양생물 장면 | 3.57초, 640×360, 약 33.33 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1868140/extras/fef7f756ab5f29045e929e66c742219b.webm?t=1791478061) |
| DV-V3 | 인물 반응 픽셀 연출 | 4.50초, 640×360, 약 33.33 fps | [클립](https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/1868140/extras/427b0d74b89f0e15fa92b38d61af46bc.webm?t=1791478061) |

전부 무음 파일. 이 FPS는 게임 성능이나 행동 프레임 수를 뜻하지 않는다. UI가 잘린 클립에서는 돈·가방·입력 수락을 확인하지 못한다. [장면 원장](SCENE_LEDGER.md)이 사실의 세부 경계다.

## 확보했지만 사실 판정에 쓰지 않은 자료

[Nintendo Treehouse E3 2019](https://www.youtube.com/watch?v=dEh3MPy4GAU): 공식 영상 페이지만 확인, 화면 재생 실패. ACNH 장면 시각 근거 아님. Moonlighter 2의 개별 Blob-Life 공지는 본문 추출 실패; 검색된 SteamDB 패치 미러의 구체적 가격 표시 설명은 현행 규칙으로 채택하지 않았다. 커뮤니티 불만/SEO 가격표는 가격 알고리즘의 근거로 사용하지 않았다.

추가 작품을 넓게 넣지 않았다. 기존 네 작품이 직접 행동, 공간 소유, 상점 학습, 낮밤·짧은 연출이라는 다른 질문을 이미 담당하며, 이번 연구에서 추가 작품보다 핵심 계약·실패 분석의 가치가 크다고 판단했다(I).

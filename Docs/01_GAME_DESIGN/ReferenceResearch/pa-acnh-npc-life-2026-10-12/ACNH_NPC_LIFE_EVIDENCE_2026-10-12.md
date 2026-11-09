# ACNH NPC 생활감 — 독립 증거 원장

검증일 2026-10-12. [관찰/추론 분류](ACNH_NPC_LIFE_README_2026-10-12.md). 제목·설명·자막·카테고리는 후보 탐색용이며 화면 관찰의 대체 근거가 아니다.

## 사전 검증 판정

| 자료군 | 실제 프레임 | HUD/카메라 | 연속성 | 음향 | 채택 범위 |
|---|---|---|---|---|---|
| AC-F 실제 실황 | 1920×1080 파일 디코딩 성공 | 시계/미니맵, 일반 필드와 대화 카메라 | 접근·대화·복귀 표본 사이 같은 공간/인물이 이어짐. 전체 무편집 인증은 아님 | AAC 신호 존재. 원작 소리 종류/방송 믹스 청취 U | S-01의 시각 순서 |
| GI-1–12 일상 캡처 GIF | 모두 640×360, GIF89a로 디코딩 성공 | 일반 게임 구도, 여러 파일에 시계/ZL 등. UI 일부가 프레임/구도에 따라 없음 | 짧게 잘린 파일. 행동 시작/끝 누락, GIF 루프/속도 변환 이력 U | 형식상 원음 없음 | 큰 자세·행동 유지·반응·공유 공간. 정확한 접지/속도/행동 시간 제외 |
| 기타 Twitch 후보 | 일부 화면/파일 확인 | 자료별 상이 | 목표 단위 부족 | 원음 청취 U | 아래 미채택 기록 |
| Reddit 후보 | 보안 차단 화면 | 게임 화면 없음 | 검증 불가 | 검증 불가 | 사용하지 않음 |

**연구 gate: 부분 수용.** 시각 행동의 제한범위 역분석을 진행한다. 하루 생활 AI·원음·장소 선택·전체 체류 시간 연구 완료는 주장하지 않는다. 세부 공백은 [검증·공백](ACNH_NPC_LIFE_VALIDATION_AND_GAPS_2026-10-12.md).

## 공개 실황

[AC-F / hollow_tones — i love animal](https://www.twitch.tv/hollow_tones/clip/GrossBlightedBatRuleFive?range=7d). 실제 플레이어 접근 후 확인한 최종 공개 URL이다. 원래 발견 링크는 `https://clips.twitch.tv/GrossBlightedBatRuleFive`. 게임 빌드·모드·녹화 원속도는 불명. 시계는 March 20, 1:25 PM 표본을 보여주지만 실제 촬영 연도/현실 시간을 증명하지 않는다. Tommy의 특수 역할을 일반 주민 스케줄로 일반화하지 않는다.

| 파일 실측 M | 값 |
|---|---|
| 파일 크기 | 21,632,861 bytes |
| 컨테이너 / 영상 스트림 길이 | 28.002063s / 27.999667s |
| 크기 / 디코딩 프레임 수 | 1920×1080 / 1628 |
| r_frame_rate / avg_frame_rate | 60/1 / 4884000/83999 |
| 오디오 | AAC, 44,100 Hz, 2ch. 디코딩한 0이 아닌 float 샘플 2,466,944개 |
| SHA-256 | `8303ec667928c5bbf6e97af20513cc3eff3fff8d4884fef95d337896e231eb01` |

프레임률은 파일 값이다. VFR/타임스탬프 차이 때문에 n/60을 실제 위치로 쓰지 않고 ffprobe의 각 프레임 PTS를 사용했다. 게임 렌더 FPS/Animator 속도는 U다. 오디오 샘플 수는 채널·디코더 패딩을 포함할 수 있고 발소리·대화음의 의미/시점을 증명하지 않는다.

## 일상 캡처: 실제 GIF 자산

원문은 Inside, 竜神橋わたる, 2020-05-13 게시. [1쪽](https://www.inside-games.jp/article/2020/05/13/128513.html), [2쪽](https://www.inside-games.jp/article/2020/05/13/128513_2.html), [3쪽](https://www.inside-games.jp/article/2020/05/13/128513_3.html). Nintendo 공식 영상이 아니다. 기사 DOM에 삽입된 아래 실제 자산을 확보해 관찰했다. 기사 설명은 R, 디코딩 화면은 O로 구분한다.

모두 640×360이며 오디오가 없다. 표시 길이는 모든 GIF 프레임 지연값을 더한 M다. 원본 녹화 FPS·배속·누락·루프 제작 방식은 U다. **2020 게시의 일상 운동을 2021-11-05의 2.0.0에 추가된 시작형 group stretching으로 취급하지 않는다.**

| ID·실제 자산 | 원문 쪽 | 프레임 수 M | GIF 표시 길이 M | 관찰/사용 |
|---|---:|---:|---:|---|
| [GI-1 / 체조](https://dl.dropboxusercontent.com/s/hlftbqm57vcck2n/%E4%BD%93%E6%93%8D.gif?) | 1 | 137 | 5.71s | S-02 보조 |
| [GI-2 / 운동 소품](https://dl.dropboxusercontent.com/s/vfl3w9fyyuz494g/%E7%AD%8B%E3%83%88%E3%83%AC.gif?) | 1 | 167 | 6.96s | S-02 세부 |
| [GI-3 / 단독 달리기](https://dl.dropboxusercontent.com/s/w1ytpxlhcs5cosd/%E3%83%80%E3%83%83%E3%82%B7%E3%83%A5%EF%BC%91.gif?) | 1 | 125 | 5.21s | 선별: 이동만; 세부 대상 선택 근거 없음 |
| [GI-4 / 공동 달리기](https://dl.dropboxusercontent.com/s/moj0fz6yqqhwb92/%EF%BC%93%E4%BA%BA%E8%B5%B0%E3%82%8A%E5%9B%9E%E3%82%8B.gif?) | 1 | 97 | 4.04s | S-06 보조 |
| [GI-5 / 청소로 소개한 장면](https://dl.dropboxusercontent.com/s/1hj8cmzkft025p4/%E6%8E%83%E9%99%A4.gif?) | 2 | 76 | 3.17s | 선별: 짧고 소품/상대 가림; 청소의 결과 미검증 |
| [GI-6 / 주민 간 만남](https://dl.dropboxusercontent.com/s/42wl3uh41y79qsm/%E6%8C%A8%E6%8B%B6.gif?) | 2 | 131 | 5.46s | S-04 세부 |
| [GI-7 / 컵을 든 주민들](https://dl.dropboxusercontent.com/s/ispq2gottyk4bup/%E3%82%B3%E3%83%BC%E3%83%92%E3%83%BC.gif?) | 2 | 99 | 4.13s | S-06; 의자 사용 미채택 |
| [GI-8 / 함께 노래로 소개한 장면](https://dl.dropboxusercontent.com/s/2ir9okdgf1we6sw/%E5%94%84%E3%81%86.gif?) | 2 | 194 | 8.09s | S-06; 노래 원음 U |
| [GI-9 / 실내 단독 활동](https://dl.dropboxusercontent.com/s/4eg214js4q86zxt/%EF%BC%91%E4%BA%BA%E3%81%A7%E5%94%84%E3%81%86.gif?) | 2 | 302 | 12.59s | S-05 세부; 노래 원음 U |
| [GI-10 / 상점 구매 반응](https://dl.dropboxusercontent.com/s/e3b8nl6pz1sar8u/%E8%B2%B7%E3%81%84%E7%89%A9%E3%82%B7%E3%83%A7%E3%83%83%E3%82%AF.gif?) | 3 | 73 | 3.04s | 선별: 구매 도중 시작, 주민 자율 탐색 전후 부족 |
| [GI-11 / 책·벌집·반응](https://dl.dropboxusercontent.com/s/czutxjoskpuc32k/%E8%9C%82%E3%81%AE%E5%B7%A3.gif?) | 3 | 121 | 5.06s | S-03 세부 |
| [GI-12 / 대화 종료·이동](https://dl.dropboxusercontent.com/s/seolk6aq9o45a9g/%E3%83%88%E3%83%A0%E3%82%BD%E3%83%B3%E3%83%80%E3%83%83%E3%82%B7%E3%83%A5.gif?) | 3 | 99 | 4.13s | S-03 세부; GI-11과 시간축 합산 금지 |

### 동일 파일 재확인용 SHA-256 M

```text
GI-1 64d1f2cb2834e3cb708e00378fde3452eb5a78238663d2f0a508aa00fdfef66a
GI-2 49b238897ad897dd29defc88db0e966b6d85a522ce563938360b5f09a103eb66
GI-3 b07d64475d0bf9795954ec0fbe8e4d964e57a51abb7bab51ff06306a27e4ed47
GI-4 d0137981f81c6fd285129d02a4d0151f54a1802be98a44ca7eb4213f9e85cf69
GI-5 98d2a40cfce43f5e476360af2e9de17408404ca6a0e459783194a07bc111e011
GI-6 1c0079ad4229b0bf166f37e43054cc7f879cdda85d731e111e43ffb17b084d7d
GI-7 ee62cbc085f2de1cc4a20b333e8cf67e8ca84cc58c2030afaebd2837864d2ec3
GI-8 393ef90bbce22144a01c243976e9787ba2521c63d63703ea1a30fe8689123ac9
GI-9 e9a208b71250e759b7edc2741e93d787cfbdb96f99a77c056a730cb6a9d1ca75
GI-10 e4cf320f175cde4ec3571373751a058f3adf04f73fa50f6ad21f799a915538a5
GI-11 e8ef21e8399a6092799701a8994d4eb8cdd764138f639af615f739dfcab072b4
GI-12 616dd67846b13f4e51a45c049d170b6e870587323a3dfde0950cdfc0da4a4c55
```

### 어떻게 장면을 확인했는가

모든 GIF는 12개 분산 표본을 선별했다. GI-2는 n0–160의 8프레임 간격, GI-6는 n20–80의 3프레임 간격, GI-7는 n0–96의 8프레임 간격, GI-9는 n0–288의 16프레임 간격과 추가 n75/100/200/301, GI-11는 n0–69의 3프레임 간격과 n120, GI-12는 n24–96의 3프레임 간격 등을 추가 확인했다. GI-5의 4프레임 간격도 선별했으나 세부 본문 근거로 확대하지 않았다. 주요 장면의 원해상도 단독 프레임도 확인했다. 표본 사이 최초 변화 프레임이나 누락 없는 전체 연속성을 인증하지 않는다.

AC-F는 2초 간격 선별 후 6–8초와 24–28초 부근을 약 0.25초 표본으로 더 확인했다. 본문 n/PTS는 원본 파일 인덱스다. 정확한 버튼 누름/놓기와 처음 이동 가능해진 순간은 미측정이다.

## 공식 문헌 D: 직접 지지하는 범위

| ID·출처 | 확인한 설명 | 사용 한계 |
|---|---|---|
| D-01 [Nintendo Explore](https://animalcrossing.nintendo.com/new-horizons/explore/) | 현실 시각/계절과 연결되는 섬 생활이라는 공식 소개 | 화면 밖 NPC simulation·일과 알고리즘 근거 아님. P.A. Soft Clock 복사 금지 |
| D-02 [Nintendo Share](https://animalcrossing.nintendo.com/new-horizons/share/) | 주민 텐트 위치 선택과 대화, 개성이 있는 방문 인물/역할 소개 | 모든 주민의 직업/행동 확률로 일반화 금지 |
| D-03 [Nintendo 공식 업데이트 기록](https://www.nintendo.com/en-gb/Support/Nintendo-Switch/Game-Updates/How-to-Update-Animal-Crossing-New-Horizons-1745568.html) | 2.0.1: 일부 주민이 집의 kitchen 외 물건으로 조리 시도, 방문 때 café 문맥으로 말하던 문제 수정. 2.0.2: 주변 오디오 장치와 group stretching 음악 문제 수정. 2.0.5: 방문 중 특정 앱 사용 뒤 주민이 남던 문제 수정 | 공식으로 확인된 과거 실패 사례. 원인 코드·AI 모델·현행 재현 여부는 U |
| D-04 [Nintendo 지원: 첫 오리엔테이션](https://support-jp.nintendo.com/app/answers/detail/a_id/36909) | 인물 옆으로 접근해 A로 말을 거는 일반 조작 설명 | AC-F의 실제 A 입력 프레임이나 자동 대화 여부를 증명하지 않음 |

D-03은 환경 대상·대화 장소·중단/복귀 문맥의 불일치가 수정 대상이었다는 근거다. P.A.에서 유사 경계를 검수할 설계 가설의 출발점이며 원작 내부 구조의 근거가 아니다.

## 미채택/제한 후보: 접근 성공과 장면 적합성의 분리

| 후보·검증한 공개 페이지 | 실제 확인 | 채택하지 않은 이유 |
|---|---|---|
| [AC-A / Raymond is on brand!](https://www.twitch.tv/simonhoneydew/clip/LachrymoseMistyArtichokeAsianGlow?range=7d) | 1080p 파일, 44.713424s. 약 2초 간격 표본 | 실내 Raymond 대화 중 시작, 오래 같은 대화창 유지. 자율 활동 시작/종료/복귀 없음 |
| [AC-B / Why Are You Crying?](https://www.twitch.tv/mellowteddy/clip/CleanBovineWombatPlanking-wXlN-RI4Ep0V8SmE?range=7d) | 1080p, 13.108667s. Pascal 등장·가리비 요청/선택 표본 | 특수 방문/교환 사건이며 일상 주민의 공간 선택·생활 단위가 아님 |
| [AC-C / sir, WHY are you like this?](https://www.twitch.tv/birlap/clip/IgnorantExcitedTruffleNerfRedBlaster-8T8BYl9vxqMmM379?range=7d) | 720p, 13.407914s. 로딩/집 앞/Inventory 표본 | 앞부분과 UI 가림, 주민 행동의 전후 부족 |
| [AC-D / Roasted](https://www.twitch.tv/sykkuno/clip/DiligentSpikyBaguetteOhMyDog?range=7d) | 브라우저의 현재 프레임은 방송 화면/다른 영상. 파일 다운로드 접근 성공 | 카테고리만 ACNH. 전체 파일 프레임 선별은 하지 않아 전 구간의 부재를 단정하지 않음 |
| [AC-E / the most ominous thing…](https://www.twitch.tv/lycel/clip/TallOpenClintBloodTrail?range=7d) | 1080p, 5.472698s. 1·3·5초 부근이 방송자 화면 | 관찰 표본에 게임 장면 없음 |
| [AC-G / same](https://www.twitch.tv/neth3/clip/StrangeDarkTroutBCouch-QkVW0REB0tOcSxa7?range=7d) | 브라우저 현재 프레임은 해변 낚시 | 주민의 생활 연결을 확인하지 못함. 파일 전체는 분석하지 않음 |
| [Reddit / Fang immediately perks up when Audie joined](https://www.reddit.com/r/AnimalCrossing/comments/hwb2q9) | 게임 대신 사이트 보안 차단 화면 | 실제 프레임·원음·연속성 검증 불가. 제목/댓글 행동을 O로 채택하지 않음 |

YouTube Treehouse는 1차 연구의 화면 접근 실패 자료다. 이번 신규 관찰 자료로 쓰지 않았다. CEDEC 보도·다른 커뮤니티 설명도 이번에 원작 알고리즘·성격별 확률·수치의 근거로 채택하지 않았다.

## P.A. 기준·기존 연구 B/C

고정 기준 [Canon v2](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/01_GAME_DESIGN/Canon/PROJECT_PA_OPENING_DEMO_CANON_V2_2026-09-16.md), [Identity](https://github.com/junido7185/Project_PA/blob/b121140ae6f5ca0f92078db5c2f75512dc8fc95a/Docs/01_GAME_DESIGN/Canon/PROJECT_PA_IDENTITY.md). Canon blob `b4d5260cd96940a4f4b90a3e5665970564c58c8e`, Identity blob `203f6b2bb8721836a4c4b5c19f2bbdc96a5b2627`는 기존 연구의 보존 기준이다.

이번에 읽은 기존 보존본: `CANON_V2_snapshot.md`(모든 페이지), `IDENTITY_snapshot.md`, `02_ACNH_LEARNING_OWNERSHIP.md`, `SOURCE_LEDGER.md`, `SCENE_LEDGER.md`, `CANON_V2_DEVELOPMENT_KNOWLEDGE.md`, `03_CANON_TRACEABILITY.md`, `03_COMMERCE_PRICE_CUSTOMER.md`, `04_PROGRESS_TIME_DELEGATION.md`, `06_INTEGRATION_AND_WORK_PACKETS.md`, `DINKUM_MOTION_EVIDENCE_PREFLIGHT_2026-10-12.md`.

기존 보고서와의 상대 링크는 배포 경로가 달라 깨질 수 있어 이번 패키지에서는 기존 파일명을 탐색 키로 쓴다. 원문 기준 GitHub URL은 고정 commit을 유지한다. 최신 코드/게임 실행 검증은 수행하지 않았다.

# README에 사용한 게임 화면

2026-11-09 검증 빌드 r258의 Windows 실행 화면에서 선별했다. 새로 게임을 실행하거나 게임 코드를 바꾸지 않고 기존 검증 캡처를 사용했다. 원본은 Unity 6000.3.2f1 Windows 플레이어의 `ExhibitionCaptureDirector`가 생성했으며, 게임 화면 이외의 합성 이미지나 콘셉트 아트는 포함하지 않는다.

## 촬영 조건

- 실행 창: 1920×1080, Windowed. 캡처 원본은 슈퍼샘플링한 3840×2160이다.
- `clean` 화면은 HUD를 숨긴 컷이다. 카메라와 상황은 시나리오별로 설정된다.
- 시나리오는 게임 UI·입력 경로와 검증용 배치·재료 지급·위치·시간 설정을 함께 사용한다. 사람이 처음부터 끝까지 직접 플레이한 기록으로 해석하지 않는다.
- `visitor`는 새 게임 → 출항 → 정착 → 자연 일몰 → 첫 영업 → 보고서를 한 세션에서 실행한다. 재료·진열 설정 등에는 검증 보조 경로가 포함된다.
- 사용자 저장 대신 별도 저장 폴더를 사용한다.
- 웹용 이미지는 구도와 내용을 유지한 채 1600×900 JPEG로 축소했다. 색·조명 보정, 오브젝트 추가·제거, UI 합성은 하지 않았다.

## 이미지 목록

| 파일 | 내용 | 원본 시나리오 |
| --- | --- | --- |
| title.jpg | 타이틀 | story |
| settlement.jpg | 가게·텐트 정착 | visitor |
| night-shop.jpg | 저녁 가게와 손님 | market |
| coast.jpg | 해안·수중 바위·해초 | beach |
| fishing.jpg | 실제 낚시 화면 | feedback |
| collection.jpg | 곤충 도감 | ui |
| crafting.jpg | 조리대 | crafting |
| price.jpg | 진열·가격 설정 | ui |
| interior.jpg | 가게 내부와 소품 | decorset |
| residents.jpg | 동료 선물 반응 | gift |
| report.jpg | 첫 영업 보고서 | visitor |
| workstations.jpg | 작업대·조리대·용광로 | crafting |

원본 상대 경로·크기·촬영 시각·SHA-256은 [captures.json](captures.json)에 있다. 원본 PNG와 전체 실행 로그는 로컬 `Logs/ParallelProduction/Feedback-20261103/`에 보존돼 있으며 Git에는 포함하지 않는다. 공개한 [회귀 결과](verification-summary.txt)는 그 실행의 요약이다. 이번 README 작업에서 게임을 다시 검증한 결과는 아니다.

루트 README의 `../hero-settlement.jpg`는 이전 포트폴리오 브랜치에서 이미 사용하던 Editor Play Mode 이미지다. r258 이미지와 맵·카메라·촬영 조건이 달라 동일 조건 전후 비교로 사용하지 않는다.

# Dinkum 벌목 심층 연구 — 2026-10-12

Project P.A. Opening Demo Canon v2 / ACT-01·02 / 기존 U-01의 **벌목 부분**에 대한 독립 후속 연구다. 바위·채광·다른 게임은 분석 범위에 넣지 않았다. 기존 Canon과 1차 연구 문서는 수정하지 않았다.

## 연구 결과와 한계

**반복 타격의 시각적 표현은 새로 직접 관찰하고 프레임으로 분해했다. 접근부터 목재 획득까지 이어지는 HUD·원본 음향 포함 연속 플레이는 확보하지 못했다. U-01은 닫지 않는다.**

핵심 근거는 공식 사이트의 1920×1080, 60 fps 미디어다. 같은 숏의 전경 인물에게서 두 번의 도끼 동작과 줄기 주변 효과를 확인했다. 이 숏은 약 1.48초이며, 앞뒤가 편집으로 끊긴다. 음향 트랙을 실제 디코딩한 결과 모든 샘플이 0이었다. 따라서 이 자료는 손·몸·도구·접촉 표현의 제한된 근거이고, 입력 응답·자원 소진·목재 지급·효과음의 근거가 아니다.

## 필요한 질문부터 읽기

| 질문 | 문서 |
|---|---|
| 실제로 무엇을 보았고, 어떤 원리를 추론할 수 있는가? | [DINKUM_LOGGING_REVERSE_ANALYSIS_2026-10-12.md](DINKUM_LOGGING_REVERSE_ANALYSIS_2026-10-12.md) |
| 출처·장면·프레임·주장의 경계를 감사하려면? | [DINKUM_LOGGING_EVIDENCE_LEDGER_2026-10-12.md](DINKUM_LOGGING_EVIDENCE_LEDGER_2026-10-12.md) |
| 파일 FPS와 실제 게임 타이밍을 어떻게 구별했는가? | [DINKUM_LOGGING_MEASUREMENTS_2026-10-12.md](DINKUM_LOGGING_MEASUREMENTS_2026-10-12.md) |
| Unity에서 첫 나무 상호작용을 어떻게 설계·검수할 것인가? | [PA_LOGGING_IMPLEMENTATION_KNOWLEDGE_2026-10-12.md](PA_LOGGING_IMPLEMENTATION_KNOWLEDGE_2026-10-12.md) |
| 무엇이 미확인이고 어떤 장면이 더 필요한가? | [DINKUM_LOGGING_CAPTURE_AND_GAPS_2026-10-12.md](DINKUM_LOGGING_CAPTURE_AND_GAPS_2026-10-12.md) |
| 어떤 검증이 끝났고 어떤 플레이 검증은 남았는가? | [DINKUM_LOGGING_VALIDATION_2026-10-12.md](DINKUM_LOGGING_VALIDATION_2026-10-12.md) |

## 다른 AI가 사용할 때의 규칙

- **O**: 직접 본 화면. 명시한 파일·프레임·구간 안에서만 사실이다.
- **M**: 파일 메타데이터, PTS, 디코딩 샘플 등 실제 측정. 게임 내부 값과 구별한다.
- **I**: 관찰을 설명하는 디자인 해석. 원작 개발자의 의도나 구현을 확정하지 않는다.
- **H**: P.A.에 대한 구현·검수 가설. 도입 여부와 수치는 실제 프로젝트에서 검증한다.
- **C**: 고정 Canon의 규칙. **B**: 직접 읽은 기존 연구의 내용. **U**: 미확인.

예를 들어 두 원형 효과의 첫 관찰 프레임 간격은 52프레임이다(M). **‘Dinkum의 도끼 쿨다운은 0.867초’라는 결론은 허용되지 않는다.** 실제 입력, 재생 편집 속도, 도구 등급, 게임 빌드, 성공 판정이 확인되지 않았기 때문이다.

이 문서 묶음은 관찰된 반복 동작의 원리를 P.A. 실험으로 옮길 수 있게 만든다. 전체 벌목의 원작 손맛을 검증 완료했다거나 P.A. 플레이 품질을 실행 PASS로 판정하는 자료가 아니다.

## 기준 문서

2026-10-11 1차 연구 묶음의 다음 보존본을 먼저 직접 읽었다: `01_DINKUM_ACTION_AND_WORLD.md`, `01_ACTION_EQUIPMENT_GATHERING.md`, `07_PLAY_UNIT_CASES.md`, `OPEN_QUESTIONS.md`, `SOURCE_LEDGER.md`, `SCENE_LEDGER.md`. 새 출처·장면 ID는 `DL-` 접두사를 써서 기존 `DK-`와 섞이지 않게 했다.

Canon/Identity는 1차 연구의 고정 commit `b121140ae6f5ca0f92078db5c2f75512dc8fc95a`에서 다시 읽었고, 동일한 blob SHA를 확인했다. 이 연구에서 현행 Unity 코드 감사·플레이 실행·저장소 수정은 수행하지 않았다. 이전 코드 분석은 이전 시점의 분석으로만 취급한다.

작성일: 2026-10-12, Asia/Seoul. 링크의 미디어는 제공자가 변경할 수 있으므로 측정 문서의 SHA-256으로 같은 파일인지 확인한다. 원작 영상·이미지·음원을 이 묶음의 구현 자산으로 배포하지 않는다.

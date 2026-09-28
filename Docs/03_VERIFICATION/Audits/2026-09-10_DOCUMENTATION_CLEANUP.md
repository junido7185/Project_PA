# Documentation Refactor & Cleanup — 2026-09-10

문서 구조 정리 결과다. 게임플레이 milestone의 신규 완료나 새 구현 승인을 뜻하지 않는다.

## 기준과 판단

- Branch: `milestone/gameplay-beta-85`; 구현 HEAD: `4374b2aa27e73c2c7fec086eac6c4130eb702306`.
- 사용자가 첨부한 Documentation Refactor & Cleanup 지시에 따라 문서 이동·중복 삭제·참조 갱신을 수행했다. 이번 범위의 명시적 승인은 기존 문서 동결/삭제 금지/복수 보고서 갱신 관행보다 우선한다. 게임 기능·씬·저장 구조를 고칠 승인은 아니다.
- 현재 상태·기능·실제 루프·큐는 [00_CURRENT](../../00_CURRENT/CURRENT_STATE.md) 네 문서로 분리했다. 코드와 직렬화 연결, 기존 실제 검증 근거를 대조했고 P4 dirty를 checkpoint PASS와 구분했다.
- [전체 목록 CSV](2026-09-10_DOCUMENTATION_CLEANUP_inventory.csv): 원래 경로·현재 경로·날짜·목적·A~H 분류·권위·최신성·중복·고유 기록·incoming/code 참조·보존 가치·처분·원본 SHA256.
- [제외 목록](2026-09-10_DOCUMENTATION_CLEANUP_excluded.json): 외부 Blender 배포물의 문서와 생성된 빌드 사본. 외부 자산의 실제 라이선스는 제외하지 않고 원본을 유지했다.

## 구조·이동·병합

설계는 `01_GAME_DESIGN`, 구현은 `02_IMPLEMENTATION`, 날짜별 감사는 `03_VERIFICATION`, 개발 이력은 `04_DEVELOPMENT_LOG`, 출처 입구는 `05_ASSET_PROVENANCE`, 발표·개인 제출은 `90_PRESENTATION`, 대체된 계획과 규칙은 `99_ARCHIVE`로 정리했다. 빈 분류 폴더는 만들지 않았다.

[이동 원장](2026-09-10_DOCUMENTATION_CLEANUP_moves.json)은 모든 이전/이후 경로를 기록한다. Git 이동 이력을 유지했다. 발표 증거의 날짜별 내부 구조와 파일명은 보존했다. P4 미커밋 자료는 기존 위치 그대로다.

STATUS / SESSION_REPORT / TODO / 개발일지 / CHANGELOG / HANDOFF의 **858개 섹션**을 월별로 병합했다. **완전히 같은 63개 섹션**만 합쳤고 나머지 795개는 각각 보존했다. [섹션별 보존 원장](2026-09-10_DOCUMENTATION_CLEANUP_history.json)은 원본 hash·출처 섹션·목적지·중복 여부·실제 보존 검사 결과를 포함한다. 문장 의미는 고치지 않고 이동한 링크를 갱신했다. 원문의 Markdown 줄끝 공백 11곳은 동일한 hard line break인 역슬래시 표기로 바꿔 diff 검사를 통과시켰다. 날짜 없는 원문은 소급 작성일을 만들지 않고 `undated` 또는 앞선 명시 날짜에 귀속했다.

CHANGELOG·HANDOFF·기존 개발일지는 짧은 기록/재개 절차와 링크로 바꾸었다. AGENTS·CLAUDE·workflow·작업자·검증 규칙은 결과를 해당 월에 한 번만 남기도록 수정했다. 옛 ACTIVE/DONE·승인 표는 역사로 보존하고 현재 실행 지시와 구분했다.

## 삭제한 파일 전체

아래 세 파일은 고유 섹션을 모두 월별 기록에 보존한 후 중복 보고서 역할을 제거했다. [삭제 원장](2026-09-10_DOCUMENTATION_CLEANUP_deleted.json)과 일치한다.

- `PROJECT_PA_STATUS.md`
- `PROJECT_PA_SESSION_REPORT.md`
- `PROJECT_PA_TODO.md`

기타 원본 문서는 이동하거나 역할을 축소했으며 삭제하지 않았다. `.tmp_template_proposal.pptx`는 최종 발표본과 다른 FR/NFR·로드맵이 있어 Drafts에 보존했다. 같은 제목의 여름 발표 PPT/PDF도 8슬라이드/7페이지로 내용이 달라 둘 다 남겼다. 기획 제출본과 수업용 PDF도 각각 보존했다.

## 경로 호환과 예외

- `Docs/01_`~`06_`, `08_`의 코드 § 인용과 루트 `PROJECT_PA_CRASH_REPORT_*.md` preflight glob을 유지했다. `07_개발일지.md`는 호환 입구다.
- `Docs/AssetProvenance/`와 원본 라이선스는 기존 Blender/library/Unity 파이프라인이 참조하므로 고정 경로로 남기고 `05_ASSET_PROVENANCE`에서 연결했다.
- Editor 검증기 5개의 증거 출력 prefix만 `Docs/90_PRESENTATION/Evidence/`로 바꿨다. 검사 로직은 동일하다. 추가 C# 7개와 Blender 제작 스크립트 2개의 주석·안내문·출처 문자열, 출처 JSON 4개와 P3 integrity의 캡처 경로만 갱신했다. `loop-state.json`은 P0/P2 evidence 경로 두 문자열만 바꿨으며 승인·티켓·상태·과거 changedPaths를 수정하지 않았다.
- 보호된 BUG_LOG/P4 원문과 automation의 과거 changedPaths, 병합 기록의 Original sources에는 당시 경로가 남아 있다. [잔존 경로 목록](2026-09-10_DOCUMENTATION_CLEANUP_references.json)에서 현재 링크와 역사적 출처를 구분했다. 삭제된 보고서의 옛 경로를 실행 입력으로 사용하지 않는다.
- 정리 전부터 없던 SETTLEMENT 설계 참조 두 종류와 Notion 이미지 네 개는 깨진 링크 대신 원래 경로와 누락 사실을 표시했다. 가짜 파일로 채우지 않았다.

## 검증

- Runtime / Editor `dotnet build`: **오류 0**. Runtime CS8785 경고 1개, Editor CS8785·CS0414 경고 2개. 첫 `--no-restore` 시도는 생성 자산 파일 부재 NETSDK1004였고 기존 프로젝트 restore 후 해결됐다. 패키지 추가 없음.
- 기존 Unity Play PASS·캡처를 재사용했다. 이번 문서 정리에서는 Unity Play·새 빌드·시각 검증을 실행하지 않았다.
- 원본 858개 섹션 SHA256 및 월별 보존 검사, 이동된 바이너리 동일성, 전체 tracked 비문서 파일 비교, 보호 dirty 6개 동일성: [안전 검사](2026-09-10_DOCUMENTATION_CLEANUP_safety.json).
- 현재 문서의 상대 Markdown 링크를 검사했다. 출처 원문이 없는 참조는 위 예외로 명시했다.
- 전체 `git diff --check`에는 기존 P4 `DepartureContinuation.prefab`의 trailing whitespace 4개가 남는다. 사용자 파일을 수정하여 없애지 않았다. 이번 정리 범위의 diff 검사 결과는 월별 완료 기록에 남긴다.
- 임시 검증 실행 로그: `Logs/DocumentationCleanup/`. 감사 CSV/JSON은 이 문서 옆에 보존한다. 재사용 validator framework를 추가하지 않았다.

## 남은 확인

문서 권위는 작게 유지한다. 과거 설계의 제안/TEMP, P4 dirty Save v16 문제, M85 저장 검증 부채는 현행 PASS로 승격하지 않았다. 후속 구현은 별도 사람 지시로만 시작한다. 이번 작업의 commit·push는 하지 않는다.

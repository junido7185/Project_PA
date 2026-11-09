# Project P.A. 제작 계약

2026-10-25 최신 직접 사용자 개정 (`PA_AUTONOMOUS_REFERENCE_REVIEW_20261025_V1`): 사용자는 사람 검수 대기 없이 개발을 계속하고, 구독 사용량 회복마다 기존 helper로 자동 재개하며, 품질 검수는 기존 레퍼런스와 실제 게임 결과를 AI가 비교하도록 명시했다. 이 개정이 아래 역사적 사람 검수 필수/FINAL_WINDOWS_HUMAN_REVIEW 문구보다 우선한다. 현 실행 묶음의 끝은 FINAL_WINDOWS_REFERENCE_REVIEW이며 전체 Canon 완성과 구분한다. 사람 수락을 받았다고 기록하지 않으며, 기능·저장·성능은 실제 실행 증거로 검증하고 화면·동작·소리의 품질은 관련 레퍼런스/동일조건 전후/독립 AI 비평으로 판정한다. 근거가 부족하면 미확인으로 남기되 독립적으로 가능한 제작을 계속한다. 기존 승인·누적 사용량·단일 owner/lease·원본 dirty/Save 보호·외부 결제/공개 제한·실제 권한 거절과 Unity/native 입력 Esc 중지는 유지한다. 증거는 기존 지원 경로 `Logs/ParallelProduction/Production-observer-codex-20261020/auto-resume-20261025/user-request.md`이며 새 승인/티켓 권위를 만들지 않는다.

2026-10-19 현재 운영 정책: 기존 Canon의 전체 게임 목표와 현재 실행 묶음의 FINAL_WINDOWS_HUMAN_REVIEW를 구분한다. 승인 목표 안의 구현·세부 설계·복구·검증은 자율 진행하며 단계마다 사용자 재승인을 요구하지 않는다. 파일 수·작업 총시간·compile/Play/validator 횟수·기계적 연속 실패·옛 티켓/모델/담당자 배정을 실행 상한으로 쓰지 않는다. 파일 소유 범위는 충돌 확인 후 기존 registry에서 조정하고, 순서는 최신 checkpoint와 품질 우선순위를 따른다. 시도·사용량·실패·거절·승인 이력은 누적 보존한다. 같은 행동을 진전 없이 반복하면 원인·바꾼 접근·새 근거를 checkpoint에 남긴 뒤 복구하고, 실행 상태가 모호하면 확인 전 재실행하지 않는다. 실제 출력/소유 파일 활동이 끊긴 프로세스의 inactivity timeout은 유지하며 작업 총시간 상한과 분리한다. 120초 복구 스냅샷·20분 안전 인계는 재개 가능한 복구 주기다. 실제 실행 정책은 Automation/LoopEngineering/loop-policy.json의 recoveryPolicy를 따른다. 사용자 dirty·Save 호환성·기존 권위/디자인 조건·현재 반려/보류·단일 Unity writer·미승인 결제/외부 공개 금지·최종 사람 검수는 유지한다. 과거 고정 담당자 문구보다 현재 유효한 state/lease와 검증된 인수인계가 우선하며, 이번 운영 정리는 제작 재개나 writer 이전 승인이 아니다.

2026-10-10 최신 참고 정책: `References/Dinkum/` 원본16장과 실제 웹 PC 영상 구간을 사용한다. 설치된 딩컴은 오류 때문에 실행하지 말라는 사용자 지시가 우선한다. 로컬 딩컴 실행·입력·모드 수정 금지. 동작은 실제 영상 시계열 근거와 한계를 구분하며 정지 이미지/자막만으로 검증하지 않는다.

낮에는 섬에서 생활하고 동물 마을을 만들며, 밤에는 유일한 잡화점을 운영하는 3D 코지 라이프 게임이다. 판매와 도구 지원이 주민 산업과 마을 풍경을 바꾼다. 졸업 시연은 완성 게임으로 이어지는 중간 단계다.

사용자가 승인한 목표 안에서 작업을 나누고 검증한 다음 작업을 계속한다. 매 티켓 추가 지시를 기다리지 않는다. 실제 승인 범위·거절·보류·최종 사람 검수 경계는 유지한다. 환경 준비와 게임 완성은 별개다.

## 작업 보호와 소유권

- 기준은 현재 로컬 branch/HEAD와 사용자 미커밋 변경이다. master 전환·reset·clean·삭제·자동 commit/push 금지. 설정 변경 전에 복원 가능한 프로젝트 내 백업을 만든다.
- 원본 작업 루트는 `C:/Users/sdjsd/Desktop/Unity/Project_PA`. 이 루트 아래 등록된 격리 worktree는 지정 파일에 한해 작업 가능하다. Project_D는 읽기 전용이며 수정·복사·병합하지 않는다.
- 2026-10-09 소유권 이력(현재 state/lease 우선): Claude Code가 제작 총괄과 원본 통합·실제 Unity Editor의 단일 소유자다. Codex는 검토·독립 지원이며 원본 제작 파일과 Unity를 동시에 수정하지 않는다. 실제 소유권은 기존 loop-state의 `demoProductionRouting.unityEditorOwner` 현재 값과 lease를 따른다.
- Claude 총괄은 제작·독립 비평·수정·검증 작업자를 현재 HEAD + 필요한 로컬 변경의 별도 worktree에 배정한다. 작업 목표·기준 HEAD·소유 파일을 기존 loop-state에 기록한다. 원본 Unity MCP는 총괄에게만 연결하고 하위 작업자에게 제외한다. 총괄이 범위·원본 충돌·diff를 검토한 뒤 통합한다.
- 변경 파일을 편집 전에 보고한다. 기존 Player/Camera/Inventory/Hotbar/Shop/Economy/NPC/World/Placement/Save 권위를 재사용한다. 대형 재작성·두 번째 권위·호환 불가능한 저장 마이그레이션은 별도 승인 대상이다.

## 실제 제작과 완료

기존 약 5분 플레이는 사용자 기준 발표 가능한 상태다. 최신 제작 순서는 섬·바다·식생 → 캐릭터·채집 동작 → 아이템·생활 소품 → UI → 전체 조화다. 재개 시 이미 수행한 월드 전후 비교를 인수하고 실제 참고 화면과 현재 GameView의 가장 앞선 미해결 품질 차이부터 개선한다. 대표 구역에서 기준을 잡고 관련 공간으로 확장하며 추가 프롬프트를 기다리지 않는다. 기존 D0~D12 범위와 결과를 재사용하되 옛 티켓 순서가 이 우선순위를 막지 않는다.

승인된 플레이 경험에 필요한 코드·scene·prefab·material·animation·UI·audio/VFX·Editor 보조 편집은 허용한다. 기존 로컬 자산을 먼저 조사하고 Unity API/Editor utility로 편집한다. 대규모 YAML 직접 편집·이름만 보고 삭제·reference 일괄 치환 금지. Golden `Prototype_FirstDay.unity`, MainGame 통합, 저장 schema는 목표가 명시적으로 포함하지 않으면 보호한다.

코드 변경은 Unity 컴파일과 관련 검증을 실행한다. 플레이어 화면 변경은 실제 입력과 GameView 이미지/영상까지 검수한다. 기능·표현·사람 수락을 분리하고 미실행은 '확인 못 함'으로 기록한다. 숫자 PASS나 CLI exit 0으로 제품 완성을 선언하지 않는다.

오류는 원인과 접근을 바꾸어 복구한다. 시도 횟수만으로 중단하지 않는다. 같은 실패를 근거 없이 반복하지 않으며 실제 진전·세션 시간·계정 사용량을 확인한다. 사용자 작업 충돌, 저장 손실 위험, 권위 재설계, native crash의 미해결 원인, 계정/관리자 제한처럼 현재 범위에서 복구할 수 없는 경우 결과를 보존하고 정확히 중단한다. 과거 횟수·사용량 기록은 지우거나 초기화하지 않는다.

## 최소 인수와 기록

시작 시 현재 Git/실행 세션과 `Docs/00_CURRENT/CODEX_HANDOFF.md`를 읽는다. 현재 상태는 `CURRENT_STATE.md`, 기능은 `CAPABILITY_REGISTRY.md`, 연결은 `GAME_LOOP_MAP.md`, 다음 범위는 `INTEGRATION_QUEUE.md`의 관련 부분만 읽는다. 동일 세션에서 재독하지 않으며 추가 문서는 `AI_WORKFLOW/00_START_HERE/DOCS_INDEX.md`로 필요할 때 찾는다.

기존 `Automation/LoopEngineering/State/loop-state.json`이 실행·승인·소유권 기록이다. 별도 상태 체계를 만들지 않는다. 종료 시 월별 개발 로그에 결과를 한 번 쓰고 바뀐 현재 사실만 갱신한다. Handoff는 현재 목표·변경 파일·검증·미해결·다음 행동·commit/push 상태만 짧게 덮어쓴다.

Task Observer는 요청 또는 반복되는 작업 방식 문제를 검토할 때만 사용한다. 도구 호출·계획·재개·완료의 필수 단계가 아니다. Serena와 문서 MCP는 선택 사항이며 장애 시 rg/파일 읽기/공식 웹 문서로 계속한다. 이번 환경 구성에서 필요한 도구 연결·복구 가능한 설치는 사용자 승인 범위다. 이후 제작 중 무관한 외부 패키지/자산은 자동 도입하지 않는다.

게임플레이·NPC·월드·UI·상점 등 제작/비평/검증에서는 `Docs/01_GAME_DESIGN/ReferenceResearch/README.md`의 최신 색인에서 현재 작업에 관련된 연구 문서만 선택해 활용한다. 계속 추가되는 자료를 고정 목록으로 대체하거나 전체 문서 열람을 선행 조건으로 강제하지 않는다. 공식 Canon·최신 구현 코드·실제 검증 결과가 우선이며, 연구의 관찰/측정과 해석/가설/미확인을 구분한다. 연구 가설을 확정 요구사항·실행 PASS로 올리거나 연구만으로 기존 시스템을 교체하지 않는다. 격리 작업자에게도 같은 색인과 필요한 원문 접근을 전달하고, 적용한 근거·한계를 실제 제작 판단에 짧게 남긴다.

상세 제작 기준은 `AI_WORKFLOW/02_AGENT_RULES/VERTICAL_SLICE_STUDIO_MODE.md`, 검증은 `AI_WORKFLOW/04_VERIFICATION/VERIFICATION_RULES.md`. 상세 파일의 옛 대기·횟수 제한·모델 고정·중복 읽기 규칙보다 이 계약과 최신 사용자 승인이 우선한다.

자동 릴레이는 기존 helper/state를 사용한다. 각 팀은 작은 소유 작업의 결과·검증·미해결·다음 행동을 남기며, 사용량을 알 수 없어도 120초 복구 스냅샷과 20분 안전 인계를 사용한다. `worker --provider auto`는 실제 사용 가능성과 능력을 확인한 팀에 배정한다. 원본 Unity 소유권은 가용성만으로 이전하지 않는다. 유효한 사용자 전환 승인과 검증된 adapter가 정확한 이전 실행 종료·동일 baseline·입력/save·in-flight를 확인한 경우에만 일관되게 전환한다. 사용량/스냅샷/환경 검사를 게임 PASS로 기록하지 않는다.

2026-10-11 인수 이력: 사용자가 Claude의 중단 작업을 Codex 팀에 이어가라고 승인했다. 실행 중인 Claude 작업자가 종료된 것을 확인한 후 현재 native Codex 총괄이 원본 Unity 통합을 단독 소유한다. 기존 state의 demoProductionRouting/lease가 현재 소유권이며 다른 팀은 격리 작업만 진행한다. 완료 또는 사용량 안전 인계 때 동일 상태에서 소유권을 정합화하고 Claude 기록을 보존한다.

2026-10-12 인계 이력: 사용자가 자신의 PowerShell에서 기존 Claude 총괄 대화에 직접 지시하기를 요청했다. Codex는 Unity를 WorldSandbox/Edit/컴파일 없음/scene dirty 없음/격리 save override 해제로 멈추고 변경·검증·미해결을 최신 Handoff에 보존했다. 원본 제작과 Unity 소유권은 Claude로 반환하며 Codex는 검토·격리 지원이다. 원래 총괄 SID `3f66c90c-d5eb-4a51-9c99-44d7744d2310`는 기존 런처의 `-Interactive -ResumeSession`으로 재개한다. 사용자가 실행하기 전에는 총괄이 동작 중이라고 기록하지 않는다. 다른 원본 CLI를 먼저 종료하며 같은 Unity를 두 총괄이 수정하지 않는다.

레퍼런스 설계 판단이 필요한 작업은 Docs/01_GAME_DESIGN/References/README.md에서 관련 결정 1~3개만 읽는다. 개발자 의도·관찰·우리 해석·P.A. 적용을 구분하고, 기존 결정으로 답할 수 있으면 웹/전체 문서 재조사를 생략한다. 이 묶음은 Canon·현재 승인·시스템 권위를 바꾸지 않는다.

환경 설치·진단·구독 릴레이·작업 패널 등 운영 도구를 변경할 때만 `Tools/PaAgentConsole/SESSION_RULES.md`를 읽는다. 환경 준비는 게임 제작 재개 승인이 아니며 현재 loop-state/Handoff가 패키지의 과거 D2 예시보다 우선한다.

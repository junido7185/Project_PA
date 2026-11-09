# 클로드 코드 데스크탑 환경 — 스킬·플러그인 전수 조사 (2026-11-09)

조사 방법: 데스크탑 세션의 스킬 목록, claude.ai 계정의 스킬·플러그인 조회 도구(ListSkills·ListPlugins), 로컬 설정 파일(`~/.claude/settings.json`, 프로젝트 `.claude/settings*.json`, `installed_plugins.json`), 플러그인 캐시 폴더를 직접 읽었다. 사용 가능 여부는 설정 값 기준이며, 실제 호출은 하지 않았다.

## 0. 한눈에 보기

| 구분 | 이 프로젝트 세션에서 켜짐 | 비고 |
| --- | --- | --- |
| claude.ai 계정 스킬 (anthropic-skills) | 켜짐 | 16개 |
| 로컬 플러그인 | **`pa-production-console`만 켜짐** | 나머지 7개는 프로젝트 설정이 꺼 둠(제작 세션 운영 정책) |
| 프로젝트 스킬 | 파일은 있음 | 이 세션의 스킬 목록에는 안 보임(확인 필요) |
| MCP 서버 | 일부만 연결 | 유니티·깃허브 연결 실패, context7 인증 필요 |

## 1. claude.ai 계정 스킬 — `anthropic-skills:*` (이 세션에서 바로 사용 가능, 16개)

| 스킬 | 용도 |
| --- | --- |
| built-in-browser | 데스크탑 앱 안 내장 브라우저 사용법 |
| chrome-browser | 사용자의 실제 크롬(확장 프로그램) 사용법 |
| computer-use | 내 PC 앱을 화면으로 보고 조작(데스크탑 제어) |
| consolidate-memory | 메모리 파일 정리(중복 병합, 낡은 사실 수정) |
| deep-research | 여러 출처를 조사해 종합 보고서 작성(하위 에이전트 사용) |
| docs | 공유·댓글 가능한 문서 작성(기본 문서 도구) |
| docx | 워드 문서 생성·편집 |
| explain-usage | 이 세션의 토큰 사용 내역을 차트로 설명 |
| google-workspace | 구글 문서·시트·슬라이드 생성·수정 |
| import-memory | 다른 AI의 메모리 내보내기를 가져오기 |
| morning | 아침 브리핑(HTML 아티팩트) 생성·정기 작업 설정 |
| pdf | PDF 읽기·병합·분할·양식 작성·OCR |
| pptx | 파워포인트 생성·편집 |
| setup-claude | 역할별 플러그인 설치·도구 연결 안내 |
| skill-creator | 스킬 만들기·개선·성능 측정 |
| xlsx | 엑셀·CSV 읽기·편집·생성 |

claude.ai 계정에 켜진 플러그인(ListPlugins 결과): **없음**.

## 2. 로컬 설치 플러그인 8개 (`installed_plugins.json`)

켜짐 표시는 `~/.claude/settings.json`(전역)과 프로젝트 `.claude/settings.json`·`settings.local.json`을 합친 결과다. 프로젝트 설정이 전역보다 우선한다.

| 플러그인 | 버전 | 전역 | 이 프로젝트 | 구성 |
| --- | --- | --- | --- | --- |
| pa-production-console (project-pa-local) | 0.3.0 | — | **켜짐** | 훅만(관찰 기록 `hooks/register.js`) |
| ecc (everything-claude-code) | 2.2.3 | 켜짐 | 꺼짐 | 스킬 293·명령 94·에이전트 68·훅·MCP |
| claude-mem (thedotmack) | 15.12.0 | 켜짐 | 꺼짐 | 스킬 30·훅·MCP |
| figma (공식) | 2.2.157 | 켜짐 | 꺼짐 | 스킬 16·MCP |
| claude-code-setup (공식) | 1.0.0 | 켜짐 | 꺼짐 | 스킬 1 |
| humanize-korean (im-not-ai) | 2.3.2 | (프로젝트 설치) | 꺼짐 | 스킬 3·에이전트 9 |
| context7 (공식) | — | 켜짐 | (설정 없음 → 전역 켜짐) | MCP |
| github (공식) | — | 켜짐 | (설정 없음 → 전역 켜짐) | MCP |

참고: `task-observer@skills-dir`도 프로젝트 설정에 꺼짐으로 적혀 있다.

### 2-1. figma 스킬 16개
figma-code-connect, figma-code-workflows, figma-create-new-file, figma-design-to-code, figma-generate-design, figma-generate-diagram, figma-generate-library, figma-generative-plugins, figma-implement-motion, figma-shaders, figma-spec, figma-swiftui, figma-use, figma-use-figjam, figma-use-motion, figma-use-slides

### 2-2. claude-mem 스킬 30개
agent-cost-report, babysit, backup-setup, build, build-and-release, ccs-align, childhood, cloud-sync, design-is, do, handoff, how-it-works, knowledge-agent, learn-codebase, make-plan, mem-search, mode-creator, oh-my-issues, owner-goals, pathfinder, release, request-progress, screenpipe, smart-explore, standup, timeline-report, version-bump, weekly-digests, what-the, wowerpoint

### 2-3. claude-code-setup · humanize-korean
- claude-code-setup 스킬: claude-automation-recommender
- humanize-korean 스킬: humanize, humanize-korean, humanize-redo
- humanize-korean 에이전트 9개: humanize-diagnostician, humanize-finalizer, humanize-monolith, korean-ai-tell-taxonomist, korean-translation-scholar, post-editese-metric-engineer, quick-rules-integrator, taxonomy-gap-analyzer, translationese-research-distiller

### 2-4. ecc — 스킬 293개
accessibility, agent-architecture-audit, agent-eval, agent-harness-construction, agent-introspection-debugging, agent-payment-x402, agent-self-evaluation, agent-sort, agentic-engineering, agentic-os, ai-first-engineering, ai-regression-testing, android-clean-architecture, angular-developer, api-connector-builder, api-design, architecture-decision-records, article-writing, automation-audit-ops, autonomous-agent-harness, autonomous-loops, backend-patterns, benchmark, benchmark-methodology, benchmark-optimization-loop, blender-motion-state-inspection, blueprint, brand-discovery, brand-voice, browser-qa, bun-runtime, canary-watch, carrier-relationship-management, cisco-ios-patterns, ck, claude-devfleet, click-path-audit, clickhouse-io, code-tour, codebase-onboarding, codehealth-mcp, coding-standards, competitive-platform-analysis, competitive-report-structure, compose-multiplatform-patterns, config-gc, configure-ecc, connections-optimizer, content-engine, content-hash-cache-pattern, context-budget, continuous-agent-loop, continuous-learning, continuous-learning-v2, contract-first, cost-aware-llm-pipeline, cost-tracking, council, council-multi-model, counterparty-channel-discipline, cpp-coding-standards, cpp-testing, crosspost, csharp-testing, customer-billing-ops, customs-trade-compliance, dart-flutter-patterns, dashboard-builder, data-scraper-agent, data-throughput-accelerator, database-migrations, deep-research, defi-amm-security, delivery-gate, deployment-patterns, design-system, dev-team, django-celery, django-patterns, django-security, django-tdd, django-verification, dmux-workflows, docker-patterns, documentation-lookup, dotnet-patterns, dynamic-workflow-mode, e2e-testing, ecc-guide, ecc-recipes, ecc-tools-cost-audit, email-ops, energy-procurement, enterprise-agent-ops, error-handling, esign-field-placement, eval-harness, evm-token-decimals, exa-search, fal-ai-media, fastapi-patterns, finance-billing-ops, flox-environments, flutter-dart-code-review, foundation-models-on-device, frontend-a11y, frontend-design-direction, frontend-patterns, frontend-slides, fsharp-testing, gan-style-harness, gateguard, generating-python-installer, git-workflow, github-ops, golang-patterns, golang-testing, google-workspace-ops, growth-log, healthcare-cdss-patterns, healthcare-emr-patterns, healthcare-eval-harness, healthcare-phi-compliance, hermes-imports, hexagonal-architecture, hipaa-compliance, homelab-network-readiness, homelab-network-setup, homelab-pihole-dns, homelab-vlan-segmentation, homelab-wireguard-vpn, hookify-rules, i18n-sync, inherit-legacy-style, intent-driven-development, inventory-demand-planning, investor-materials, investor-outreach, ios-icon-gen, iterative-retrieval, ito-baskets, ito-compute, ito-inference, ito-training, java-coding-standards, jira-integration, jpa-patterns, knowledge-ops, kotlin-coroutines-flows, kotlin-exposed-patterns, kotlin-ktor-patterns, kotlin-patterns, kotlin-testing, kubernetes-patterns, laravel-patterns, laravel-plugin-discovery, laravel-security, laravel-tdd, laravel-verification, latency-critical-systems, lead-intelligence, liquid-glass-design, living-docs-governance, llm-trading-agent-security, logistics-exception-management, loop-design-check, mailtrap-email-integration, make-interfaces-feel-better, manim-video, market-research, marketing-campaign, master-agreement-generator, mcp-server-patterns, messages-ops, ml-adoption-playbook, mle-workflow, motion-advanced, motion-foundations, motion-patterns, mysql-patterns, nanoclaw-repl, nasiko-control-plane, nestjs-patterns, netmiko-ssh-automation, network-bgp-diagnostics, network-config-validation, network-interface-health, nextjs-turbopack, nodejs-keccak256, nutrient-document-processing, nuxt4-patterns, openclaw-persona-forge, opensource-pipeline, operator-approval-loop, orch-add-feature, orch-build-mvp, orch-change-feature, orch-fix-defect, orch-pipeline, orch-refine-code, parallel-execution-optimizer, perl-patterns, perl-security, perl-testing, plan-canvas, plan-orchestrate, plankton-code-quality, postgres-patterns, prediction-market-oracle-research, prediction-market-risk-review, prisma-patterns, product-capability, product-lens, production-audit, production-scheduling, project-flow-ops, prompt-optimizer, python-patterns, python-testing, pytorch-patterns, quality-nonconformance, quarkus-patterns, quarkus-security, quarkus-tdd, quarkus-verification, rails-patterns, ralphinho-rfc-pipeline, react-native-patterns, react-patterns, react-performance, react-testing, recsys-pipeline-architect, recursive-decision-ledger, redis-patterns, regex-vs-llm-structured-text, remotion-video-creation, repo-scan, research-ops, returns-reverse-logistics, rules-distill, rust-patterns, rust-testing, safety-guard, santa-method, scientific-db-pubmed-database, scientific-db-uspto-database, scientific-pkg-gget, scientific-thinking-literature-review, scientific-thinking-scholar-evaluation, search-first, security-bounty-hunter, security-review, security-scan, seo, skill-comply, skill-scout, skill-stocktake, social-graph-ranker, social-publisher, springboot-patterns, springboot-security, springboot-tdd, springboot-verification, strategic-compact, swift-actor-persistence, swift-concurrency-6-2, swift-protocol-di-testing, swiftui-patterns, taste, taste-application, taste-distillation, tasteforge-video, tdd-workflow, team-agent-orchestration, team-builder, terminal-opener, terminal-ops, tinystruct-patterns, token-budget-advisor, ui-demo, ui-to-vue, uncloud, unified-memory, unified-notifications-ops, verification-loop, video-editing, videodb, visa-doc-translate, vite-patterns, vue-patterns, windows-desktop-e2e, workspace-surface-audit, x-api

게임 제작과 가까운 것: tdd-workflow, verification-loop, e2e-testing, windows-desktop-e2e, browser-qa, click-path-audit, csharp-testing, benchmark, latency-critical-systems, motion-foundations·motion-patterns·motion-advanced, blender-motion-state-inspection, design-system, frontend-design-direction, make-interfaces-feel-better, ui-demo, context-budget, strategic-compact, continuous-learning-v2, safety-guard, gateguard.

### 2-5. ecc — 슬래시 명령 94개
aside, auto-update, build-fix, checkpoint, code-review, cost-report, cpp-build, cpp-review, cpp-test, ecc-guide, epic-claim, epic-decompose, epic-publish, epic-review, epic-sync, epic-unblock, epic-validate, evolve, fastapi-review, feature-dev, flutter-build, flutter-review, flutter-test, gan-build, gan-design, go-build, go-review, go-test, gradle-build, harness-audit, hookify-configure, hookify-help, hookify-list, hookify, instinct-export, instinct-import, instinct-status, jira, kotlin-build, kotlin-review, kotlin-test, learn-eval, learn, loop-start, loop-status, marketing-campaign, model-route, multi-backend, multi-execute, multi-frontend, multi-plan, multi-workflow, orch-add-feature, orch-build-mvp, orch-change-feature, orch-fix-defect, orch-refine-code, orch-review, plan-canvas, plan-prd, plan, pm2, pr, project-init, projects, promote, prp-commit, prp-implement, prp-plan, prp-pr, prp-prd, prune, python-review, quality-gate, react-build, react-review, react-test, refactor-clean, resume-session, review-pr, rust-build, rust-review, rust-test, santa-loop, save-session, security-scan, sessions, setup-pm, skill-create, skill-health, test-coverage, update-codemaps, update-docs, vue-review

### 2-6. ecc — 에이전트 68개
a11y-architect, agent-evaluator, architect, build-error-resolver, chief-of-staff, code-architect, code-explorer, code-reviewer, code-simplifier, comment-analyzer, conversation-analyzer, cpp-build-resolver, cpp-reviewer, csharp-reviewer, dart-build-resolver, database-reviewer, django-build-resolver, django-reviewer, doc-updater, docs-lookup, e2e-runner, fastapi-reviewer, flutter-reviewer, fsharp-reviewer, gan-evaluator, gan-generator, gan-planner, go-build-resolver, go-reviewer, harmonyos-app-resolver, harness-optimizer, healthcare-reviewer, homelab-architect, java-build-resolver, java-reviewer, kotlin-build-resolver, kotlin-reviewer, loop-operator, marketing-agent, mle-reviewer, network-architect, network-config-reviewer, network-troubleshooter, opensource-forker, opensource-packager, opensource-sanitizer, performance-optimizer, php-reviewer, planner, pr-test-analyzer, python-reviewer, pytorch-build-resolver, rag-pipeline-reviewer, react-build-resolver, react-reviewer, refactor-cleaner, rust-build-resolver, rust-reviewer, security-reviewer, seo-specialist, silent-failure-hunter, spec-miner, swift-build-resolver, swift-reviewer, tdd-guide, type-design-analyzer, typescript-reviewer, vue-reviewer

### 2-7. ecc 훅·MCP
- 훅 이벤트: PreToolUse 9, PreCompact 1, SessionStart 2, PostToolUse 2, PostToolUseFailure 2, Stop 7, SessionEnd 1
- MCP: chrome-devtools

## 3. 사용자 전역·프로젝트 스킬 파일

| 위치 | 스킬 | 설명 |
| --- | --- | --- |
| `~/.claude/skills/` | impeccable | 웹·앱 화면의 디자인·비평·다듬기 |
| 〃 | unity-mcp-skill | 유니티 에디터를 MCP로 조작하는 방법 |
| 〃 | learned | 학습한 스킬 보관 폴더(현재 비어 있음) |
| 〃 | synced | 계정에서 동기화된 스킬(위 1번과 같은 항목: built-in-browser, chrome-browser, computer-use, deep-research, docs, docx, google-workspace, import-memory, morning, pdf, pptx, skill-creator, xlsx) |
| 프로젝트 `.claude/skills/` | pa-demo-factory | 승인된 오프닝 데모를 실제 유니티 게임뷰로 완성·재개 |
| 〃 | task-observer | 반복되는 작업 마찰·스킬 개선 관찰(명시 요청 시에만) |
| 프로젝트 `.agents/skills/` | pa-debug | 버그 하나에서 처음으로 증명된 끊어진 지점만 진단(파일 수정 없음) |
| 〃 | pa-demo-completion | 데모를 가장 이른 필수 경로부터 하나씩 끝까지 진행 |
| 〃 | pa-implement | 끊어진 지점이 확정된 뒤 가장 작은 올바른 수정 |
| 〃 | pa-visual-director | 화면별로 기능 시제품에서 다듬어진 버티컬 슬라이스까지 연출 |
| 〃 | project-pa-vertical-slice | 기존 시스템과 아트로 완성감 있는 3D 버티컬 슬라이스 제작 |
| 〃 | pa-demo-factory, task-observer | `.claude/skills`와 같은 이름(에이전트용 사본) |

※ 위 스킬 파일은 폴더에는 있으나, 이 데스크탑 세션의 스킬 목록에는 나타나지 않았다. 켜지는 조건은 따로 확인이 필요하다.

## 4. MCP 서버·연결 도구 (이 세션)

| 서버 | 상태 | 용도 |
| --- | --- | --- |
| Claude_Browser (내장 브라우저) | 사용 가능 | 웹 확인·미리보기 |
| claude-in-chrome | 사용 가능(지연 로드) | 실제 크롬 조작 |
| terminal | 사용 가능 | 터미널 탭 실행·읽기 |
| scheduled-tasks | 사용 가능(지연 로드) | 예약 작업 만들기·실행 |
| visualize | 사용 가능 | 인라인 도표·위젯 |
| ccd_* (session, session_mgmt, sidebar, settings, view, window, pr, host, directory, connectors) | 사용 가능 | 데스크탑 앱 기능 제어(세션·사이드바·PR 등) |
| mcp-registry | 사용 가능(지연 로드) | 커넥터 검색·추천 |
| 문서 서버(Docs) · 구글 드라이브 | 사용 가능(지연 로드) | 문서·드라이브 파일 |
| unityMCP | **연결 실패**(ECONNREFUSED) | 유니티 에디터 조작(유니티·MCP 서버 실행 필요) |
| plugin:github | **연결 실패**(400 인증 헤더 형식 오류) | 깃허브 |
| plugin:context7 | **인증 필요** | 라이브러리 문서 검색 |

## 5. 눈에 띈 점

1. **현재 세션에서 쓸 수 있는 것은 사실상 계정 스킬 16개와 `pa-production-console`뿐이다.** ecc·claude-mem·figma 등은 프로젝트 설정이 꺼 둔 상태다(`CLAUDE.md`의 "제작 세션에서는 플러그인 비활성화" 방침). 필요한 것만 일회성으로 켜면 된다.
2. **깃허브 플러그인 인증 오류**: 전역 설정에서 켜져 있으나 인증 헤더 형식 오류로 연결 실패 중이다.
3. **context7**: 인증이 필요하다. 데스크탑 앱의 연결 설정에서 인증해야 한다.
4. **유니티 MCP**: 유니티 에디터와 MCP 서버가 켜져 있어야 연결된다. 이전 세션 기록상 에디터는 배치 모드로만 돌렸다.
5. 이 조사는 목록 확인까지이며, 개별 스킬·플러그인의 실제 동작은 확인하지 않았다.

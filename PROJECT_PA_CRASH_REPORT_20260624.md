# PROJECT_PA Unity Editor Crash Report — 2026-06-24

조사 성격: **원인 조사 및 최소 복구 제안만**. 게임 코드/씬/Library/Assets 변경 없음. 커밋/푸시 없음.

## 1. 크래시 시각

오늘(2026-06-24) **인터랙티브 Unity Editor 세션이 3회 연속 동일 증상으로 크래시**:

| 크래시 폴더 | 시각(대략) | 비고 |
|---|---|---|
| `Crash_2026-06-24_064909748` | 06:49 | 동일 D3D12 device removed |
| `Crash_2026-06-24_093757704` | 09:37 | **지정된 크래시**. 헤더 Date `2026-06-24T09:37:40Z` |
| `Crash_2026-06-24_094010431` | 09:40 | 가장 최근(현재 `Editor.log` 와 동일 세션) |

모든 세션 헤더: `BatchMode: 0, IsHumanControllingUs: 1` → **사람이 GUI로 Editor를 사용하던 중** 발생.
(내가 이전에 돌린 자동 검증기는 `BatchMode: 1` 배치 세션이라 이 크래시들과 다른 별개 실행이다.)

## 2. 읽은 로그 경로

- `C:\Users\sdjsd\AppData\Local\Unity\Editor\Editor.log` (= 09:40 세션, 크래시 포함)
- `C:\Users\sdjsd\AppData\Local\Unity\Editor\Editor-prev.log` (= 09:37 세션, 크래시 포함)
- `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-06-24_093757704\Editor.log` (지정)
- `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-06-24_094010431\Editor.log`
- `C:\Users\sdjsd\AppData\Local\Temp\Unity\Editor\Crashes\Crash_2026-06-24_064909748\Editor.log`
- `crash.dmp` 파일들은 존재하지만(바이너리 미니덤프) 이번 조사는 텍스트 로그로 충분해 디버거 분석은 생략.

## 3. 마지막 관련 로그 (지정 크래시 09:37, 동일하게 다른 2건도 일치)

크래시 직전 시퀀스는 **정상적인 프로젝트 로드/씬 오픈**이었고, 그 직후 그래픽 표시(swapchain present) 단계에서 디바이스가 제거됨:

```
[Project] Loading completed in 11.084 seconds
...
Asset Pipeline Refresh ... NoUpdateAssetOptions
[Indexing] Starting Initial Indexing for Assets
Android Extension - Scanning For ADB Devices 2229 ms
d3d12: swapchain present failed (887a0005).
d3d12: swapchain present failed (887a0005).
d3d12: Device failed error (887a0005).
d3d12: Device removed reason (887a0006).
d3d12: GfxDevice was not out of Local memory      <-- GPU 메모리 부족 아님
d3d12: GfxDevice was not out of Non-Local memory  <-- GPU 메모리 부족 아님
Unrecoverable D3D12 device error! Run with -force-d3d12-debug and see logs for more info.
Crash!!!
...
0x...(KERNELBASE) RaiseException
d3d12 : Creation of resource 'TexturesD3D12::CreateTextureInternal() Texture' (1024 x 1024) format 65 failed (887a0005).
```

GPU 정보(로그):
- Vendor: NVIDIA, Device: **NVIDIA GeForce RTX 4070 Laptop GPU**
- **Driver Version: 32.0.15.8088**
- Direct3D 12 [feature level 12.2], Graphics Memory 7948 MB
- 크래시 시점 Local 메모리 사용량 약 214MB / Budget 약 7.5GB → **VRAM 여유 충분**

에러 코드 의미:
- `0x887a0005` = `DXGI_ERROR_DEVICE_REMOVED`
- `0x887a0006` = `DXGI_ERROR_DEVICE_HUNG` (device removed reason)
- `d3d12: failed to query info queue interface (0x80004002)` 는 디버그 레이어 미설치 안내일 뿐 무해(원인 아님).

## 4. 추정 원인

**분류 B — Unity Editor / GPU / D3D12 / 그래픽 드라이버 문제.**

GPU 디바이스가 표시(present) 도중 제거/행(hung)되어 D3D12가 복구 불가 상태가 됐고 Editor가 종료됨.
관리(C#) 예외 스택, 최근 Project_PA 스크립트, 검증기, 메모리 부족 징후가 **전혀 없음**.

## 5. 확신도

**높음 (High).**

근거:
1. 오늘 3회 크래시가 **모두 동일한 `887a0005`/`887a0006` (DEVICE_REMOVED/HUNG)** 서명.
2. 크래시 지점이 그래픽 표시 계층(`swapchain present failed`)이며, 디바이스 제거 후 텍스처 생성 실패가 뒤따름 — 전형적인 GPU 디바이스 제거.
3. `GfxDevice was not out of Local/Non-Local memory` → 메모리 부족(분류 D) 아님.
4. 크래시 직전 로그가 정상 로드 시퀀스뿐이고 NullReference/스크립트 예외/InitializeOnLoad 오류가 없음 → 분류 A(코드) 아님.
5. AssetDatabase/Library 손상 메시지 없음 → 분류 C 아님.
6. 최근 추가 Editor 스크립트(`PA_CustomerPanelLayoutValidator` 등)는 모두 `[InitializeOnLoad]` 생성자 첫 줄에서
   `if (!SessionState.GetBool(ActiveKey, false)) return;` 로 게이트되어, 검증 세션이 아닐 때(=일반 GUI 오픈)는
   **아무 것도 자동 실행하지 않음** → 인터랙티브 크래시를 유발할 수 없음.

## 6. 최근 Claude 작업과의 관련성

**직접적 인과 없음.** 최근 작업(SPY-002/SPY-003 프레젠테이션, IL-001 채집, CDN-002 영업 게이트, 손님 도착 페이싱, 패널 레이아웃 검증기)은:
- 모두 일반 게임 로직/사이드카 + 배치 전용 검증기이며, 크래시 로그 경로에 등장하지 않는다.
- 검증기들은 SessionState 게이트로 인터랙티브 오픈 시 자동 실행되지 않는다.

주의(인과 아님, 참고): 배치 검증기 중 `PA_CustomerPanelLayoutValidator` / `PA_GatheringShopReview` 는
RenderTexture 생성 + `Camera.Render` + 캔버스 ScreenSpaceOverlay→Camera 전환으로 GPU를 잠깐 쓴다.
이미 불안정한 드라이버라면 GPU 작업이 device-removed를 더 자주 노출시킬 수는 있으나, **근본 원인은 드라이버/디바이스**이고
이번 3건은 배치가 아니라 인터랙티브 세션에서 발생했다.

## 7. 수정 여부

**코드/씬/설정 수정 없음.** 원인이 GPU/D3D12 드라이버이므로 프로젝트 코드 변경이 부적절하고 불필요.
- ProjectSettings 그래픽 API 변경하지 않음(사용자 확인 필요).
- 검증기/스크립트 비활성화하지 않음(자동 실행이 아니므로 불필요).
- Library/Assets 삭제하지 않음.

## 8. 가장 안전한 복구 순서

1. **PC 재부팅** — DEVICE_HUNG 은 드라이버/디바이스가 일시적으로 나쁜 상태일 때 흔하며, 재부팅으로 해소되는 경우가 많다. (가장 먼저, 가장 안전)
2. 노트북이라면 **AC 전원 연결 + 통풍 확인** — 랩탑 GPU 의 전력/발열로 인한 TDR 가능성.
3. Unity Hub 재실행 → 프로젝트 재오픈. 정상 진입되면 종료.
4. 재오픈 시 또 present 단계에서 크래시하면, **D3D11 폴백 1회 테스트**(아래 §재실행/재현)로 D3D12 드라이버 문제인지 확인.
5. D3D11 에서 안정적이면, **NVIDIA 드라이버(현재 32.0.15.8088) 업데이트 또는 직전 안정 버전으로 롤백** 검토(사용자 판단). Studio Driver 권장.
6. (선택) 그래도 불명확하면 `-force-d3d12-debug` 로 device-removed 상세 원인 수집(아래).

## 9. 지금 절대 하면 안 되는 행동

- ProjectSettings 그래픽 API(D3D12→D3D11 등)를 사용자 확인 없이 임의 변경 금지.
- `Library/` 전체 삭제 금지(이번 증상은 Library 손상이 아님; 불필요한 재임포트 시간 낭비 + 위험).
- `Assets/` 삭제, 씬/스크립트 대규모 롤백, `git reset --hard`, `git clean -fd`, `rm -rf` 금지.
- 외부 패키지 import / Git push 금지.
- 최근 Editor 스크립트 삭제 금지(원인 아님).

## 10. 재현/진단 테스트 방법 (안전, ProjectSettings 미변경)

아래는 **임시 실행 인자**일 뿐 프로젝트 설정을 바꾸지 않는다. Unity Hub 를 닫고 Git Bash/PowerShell 에서 1회 실행:

D3D11 강제(폴백)로 재현 여부 확인:
```text
"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe" -projectpath "C:\Users\sdjsd\Desktop\Unity\Project_PA" -force-d3d11
```
- 크래시가 사라지면 → D3D12/드라이버 문제로 강하게 확정. (이때도 ProjectSettings 는 그대로 두고, 드라이버 조치를 우선 검토)

D3D12 device-removed 상세 로그 수집(선택, Graphics Tools 필요할 수 있음):
```text
"C:\Program Files\Unity\Hub\Editor\6000.3.2f1\Editor\Unity.exe" -projectpath "C:\Users\sdjsd\Desktop\Unity\Project_PA" -force-d3d12-debug
```

> 주: 위 명령은 Unity 인스턴스가 떠 있지 않을 때 실행. Hub 를 통하지 않고 직접 실행하면 라이선스 안내가 한 번 뜰 수 있음.

## 11. 수정이 필요한 파일

**없음.** 이번 크래시는 코드/씬/에셋 결함이 아니라 GPU/D3D12 디바이스 제거이므로 수정 대상 파일이 없다.
(최근 Editor 검증기는 SessionState 게이트로 안전 — 자동 실행 차단이 이미 되어 있음.)

## 12. 사람이 직접 확인해야 할 항목

- [ ] PC 재부팅 후 Unity 정상 진입되는지.
- [ ] (재발 시) 위 `-force-d3d11` 테스트에서 크래시가 사라지는지.
- [ ] **NVIDIA 드라이버 버전 32.0.15.8088** 의 최신/직전 버전 존재 여부 및 업데이트·롤백.
- [ ] 랩탑 AC 전원/발열/전원관리(고성능) 상태.
- [ ] Windows 이벤트 뷰어에 nvlddmkm(TDR) 또는 디스플레이 드라이버 응답중지/복구 이벤트가 있는지.
- [ ] Unity 외 다른 GPU 앱(브라우저 GPU 가속, 게임)에서도 화면 깨짐/드라이버 리셋이 있는지(있으면 시스템 차원 드라이버 문제).
- [ ] (확실해지면) ProjectSettings 그래픽 API 조정 여부는 사용자가 직접 결정.

## 부록 — 조사 시점 git status (요약, master 브랜치)

수정/미추적 파일은 직전 스프린트들의 작업물(프로필 데이터, 사이드카 스크립트, 검증기, 문서)이며 이번 크래시와 무관.
이번 조사로 추가된 파일은 본 리포트(`PROJECT_PA_CRASH_REPORT_20260624.md`) 한 개뿐. 코드 변경 없음.

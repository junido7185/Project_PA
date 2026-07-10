# BASELINE_COMPILE — Task 003 컴파일 기준선 기록

작성: 2026-07-10
작업: Task 003 - 컴파일 기준선 기록
범위: `Assembly-CSharp.csproj`

## 사전 상태

- 작업 경로: `C:\Users\sdjsd\Desktop\Unity\Project_PA`
- Git 루트: `C:/Users/sdjsd/Desktop/Unity/Project_PA`
- Unity Editor 상태: `Get-Process Unity`에서 프로세스 감지 안 됨
- 최신 크래시 리포트: `PROJECT_PA_CRASH_REPORT_20260625.md`
- 작업 전 `git status --short`: 아래 삭제 상태는 작업 전부터 존재했고 이번 작업에서 건드리지 않음

```text
 D SubmissionPackages/Project_PA_Source_20260620.zip
 D SubmissionPackages/Project_PA_Windows_20260620.zip
```

## 실행 명령

```powershell
dotnet build Assembly-CSharp.csproj --nologo
```

## 결과 요약

| 항목 | 결과 |
|---|---|
| Exit code | 0 |
| 빌드 결과 | 성공 |
| 경고 | 1 |
| 오류 | 0 |
| 출력 DLL | `Temp/bin/Debug/Assembly-CSharp.dll` |
| 경과 시간 | `00:00:29.27` |

## 경고 내용

```text
CSC : warning CS8785: 생성기 'AttributeBasedFieldGenerator'이(가) 소스를 생성하지 못했습니다.
출력에 기여하지 않으므로 컴파일 오류가 발생할 수 있습니다.
예외의 형식은 'IndexOutOfRangeException'이고 메시지는 'Index was outside the bounds of the array.'입니다.
```

## 원문 출력 핵심

```text
Assembly-CSharp -> C:\Users\sdjsd\Desktop\Unity\Project_PA\Temp\bin\Debug\Assembly-CSharp.dll

빌드했습니다.

경고 1개
오류 0개

경과 시간: 00:00:29.27
```

## 해석

- 현재 기준선은 `0 errors / 1 warning`이다.
- 경고는 C# 소스 생성기 `AttributeBasedFieldGenerator`의 `IndexOutOfRangeException`이며, 이번 Task에서는 수정하지 않는다.
- 빌드 산출물은 `Temp/bin/Debug`에 생성/갱신되었지만 git 추적 변경으로 나타나지 않았다.
- Unity Play Mode, 씬 로드, 검증기 실행은 이번 Task 범위가 아니어서 실행하지 않았다.

## 확인하지 못한 점

- Unity Editor Console의 경고/에러와 동일한지는 확인하지 못했다.
- `PA_FinalDemoRouteValidator`, `PA_LongPlayProgressionValidator`는 실행하지 않았다. 두 검증기는 별도 Task 004 대상이다.


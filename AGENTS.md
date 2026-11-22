# Visual support tool — 인수인계 문서 (AGENTS.md)

> 작성일 2026-11-15. 폴더의 코드·프로젝트 파일·기존 문서와 실제 게임 설치 폴더(Steam `.acf`, `UnityPlayer.dll`, `MelonLoader.dll`)를 읽어 정리했다.
> 이 문서 작성 시점에 인게임 재검증은 하지 않았다. **"실측"** 표시는 디스크에서 직접 읽은 값, **"미확인"**은 다음 세션에서 확인할 항목이다.

## 0. 프로젝트 개요

- **무엇인가:** Visual C++ 런타임 헬퍼(Avalonia, net8.0-windows). 설치된 VC++ 재배포 패키지를 레지스트리로 점검하고 다운로드·설치·제거, 스팀 게임 탐색까지 지원.
- **종류:** 게임 모드가 아닌 앱/도구
- **최근 수정일(파일 기준):** 2026-11-05
- **git 원격:** `H:\local-repos\Visual`
- **최근 커밋:**
  - `69c06bf Add GitHub Sponsors funding configuration`
  - `6c7219d docs: Add troubleshooting guide for DllNotFoundException and VC runtime dependencies`
  - `f00aab6 style: Clean up toolbar by replacing long steam path text box with dedicated open folder buttons`
  - `9905eb6 feat: Add silent uninstall and batch removal capabilities (UninstallerService)`
  - `c81b094 feat: Initial release of Visual C++ Runtime Helper with Avalonia UI`
- **먼저 읽을 기존 문서:**
  - [README.md](README.md)
  - [docs\troubleshooting-guide.md](docs/troubleshooting-guide.md)

## 1. 게임 버전 및 고정 여부

- 게임 모드가 아니므로 해당 없음.

## 2. 런타임 백엔드 및 디컴파일 경로

- 게임 모드가 아님. 런타임/언어: .NET (`net8.0-windows`)
- 모드/앱 프로젝트 `VisualSupportTool.csproj` 타깃 프레임워크: `net8.0-windows` (SDK 스타일)

## 3. 설계 의도와 우회 사유 (Rationale)

코드 주석과 기존 문서에서 작성자가 남긴 의도·주의 문장을 그대로 옮겼다(파일:줄).

- `README.md:27` — 비트 Windows 환경에서 수많은 스팀/인디/모딩 툴이 32비트 바이너리를 요구한다는 점을 반영하여, 누락된 핵심 런타임을 안내하고 원클릭 해결을 유도합니다.
- `Services\SystemAdvisorService.cs:98` — 64비트 OS에서 x86 런타임의 중요성 강조

## 4. 핵심 훅 시그니처와 주입 파이프라인

- **주요 소스 파일(큰 순):**
  - `ViewModels\MainViewModel.cs`
  - `Services\SteamFinderService.cs`
  - `Services\RegistryCheckerService.cs`
  - `Services\SystemAdvisorService.cs`
  - `Services\UninstallerService.cs`
  - `Services\InstallerService.cs`
  - `Models\VcPackageInfo.cs`
  - `Services\DownloaderService.cs`
  - `ViewLocator.cs`
  - `Program.cs`
  - `App.axaml.cs`
  - `Views\MainWindow.axaml.cs`

## 5. 시도했다가 실패한 것들 (Dead Ends)

코드 주석과 기존 문서에서 실패·크래시·금지 관련 문장을 그대로 옮겼다(파일:줄). 같은 방법을 다시 시도하기 전에 해당 위치를 읽을 것.

- `README.md:43` — ❓ **"플러그인 폴더에 DLL 파일이 분명히 존재하는데 왜 `DllNotFoundException`이 발생할까요?"**
- `README.md:44` — Unity(Mono/Il2Cpp) 및 스팀 게임 실행 시 자주 발생하는 **가짜 `DllNotFoundException` (Win32 Error 126)**의 작동 원리와, 왜 Visual C++ 런타임(특히 32비트 x86)이 필요한지에 대한 실전 사례 분석은 [**실전 트러블슈팅 가이드 (docs/troubleshooting-guide.md)**](docs/troubleshooting-g
- `docs\troubleshooting-guide.md:3` — 이 문서는 게임 및 애플리케이션 실행 중 자주 마주치는 **"가짜 `DllNotFoundException`"**과 **런타임 종속성 문제**의 원인을 심층 분석하고, **Visual Support Tool**을 통해 이를 진단하고 해결하는 방법을 설명합니다.
- `docs\troubleshooting-guide.md:7` — 📌 1. 핵심 문제: "DLL 파일이 폴더에 멀쩡히 있는데 왜 찾을 수 없다고 할까?"
- `docs\troubleshooting-guide.md:9` — Unity 게임(Mono/Il2Cpp 기반), 스팀 게임, 혹은 모딩 도구를 실행할 때 다음과 같은 에러 로그를 마주하는 경우가 매우 많습니다.
- `docs\troubleshooting-guide.md:19` — 사용자가 직접 에러 메시지에 적힌 경로(`Plugins/CommunityExpressSW.dll`)를 열어보면, **해당 파일은 이미 그 자리에 정확히 존재**합니다. 그럼에도 불구하고 엔진은 왜 `DllNotFoundException`을 뱉어낼까요?
- `docs\troubleshooting-guide.md:25` — 이 현상의 근본 원인은 **Windows 운영체제의 동적 링크 라이브러리(DLL) 로딩 방식**과 **.NET/Mono 런타임의 에러 처리 구조**에 있습니다.
- `docs\troubleshooting-guide.md:44` — 이때 **다음 두 경우 모두 Windows는 똑같이 `ERROR_MOD_NOT_FOUND` (에러 코드 126)를 반환**합니다:
- `Program.cs:8` — Initialization code. Don't use any Avalonia, third-party APIs or any
- `Program.cs:15` — Avalonia configuration, don't remove; also used by visual designer.
- `Services\SystemAdvisorService.cs:48` — Windows 11은 레지스트리 ProductName에 호환성 문제로 "Windows 10"으로 남아있는 경우가 많으므로 빌드 번호로 보정

## 6. 빌드 명령 및 배포 경로

- `VisualSupportTool.csproj`:
  ```powershell
  dotnet build "VisualSupportTool.csproj" -c Release
  ```

## 7. 다음 작업자에게

- 작업 전 기존 문서(0절)부터 읽는다.
- 인게임 동작을 확인하면 3·5절의 **미확인** 항목을 실제 내용으로 바꾼다.

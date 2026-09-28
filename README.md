# 🛠️ Visual C++ Runtime Helper (Visual Support Tool)

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8.0" />
  <img src="https://img.shields.io/badge/Avalonia-12.1.3-8B5CF6?style=for-the-badge&logo=avalonia&logoColor=white" alt="Avalonia UI" />
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011-0078D6?style=for-the-badge&logo=windows&logoColor=white" alt="Windows" />
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License" />
</p>

<p align="center">
  <b>Steam의 <code>_CommonRedist\vcredist</code> 파일과 시스템 레지스트리를 대조하여 필요한 런타임을 한눈에 확인하고 즉시 다운로드, 설치, 제거까지 지원하는 통합 관리 도구</b>
</p>

---

## 📌 주요 특징

* 🎮 **Steam vcredist 자동 탐색 & 오프라인 캐시 활용**
  * `Steamworks Shared\_CommonRedist\vcredist` 및 `libraryfolders.vdf`를 자동 스캔하여 로컬에 이미 보관된 설치 파일(2005~2022)을 감지합니다.
  * 이미 Steam 폴더에 파일이 있다면 별도의 인터넷 다운로드 없이 **초고속 오프라인 설치**를 진행합니다.

* 🔍 **Windows 시스템 레지스트리 실시간 검사**
  * 현재 내 PC에 어떤 버전(2005, 2008, 2010, 2012, 2013, 2015-2022)의 **x86(32비트)** 및 **x64(64비트)** 런타임이 설치되어 있는지 정확히 진단합니다.

* 💡 **스마트 OS 환경 진단 & 권장 가이드 (Smart Advisor)**
  * Windows 에디션 및 빌드를 정밀 감지하여 사용자 시스템에 맞춘 권장 조치를 실시간으로 제안합니다.
  * 64비트 Windows 환경에서 수많은 스팀/인디/모딩 툴이 32비트 바이너리를 요구한다는 점을 반영하여, 누락된 핵심 런타임을 안내하고 원클릭 해결을 유도합니다.

* 🌐 **마이크로소프트 공식 CDN 비동기 다운로드**
  * 로컬에 파일이 없거나 최신 공식 빌드가 필요한 경우, Microsoft 공식 CDN 서버(`aka.ms`, `download.microsoft.com`)에서 백그라운드로 안전하게 다운로드합니다.

* ⚡ **원클릭 사일런트(무인) 일괄 설치**
  * **[미설치 패키지 원클릭 설치]** 버튼 한 번으로, 누락된 패키지만 골라 사일런트 스위치(`/quiet`, `/q`, `/norestart`)와 함께 UAC 관리자 권한으로 자동 순차 설치합니다.

* 🗑️ **안전한 무인 제거(Uninstall) 및 클린 관리**
  * 런타임이 충돌하거나 손상되었을 때 개별 행의 **`[🗑️ 제거]`** 버튼 또는 상단의 **`[선택 항목 일괄 제거]`** 버튼을 통해 무인 모드로 깨끗하게 제거할 수 있습니다.
  * 번들 언인스톨러 스위치(`/uninstall /quiet /norestart`) 및 MSI 고유 제품 코드(`MsiExec.exe /X{GUID} /qn /norestart`)를 지능적으로 분석하여 안전하게 제거합니다.

---

## 📋 지원 패키지 목록

| 패키지 | 아키텍처 | 설치 옵션 | 제거 옵션 | 제공 출처 |
| :--- | :---: | :---: | :---: | :--- |
| **Visual C++ 2015-2022 (v14x)** | x64 / x86 | `/install /quiet /norestart` | `/uninstall /quiet /norestart` | MS aka.ms / Steam 2022~2015 |
| **Visual C++ 2013 (v120)** | x64 / x86 | `/install /quiet /norestart` | `/uninstall /quiet /norestart` | MS CDN / Steam 2013 |
| **Visual C++ 2012 Update 4 (v110)** | x64 / x86 | `/install /quiet /norestart` | `/uninstall /quiet /norestart` | MS CDN / Steam 2012 |
| **Visual C++ 2010 SP1 (v100)** | x64 / x86 | `/q /norestart` | `MsiExec /X{GUID} /qn` | MS CDN / Steam 2010 |
| **Visual C++ 2008 SP1 (v90)** | x64 / x86 | `/q` | `MsiExec /X{GUID} /qn` | MS CDN / Steam 2008 |
| **Visual C++ 2005 SP1 (v80)** | x64 / x86 | `/q` | `MsiExec /X{GUID} /qn` | MS CDN / Steam 2005 |

---

## 🛠️ 기술 스택 및 아키텍처

* **Language & Framework:** C# (.NET 8.0 LTS Windows)
* **UI Framework:** [Avalonia UI 12.1.3](https://avaloniaui.net/) (Fluent Theme)
* **Architecture Pattern:** MVVM (CommunityToolkit.Mvvm)
* **Services:**
  * `SteamFinderService`: Steam 설치 경로 및 VDF 라이브러리 파싱 & vcredist 파일 검색
  * `RegistryCheckerService`: 32-bit / 64-bit Windows 레지스트리 런타임 키 및 언인스톨 GUID 쿼리
  * `SystemAdvisorService`: Windows OS 버전 및 빌드 감지, 스마트 설치 권장 알고리즘
  * `DownloaderService`: `HttpClient` 기반 비동기 청크 다운로드 및 진행률 리포트
  * `InstallerService`: UAC 관리자 권한 프로세스 제어 및 무인 설치 실행
  * `UninstallerService`: 번들 래퍼 및 MsiExec 기반 무인 백그라운드 제거 엔진

---

## 🚀 시작하기

### 요구 사항
* Windows 10 (1809 이상) 또는 Windows 11
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 이상 (소스 빌드 시)

### 빌드 및 실행
```powershell
# 1. 저장소 복제
git clone https://github.com/magjangin/Visual-support-tool.git
cd Visual-support-tool

# 2. 복원 및 실행
dotnet run
```

---

## 📄 라이선스

이 프로젝트는 [MIT License](LICENSE)에 따라 배포됩니다.

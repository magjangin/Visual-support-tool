using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using VisualSupportTool.Models;

namespace VisualSupportTool.Services;

[SupportedOSPlatform("windows")]
public class SystemAdvisorService
{
    public record SystemInfoResult(
        string OsName,
        string DisplayVersion,
        string BuildNumber,
        bool Is64Bit,
        string FullOsSummary
    );

    public record RecommendationResult(
        string Level, // "Warning", "Success", "Info"
        string Title,
        string MainMessage,
        string ActionHint
    );

    public SystemInfoResult GetSystemInfo()
    {
        string osName = "Windows";
        string displayVersion = "";
        string build = "";
        string ubr = "";
        bool is64Bit = Environment.Is64BitOperatingSystem;

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null)
            {
                var product = key.GetValue("ProductName") as string ?? "Windows";
                displayVersion = key.GetValue("DisplayVersion") as string ?? "";
                build = key.GetValue("CurrentBuild") as string ?? "";
                var ubrVal = key.GetValue("UBR");
                if (ubrVal != null) ubr = ubrVal.ToString() ?? "";

                // Windows 11은 레지스트리 ProductName에 호환성 문제로 "Windows 10"으로 남아있는 경우가 많으므로 빌드 번호로 보정
                if (int.TryParse(build, out int buildNum) && buildNum >= 22000)
                {
                    osName = product.Replace("Windows 10", "Windows 11");
                }
                else
                {
                    osName = product;
                }
            }
        }
        catch
        {
            osName = RuntimeInformation.OSDescription;
        }

        var buildSummary = !string.IsNullOrEmpty(ubr) ? $"{build}.{ubr}" : build;
        var bitStr = is64Bit ? "64-bit (x64)" : "32-bit (x86)";
        var verPart = !string.IsNullOrEmpty(displayVersion) ? $" {displayVersion}" : "";
        var fullSummary = $"{osName}{verPart} ({bitStr}, 빌드 {buildSummary})";

        return new SystemInfoResult(osName, displayVersion, buildSummary, is64Bit, fullSummary);
    }

    public RecommendationResult GenerateRecommendation(
        SystemInfoResult sysInfo,
        IReadOnlyList<VcPackageInfo> packages)
    {
        var missingPackages = packages.Where(p => !p.IsSystemInstalled).ToList();
        var missingCount = missingPackages.Count;
        var totalCount = packages.Count;

        // 1. 모든 패키지가 설치되어 있는 경우
        if (missingCount == 0)
        {
            return new RecommendationResult(
                Level: "Success",
                Title: "🟢 시스템 런타임 환경 완벽: 모든 패키지 설치됨",
                MainMessage: $"{sysInfo.FullOsSummary} 환경에 필수적인 모든 Visual C++ 런타임(2005~2022 x86/x64)이 정상 설치되어 있습니다.",
                ActionHint: "스팀 게임, 고전 게임 및 모딩 도구 구동 시 DLL 누락 오류 없이 안정적으로 실행 가능합니다. 추가 작업이 필요하지 않습니다."
            );
        }

        // 2. 미설치 패키지가 있는 경우
        var missingNames = string.Join(", ", missingPackages.Select(p => $"{p.Year} {p.ArchTag}"));

        string steamHint = packages.All(p => p.IsLocalFileFound)
            ? "Steam _CommonRedist에 모든 설치 파일이 보관되어 있어 다운로드 없이 즉시 오프라인 고속 설치가 가능합니다."
            : "일부 파일은 마이크로소프트 공식 서버에서 자동 다운로드 후 순차 설치됩니다.";

        // 64비트 OS에서 x86 런타임의 중요성 강조
        string bitReason = sysInfo.Is64Bit
            ? "현재 64비트 Windows 환경이지만, 수많은 스팀 게임과 모드 툴, 인디 게임은 32비트(x86)로 제작되어 있어 x86 런타임이 없으면 실행되지 않습니다."
            : "32비트 런타임들을 설치해야 게임 및 프로그램이 정상 구동됩니다.";

        return new RecommendationResult(
            Level: "Warning",
            Title: $"⚠️ 권장 조치: {missingCount}개의 Visual C++ 런타임 누락 감지",
            MainMessage: $"{bitReason}\n누락된 버전: [{missingNames}] - 방치 시 'MSVCR100.dll', 'MSVCP140.dll' 등의 런타임 누락 에러가 발생할 수 있습니다.",
            ActionHint: $"👉 상단의 [⚡ 미설치 패키지 원클릭 설치] 버튼을 누르는 것을 적극 권장합니다. ({steamHint})"
        );
    }
}

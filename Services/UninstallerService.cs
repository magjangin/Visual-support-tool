using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VisualSupportTool.Models;

namespace VisualSupportTool.Services;

public class UninstallerService
{
    public record UninstallResult(bool Success, string Message);

    public async Task<UninstallResult> UninstallAsync(VcPackageInfo package)
    {
        try
        {
            bool executedAny = false;

            // 전략 1: 로컬에 보관된 공식 설치 파일이 있는 경우 (2012, 2013, 2015-2022 번들)
            if (package.IsLocalFileFound && !string.IsNullOrEmpty(package.LocalFilePath) && File.Exists(package.LocalFilePath))
            {
                if (package.Year.Contains("2015") || package.Year.Contains("2022") || package.Year == "2013" || package.Year == "2012")
                {
                    var exitCode = await RunProcessAsync(package.LocalFilePath, "/uninstall /quiet /norestart");
                    if (exitCode == 0 || exitCode == 3010 || exitCode == 1605)
                    {
                        return new UninstallResult(true, "제거 완료 (번들 언인스톨러)");
                    }
                }
            }

            // 전략 2: 레지스트리 UninstallCommands에 등록된 실행 파일 확인 (Package Cache 등)
            foreach (var cmd in package.UninstallCommands)
            {
                if (string.IsNullOrWhiteSpace(cmd)) continue;

                // exe 경로 추출 (따옴표 고려)
                var match = Regex.Match(cmd, @"^""?([^""]+\.exe)""?\s*(.*)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var exePath = match.Groups[1].Value;
                    if (File.Exists(exePath))
                    {
                        var args = "/uninstall /quiet /norestart";
                        await RunProcessAsync(exePath, args);
                        executedAny = true;
                    }
                }
            }

            // 전략 3: MSI GUID 목록에 대해 msiexec /X {GUID} /qn /norestart 실행 (2005, 2008, 2010 등)
            if (package.InstalledGuids.Count > 0)
            {
                foreach (var guid in package.InstalledGuids)
                {
                    await RunProcessAsync("msiexec.exe", $"/X{guid} /qn /norestart");
                    executedAny = true;
                }
            }

            if (!executedAny)
            {
                return new UninstallResult(false, "제거 명령 또는 설치 레지스트리 정보를 찾을 수 없습니다.");
            }

            return new UninstallResult(true, "제거 완료");
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return new UninstallResult(false, "UAC 관리자 권한 승인이 거부되었습니다.");
        }
        catch (Exception ex)
        {
            return new UninstallResult(false, $"제거 오류: {ex.Message}");
        }
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = true,
            Verb = "runas"
        };

        using var process = Process.Start(startInfo);
        if (process == null) return -1;

        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace VisualSupportTool.Services;

public class InstallerService
{
    public record InstallResult(bool Success, int ExitCode, string Message);

    public async Task<InstallResult> InstallAsync(Models.VcPackageInfo package)
    {
        if (string.IsNullOrWhiteSpace(package.LocalFilePath) || !File.Exists(package.LocalFilePath))
        {
            return new InstallResult(false, -1, "설치할 로컬 파일이 없습니다. 먼저 다운로드하거나 파일을 확인해주세요.");
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = package.LocalFilePath,
                Arguments = package.SilentArgs,
                UseShellExecute = true,
                Verb = "runas" // UAC 관리자 권한 요청
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new InstallResult(false, -1, "프로세스를 시작할 수 없습니다.");
            }

            await process.WaitForExitAsync();

            int exitCode = process.ExitCode;

            // 표준 성공 및 허용 가능한 종료 코드
            // 0: 성공
            // 3010: 성공 (재부팅 필요)
            // 1638: 이미 동일하거나 상위 버전 설치됨
            // 5100: 컴퓨터가 운영 체제 요구 사항을 충족하지 않음 또는 이미 설치됨
            if (exitCode == 0)
            {
                return new InstallResult(true, exitCode, "설치 완료 (성공)");
            }
            else if (exitCode == 3010)
            {
                return new InstallResult(true, exitCode, "설치 완료 (시스템 재부팅 필요)");
            }
            else if (exitCode == 1638)
            {
                return new InstallResult(true, exitCode, "이미 최신 버전이 설치되어 있습니다.");
            }
            else if (exitCode == 1602)
            {
                return new InstallResult(false, exitCode, "사용자에 의해 설치가 취소되었습니다.");
            }
            else
            {
                return new InstallResult(false, exitCode, $"설치 종료 코드: {exitCode}");
            }
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // 사용자가 UAC 창에서 '아니오'를 누름
            return new InstallResult(false, 1223, "UAC 관리자 권한 승인이 거부되었습니다.");
        }
        catch (Exception ex)
        {
            return new InstallResult(false, -1, $"오류 발생: {ex.Message}");
        }
    }
}

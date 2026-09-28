using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace VisualSupportTool.Services;

public class DownloaderService
{
    private static readonly HttpClient HttpClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = true
    })
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public string DefaultDownloadDirectory { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Downloads",
        "VCRedist_Packages"
    );

    public async Task<string> DownloadPackageAsync(
        Models.VcPackageInfo package,
        string? targetDirectory = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var destDir = string.IsNullOrWhiteSpace(targetDirectory) ? DefaultDownloadDirectory : targetDirectory;
        Directory.CreateDirectory(destDir);

        var destinationFile = Path.Combine(destDir, $"{package.Year}_{package.Architecture}_{package.DefaultFileName}");

        // 이미 완료된 유효한 파일이 있으면 스킵 또는 재사용
        if (File.Exists(destinationFile) && new FileInfo(destinationFile).Length > 1024 * 100)
        {
            progress?.Report(100.0);
            return destinationFile;
        }

        using var response = await HttpClient.GetAsync(package.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

        var buffer = new byte[81920];
        long totalRead = 0;
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var percentage = (double)totalRead / totalBytes * 100.0;
                progress?.Report(percentage);
            }
        }

        progress?.Report(100.0);
        return destinationFile;
    }
}

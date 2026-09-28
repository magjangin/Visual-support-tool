using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace VisualSupportTool.Services;

public class SteamFinderService
{
    private static readonly string[] KnownDefaultPaths =
    [
        @"H:\steam",
        @"C:\Program Files (x86)\Steam",
        @"C:\Steam",
        @"D:\Steam",
        @"D:\SteamLibrary",
        @"E:\Steam",
        @"E:\SteamLibrary",
        @"F:\Steam",
        @"F:\SteamLibrary",
        @"G:\Steam",
        @"G:\SteamLibrary",
        @"H:\SteamLibrary"
    ];

    public List<string> DetectSteamDirectories()
    {
        var steamDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. 레지스트리 탐색
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                steamDirs.Add(Path.GetFullPath(path));
            }
        }
        catch { }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Valve\Steam");
            var path = key?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                steamDirs.Add(Path.GetFullPath(path));
            }
        }
        catch { }

        // 2. 알려진 기본 경로 탐색
        foreach (var path in KnownDefaultPaths)
        {
            if (Directory.Exists(path))
            {
                steamDirs.Add(Path.GetFullPath(path));
            }
        }

        // 3. 각 Steam 디렉토리의 libraryfolders.vdf 탐색하여 추가 라이브러리 폴더 수집
        var allLibraries = new HashSet<string>(steamDirs, StringComparer.OrdinalIgnoreCase);
        foreach (var steamDir in steamDirs)
        {
            var vdfPath = Path.Combine(steamDir, "steamapps", "libraryfolders.vdf");
            if (File.Exists(vdfPath))
            {
                var parsedPaths = ParseLibraryFoldersVdf(vdfPath);
                foreach (var p in parsedPaths)
                {
                    if (Directory.Exists(p))
                    {
                        allLibraries.Add(Path.GetFullPath(p));
                    }
                }
            }
        }

        return [.. allLibraries];
    }

    public List<string> FindCommonRedistPaths(string? customPath = null)
    {
        var redistPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(customPath))
        {
            if (Directory.Exists(customPath))
            {
                // 사용자가 vcredist 폴더 자체를 주었을 경우
                if (customPath.EndsWith("vcredist", StringComparison.OrdinalIgnoreCase))
                {
                    redistPaths.Add(customPath);
                }
                else
                {
                    var subRedist = Path.Combine(customPath, "steamapps", "common", "Steamworks Shared", "_CommonRedist", "vcredist");
                    if (Directory.Exists(subRedist))
                    {
                        redistPaths.Add(subRedist);
                    }
                    else
                    {
                        redistPaths.Add(customPath);
                    }
                }
            }
        }

        var steamDirs = DetectSteamDirectories();
        foreach (var dir in steamDirs)
        {
            var target = Path.Combine(dir, "steamapps", "common", "Steamworks Shared", "_CommonRedist", "vcredist");
            if (Directory.Exists(target))
            {
                redistPaths.Add(Path.GetFullPath(target));
            }
        }

        return [.. redistPaths];
    }

    public Dictionary<string, string> ScanVcRedistFiles(IEnumerable<string> redistFolders)
    {
        // Key: "2005_x86", "2005_x64", "2008_x86", ..., "2022_x64"
        var foundFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var folder in redistFolders)
        {
            if (!Directory.Exists(folder)) continue;

            try
            {
                var files = Directory.GetFiles(folder, "*.exe", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var parentDirName = Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty);

                    // 연도 감지
                    var yearMatch = Regex.Match(parentDirName, @"20\d{2}");
                    var year = yearMatch.Success ? yearMatch.Value : null;

                    if (string.IsNullOrEmpty(year))
                    {
                        var fileYearMatch = Regex.Match(fileName, @"20\d{2}");
                        if (fileYearMatch.Success) year = fileYearMatch.Value;
                    }

                    if (string.IsNullOrEmpty(year)) continue;

                    var isX64 = fileName.Contains("x64", StringComparison.OrdinalIgnoreCase);
                    var isX86 = fileName.Contains("x86", StringComparison.OrdinalIgnoreCase);

                    if (isX64)
                    {
                        var key = $"{year}_x64";
                        if (!foundFiles.ContainsKey(key)) foundFiles[key] = file;
                    }
                    else if (isX86)
                    {
                        var key = $"{year}_x86";
                        if (!foundFiles.ContainsKey(key)) foundFiles[key] = file;
                    }
                }
            }
            catch { }
        }

        return foundFiles;
    }

    private static List<string> ParseLibraryFoldersVdf(string vdfPath)
    {
        var result = new List<string>();
        try
        {
            var lines = File.ReadAllLines(vdfPath);
            var regex = new Regex(@"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);
            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    var raw = match.Groups[1].Value.Replace(@"\\", @"\");
                    if (Directory.Exists(raw))
                    {
                        result.Add(raw);
                    }
                }
            }
        }
        catch { }
        return result;
    }
}

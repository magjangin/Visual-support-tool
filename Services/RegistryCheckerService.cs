using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace VisualSupportTool.Services;

[SupportedOSPlatform("windows")]
public class RegistryCheckerService
{
    public record InstalledVcEntry(
        string DisplayName,
        string DisplayVersion,
        bool IsX64,
        string KeyName,
        string? UninstallString,
        string? QuietUninstallString
    );

    public List<InstalledVcEntry> GetInstalledVcList()
    {
        var result = new List<InstalledVcEntry>();
        var views = new[] { RegistryView.Registry64, RegistryView.Registry32 };

        foreach (var view in views)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstallKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstallKey == null) continue;

                foreach (var subKeyName in uninstallKey.GetSubKeyNames())
                {
                    try
                    {
                        using var subKey = uninstallKey.OpenSubKey(subKeyName);
                        if (subKey == null) continue;

                        var displayName = subKey.GetValue("DisplayName") as string;
                        if (string.IsNullOrWhiteSpace(displayName)) continue;

                        if (displayName.Contains("Visual C++", StringComparison.OrdinalIgnoreCase))
                        {
                            var version = subKey.GetValue("DisplayVersion") as string ?? string.Empty;
                            var uninstallString = subKey.GetValue("UninstallString") as string;
                            var quietUninstallString = subKey.GetValue("QuietUninstallString") as string;

                            bool isX64 = displayName.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                                         displayName.Contains("64-bit", StringComparison.OrdinalIgnoreCase);

                            if (!isX64 && !displayName.Contains("x86", StringComparison.OrdinalIgnoreCase))
                            {
                                isX64 = (view == RegistryView.Registry64);
                            }

                            result.Add(new InstalledVcEntry(displayName, version, isX64, subKeyName, uninstallString, quietUninstallString));
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        return result;
    }

    public void UpdatePackageInstalledStatus(IEnumerable<Models.VcPackageInfo> packages)
    {
        var installedList = GetInstalledVcList();

        foreach (var pkg in packages)
        {
            var matches = FindMatchingInstalls(pkg, installedList);
            pkg.UninstallCommands.Clear();
            pkg.InstalledGuids.Clear();

            if (matches.Count > 0)
            {
                pkg.IsSystemInstalled = true;

                // 버전 정보 표시 (가장 구체적인 버전)
                var bestMatch = matches.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.DisplayVersion)) ?? matches[0];
                pkg.InstalledVersion = string.IsNullOrWhiteSpace(bestMatch.DisplayVersion)
                    ? "설치됨"
                    : bestMatch.DisplayVersion;

                foreach (var m in matches)
                {
                    if (!string.IsNullOrWhiteSpace(m.QuietUninstallString))
                        pkg.UninstallCommands.Add(m.QuietUninstallString);
                    else if (!string.IsNullOrWhiteSpace(m.UninstallString))
                        pkg.UninstallCommands.Add(m.UninstallString);

                    if (m.KeyName.StartsWith('{') && m.KeyName.EndsWith('}'))
                    {
                        pkg.InstalledGuids.Add(m.KeyName);
                    }
                }
            }
            else
            {
                pkg.IsSystemInstalled = false;
                pkg.InstalledVersion = "미설치";
            }
        }
    }

    private static List<InstalledVcEntry> FindMatchingInstalls(Models.VcPackageInfo pkg, List<InstalledVcEntry> installedList)
    {
        bool targetIsX64 = pkg.Architecture == Models.PackageArch.X64;
        var matched = new List<InstalledVcEntry>();

        foreach (var item in installedList)
        {
            bool itemIsX64 = item.IsX64 || item.DisplayName.Contains("x64", StringComparison.OrdinalIgnoreCase);
            bool itemIsX86 = !item.IsX64 || item.DisplayName.Contains("x86", StringComparison.OrdinalIgnoreCase);

            if (targetIsX64 && !itemIsX64) continue;
            if (!targetIsX64 && !itemIsX86) continue;

            // 2015-2022 (v14x)
            if (pkg.Year == "2015-2022" || pkg.Year == "2022" || pkg.Year == "2019" || pkg.Year == "2017" || pkg.Year == "2015")
            {
                if (item.DisplayName.Contains("2015-2022", StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains("2022", StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains("2019", StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains("2017", StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains("2015", StringComparison.OrdinalIgnoreCase) ||
                    item.DisplayName.Contains("v14", StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(item);
                }
            }
            else
            {
                if (item.DisplayName.Contains(pkg.Year, StringComparison.OrdinalIgnoreCase))
                {
                    matched.Add(item);
                }
            }
        }

        return matched;
    }
}

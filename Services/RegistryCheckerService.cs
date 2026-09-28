using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace VisualSupportTool.Services;

[SupportedOSPlatform("windows")]
public class RegistryCheckerService
{
    public record InstalledVcEntry(string DisplayName, string DisplayVersion, bool IsX64);

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
                            bool isX64 = displayName.Contains("x64", StringComparison.OrdinalIgnoreCase) ||
                                         displayName.Contains("64-bit", StringComparison.OrdinalIgnoreCase);

                            // x86 표시가 명시적으로 있거나 64비트가 아니면 x86으로 판별
                            if (!isX64 && !displayName.Contains("x86", StringComparison.OrdinalIgnoreCase))
                            {
                                isX64 = (view == RegistryView.Registry64);
                            }

                            result.Add(new InstalledVcEntry(displayName, version, isX64));
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
            var match = FindMatchingInstall(pkg, installedList);
            if (match != null)
            {
                pkg.IsSystemInstalled = true;
                pkg.InstalledVersion = string.IsNullOrWhiteSpace(match.DisplayVersion)
                    ? "설치됨"
                    : match.DisplayVersion;
            }
            else
            {
                pkg.IsSystemInstalled = false;
                pkg.InstalledVersion = "미설치";
            }
        }
    }

    private static InstalledVcEntry? FindMatchingInstall(Models.VcPackageInfo pkg, List<InstalledVcEntry> installedList)
    {
        bool targetIsX64 = pkg.Architecture == Models.PackageArch.X64;

        foreach (var item in installedList)
        {
            // 아키텍처 일치 검사
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
                    return item;
                }
            }
            else
            {
                // 2013, 2012, 2010, 2008, 2005
                if (item.DisplayName.Contains(pkg.Year, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
        }

        return null;
    }
}

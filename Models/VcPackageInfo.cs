using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VisualSupportTool.Models;

public enum PackageArch
{
    X86,
    X64
}

public partial class VcPackageInfo : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public PackageArch Architecture { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
    public string DefaultFileName { get; set; } = string.Empty;
    public string SilentArgs { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileSourceDisplay))]
    [NotifyPropertyChangedFor(nameof(FileNameOnly))]
    [NotifyPropertyChangedFor(nameof(IsFromSteam))]
    private string? _localFilePath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FileSourceDisplay))]
    private bool _isLocalFileFound;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMissingInstall))]
    private bool _isSystemInstalled;

    [ObservableProperty]
    private string _installedVersion = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _statusMessage = "확인 대기 중";

    public bool IsX64 => Architecture == PackageArch.X64;
    public string ArchTag => IsX64 ? "x64" : "x86";
    public string ArchDisplay => IsX64 ? "64-bit (x64)" : "32-bit (x86)";

    public bool IsMissingInstall => !IsSystemInstalled;
    public bool CanUninstall => IsSystemInstalled;

    public List<string> UninstallCommands { get; set; } = [];
    public List<string> InstalledGuids { get; set; } = [];

    public bool IsFromSteam => !string.IsNullOrEmpty(LocalFilePath) &&
        LocalFilePath.Contains("Steamworks Shared", StringComparison.OrdinalIgnoreCase);

    public string FileNameOnly => string.IsNullOrEmpty(LocalFilePath)
        ? DefaultFileName
        : Path.GetFileName(LocalFilePath);

    public string FileSourceDisplay
    {
        get
        {
            if (!IsLocalFileFound || string.IsNullOrEmpty(LocalFilePath))
                return "파일 없음 (다운로드 필요)";
            return IsFromSteam
                ? $"Steam 캐시 보유 ({FileNameOnly})"
                : $"다운로드 파일 보유 ({FileNameOnly})";
        }
    }
}

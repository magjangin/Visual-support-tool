using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VisualSupportTool.Models;
using VisualSupportTool.Services;

namespace VisualSupportTool.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly SteamFinderService _steamFinder = new();
    private readonly RegistryCheckerService _registryChecker = new();
    private readonly DownloaderService _downloader = new();
    private readonly InstallerService _installer = new();
    private readonly UninstallerService _uninstaller = new();
    private readonly SystemAdvisorService _systemAdvisor = new();

    [ObservableProperty]
    private string _steamPath = @"H:\steam\steamapps\common\Steamworks Shared\_CommonRedist\vcredist";

    [ObservableProperty]
    private string _downloadPath = string.Empty;

    [ObservableProperty]
    private string _statusText = "준비 완료";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _detectedSteamLocations = string.Empty;

    [ObservableProperty]
    private int _installedCount;

    [ObservableProperty]
    private int _missingCount;

    [ObservableProperty]
    private int _readyFilesCount;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasMissingInstalls;

    [ObservableProperty]
    private string _osDescription = string.Empty;

    [ObservableProperty]
    private string _advisorTitle = string.Empty;

    [ObservableProperty]
    private string _advisorMessage = string.Empty;

    [ObservableProperty]
    private string _advisorActionHint = string.Empty;

    [ObservableProperty]
    private string _advisorLevel = "Info";

    [ObservableProperty]
    private bool _isAdvisorWarning;

    [ObservableProperty]
    private bool _isAdvisorSuccess;

    public ObservableCollection<VcPackageInfo> Packages { get; } = [];

    public MainViewModel()
    {
        DownloadPath = _downloader.DefaultDownloadDirectory;
        InitializePackages();
        InitialScan();
    }

    private void InitializePackages()
    {
        Packages.Clear();

        Packages.Add(new VcPackageInfo
        {
            Key = "2015-2022_x64",
            Year = "2015-2022",
            DisplayName = "Visual C++ 2015-2022 (v14x) 64-bit",
            ShortTitle = "Visual C++ 2015-2022",
            Architecture = PackageArch.X64,
            DefaultFileName = "vc_redist.x64.exe",
            DownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x64.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2015-2022_x86",
            Year = "2015-2022",
            DisplayName = "Visual C++ 2015-2022 (v14x) 32-bit",
            ShortTitle = "Visual C++ 2015-2022",
            Architecture = PackageArch.X86,
            DefaultFileName = "vc_redist.x86.exe",
            DownloadUrl = "https://aka.ms/vs/17/release/vc_redist.x86.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2013_x64",
            Year = "2013",
            DisplayName = "Visual C++ 2013 (v120) 64-bit",
            ShortTitle = "Visual C++ 2013",
            Architecture = PackageArch.X64,
            DefaultFileName = "vcredist_x64.exe",
            DownloadUrl = "https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x64.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2013_x86",
            Year = "2013",
            DisplayName = "Visual C++ 2013 (v120) 32-bit",
            ShortTitle = "Visual C++ 2013",
            Architecture = PackageArch.X86,
            DefaultFileName = "vcredist_x86.exe",
            DownloadUrl = "https://download.microsoft.com/download/2/E/6/2E61CFA4-993B-4DD4-91DA-3737CD5CD6E3/vcredist_x86.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2012_x64",
            Year = "2012",
            DisplayName = "Visual C++ 2012 Update 4 (v110) 64-bit",
            ShortTitle = "Visual C++ 2012",
            Architecture = PackageArch.X64,
            DefaultFileName = "vcredist_x64.exe",
            DownloadUrl = "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x64.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2012_x86",
            Year = "2012",
            DisplayName = "Visual C++ 2012 Update 4 (v110) 32-bit",
            ShortTitle = "Visual C++ 2012",
            Architecture = PackageArch.X86,
            DefaultFileName = "vcredist_x86.exe",
            DownloadUrl = "https://download.microsoft.com/download/1/6/B/16B06F60-3B20-4FF2-B699-5E9B7962F9AE/VSU_4/vcredist_x86.exe",
            SilentArgs = "/install /quiet /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2010_x64",
            Year = "2010",
            DisplayName = "Visual C++ 2010 SP1 (v100) 64-bit",
            ShortTitle = "Visual C++ 2010",
            Architecture = PackageArch.X64,
            DefaultFileName = "vcredist_x64.exe",
            DownloadUrl = "https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x64.exe",
            SilentArgs = "/q /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2010_x86",
            Year = "2010",
            DisplayName = "Visual C++ 2010 SP1 (v100) 32-bit",
            ShortTitle = "Visual C++ 2010",
            Architecture = PackageArch.X86,
            DefaultFileName = "vcredist_x86.exe",
            DownloadUrl = "https://download.microsoft.com/download/1/6/5/165255E7-1014-4D0A-B094-B6A430A6BFFC/vcredist_x86.exe",
            SilentArgs = "/q /norestart"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2008_x64",
            Year = "2008",
            DisplayName = "Visual C++ 2008 SP1 (v90) 64-bit",
            ShortTitle = "Visual C++ 2008",
            Architecture = PackageArch.X64,
            DefaultFileName = "vcredist_x64.exe",
            DownloadUrl = "https://download.microsoft.com/download/d/d/9/dd9a82d0-52ef-42db-a182-798699683b16/vcredist_x64.exe",
            SilentArgs = "/q"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2008_x86",
            Year = "2008",
            DisplayName = "Visual C++ 2008 SP1 (v90) 32-bit",
            ShortTitle = "Visual C++ 2008",
            Architecture = PackageArch.X86,
            DefaultFileName = "vcredist_x86.exe",
            DownloadUrl = "https://download.microsoft.com/download/d/d/9/dd9a82d0-52ef-42db-a182-798699683b16/vcredist_x86.exe",
            SilentArgs = "/q"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2005_x64",
            Year = "2005",
            DisplayName = "Visual C++ 2005 SP1 (v80) 64-bit",
            ShortTitle = "Visual C++ 2005",
            Architecture = PackageArch.X64,
            DefaultFileName = "vcredist_x64.exe",
            DownloadUrl = "https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x64.EXE",
            SilentArgs = "/q"
        });

        Packages.Add(new VcPackageInfo
        {
            Key = "2005_x86",
            Year = "2005",
            DisplayName = "Visual C++ 2005 SP1 (v80) 32-bit",
            ShortTitle = "Visual C++ 2005",
            Architecture = PackageArch.X86,
            DefaultFileName = "vcredist_x86.exe",
            DownloadUrl = "https://download.microsoft.com/download/8/B/4/8B42259F-5D70-43F4-AC2E-4B208FD8D66A/vcredist_x86.EXE",
            SilentArgs = "/q"
        });

        TotalCount = Packages.Count;
    }

    private void UpdateCounts()
    {
        TotalCount = Packages.Count;
        InstalledCount = Packages.Count(p => p.IsSystemInstalled);
        MissingCount = Packages.Count(p => !p.IsSystemInstalled);
        ReadyFilesCount = Packages.Count(p => p.IsLocalFileFound);
        HasMissingInstalls = MissingCount > 0;
        UpdateAdvisor();
    }

    private void UpdateAdvisor()
    {
        try
        {
            var sysInfo = _systemAdvisor.GetSystemInfo();
            OsDescription = sysInfo.FullOsSummary;

            var rec = _systemAdvisor.GenerateRecommendation(sysInfo, Packages.ToList());
            AdvisorTitle = rec.Title;
            AdvisorMessage = rec.MainMessage;
            AdvisorActionHint = rec.ActionHint;
            AdvisorLevel = rec.Level;
            IsAdvisorWarning = rec.Level == "Warning";
            IsAdvisorSuccess = rec.Level == "Success";
        }
        catch { }
    }

    [RelayCommand]
    public void ScanAll()
    {
        IsBusy = true;
        StatusText = "Steam 및 로컬 파일 탐색 및 레지스트리 상태 확인 중...";

        try
        {
            // 1. 레지스트리 설치 상태 확인
            _registryChecker.UpdatePackageInstalledStatus(Packages);

            // 2. Steam Redist 폴더 스캔
            var redistDirs = _steamFinder.FindCommonRedistPaths(SteamPath);
            DetectedSteamLocations = redistDirs.Count > 0 ? string.Join(", ", redistDirs) : "감지된 Steam vcredist 경로 없음";

            var foundFiles = _steamFinder.ScanVcRedistFiles(redistDirs);

            // 3. 로컬 다운로드 폴더도 스캔
            if (Directory.Exists(DownloadPath))
            {
                var downloadedMap = _steamFinder.ScanVcRedistFiles([DownloadPath]);
                foreach (var kvp in downloadedMap)
                {
                    if (!foundFiles.ContainsKey(kvp.Key))
                    {
                        foundFiles[kvp.Key] = kvp.Value;
                    }
                }
            }

            // 4. 각 패키지에 매핑
            foreach (var pkg in Packages)
            {
                string? matchedFile = null;

                if (pkg.Year == "2015-2022")
                {
                    var arch = pkg.Architecture == PackageArch.X64 ? "x64" : "x86";
                    string[] candidates = [$"2022_{arch}", $"2019_{arch}", $"2017_{arch}", $"2015_{arch}"];
                    foreach (var c in candidates)
                    {
                        if (foundFiles.TryGetValue(c, out var f))
                        {
                            matchedFile = f;
                            break;
                        }
                    }
                }
                else
                {
                    var arch = pkg.Architecture == PackageArch.X64 ? "x64" : "x86";
                    var key = $"{pkg.Year}_{arch}";
                    foundFiles.TryGetValue(key, out matchedFile);
                }

                if (!string.IsNullOrEmpty(matchedFile) && File.Exists(matchedFile))
                {
                    pkg.LocalFilePath = matchedFile;
                    pkg.IsLocalFileFound = true;
                    pkg.DownloadProgress = 100.0;
                    pkg.StatusMessage = "파일 준비됨";
                }
                else
                {
                    pkg.LocalFilePath = null;
                    pkg.IsLocalFileFound = false;
                    pkg.DownloadProgress = 0.0;
                    pkg.StatusMessage = "다운로드 필요";
                }
            }

            UpdateCounts();
            StatusText = $"스캔 완료: 파일 {ReadyFilesCount}/{TotalCount}개 준비됨, 시스템 설치 {InstalledCount}/{TotalCount}개 확인됨.";
        }
        catch (Exception ex)
        {
            StatusText = $"스캔 중 오류 발생: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void SelectAll()
    {
        foreach (var p in Packages) p.IsSelected = true;
    }

    [RelayCommand]
    public void DeselectAll()
    {
        foreach (var p in Packages) p.IsSelected = false;
    }

    [RelayCommand]
    public void SelectMissingOnly()
    {
        foreach (var p in Packages) p.IsSelected = !p.IsSystemInstalled;
    }

    [RelayCommand]
    public async Task DownloadSingleAsync(VcPackageInfo package)
    {
        if (package == null || IsBusy) return;

        IsBusy = true;
        package.IsProcessing = true;
        package.StatusMessage = "다운로드 중...";
        StatusText = $"{package.ShortTitle} {package.ArchTag} 다운로드 중...";

        try
        {
            var progress = new Progress<double>(p => package.DownloadProgress = p);
            var savedPath = await _downloader.DownloadPackageAsync(package, DownloadPath, progress, CancellationToken.None);
            package.LocalFilePath = savedPath;
            package.IsLocalFileFound = true;
            package.StatusMessage = "다운로드 완료";
            StatusText = $"{package.ShortTitle} {package.ArchTag} 다운로드 완료";
            UpdateCounts();
        }
        catch (Exception ex)
        {
            package.StatusMessage = $"다운로드 실패: {ex.Message}";
            StatusText = $"다운로드 오류: {ex.Message}";
        }
        finally
        {
            package.IsProcessing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task DownloadAllMissingFilesAsync()
    {
        if (IsBusy) return;

        var targets = Packages.Where(p => !p.IsLocalFileFound).ToList();
        if (targets.Count == 0)
        {
            StatusText = "모든 패키지 파일이 이미 로컬에 준비되어 있습니다.";
            return;
        }

        IsBusy = true;
        int completed = 0;

        foreach (var pkg in targets)
        {
            pkg.IsProcessing = true;
            pkg.StatusMessage = "대기 중...";
        }

        foreach (var pkg in targets)
        {
            pkg.StatusMessage = "다운로드 중...";
            StatusText = $"[{completed + 1}/{targets.Count}] {pkg.ShortTitle} {pkg.ArchTag} 다운로드 중...";

            try
            {
                var progress = new Progress<double>(p => pkg.DownloadProgress = p);
                var saved = await _downloader.DownloadPackageAsync(pkg, DownloadPath, progress);
                pkg.LocalFilePath = saved;
                pkg.IsLocalFileFound = true;
                pkg.StatusMessage = "다운로드 완료";
            }
            catch (Exception ex)
            {
                pkg.StatusMessage = $"다운로드 실패: {ex.Message}";
            }
            finally
            {
                pkg.IsProcessing = false;
                completed++;
                OverallProgress = (double)completed / targets.Count * 100.0;
            }
        }

        UpdateCounts();
        IsBusy = false;
        StatusText = $"총 {targets.Count}개 중 {completed}개 다운로드 완료되었습니다.";
    }

    [RelayCommand]
    public async Task InstallSingleAsync(VcPackageInfo package)
    {
        if (package == null || IsBusy) return;

        if (!package.IsLocalFileFound)
        {
            await DownloadSingleAsync(package);
            if (!package.IsLocalFileFound) return;
        }

        IsBusy = true;
        package.IsProcessing = true;
        package.StatusMessage = "설치 실행 중...";
        StatusText = $"{package.ShortTitle} {package.ArchTag} 설치 중...";

        try
        {
            var result = await _installer.InstallAsync(package);
            package.StatusMessage = result.Message;
            StatusText = $"{package.ShortTitle} {package.ArchTag}: {result.Message}";
            _registryChecker.UpdatePackageInstalledStatus([package]);
            UpdateCounts();
        }
        catch (Exception ex)
        {
            package.StatusMessage = $"설치 오류: {ex.Message}";
            StatusText = $"설치 실패: {ex.Message}";
        }
        finally
        {
            package.IsProcessing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task InstallMissingAsync()
    {
        if (IsBusy) return;

        var targets = Packages.Where(p => !p.IsSystemInstalled).ToList();
        if (targets.Count == 0)
        {
            StatusText = "현재 시스템에 모든 Visual C++ 런타임이 이미 설치되어 있습니다!";
            return;
        }

        IsBusy = true;
        int index = 0;

        foreach (var pkg in targets)
        {
            index++;
            StatusText = $"[{index}/{targets.Count}] {pkg.ShortTitle} {pkg.ArchTag} 준비 및 설치 중...";
            pkg.IsProcessing = true;

            if (!pkg.IsLocalFileFound)
            {
                pkg.StatusMessage = "자동 다운로드 중...";
                try
                {
                    var saved = await _downloader.DownloadPackageAsync(pkg, DownloadPath);
                    pkg.LocalFilePath = saved;
                    pkg.IsLocalFileFound = true;
                }
                catch (Exception ex)
                {
                    pkg.StatusMessage = $"다운로드 실패: {ex.Message}";
                    pkg.IsProcessing = false;
                    continue;
                }
            }

            pkg.StatusMessage = "설치 중...";
            var result = await _installer.InstallAsync(pkg);
            pkg.StatusMessage = result.Message;
            pkg.IsProcessing = false;
            OverallProgress = (double)index / targets.Count * 100.0;
        }

        _registryChecker.UpdatePackageInstalledStatus(Packages);
        UpdateCounts();
        IsBusy = false;
        StatusText = "미설치 패키지 일괄 설치 작업이 완료되었습니다.";
    }

    [RelayCommand]
    public async Task UninstallSingleAsync(VcPackageInfo package)
    {
        if (package == null || IsBusy) return;

        IsBusy = true;
        package.IsProcessing = true;
        package.StatusMessage = "제거 실행 중 (UAC 권한 요청)...";
        StatusText = $"{package.ShortTitle} {package.ArchTag} 제거 중...";

        try
        {
            var result = await _uninstaller.UninstallAsync(package);
            package.StatusMessage = result.Message;
            StatusText = $"{package.ShortTitle} {package.ArchTag}: {result.Message}";
            _registryChecker.UpdatePackageInstalledStatus(Packages);
            UpdateCounts();
        }
        catch (Exception ex)
        {
            package.StatusMessage = $"제거 실패: {ex.Message}";
            StatusText = $"제거 실패: {ex.Message}";
        }
        finally
        {
            package.IsProcessing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CleanReinstallSingleAsync(VcPackageInfo package)
    {
        if (package == null || IsBusy) return;

        IsBusy = true;
        package.IsProcessing = true;
        StatusText = $"{package.ShortTitle} {package.ArchTag} 클린 재설치 중 (제거 후 재설치)...";

        try
        {
            // 1단계: 제거
            package.StatusMessage = "기존 버전 제거 중...";
            await _uninstaller.UninstallAsync(package);
            _registryChecker.UpdatePackageInstalledStatus(Packages);

            // 2단계: 설치 (로컬 파일 없으면 다운로드)
            if (!package.IsLocalFileFound)
            {
                package.StatusMessage = "설치 파일 다운로드 중...";
                var saved = await _downloader.DownloadPackageAsync(package, DownloadPath);
                package.LocalFilePath = saved;
                package.IsLocalFileFound = true;
            }

            package.StatusMessage = "재설치 실행 중...";
            var installResult = await _installer.InstallAsync(package);
            package.StatusMessage = $"클린 재설치: {installResult.Message}";
            StatusText = $"{package.ShortTitle} {package.ArchTag}: {installResult.Message}";

            _registryChecker.UpdatePackageInstalledStatus(Packages);
            UpdateCounts();
        }
        catch (Exception ex)
        {
            package.StatusMessage = $"클린 재설치 오류: {ex.Message}";
            StatusText = $"클린 재설치 실패: {ex.Message}";
        }
        finally
        {
            package.IsProcessing = false;
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task UninstallSelectedAsync()
    {
        if (IsBusy) return;

        var targets = Packages.Where(p => p.IsSelected && p.IsSystemInstalled).ToList();
        if (targets.Count == 0)
        {
            StatusText = "제거할 선택된 설치 패키지가 없습니다.";
            return;
        }

        IsBusy = true;
        int completed = 0;

        foreach (var pkg in targets)
        {
            completed++;
            StatusText = $"[{completed}/{targets.Count}] {pkg.ShortTitle} {pkg.ArchTag} 제거 중...";
            pkg.IsProcessing = true;
            pkg.StatusMessage = "제거 중...";

            try
            {
                var result = await _uninstaller.UninstallAsync(pkg);
                pkg.StatusMessage = result.Message;
            }
            catch (Exception ex)
            {
                pkg.StatusMessage = $"제거 실패: {ex.Message}";
            }
            finally
            {
                pkg.IsProcessing = false;
                OverallProgress = (double)completed / targets.Count * 100.0;
            }
        }

        _registryChecker.UpdatePackageInstalledStatus(Packages);
        UpdateCounts();
        IsBusy = false;
        StatusText = $"선택한 {targets.Count}개 패키지 제거 작업이 완료되었습니다.";
    }

    [RelayCommand]
    public void OpenFileLocation(VcPackageInfo? package)
    {
        if (package == null || string.IsNullOrEmpty(package.LocalFilePath) || !File.Exists(package.LocalFilePath))
        {
            if (Directory.Exists(DownloadPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DownloadPath,
                    UseShellExecute = true
                });
            }
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{package.LocalFilePath}\"",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    public void OpenSteamFolder()
    {
        if (Directory.Exists(SteamPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = SteamPath,
                UseShellExecute = true
            });
        }
    }

    [RelayCommand]
    public void OpenDownloadFolder()
    {
        if (Directory.Exists(DownloadPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = DownloadPath,
                UseShellExecute = true
            });
        }
    }

    private void InitialScan()
    {
        ScanAll();
    }
}

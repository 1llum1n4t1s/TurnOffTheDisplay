using System.Diagnostics;
using System.Runtime.Versioning;

namespace TurnOffTheDisplay;

/// <summary>
/// スタートメニューに「ディスプレイ＠OFF」フォルダを作成し、
/// アプリと README.txt へのショートカットを配置する
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StartMenuShortcut
{
    private const string FolderName = "ディスプレイ＠OFF";
    private const string AppShortcutName = "ディスプレイ＠OFF.lnk";
    private const string ReadmeShortcutName = "README.txt.lnk";

    /// <summary>
    /// スタートメニューにフォルダとショートカットを作成する
    /// </summary>
    public static void Create()
    {
        try
        {
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            // 旧バージョンのフラットなショートカットを削除
            RemoveLegacyShortcut(programsFolder);

            var appFolder = Path.Combine(programsFolder, FolderName);
            Directory.CreateDirectory(appFolder);

            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            var appDir = Path.GetDirectoryName(exePath)!;

            // アプリのショートカット
            CreateShortcut(
                Path.Combine(appFolder, AppShortcutName),
                exePath,
                appDir);

            // README.txt のショートカット
            var readmePath = Path.Combine(appDir, "README.txt");
            if (File.Exists(readmePath))
            {
                CreateShortcut(
                    Path.Combine(appFolder, ReadmeShortcutName),
                    readmePath,
                    appDir);
            }
        }
        catch
        {
            // ショートカット作成の失敗はアプリの動作に影響しないため無視
        }
    }

    /// <summary>
    /// スタートメニューからフォルダごと削除する
    /// </summary>
    public static void Remove()
    {
        try
        {
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            RemoveLegacyShortcut(programsFolder);
            var appFolder = Path.Combine(programsFolder, FolderName);
            if (Directory.Exists(appFolder))
            {
                Directory.Delete(appFolder, recursive: true);
            }
        }
        catch
        {
            // 削除の失敗はアプリの動作に影響しないため無視
        }
    }

    /// <summary>
    /// 旧バージョンが作成したフラットなショートカット（Programs 直下）を削除する
    /// </summary>
    private static void RemoveLegacyShortcut(string programsFolder)
    {
        try
        {
            var legacyPath = Path.Combine(programsFolder, AppShortcutName);
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        catch
        {
            // 旧ショートカットの削除失敗は無視
        }
    }

    /// <summary>
    /// PowerShell の WScript.Shell COM を使ってショートカットを作成する
    /// </summary>
    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
    {
        var script = string.Join("; ",
            "$ws = New-Object -ComObject WScript.Shell",
            $"$sc = $ws.CreateShortcut('{Escape(shortcutPath)}')",
            $"$sc.TargetPath = '{Escape(targetPath)}'",
            $"$sc.WorkingDirectory = '{Escape(workingDir)}'",
            "$sc.Save()");

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -Command \"{script}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
        });
        process?.WaitForExit(10000);
    }

    private static string Escape(string s) => s.Replace("'", "''");
}

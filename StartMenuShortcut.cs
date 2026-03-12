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
    /// フォルダ構成が未完成の場合のみショートカットを作成する（起動時用）
    /// </summary>
    public static void EnsureCreated()
    {
        var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        var appFolder = Path.Combine(programsFolder, FolderName);
        var appShortcut = Path.Combine(appFolder, AppShortcutName);

        // フォルダとアプリショートカットが揃っていれば何もしない
        if (File.Exists(appShortcut))
        {
            return;
        }

        Create();
    }

    /// <summary>
    /// スタートメニューにフォルダとショートカットを作成する
    /// </summary>
    public static void Create()
    {
        try
        {
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);

            // 旧バージョンのフラットなショートカットを削除
            RemoveLegacyShortcuts(programsFolder);

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
            RemoveLegacyShortcuts(programsFolder);
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
    /// 旧バージョン/Velopackが作成したフラットなショートカット（Programs 直下）を削除する。
    /// packTitle ベース（ディスプレイ＠OFF.lnk）と packId ベース（TurnOffTheDisplay.lnk）の
    /// 両方を対象にする。
    /// </summary>
    private static void RemoveLegacyShortcuts(string programsFolder)
    {
        string[] legacyNames = [AppShortcutName, "TurnOffTheDisplay.lnk"];
        foreach (var name in legacyNames)
        {
            try
            {
                var legacyPath = Path.Combine(programsFolder, name);
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
    }

    /// <summary>
    /// PowerShell の WScript.Shell COM を使ってショートカットを作成する。
    /// 日本語パス対応のため -EncodedCommand（Base64）で渡す。
    /// </summary>
    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
    {
        var script = string.Join("; ",
            "$ws = New-Object -ComObject WScript.Shell",
            $"$sc = $ws.CreateShortcut('{Escape(shortcutPath)}')",
            $"$sc.TargetPath = '{Escape(targetPath)}'",
            $"$sc.WorkingDirectory = '{Escape(workingDir)}'",
            "$sc.Save()");

        var encoded = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encoded}",
            CreateNoWindow = true,
            UseShellExecute = false,
        });
        process?.WaitForExit(10000);
    }

    private static string Escape(string s) => s.Replace("'", "''");
}

using System.Runtime.Versioning;

namespace TurnOffTheDisplay;

/// <summary>
/// Velopack の旧 StartMenu 配置から StartMenuRoot 配置への移行を行う。
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StartMenuShortcutMigration
{
    private const string LegacyAuthorFolderName = "ゆろち";
    private const string ShortcutFileName = "ディスプレイ＠OFF.lnk";

    /// <summary>
    /// 旧発行者フォルダ内のアプリショートカットを Programs 直下へ移す。
    /// </summary>
    public static void MoveLegacyShortcutToRoot()
    {
        try
        {
            var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
            if (string.IsNullOrWhiteSpace(programsFolder) || !Directory.Exists(programsFolder))
            {
                return;
            }

            var legacyShortcut = Path.Combine(programsFolder, LegacyAuthorFolderName, ShortcutFileName);
            if (!File.Exists(legacyShortcut))
            {
                return;
            }

            var rootShortcut = Path.Combine(programsFolder, ShortcutFileName);
            if (File.Exists(rootShortcut))
            {
                // 新しい Velopack 配置が既に作成済みなら、旧リンクだけを取り除く。
                File.Delete(legacyShortcut);
            }
            else
            {
                File.Move(legacyShortcut, rootShortcut);
            }

            // 「ゆろち」フォルダは他アプリも共有するため、フォルダ自体には触れない。
        }
        catch
        {
            // ショートカット移行の失敗はアプリの動作に影響しないため無視
        }
    }
}

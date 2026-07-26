using Avalonia;
using System.Runtime.InteropServices;
using Velopack;

namespace TurnOffTheDisplay;

/// <summary>
/// アプリケーションのエントリーポイント
/// </summary>
internal class Program
{
    internal const string UpdateCheckArg = "--update-check";
    private const string AppUserModelId = "velopack.TurnOffTheDisplay";

    /// <summary>
    /// 自動更新の配信元ベース URL (Cloudflare R2 totd-updates / カスタムドメイン totd.kagayoi.com)
    /// </summary>
    internal const string UpdateBaseUrl = "https://totd.kagayoi.com";

    /// <summary>
    /// 更新チェック (マニフェスト取得) のタイムアウト。無応答ネットワークでのゴーストプロセス常駐を素早く防ぐ
    /// </summary>
    private static readonly TimeSpan UpdateCheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// 更新ダウンロードのタイムアウト。低速回線/大きな更新でも完了できるよう長めに取りつつ、停止時の常駐は防ぐ。
    /// チェックとは別枠にすることで、正常だが時間のかかるダウンロードがチェック用の短い budget で打ち切られないようにする
    /// </summary>
    private static readonly TimeSpan UpdateDownloadTimeout = TimeSpan.FromMinutes(10);

    /// <summary>
    /// アプリケーションのエントリーポイント。Velopack のブートストラップを実行後、Avalonia を起動する。
    /// --update-check 引数が指定された場合は UI なしでサイレント更新チェックのみ実行する。
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        TrySetCurrentProcessAppUserModelId();

        VelopackApp.Build()
            .OnAfterInstallFastCallback(v =>
            {
                StartupRegistration.Register();
                StartMenuShortcutMigration.MoveLegacyShortcutToRoot();
            })
            .OnAfterUpdateFastCallback(v =>
            {
                StartupRegistration.Register();
                StartMenuShortcutMigration.MoveLegacyShortcutToRoot();
            })
            .OnBeforeUninstallFastCallback(v =>
            {
                StartupRegistration.Unregister();
            })
            .Run();

        // サイレント更新チェックモード
        if (args.Length > 0 && args[0] == UpdateCheckArg)
        {
            RunSilentUpdateCheckAsync().GetAwaiter().GetResult();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void TrySetCurrentProcessAppUserModelId()
    {
        try { _ = SetCurrentProcessExplicitAppUserModelID(AppUserModelId); }
        catch { /* シェル連携の失敗だけで起動を止めない */ }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    /// <summary>
    /// UI なしでサイレント更新チェックを実行する。
    /// Windows ログイン時のスタートアップから呼び出される。
    /// </summary>
    private static async Task RunSilentUpdateCheckAsync()
    {
        try
        {
            var source = new Velopack.Sources.SimpleWebSource(UpdateBaseUrl);
            var updateManager = new UpdateManager(source);

            if (!updateManager.IsInstalled)
            {
                return;
            }

            // チェックは小さい I/O。短いタイムアウトで無応答を素早く検出
            // (CheckForUpdatesAsync は CancellationToken を受けないため WaitAsync でかける)
            using var checkCts = new CancellationTokenSource(UpdateCheckTimeout);
            var updateInfo = await updateManager.CheckForUpdatesAsync().WaitAsync(checkCts.Token);
            if (updateInfo is null)
            {
                return;
            }

            // ダウンロードはチェックとは別枠の長めタイムアウト (低速回線で正常な DL が打ち切られないように)
            using var downloadCts = new CancellationTokenSource(UpdateDownloadTimeout);
            await updateManager.DownloadUpdatesAsync(updateInfo, null, downloadCts.Token);
            updateManager.ApplyUpdatesAndExit(updateInfo);
        }
        catch (Exception ex)
        {
            // サイレントモードではエラー (タイムアウト含む) を無視して終了。
            // 原因究明用に Debug ビルドのみ出力 (Release/AOT では Conditional により除去される)
            System.Diagnostics.Debug.WriteLine($"Silent update check failed: {ex}");
        }
    }

    /// <summary>
    /// Avalonia アプリケーションをビルドする
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

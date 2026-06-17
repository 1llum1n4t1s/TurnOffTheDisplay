using Avalonia;
using Velopack;

namespace TurnOffTheDisplay;

/// <summary>
/// アプリケーションのエントリーポイント
/// </summary>
internal class Program
{
    internal const string UpdateCheckArg = "--update-check";

    /// <summary>
    /// 自動更新の配信元ベース URL (Cloudflare R2 totd-updates / カスタムドメイン totd.nephilim.jp)
    /// </summary>
    internal const string UpdateBaseUrl = "https://totd.nephilim.jp";

    /// <summary>
    /// サイレント更新チェックの I/O 全体のタイムアウト (無応答ネットワークでのゴーストプロセス常駐を防ぐ)
    /// </summary>
    private static readonly TimeSpan UpdateCheckTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// アプリケーションのエントリーポイント。Velopack のブートストラップを実行後、Avalonia を起動する。
    /// --update-check 引数が指定された場合は UI なしでサイレント更新チェックのみ実行する。
    /// </summary>
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build()
            .OnAfterInstallFastCallback(v =>
            {
                StartupRegistration.Register();
            })
            .OnAfterUpdateFastCallback(v =>
            {
                StartupRegistration.Register();
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

            using var cts = new CancellationTokenSource(UpdateCheckTimeout);

            // CheckForUpdatesAsync は CancellationToken を受けないため WaitAsync でタイムアウトをかける
            var updateInfo = await updateManager.CheckForUpdatesAsync().WaitAsync(cts.Token);
            if (updateInfo is null)
            {
                return;
            }

            await updateManager.DownloadUpdatesAsync(updateInfo, null, cts.Token);
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

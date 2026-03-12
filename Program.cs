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
                StartMenuShortcut.Create();
            })
            .OnAfterUpdateFastCallback(v =>
            {
                StartupRegistration.Register();
                StartMenuShortcut.Create();
            })
            .OnBeforeUninstallFastCallback(v =>
            {
                StartupRegistration.Unregister();
                StartMenuShortcut.Remove();
            })
            .Run();

        // サイレント更新チェックモード
        if (args.Length > 0 && args[0] == UpdateCheckArg)
        {
            RunSilentUpdateCheck();
            return;
        }

        // Velopack コールバックが発火しなかった場合の保険
        StartMenuShortcut.EnsureCreated();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// UI なしでサイレント更新チェックを実行する。
    /// Windows ログイン時のスタートアップから呼び出される。
    /// </summary>
    private static void RunSilentUpdateCheck()
    {
        try
        {
            var source = new Velopack.Sources.GithubSource(
                "https://github.com/1llum1n4t1s/TurnOffTheDisplay", string.Empty, prerelease: false);
            var updateManager = new UpdateManager(source);

            if (!updateManager.IsInstalled)
            {
                return;
            }

            var updateInfo = updateManager.CheckForUpdatesAsync().GetAwaiter().GetResult();
            if (updateInfo is null)
            {
                return;
            }

            updateManager.DownloadUpdatesAsync(updateInfo).GetAwaiter().GetResult();
            updateManager.ApplyUpdatesAndExit(updateInfo);
        }
        catch
        {
            // サイレントモードではエラーを無視して終了
        }
    }

    /// <summary>
    /// Avalonia アプリケーションをビルドする
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

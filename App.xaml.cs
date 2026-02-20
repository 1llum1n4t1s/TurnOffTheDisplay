using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Velopack;
using Velopack.Sources;

namespace TurnOffTheDisplay;

/// <summary>
/// Avalonia アプリケーションクラス
/// </summary>
public class App : Application
{
    private const string UpdateRepositoryUrl = "https://github.com/1llum1n4t1s/TurnOffTheDisplay";

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Initialize()
    {
        InitializeComponent();
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var shouldContinue = await CheckForUpdatesAsync();
            if (!shouldContinue)
            {
                desktop.Shutdown();
                return;
            }

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 更新確認を非同期で実行します。アプリケーションがインストールされていない場合は例外をキャッチして続行します。
    /// </summary>
    /// <returns>アプリケーション起動を続行する場合は true、再起動を待つ場合は false</returns>
    private static async System.Threading.Tasks.Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource(UpdateRepositoryUrl, string.Empty, prerelease: false);
            var updateManager = new UpdateManager(source);

            var updateInfo = await updateManager.CheckForUpdatesAsync();
            if (updateInfo is null)
            {
                return true;
            }

            await updateManager.DownloadUpdatesAsync(updateInfo);
            updateManager.ApplyUpdatesAndRestart(updateInfo);
            return false;
        }
        catch (Velopack.Exceptions.NotInstalledException)
        {
            // アプリケーションがインストールされていない場合は、スキップして起動を続行
            return true;
        }
    }
}

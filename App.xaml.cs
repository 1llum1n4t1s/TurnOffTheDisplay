using System.Threading.Tasks;
using System.Windows;
using Velopack;
using Velopack.Sources;

namespace TurnOffTheDisplay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const string UpdateRepositoryUrl = "https://github.com/1llum1n4t1s/TurnOffTheDisplay";

    protected override async void OnStartup(StartupEventArgs e)
    {
        VelopackApp.Build().Run();

        var shouldContinue = await CheckForUpdatesAsync();
        if (!shouldContinue)
        {
            return;
        }

        base.OnStartup(e);
    }

    /// <summary>
    /// 更新確認を非同期で実行します。アプリケーションがインストールされていない場合は例外をキャッチして続行します。
    /// </summary>
    /// <returns>アプリケーション起動を続行する場合は true、再起動を待つ場合は false</returns>
    private static async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var source = new GithubSource(UpdateRepositoryUrl, "TurnOffTheDisplay", prerelease: false);
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

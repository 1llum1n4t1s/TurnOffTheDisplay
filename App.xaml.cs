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
        base.OnStartup(e);

        VelopackApp.Build().Run();

        await CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        var source = new GithubSource(UpdateRepositoryUrl, "TurnOffTheDisplay", prerelease: false);
        using var updateManager = new UpdateManager(source);

        var updateInfo = await updateManager.CheckForUpdatesAsync();
        if (updateInfo is null)
        {
            return;
        }

        await updateManager.DownloadUpdatesAsync(updateInfo);
        updateManager.ApplyUpdatesAndRestart(updateInfo);
    }
}

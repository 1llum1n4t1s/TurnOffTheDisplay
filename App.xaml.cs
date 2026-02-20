using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace TurnOffTheDisplay;

/// <summary>
/// Avalonia アプリケーションクラス
/// 更新確認は Program.cs の --update-check モードで Windows ログイン時に実行される
/// </summary>
public class App : Application
{
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void Initialize()
    {
        InitializeComponent();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

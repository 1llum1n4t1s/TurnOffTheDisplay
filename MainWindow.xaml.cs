using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using TurnOffTheDisplay.ViewModels;

namespace TurnOffTheDisplay;

/// <summary>
/// ディスプレイをスタンバイモードに移行するメインウィンドウ
/// </summary>
public class MainWindow : Window
{
    private const int WM_SYSCOMMAND = 0x0112;
    private const nint SC_MONITORPOWER = 0xF170;

    // モニタ電源 OFF。Avalonia に対応 API が無いため Win32 を直接呼ぶ
    [DllImport("user32.dll")]
    private static extern nint SendMessage(nint hWnd, int Msg, nint wParam, nint lParam);

    private readonly MainWindowViewModel _viewModel;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainWindowViewModel(TurnOffDisplayAndClose, Close);
        DataContext = _viewModel;

        Closed += (_, _) => _viewModel.Cleanup();
    }

    /// <summary>
    /// ディスプレイ OFF → ウィンドウを閉じる
    /// 自ウィンドウハンドルに SC_MONITORPOWER を送信する
    /// </summary>
    private void TurnOffDisplayAndClose()
    {
        var hWnd = TryGetPlatformHandle()?.Handle ?? nint.Zero;
        if (hWnd != nint.Zero)
        {
            SendMessage(hWnd, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
        }

        Close();
    }
}

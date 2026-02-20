using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TurnOffTheDisplay.ViewModels;

namespace TurnOffTheDisplay;

/// <summary>
/// ディスプレイをスタンバイモードに移行するメインウィンドウ
/// </summary>
public class MainWindow : Window
{
    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_MAXIMIZEBOX = 0x00010000;

    private const int WM_SYSCOMMAND = 0x0112;
    private const nint SC_MONITORPOWER = 0xF170;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

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

        _viewModel = new MainWindowViewModel(TurnOffDisplayAndClose);
        DataContext = _viewModel;

        Closed += (_, _) => _viewModel.Cleanup();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RemoveMinMaxButtons();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.Escape)
        {
            _viewModel.CancelCommand.Execute(null);
        }
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

    /// <summary>
    /// Win32 API で最小化・最大化ボタンを除去する
    /// </summary>
    private void RemoveMinMaxButtons()
    {
        var handle = TryGetPlatformHandle();
        if (handle is null) return;

        var hWnd = handle.Handle;
        var style = GetWindowLongPtr(hWnd, GWL_STYLE);
        style &= ~(nint)WS_MINIMIZEBOX;
        style &= ~(nint)WS_MAXIMIZEBOX;
        SetWindowLongPtr(hWnd, GWL_STYLE, style);
    }
}

using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace TurnOffTheDisplay.ViewModels;

/// <summary>
/// メインウィンドウの ViewModel
/// カウントダウン処理を管理する
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private const int CountdownSeconds = 5;

    private readonly Action _turnOffAndClose;
    private readonly Action _closeWindow;
    private readonly DispatcherTimer _countdownTimer;
    private int _count = CountdownSeconds;

    /// <summary>
    /// カウントダウン表示テキスト
    /// </summary>
    [ObservableProperty]
    private string _countText = CountdownSeconds.ToString();

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="turnOffAndClose">ディスプレイ OFF してウィンドウを閉じるアクション</param>
    /// <param name="closeWindow">ウィンドウを閉じるアクション（キャンセル時用）</param>
    public MainWindowViewModel(Action turnOffAndClose, Action closeWindow)
    {
        _turnOffAndClose = turnOffAndClose;
        _closeWindow = closeWindow;

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += CountdownTimer_Tick;
        _countdownTimer.Start();
    }

    /// <summary>
    /// タイマーの Tick イベント
    /// カウントダウン処理とディスプレイ OFF 処理を実行
    /// </summary>
    private void CountdownTimer_Tick(object? sender, EventArgs e)
    {
        if (_count > 0)
        {
            _count--;
            CountText = _count.ToString();
        }
        else
        {
            _countdownTimer.Stop();
            _turnOffAndClose();
        }
    }

    /// <summary>
    /// キャンセルコマンド — タイマーを停止してウィンドウを閉じる（ディスプレイ OFF しない）
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _countdownTimer.Stop();
        _closeWindow();
    }

    /// <summary>
    /// リソース解放（ウィンドウ Closed から呼び出す）
    /// </summary>
    public void Cleanup()
    {
        _countdownTimer.Stop();
        _countdownTimer.Tick -= CountdownTimer_Tick;
    }
}

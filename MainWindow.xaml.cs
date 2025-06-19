using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace TurnOffTheDisplay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    // Win32Apiで送信するコマンドを定義
    private const int HWND_BROADCAST = 0xFFFF;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;

    private int count = 5; // カウントダウンの初期値
    private readonly DispatcherTimer _countdownTimer;

    // PostMessageを定義
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool PostMessage(int hWnd, int Msg, int wParam, int lParam);
    
    public MainWindow()
    {
        InitializeComponent();

        // タイマーの設定
        _countdownTimer = new DispatcherTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとの間隔
        _countdownTimer.Tick += CountdownTimer_Tick;
        _countdownTimer.Start();
    }

    // タイマーのTickイベント
    private void CountdownTimer_Tick(object sender, EventArgs e)
    {
        if (count > 0)
        {
            count--;
            lblCount.Content = count.ToString(); // ラベルにカウントダウン表示
        }
        else
        {
            // タイマー停止
            _countdownTimer.Stop();

            // ディスプレイをOFFにする処理をここに追加
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);

            // ウィンドウを閉じる
            Close();
        }
    }
}

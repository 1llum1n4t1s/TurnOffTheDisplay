using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace TurnOffTheDisplay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    // Win32Apiで送信するコマンドを定義
    private const int HWND_BROADCAST = 0xFFFF;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MONITORPOWER = 0xF170;

    private int count = 5; // カウントダウンの初期値
    private DispatcherTimer countdownTimer;

    // PostMessageを定義
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool PostMessage(int hWnd, int Msg, int wParam, int lParam);

    // カウントダウン時間
    private int _countdown = 10; // 10秒のカウントダウン

    public MainWindow()
    {
        InitializeComponent();

        // タイマーの設定
        countdownTimer = new DispatcherTimer();
        countdownTimer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとの間隔
        countdownTimer.Tick += CountdownTimer_Tick;
        countdownTimer.Start();
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
            countdownTimer.Stop();

            // ディスプレイをOFFにする処理をここに追加
            PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);

            // ウィンドウを閉じる
            this.Close();
        }
    }
}

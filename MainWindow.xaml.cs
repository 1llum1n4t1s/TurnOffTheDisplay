using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace TurnOffTheDisplay;

/// <summary>
/// ディスプレイをスタンバイモードに移行するメインウィンドウ
/// カウントダウン後にディスプレイをOFFにする機能を提供
/// </summary>
public partial class MainWindow : Window
{
    // Win32Apiで送信するコマンドを定義
    private const int HWND_BROADCAST = 0xFFFF;        // 全ウィンドウにメッセージを送信
    private const int WM_SYSCOMMAND = 0x0112;         // システムコマンドメッセージ
    private const int SC_MONITORPOWER = 0xF170;       // モニターパワー制御

    private int count = 5; // カウントダウンの初期値
    private readonly DispatcherTimer _countdownTimer;

    // PostMessageを定義（ディスプレイ制御用）
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool PostMessage(int hWnd, int Msg, int wParam, int lParam);
    
    /// <summary>
    /// メインウィンドウのコンストラクタ
    /// カウントダウンタイマーの初期化と開始を行う
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // アイコンをコードから設定
        try
        {
            var iconUri = new Uri("pack://application:,,,/icon/icon.ico");
            this.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
        }
        catch (Exception ex)
        {
            // アイコン設定に失敗した場合はログに記録（デバッグ用）
            System.Diagnostics.Debug.WriteLine($"アイコン設定エラー: {ex.Message}");
        }

        // タイマーの設定
        _countdownTimer = new DispatcherTimer();
        _countdownTimer.Interval = TimeSpan.FromSeconds(1); // 1秒ごとの間隔
        _countdownTimer.Tick += CountdownTimer_Tick;
        _countdownTimer.Start();

        // ウィンドウが閉じられる際のリソース解放を設定
        Closed += MainWindow_Closed;
    }

    /// <summary>
    /// タイマーのTickイベント
    /// カウントダウン処理とディスプレイOFF処理を実行
    /// </summary>
    /// <param name="sender">イベント送信元</param>
    /// <param name="e">イベント引数</param>
    private void CountdownTimer_Tick(object? sender, EventArgs e)
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

            try
            {
                // ディスプレイをOFFにする処理
                var result = PostMessage(HWND_BROADCAST, WM_SYSCOMMAND, SC_MONITORPOWER, 2);
                
                if (!result)
                {
                    MessageBox.Show("ディスプレイの電源を切る処理に失敗しました。", 
                        "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ディスプレイ制御でエラーが発生しました: {ex.Message}", 
                    "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // ウィンドウを閉じる
                Close();
            }
        }
    }

    /// <summary>
    /// ウィンドウが閉じられる際のリソース解放処理
    /// </summary>
    /// <param name="sender">イベント送信元</param>
    /// <param name="e">イベント引数</param>
    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        // タイマーの停止とリソース解放
        if (_countdownTimer != null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Tick -= CountdownTimer_Tick;
        }
    }

    /// <summary>
    /// キーボードイベント処理（ESCキーでキャンセル）
    /// </summary>
    /// <param name="e">キーイベント引数</param>
    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // ESCキーでキャンセル
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelOperation();
        }
    }

    /// <summary>
    /// ウィンドウのキーイベントハンドラー
    /// </summary>
    /// <param name="sender">イベント送信元</param>
    /// <param name="e">キーイベント引数</param>
    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // ESCキーでキャンセル
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            CancelOperation();
        }
    }

    /// <summary>
    /// キャンセルボタンのクリックイベントハンドラー
    /// </summary>
    /// <param name="sender">イベント送信元</param>
    /// <param name="e">イベント引数</param>
    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        CancelOperation();
    }

    /// <summary>
    /// 操作をキャンセルする処理
    /// </summary>
    private void CancelOperation()
    {
        _countdownTimer?.Stop();
        Close();
    }
}

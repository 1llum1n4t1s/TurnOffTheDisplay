using System.Runtime.Versioning;
using Microsoft.Win32;

namespace TurnOffTheDisplay;

/// <summary>
/// Windows スタートアップへのアプリケーション登録を管理するクラス
/// ログイン時にサイレント更新チェックを実行するために使用
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string EntryName = "TurnOffTheDisplay";

    /// <summary>
    /// スタートアップにアプリケーションを登録する
    /// </summary>
    public static void Register()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null)
            {
                return;
            }

            var value = $"\"{exePath}\" --update-check";
            key.SetValue(EntryName, value);
        }
        catch
        {
            // スタートアップ登録の失敗はアプリの動作に影響しないため無視
        }
    }

    /// <summary>
    /// スタートアップからアプリケーションの登録を解除する
    /// </summary>
    public static void Unregister()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
            if (key == null) return;

            if (key.GetValue(EntryName) != null)
            {
                key.DeleteValue(EntryName);
            }
        }
        catch
        {
            // 登録解除の失敗はアプリの動作に影響しないため無視
        }
    }
}

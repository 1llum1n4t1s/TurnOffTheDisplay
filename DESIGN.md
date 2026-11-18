# DESIGN.md

「ディスプレイ＠OFF」の設計。開発コマンド・リリース手順・規約は [AGENTS.md](AGENTS.md)、利用者向けの案内は [README.md](README.md) を参照する。

## 目的と範囲

Windows の電源プランにある「ディスプレイの電源を切る」と同じ動作を、起動するだけで実行する小さなデスクトップアプリ。起動後 5 秒のカウントダウンを置き、ESC / ボタンでキャンセルできる。Windows 10/11 x64 専用、Avalonia UI 12 + .NET 11、Native AOT、Velopack による配布と自動更新。テストプロジェクトは無い（R2 配布スクリプトの検証 `tests/R2Transfer/` を除く）。

## 構成要素と責務

| 要素 | 責務 |
| --- | --- |
| `Program.cs` | エントリ。Velopack のフック登録、`--update-check` のサイレント更新、通常時の Avalonia 起動を振り分ける |
| `App` / `MainWindow` | Avalonia 起動と、唯一の Win32 呼び出し（モニタ電源 OFF）を持つ View |
| `ViewModels/MainWindowViewModel` | 5 秒カウントダウンとキャンセル。View に依存しない |
| `MainWindow.xaml` | 表示と入力（ESC は `KeyBinding`、ボタンとも `CancelCommand`）。ウィンドウ属性（最小化・リサイズ不可、最前面） |
| `StartupRegistration` | HKCU `Run` へ `"<exe>" --update-check` を登録・解除 |
| `StartMenuShortcutMigration` | 旧「ゆろち」フォルダ内のショートカットを Programs 直下へ移行 |
| `scripts/` | 署名付き Velopack リリースと Cloudflare R2 への配信 |

## データフロー

### 通常起動（UI）
1. `MainWindow` が VM を生成し、2 つの `Action`（OFF して閉じる / 閉じるだけ）を注入する。
2. VM の `DispatcherTimer`（1 秒間隔）が減算し、0 になるとタイマーを止めて「OFF して閉じる」を呼ぶ。
3. `MainWindow.TurnOffDisplayAndClose` が自ウィンドウのハンドルへ `WM_SYSCOMMAND` / `SC_MONITORPOWER`（引数 2 = 電源オフ）を `SendMessage` し、ウィンドウを閉じる。
4. キャンセル時はタイマーを止めて閉じるだけで、ディスプレイには触れない。

### サイレント更新
1. インストール / 更新時の Velopack コールバックが `StartupRegistration.Register` を呼び、Run キーへ `--update-check` 付きで登録する（アンインストール前に解除）。
2. Windows サインイン時にその引数で起動すると UI を出さず、`SimpleWebSource("https://totd.kagayoi.com")`（R2 バケット `totd-updates`）で更新を確認し、あればダウンロードして適用・終了する。
3. 例外は全て握り潰す（Debug 出力のみ）。未インストール（開発実行）では何もしない。

### リリース
`Directory.Build.props` の `<Version>` → `scripts/release-local.ps1`（`dotnet publish -r win-x64` → `vpk pack`、署名は Sigilune → R2 へ upload → 固定名ファイルの Cloudflare キャッシュパージを要求 → manifest 外の旧 nupkg を削除）。main の commit に付けた軽量タグ `v<版>` の push で起動する `release.yml` は、先に `guard` ジョブ（タグ・commit・版の検査）と `verify` ジョブ（`dotnet-build.yml` を `workflow_call` で実行）が通った場合だけ、同スクリプトを Environment `release` のシークレットで実行する。`dotnet-build.yml` はブランチの push / PR で同じ検証（R2 転送ヘルパーの検査と Release ビルド）だけを行い、配信しない。

## 不変条件

- バージョンの単一情報源は `Directory.Build.props` の `<Version>` のみ。
- View が VM へ注入するコールバックで OFF とクローズを分け、VM は Avalonia のウィンドウ API を参照しない（`DispatcherTimer` のみ使用）。
- カウントダウンは先に減算してから判定する。0 を 1 秒表示してから OFF すると実測が 6 秒になり、5 秒の契約とずれるため。
- `--update-check` モードは UI を起動せず、必ず終了する。更新の確認（30 秒）とダウンロード（10 分）は別々のタイムアウトを持つ。
- 配信ドメインは `totd.kagayoi.com`。旧 `totd.nephilim.jp` は出荷済みバイナリの更新経路として 2027/05/31 の失効まで route に残す。旧ホストの設定は期限まで消さない。

## 設計判断と理由・トレードオフ

- **Win32 P/Invoke は 2 箇所に限定**: モニタ電源 OFF（`user32!SendMessage`）は Avalonia に API が無いため必須。もう 1 つは AppUserModelID 設定（`shell32`、失敗しても起動は止めない）。それ以外の Win32 ハック（最小化ボタン抑止など）は `CanMinimize` / `CanResize` で代替した。
- **Native AOT + トリム警告ゼロ**: 起動速度と配布サイズのため。リフレクションや動的コード生成は入れられない。XAML は compiled bindings と `AvaloniaResource` で解決し、トリマー root 記述子は置かない（自アセンブリ全保持はサイズ削減に逆行）。`UseSystemResourceKeys` などの feature switch で更に削る。
- **`InvariantGlobalization` は未設定**: Windows AOT では ICU を同梱しないため削減効果が小さく、Velopack の更新処理のカルチャ安全性に影響し得るため。
- **サイレント更新は専用起動引数 + Run キー**: 常駐サービスを持たず、サインイン時だけ短時間動く。ネットワーク不通時のプロセス常駐をタイムアウトで防ぐ代わり、失敗は利用者に通知されず次回サインインまで再試行されない。
- **配信は Cloudflare R2 + カスタムドメイン**: 更新ファイルの保管と配信を静的に完結できる。バージョン付き成果物は不変キャッシュ、固定名ファイル（manifest 等）は毎回再検証にして、更新反映の遅れを避ける。R2 書込みは同梱の PowerShell/.NET 実装で行い、Node.js・Wrangler に依存しない。
- **署名は Sigilune（Certum SimplySign のクラウド鍵）**: ローカル PC やトークンに依存せず CI で署名できる。署名に失敗した場合は署名を省略せず中止する。
- **スタートメニューのショートカットは直下配置**: 共有の「ゆろち」フォルダを増やさないため。旧配置は更新後のコールバックで移行し、フォルダ自体には触れない（他アプリと共有しているため）。

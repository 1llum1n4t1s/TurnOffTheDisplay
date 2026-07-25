# CLAUDE.md

This file provides guidance to Claude Code and other coding agents working in this repository.

「ディスプレイ＠OFF」: Windows でディスプレイをスタンバイへ移行する常駐デスクトップアプリ。Avalonia UI + .NET 10 (`net10.0-windows8.0`)、x64 専用、Native AOT、Velopack による自動更新。

## コマンド

```bash
# ビルド (ソリューションは TurnOffTheDisplay.slnx)
dotnet build TurnOffTheDisplay.csproj -c Release

# 開発実行 (UI 起動。5秒カウントダウン後にディスプレイ OFF が走るので ESC で止める)
dotnet run

# Native AOT 発行 (配布物の本検証。RID 必須)
dotnet publish TurnOffTheDisplay.csproj -c Release -r win-x64
```

**Git Bash から AOT publish する場合**は、先に VS Installer を PATH へ通す。通さないとネイティブリンク段で `vswhere.exe` が解決できず `MSB3073` で落ちる:
```bash
export PATH="/c/Program Files (x86)/Microsoft Visual Studio/Installer:$PATH"
```

**テストプロジェクトは存在しない。** 検証は「`dotnet build` 成功」で行い、AOT に関わる変更（csproj のトリミング設定・新 API・P/Invoke 等）は「`dotnet publish -r win-x64` のリンク成功（IL2xxx/IL3xxx 警告ゼロ）」で確認する。`IsAotCompatible=true` により通常 build でも AOT/トリムアナライザが走るので、build の警告ゼロを基準にできる。

## リリース

リリースは **CI ではなくローカルスクリプト**で行う（Certum/SimplySign のクラウド署名が Desktop 接続 + スマホトークンを要し CI から署名できないため。旧 `velopack-release.yml` は廃止済み）。

```bash
pwsh -NoProfile -File scripts/release-local.ps1               # フル (build+署名+R2 upload+旧 nupkg cleanup)
pwsh -NoProfile -File scripts/release-local.ps1 -SkipUpload   # build+署名のみ (動作確認)
```

前提: SimplySign Desktop 接続済み（証明書が `Cert:\CurrentUser\My` に見える）/ `Directory.Build.props` の `<Version>` が目的版（`/vava` 済み）/ `C:\Users\IMT\dev\Secret\secrets.json` に `cloudflare.api_token`。正規ルートは **`/vava` スキル**（バージョン bump → release ブランチ → ローカル署名リリース連携）。

## アーキテクチャ

### 2 つのエントリ経路（`Program.cs`）
`Main` は先頭で必ず `VelopackApp.Build()…Run()` を実行する（インストール/更新/アンインストールの fast callback で `StartupRegistration` のスタートアップ登録/解除を行う）。その後:
- 引数が `--update-check` → `RunSilentUpdateCheckAsync()` を実行して **UI を起動せず終了**（サイレント更新モード）
- それ以外 → `BuildAvaloniaApp().StartWithClassicDesktopLifetime()` で通常 UI 起動

### サイレント自動更新フロー
`StartupRegistration` が HKCU `…\Run` に `"<exe>" --update-check` を登録する（インストール/更新時の Velopack callback 経由）。Windows ログイン時にこの引数付きで起動し、`SimpleWebSource("https://totd.kagayoi.com")`（= Cloudflare R2）から更新を取得・適用する。**チェック (30秒) とダウンロード (10分) は別々の `CancellationTokenSource`** を持つ（無応答ネットでの常駐を防ぎつつ、低速回線の正常 DL を打ち切らない）。例外は全て握り潰し（サイレント用途）、`Debug.WriteLine` のみ（Release/AOT では除去）。

### ディスプレイ OFF の仕組み
`MainWindowViewModel` が `DispatcherTimer` で 5秒カウントダウンし、0 で注入された「OFF して閉じる」コールバックを呼ぶ。実体は `MainWindow.TurnOffDisplayAndClose()` が自ウィンドウハンドルへ `WM_SYSCOMMAND` / `SC_MONITORPOWER` を `SendMessage` する **唯一残した P/Invoke**（Avalonia にモニタ電源 API が無いため正当）。ESC / キャンセルは `MainWindow.xaml` の `<Window.KeyBindings>` と Button が `CancelCommand` を叩く（入力処理はコードビハインドに持たず XAML 側で完結）。最小化/最大化ボタンの抑止は `CanMinimize="False"` + `CanResize="False"` で行う（Win32 ハックは使わない）。

### MVVM の約束
View が VM コンストラクタに **2 つの `Action`（OFF+close / close のみ）を注入**し、VM は View 非依存に保つ。`CommunityToolkit.Mvvm` のソースジェネレータ（`[ObservableProperty]` / `[RelayCommand]`）を使うため、対象クラスは `partial` 必須。

### バージョン管理（単一情報源）
バージョンは **`Directory.Build.props` の `<Version>` のみ**で定義し、`csproj` はこれを継承する（csproj に `<Version>` リテラルは持たせない）。`release-local.ps1` も XPath で props から読む。バージョン変更は `/vava` 経由のみ — コード修正のついでに書き換えない。

### Native AOT 制約
`PublishAot=true`。リフレクション依存・動的コード生成・未注釈のトリミング非互換コードは入れない（AOT/トリムを壊すため）。サイズ削減 feature switch は csproj に集約（`UseSystemResourceKeys` / `EventSourceSupport=false` / `HttpActivityPropagationSupport=false`、Release のみ `DebuggerSupport=false`）。`InvariantGlobalization` は **意図的に未設定**（Windows AOT では ICU 非同梱で削減僅少な一方、Velopack 更新処理のカルチャ安全性に影響し得るため）。`TrimmerRoots.xml` は自アセンブリを `preserve="all"`（XAML 反射ロード救済）。

### 配信インフラ（2 系統・互いに独立）
- **アプリ更新**: Velopack → R2 バケット `totd-updates`、カスタムドメイン `totd.kagayoi.com`。配信は `release-local.ps1`（R2 upload + manifest 外の旧 `*.nupkg` を自動 cleanup）。
- **ランディングページ**: `web/` は同一ホスト名に被せる Cloudflare Worker (`totd-landing`)。`worker.js` が `/` と `/index.html` だけバンドル HTML を返し、**それ以外のパス（更新ファイル）は `fetch(request)` で R2 へ無加工委譲**する（Worker Route は同ゾーン fetch の対象外＝再帰しない）。`web/**` push 時に `deploy-landing.yml` がデプロイ。更新配信とは無関係。

## CI（`.github/workflows/`）
- `dotnet-build.yml`: 非 release ブランチ / PR で build + test（テストプロジェクト無しのため `continue-on-error`）。配信はしない。
- `deploy-landing.yml`: `web/**` push でランディング Worker をデプロイ。
- リリース用 CI は無い（ローカル署名スクリプトに置換済み）。

## 注意点
- 日本語コメント/文字列を含む PowerShell スクリプトは **UTF-8 BOM 付き**で保存する（PSScriptAnalyzer `PSUseBOMForUnicodeEncodedFile`。BOM 無しだと Windows PowerShell 5.1 が Shift-JIS と誤認して文字化けする）。
- `vpk`（Velopack CLI）はリリース時に NuGet の最新安定版へ解決する（ハードコード固定はしない）。`wrangler` はサプライチェーン対策でバージョン固定する。

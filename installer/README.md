# セットアップ

Windowsで.NET 10 SDK、PowerShell 7、Inno Setup 6を使用します。通常は次のコマンドでソースから配布物を生成し、セットアップを作成します。

~~~powershell
./scripts/build-installer.ps1 -Compiler 'C:\path\to\ISCC.exe'
~~~

既存の配布フォルダーを使う場合は-Packageを指定します。
本番AppIdは固定し、旧版からの上書き更新に対応します。バージョンはcsprojから取得します。

## 隔離テスト

~~~powershell
./scripts/build-installer.ps1 -Package 'artifacts/packages/生成したフォルダー' -TestBuild -TestRoot 'artifacts/installer-test-new'
dotnet build tools/JsonMaintenance -c Release
./scripts/test-installer.ps1 -Package 'artifacts/packages/生成したフォルダー' -TestRoot 'artifacts/installer-test-new'
~~~

テスト先は未使用のartifacts内フォルダーにしてください。テスト版は別AppIdを使い、アプリ登録・ショートカットを作成しません。既存のアプリフォルダーは上書きしません。
新規配置、共通初期化処理、再インストール時のデータ保持、アンインストール後のデータ保持を確認します。WPF画面の起動確認は別途必要です。

保存先はインストール先のdataです。セットアップは初期配布ファイルを配置し、アプリが起動時にdataを初期化します。dataはアンインストール後も保持します。
コード署名は未設定です。
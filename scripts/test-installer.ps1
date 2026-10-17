param([Parameter(Mandatory)][string]$Package,[Parameter(Mandatory)][string]$TestRoot)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$testRoot=[IO.Path]::GetFullPath((Join-Path $root $TestRoot))
$allowed=[IO.Path]::GetFullPath((Join-Path $root 'artifacts'))+[IO.Path]::DirectorySeparatorChar
if(-not $testRoot.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'テスト先はartifacts内にしてください'}
[xml]$project=Get-Content (Join-Path $root 'src/AiPromptHelper/AiPromptHelper.csproj')
$version=[string]$project.Project.PropertyGroup.Version
$app=Join-Path $testRoot 'app'
$data=Join-Path $app 'data'
$local=Join-Path $testRoot 'local'
$package=(Resolve-Path -LiteralPath $Package).Path
$setup=Join-Path $testRoot "setup/TsunaguChat-$version-Setup.exe"
$probe=Join-Path $root 'tools/JsonMaintenance/bin/Release/net10.0/JsonMaintenance.dll'
if(Test-Path -LiteralPath $app){throw '既存のテスト先を上書きしません'}
function RunSetup($name){
 $p=Start-Process -FilePath $setup -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-',('/LOG="'+(Join-Path $testRoot $name)+'"')) -WindowStyle Hidden -PassThru -Wait
 if($p.ExitCode -ne 0){throw 'インストール失敗'}
}
function InitializeData {
 & dotnet $probe --initialize $app $local
 if($LASTEXITCODE -ne 0){throw '起動時のデータ初期化失敗'}
}
RunSetup 'fresh.log'
foreach($file in Get-ChildItem -LiteralPath $package -File -Recurse){
 $target=Join-Path $app ([IO.Path]::GetRelativePath($package,$file.FullName))
 if((Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash){throw '配置ファイル不一致'}
}
InitializeData
if((Get-ChildItem -LiteralPath (Join-Path $data 'templates') -Filter '*.json').Count -ne 10){throw '初期テンプレート数が不正'}
if(Test-Path -LiteralPath (Join-Path $local 'AiPromptHelper')){throw '旧データ保存先が生成されました'}
'PASS fresh install + shared startup initialization: payload matched, 10 templates, data under app; no legacy directory'
$t=Get-Content -LiteralPath (Join-Path $data 'templates/ideas.json') -Raw | ConvertFrom-Json
$t.name='編集済み';$t | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath (Join-Path $data 'templates/ideas.json') -Encoding utf8
$before=@{}
Get-ChildItem -LiteralPath $data -File -Recurse | ForEach-Object {$before[$_.FullName]=(Get-FileHash -LiteralPath $_.FullName).Hash}
RunSetup 'reinstall.log'
InitializeData
foreach($path in $before.Keys){if((Get-FileHash -LiteralPath $path).Hash -ne $before[$path]){throw '再インストールまたは再起動で既存データ変更'}}
'PASS reinstall and startup: user data preserved'
$uninstaller=(Get-ChildItem -LiteralPath $app -Filter 'unins*.exe' | Select-Object -First 1).FullName
if(-not [IO.Path]::GetFullPath($uninstaller).StartsWith([IO.Path]::GetFullPath($app)+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw '不正な削除対象'}
$p=Start-Process -FilePath $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait
if($p.ExitCode -ne 0 -or (Test-Path -LiteralPath (Join-Path $app 'TsunaguChat.exe'))){throw 'アンインストール失敗'}
foreach($path in $before.Keys){if((Get-FileHash -LiteralPath $path).Hash -ne $before[$path]){throw 'アンインストールでdataが変更'}}
'PASS uninstall: executable removed, data retained within same app folder'
'Installer tests completed.'

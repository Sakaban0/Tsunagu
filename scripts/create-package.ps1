param([switch]$SkipPublish)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    if(-not $SkipPublish) { & ./scripts/publish.ps1 }
    [xml]$project=Get-Content src/AiPromptHelper/AiPromptHelper.csproj
    $version=[string]$project.Project.PropertyGroup.Version
    $exe=Join-Path $root "artifacts/publish/TsunaguChat-$version-win-x64/TsunaguChat.exe"
    if(-not (Test-Path -LiteralPath $exe)){throw 'EXEを先に発行してください'}
    if((Get-Item $exe).VersionInfo.ProductVersion.Split('+')[0] -ne $version){throw 'EXEのバージョンが一致しません'}
    $name="TsunaguChat-$version-"+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')
    $package=Join-Path $root "artifacts/packages/$name"
    New-Item -ItemType Directory $package | Out-Null
    New-Item -ItemType Directory "$package/templates","$package/docs","$package/licenses" | Out-Null
    Copy-Item $exe $package
    Copy-Item distribution/README.md,config/answer_styles.json,THIRD-PARTY-NOTICES.md $package
    if(Test-Path LICENSE){Copy-Item LICENSE $package}
    Copy-Item docs/USER-GUIDE.md,docs/TEMPLATE-GENERATION-PROMPT.md "$package/docs"
    $ids=@{}; $names=@{}
    foreach($file in @(Get-ChildItem templates/*.json)+@(Get-ChildItem distribution/templates/*.json)){
        $t=Get-Content $file.FullName -Raw | ConvertFrom-Json
        if($ids.ContainsKey($t.id) -or $names.ContainsKey($file.Name)){throw 'テンプレートのIDまたはファイル名が重複しています'}
        $ids[$t.id]=$true; $names[$file.Name]=$true
        Copy-Item $file.FullName "$package/templates"
    }
    dotnet run --project tools/JsonMaintenance -c Release -- --check $package
    if($LASTEXITCODE -ne 0){throw '配布JSONの検証に失敗しました'}
    $assets=Get-Content src/AiPromptHelper/obj/project.assets.json -Raw | ConvertFrom-Json
    $packs=@($assets.project.frameworks.PSObject.Properties.Value.downloadDependencies | Where-Object {$_.name -match '^Microsoft\.(NETCore|WindowsDesktop)\.App\.Runtime\.win-x64$'})
    if($packs.Count -ne 2){throw '発行に使用したランタイムパックを特定できません'}
    foreach($pack in $packs){
        $packVersion=($pack.version.Trim('[',']').Split(',')[0]).Trim()
        $relative=$pack.name.ToLowerInvariant()+'/'+$packVersion
        $folder=$null
        foreach($base in $assets.packageFolders.PSObject.Properties.Name) {
            $candidate=Join-Path $base $relative
            if(Test-Path $candidate){$folder=$candidate;break}
        }
        if(-not $folder){throw 'ランタイムパックが見つかりません'}
        $notices=@(Get-ChildItem -LiteralPath $folder -File | Where-Object {$_.Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES(\.TXT)?)$'})
        if(-not ($notices.Name -match '^LICENSE')){throw 'ランタイムライセンスが見つかりません'}
        if($pack.name -match 'NETCore' -and -not ($notices.Name -match '^THIRD-PARTY')){throw 'ランタイム第三者告知が見つかりません'}
        foreach($notice in $notices){
            Copy-Item $notice.FullName (Join-Path "$package/licenses" ($pack.name+'-'+$packVersion+'-'+$notice.Name))
        }
    }
    Compress-Archive -LiteralPath $package -DestinationPath "$package.zip"
    Write-Host "配布フォルダー: $package"
    return $package
} finally {Pop-Location}
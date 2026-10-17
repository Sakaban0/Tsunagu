param([string]$Package,[string]$Compiler,[switch]$TestBuild,[string]$TestRoot)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    [xml]$project=Get-Content src/AiPromptHelper/AiPromptHelper.csproj
    $version=[string]$project.Project.PropertyGroup.Version
    if(-not $Compiler){
        $pf=[Environment]::GetFolderPath('ProgramFilesX86')
        $candidates=@('artifacts/installer-tools/InnoSetup/ISCC.exe', "$pf\Inno Setup 6\ISCC.exe", "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe")
        $Compiler=$candidates | Where-Object {Test-Path -LiteralPath $_} | Select-Object -First 1
    }
    if(-not $Compiler){throw 'Inno Setupをインストールし、-CompilerでISCC.exeを指定してください'}
    $compilerPath=(Resolve-Path -LiteralPath $Compiler).Path
    if(-not $Package){$Package=(& ./scripts/create-package.ps1 | Select-Object -Last 1)}
    $packagePath=(Resolve-Path -LiteralPath $Package).Path
    $actual=(Get-Item -LiteralPath (Join-Path $packagePath 'TsunaguChat.exe')).VersionInfo.ProductVersion.Split('+')[0]
    if($actual -ne $version){throw "EXEの版が異なります: $actual"}
    dotnet run --project tools/JsonMaintenance -c Release -- --check $packagePath
    if($LASTEXITCODE -ne 0){throw '配布JSONが不正です'}
    $output=Join-Path $root 'artifacts/installer'
    $compilerArguments=@("/DPayloadDir=$packagePath","/DOutputPath=$output","/DAppVersion=$version")
    if($TestBuild){
        if(-not $TestRoot){throw '-TestBuildには専用の-TestRootを指定してください'}
        $testPath=[IO.Path]::GetFullPath((Join-Path $root $TestRoot))
        $allowed=[IO.Path]::GetFullPath((Join-Path $root 'artifacts'))+[IO.Path]::DirectorySeparatorChar
        if(-not $testPath.StartsWith($allowed,[StringComparison]::OrdinalIgnoreCase)){throw 'テスト先はartifacts内にしてください'}
        if(Test-Path (Join-Path $testPath 'app')){throw '既存のテストアプリは上書きしません'}
        $output=Join-Path $testPath 'setup'
        $compilerArguments=@("/DPayloadDir=$packagePath","/DOutputPath=$output","/DAppVersion=$version",'/DTestBuild',"/DTestAppDir=$testPath\app")
    }
    & $compilerPath @compilerArguments installer/TsunaguChat.iss
    if($LASTEXITCODE -ne 0){throw 'インストーラーのコンパイルに失敗しました'}
    Get-Item -LiteralPath (Join-Path $output "TsunaguChat-$version-Setup.exe") | Select-Object FullName,Length
} finally {Pop-Location}
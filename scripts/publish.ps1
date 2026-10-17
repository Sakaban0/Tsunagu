$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    & ./scripts/build.ps1
    [xml]$project=Get-Content src/AiPromptHelper/AiPromptHelper.csproj
    $version=[string]$project.Project.PropertyGroup.Version
    dotnet publish src/AiPromptHelper/AiPromptHelper.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:PublishTrimmed=false -p:DebugType=none -p:DebugSymbols=false -o "artifacts/publish/TsunaguChat-$version-win-x64"
    if ($LASTEXITCODE -ne 0) { throw 'EXEの発行に失敗しました。' }
} finally { Pop-Location }
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    dotnet build AiPromptHelper.sln -c Release
    if ($LASTEXITCODE -ne 0) { throw 'ビルドに失敗しました。' }
    dotnet tests/AiPromptHelper.Tests/bin/Release/net10.0/AiPromptHelper.Tests.dll
    if ($LASTEXITCODE -ne 0) { throw 'テストに失敗しました。' }
} finally { Pop-Location }

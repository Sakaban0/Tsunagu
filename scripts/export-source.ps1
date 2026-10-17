$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
Push-Location $root
try {
    $out=Join-Path $root ('artifacts/source/TsunaguChat-source-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
    New-Item -ItemType Directory $out | Out-Null
    $files=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'public-files.txt')
    foreach($rel in $files){
        if([string]::IsNullOrWhiteSpace($rel)){continue}
        if([IO.Path]::IsPathRooted($rel) -or $rel -match '(^|/)\.\.(/|$)'){throw '公開リストのパスが不正です'}
        $source=Join-Path $root $rel
        if(-not(Test-Path -LiteralPath $source -PathType Leaf)){throw "公開ファイルがありません: $rel"}
        if(((Get-Item -LiteralPath $source).Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0){throw 'リンクファイルは公開できません'}
        $dest=Join-Path $out $rel
        New-Item -ItemType Directory -Force (Split-Path $dest -Parent) | Out-Null
        Copy-Item -LiteralPath $source -Destination $dest
    }
    Compress-Archive -LiteralPath $out -DestinationPath "$out.zip"
    Write-Host "公開用ソース: $out.zip"
    return $out
} finally {Pop-Location}
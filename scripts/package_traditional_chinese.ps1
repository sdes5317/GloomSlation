[CmdletBinding()]
param(
    [string]$MelonLoaderZip
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$distDir = Join-Path $repoRoot 'dist'
$sourceDir = Join-Path $repoRoot 'Mods/GloomSlation/TraditionalChinese'
$releaseDll = Join-Path $repoRoot 'bin/Release/netstandard2.1/GloomSlation.dll'
$configTemplate = Join-Path $repoRoot 'Mods/GloomSlation/cfg.example.toml'
$fontLicense = Join-Path $repoRoot 'tools/FontBundle/Assets/Fonts/OFL.txt'
if (-not $MelonLoaderZip) {
    $MelonLoaderZip = Join-Path $repoRoot 'bin/deps/MelonLoader.x64.zip'
}

$categories = @('Areas', 'Credits', 'Dialogue', 'Documents', 'Items', 'Journal', 'Menus', 'Prompts')
$requiredFiles = @($MelonLoaderZip, $releaseDll, $configTemplate, $fontLicense)
$requiredFiles += @(@('README.md', 'font.bundle', 'fontMap.json') | ForEach-Object { Join-Path $sourceDir $_ })
$requiredFiles += @($categories | ForEach-Object { Join-Path $sourceDir $_ })
foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Required file is missing: $file"
    }
}

# Pin the official MelonLoader 0.7.3 x64 archive used for this package.
$expectedLoaderHash = '5B2B2F3D1CD42B59EC886C5BDC2663EDAE87A0097A4F4A8F58C0965A99DDA416'
$loaderHash = (Get-FileHash -LiteralPath $MelonLoaderZip -Algorithm SHA256).Hash
if ($loaderHash -ne $expectedLoaderHash) {
    throw "MelonLoader archive hash mismatch: $MelonLoaderZip"
}

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

$dateStamp = (Get-Date).ToString('yyyyMMdd')
$pattern = '^GloomSlation-TraditionalChinese-' + $dateStamp + '_(\d+)\.zip$'
$existingNumbers = @(
    Get-ChildItem -LiteralPath $distDir -File -Force | ForEach-Object {
        if ($_.Name -match $pattern) {
            [int]$Matches[1]
        }
    }
)

$nextNumber = 1
if ($existingNumbers.Count -gt 0) {
    $nextNumber = ($existingNumbers | Measure-Object -Maximum).Maximum + 1
}

$zipName = "GloomSlation-TraditionalChinese-${dateStamp}_${nextNumber}.zip"
$zipPath = Join-Path $distDir $zipName
if (Test-Path -LiteralPath $zipPath) {
    throw "Target package already exists: $zipPath"
}

$stageDir = Join-Path $distDir ("stage_traditional_chinese_{0}" -f [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stageDir | Out-Null

try {
    Expand-Archive -LiteralPath $MelonLoaderZip -DestinationPath $stageDir
    foreach ($loaderPart in @('MelonLoader', 'version.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $stageDir $loaderPart))) {
            throw "MelonLoader archive is missing $loaderPart"
        }
    }

    $modDir = Join-Path $stageDir 'Mods'
    $languageDir = Join-Path $modDir 'GloomSlation/TraditionalChinese'
    New-Item -ItemType Directory -Path $languageDir -Force | Out-Null
    Copy-Item -LiteralPath $releaseDll -Destination (Join-Path $modDir 'GloomSlation.dll')
    Copy-Item -LiteralPath $configTemplate -Destination (Join-Path $modDir 'GloomSlation/cfg.toml')
    foreach ($name in ($categories + @('font.bundle', 'fontMap.json'))) {
        Copy-Item -LiteralPath (Join-Path $sourceDir $name) -Destination (Join-Path $languageDir $name)
    }
    Copy-Item -LiteralPath $fontLicense -Destination (Join-Path $languageDir 'OFL.txt')
    Copy-Item -LiteralPath (Join-Path $sourceDir 'README.md') -Destination (Join-Path $stageDir 'README.md')

    $packageContents = @(Get-ChildItem -LiteralPath $stageDir | ForEach-Object FullName)
    Compress-Archive -LiteralPath $packageContents -DestinationPath $zipPath
    Write-Host "Created $zipPath"
}
finally {
    # Delete only the unique staging directory created by this invocation.
    $resolvedDist = (Resolve-Path -LiteralPath $distDir).Path.TrimEnd('\', '/')
    $resolvedStage = (Resolve-Path -LiteralPath $stageDir).Path
    if (-not $resolvedStage.StartsWith($resolvedDist + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove a staging directory outside dist: $resolvedStage"
    }
    Remove-Item -LiteralPath $resolvedStage -Recurse -Force
}

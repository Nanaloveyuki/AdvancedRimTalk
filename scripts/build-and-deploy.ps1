[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$GameModPath = "D:\Appdata\Steam\steamapps\common\RimWorld\Mods\AdvancedRimTalk",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$expectedPackageId = "advancedrimtalk.prompt"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$projectPath = Join-Path $repoRoot "AdvancedRimTalk.csproj"
$sourceAbout = Join-Path $repoRoot "About\About.xml"
$sourceLanguages = Join-Path $repoRoot "Languages"
$sourceAssembly = Join-Path $repoRoot "tmp\build\AdvancedRimTalk.dll"
$sourcePdb = Join-Path $repoRoot "tmp\build\AdvancedRimTalk.pdb"
$sourceDocs = Join-Path $repoRoot "docs"

$targetFull = [System.IO.Path]::GetFullPath($GameModPath).TrimEnd([char[]]"\/")
$modsRoot = Split-Path -Parent $targetFull
if (-not (Test-Path -LiteralPath $modsRoot -PathType Container)) {
    throw "RimWorld Mods directory not found: $modsRoot"
}

$resolvedModsRoot = (Resolve-Path -LiteralPath $modsRoot).Path.TrimEnd([char[]]"\/")
$targetParent = [System.IO.DirectoryInfo]::new($targetFull).Parent.FullName.TrimEnd([char[]]"\/")
if (-not [string]::Equals($resolvedModsRoot, $targetParent, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "GameModPath must be a direct child of the RimWorld Mods directory: $resolvedModsRoot"
}

if ([string]::Equals($targetFull, $repoRoot.TrimEnd([char[]]"\/"), [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Deployment target must not be the source repository."
}

if (Get-Process -Name @("RimWorldWin64", "RimWorldWin64Steam", "RimWorldWin", "RimWorld") -ErrorAction SilentlyContinue) {
    throw "RimWorld is running. Exit the game before deploying."
}

foreach ($requiredFile in @($projectPath, $sourceAbout, (Join-Path $repoRoot "README.md"), (Join-Path $repoRoot "LICENSE"))) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "Required source file not found: $requiredFile"
    }
}

if (-not (Test-Path -LiteralPath $sourceLanguages -PathType Container)) {
    throw "Language directory not found: $sourceLanguages"
}
if (-not (Test-Path -LiteralPath $sourceDocs -PathType Container)) {
    throw "Documentation directory not found: $sourceDocs"
}

try {
    [xml]$sourceMetadata = Get-Content -LiteralPath $sourceAbout -Raw
}
catch {
    throw "Invalid source About.xml: $sourceAbout"
}

if ($sourceMetadata.ModMetaData.packageId -ne $expectedPackageId) {
    throw "Source About.xml has an unexpected packageId."
}

if (Test-Path -LiteralPath $targetFull) {
    $targetItem = Get-Item -LiteralPath $targetFull -Force
    if (($targetItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Refusing to deploy through a filesystem reparse point: $targetFull"
    }

    if (-not $targetItem.PSIsContainer) {
        throw "Deployment target is not a directory: $targetFull"
    }

    $targetAbout = Join-Path $targetFull "About\About.xml"
    if (Test-Path -LiteralPath $targetAbout -PathType Leaf) {
        try {
            [xml]$targetMetadata = Get-Content -LiteralPath $targetAbout -Raw
        }
        catch {
            throw "Invalid target About.xml: $targetAbout"
        }

        if ($targetMetadata.ModMetaData.packageId -ne $expectedPackageId) {
            throw "Existing target directory belongs to another Mod: $targetFull"
        }
    }
    elseif (@(Get-ChildItem -LiteralPath $targetFull -Force).Count -gt 0) {
        throw "Refusing to deploy into a non-empty directory without matching About.xml: $targetFull"
    }
}

if (-not $SkipBuild) {
    Write-Host "Building $Configuration..."
    & dotnet build $projectPath --configuration $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}

if (-not (Test-Path -LiteralPath $sourceAssembly -PathType Leaf)) {
    throw "Build output not found: $sourceAssembly"
}

$languageFiles = @(Get-ChildItem -LiteralPath $sourceLanguages -File -Recurse)
if ($languageFiles.Count -eq 0) {
    throw "No language files found under: $sourceLanguages"
}

$targetFiles = @(
    [pscustomobject]@{
        SourcePath = $sourceAssembly
        RelativePath = "Assemblies\AdvancedRimTalk.dll"
    }
)

foreach ($aboutFile in @(Get-ChildItem -LiteralPath (Split-Path -Parent $sourceAbout) -File -Recurse)) {
    $targetFiles += [pscustomobject]@{
        SourcePath = $aboutFile.FullName
        RelativePath = $aboutFile.FullName.Substring($repoRoot.Length).TrimStart([char[]]"\/")
    }
}

foreach ($rootFile in @("README.md", "LICENSE")) {
    $targetFiles += [pscustomobject]@{
        SourcePath = Join-Path $repoRoot $rootFile
        RelativePath = $rootFile
    }
}

if (Test-Path -LiteralPath $sourcePdb -PathType Leaf) {
    $targetFiles += [pscustomobject]@{
        SourcePath = $sourcePdb
        RelativePath = "Assemblies\AdvancedRimTalk.pdb"
    }
}

foreach ($languageFile in $languageFiles) {
    $relativeLanguagePath = $languageFile.FullName.Substring($sourceLanguages.Length).TrimStart([char[]]"\/")
    $targetFiles += [pscustomobject]@{
        SourcePath = $languageFile.FullName
        RelativePath = Join-Path "Languages" $relativeLanguagePath
    }
}

foreach ($docFile in @(Get-ChildItem -LiteralPath $sourceDocs -File -Recurse)) {
    $relativeDocPath = $docFile.FullName.Substring($sourceDocs.Length).TrimStart([char[]]"/\")
    $targetFiles += [pscustomobject]@{
        SourcePath = $docFile.FullName
        RelativePath = Join-Path "docs" $relativeDocPath
    }
}

foreach ($targetFile in $targetFiles) {
    $destination = Join-Path $targetFull $targetFile.RelativePath
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $destination) | Out-Null
    Copy-Item -LiteralPath $targetFile.SourcePath -Destination $destination -Force

    $sourceHash = (Get-FileHash -LiteralPath $targetFile.SourcePath -Algorithm SHA256).Hash
    $targetHash = (Get-FileHash -LiteralPath $destination -Algorithm SHA256).Hash
    if ($sourceHash -ne $targetHash) {
        throw "Deployment verification failed: $destination"
    }
}

Write-Host "Deployed AdvancedRimTalk $Configuration build to: $targetFull"
Write-Host "Verified $($targetFiles.Count) file(s) by SHA-256."

[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputDirectory,

    [Parameter()]
    [switch]$NoOverwrite
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$extensionRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent (Split-Path -Parent $extensionRoot)
$manifestPath = Join-Path $extensionRoot 'package.json'

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Extension manifest was not found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$name = [string]$manifest.name
$version = [string]$manifest.version
if ([string]::IsNullOrWhiteSpace($name) -or [string]::IsNullOrWhiteSpace($version)) {
    throw 'package.json must define non-empty name and version values.'
}

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'vscode'
}

$outputDirectoryPath = [System.IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Force -Path $outputDirectoryPath | Out-Null

$packageFileName = "$name-$version.vsix"
$packagePath = [System.IO.Path]::GetFullPath((Join-Path $outputDirectoryPath $packageFileName))
$outputDirectoryPrefix = $outputDirectoryPath.TrimEnd(
    [System.IO.Path]::DirectorySeparatorChar,
    [System.IO.Path]::AltDirectorySeparatorChar)
if (-not $packagePath.StartsWith($outputDirectoryPrefix + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write outside the output directory: $packagePath"
}

if (Test-Path -LiteralPath $packagePath) {
    if ($NoOverwrite) {
        throw "The output package already exists. Choose another directory or omit -NoOverwrite: $packagePath"
    }

    $existing = Get-Item -LiteralPath $packagePath
    if ($existing.PSIsContainer) {
        throw "The output path is a directory, not a package file: $packagePath"
    }

    Remove-Item -LiteralPath $packagePath -Force
}

$vsce = Get-Command vsce.cmd -ErrorAction SilentlyContinue
if ($null -eq $vsce) {
    $vsce = Get-Command vsce -ErrorAction SilentlyContinue
}

$npx = Get-Command npx.cmd -ErrorAction SilentlyContinue
if ($null -eq $npx) {
    $npx = Get-Command npx -ErrorAction SilentlyContinue
}

if ($null -eq $vsce -and $null -eq $npx) {
    throw 'Neither vsce nor npx was found in PATH.'
}

Push-Location $extensionRoot
try {
    if ($null -ne $vsce) {
        & $vsce.Source package `
            --out $packagePath `
            --no-dependencies `
            --allow-missing-repository
    }
    else {
        & $npx.Source --yes @vscode/vsce package `
            --out $packagePath `
            --no-dependencies `
            --allow-missing-repository
    }

    if ($LASTEXITCODE -ne 0) {
        throw "VSIX packaging failed with exit code $LASTEXITCODE."
    }
}
finally {
    Pop-Location
}

if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
    throw "VSIX packaging reported success but the package was not created: $packagePath"
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    $requiredEntries = @(
        'extension/package.json',
        'extension/language-configuration.json',
        'extension/README.md',
        'extension/syntaxes/arti.tmLanguage.json',
        'extension/syntaxes/arti-markdown-block.tmLanguage.json',
        'extension/syntaxes/arti-markdown-fence.tmLanguage.json'
    )

    foreach ($requiredEntry in $requiredEntries) {
        if ($entries -notcontains $requiredEntry) {
            throw "The VSIX is missing required entry: $requiredEntry"
        }
    }

    $licenseEntries = @(
        'extension/LICENSE',
        'extension/LICENSE.md',
        'extension/LICENSE.txt'
    )
    if (-not ($licenseEntries | Where-Object { $entries -contains $_ })) {
        throw 'The VSIX is missing a license entry.'
    }

    if ($entries | Where-Object { $_ -like 'extension/scripts/*' }) {
        throw 'Development packaging scripts must not be included in the VSIX.'
    }
}
finally {
    $archive.Dispose()
}

$package = Get-Item -LiteralPath $packagePath
Write-Output ("VSIX created: {0} ({1:N0} bytes)" -f $package.FullName, $package.Length)

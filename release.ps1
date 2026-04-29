param(
    [string]$Tag,
    [string]$Title,
    [string]$Notes,
    [string]$NotesFile,
    [string]$Repository = 'Foahh/PenguinTools',
    [switch]$Draft,
    [switch]$Prerelease,
    [switch]$SkipBuild,
    [switch]$AllowDirty,
    [switch]$Clobber
)

$ErrorActionPreference = 'Stop'

function Assert-Command {
    param([Parameter(Mandatory = $true)][string]$Name)

    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found on PATH."
    }
}

function Get-ProjectVersion {
    [xml]$commonProps = Get-Content (Join-Path $PSScriptRoot 'Common.props')
    return $commonProps.Project.PropertyGroup.Version
}

function Compress-PublishOutput {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path $Source -PathType Container)) {
        throw "Publish output '$Source' does not exist. Run release.ps1 without -SkipBuild first."
    }

    if (Test-Path $Destination) {
        Remove-Item $Destination
    }

    Compress-Archive -Path (Join-Path $Source '*') -DestinationPath $Destination
}

function Copy-ReleaseAsset {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    if (-not (Test-Path $Source -PathType Leaf)) {
        throw "Publish asset '$Source' does not exist. Run release.ps1 without -SkipBuild first."
    }

    Copy-Item -Path $Source -Destination $Destination -Force
}

Assert-Command 'git'
Assert-Command 'gh'
if (-not $SkipBuild) {
    Assert-Command 'dotnet'
}

if (-not $Tag) {
    $Tag = "v$(Get-ProjectVersion)"
}

if (-not $Title) {
    $Title = $Tag
}

if ($NotesFile -and $Notes) {
    throw 'Use either -Notes or -NotesFile, not both.'
}

if (-not $NotesFile -and -not $Notes) {
    $Notes = "Release $Tag"
}

$repoRoot = (Resolve-Path $PSScriptRoot).Path
Push-Location $repoRoot
try {
    if (-not $AllowDirty) {
        $status = git status --porcelain
        if ($status) {
            throw 'Working tree is dirty. Commit or stash changes, or pass -AllowDirty.'
        }
    }

    if (-not $SkipBuild) {
        $env:CI = 'true'
        & (Join-Path $repoRoot 'build.ps1')
        if ($LASTEXITCODE -ne 0) {
            throw 'Build failed.'
        }
    }

    $artifactRoot = Join-Path $repoRoot "artifacts\release\$Tag"
    if (Test-Path $artifactRoot) {
        Remove-Item $artifactRoot -Recurse -Force
    }
    New-Item -ItemType Directory -Path $artifactRoot | Out-Null

    $assetDefinitions = @(
        @{
            SourceFile = 'PenguinTools/bin/Release/net10.0-windows/publish/WinX64-FrameworkDependent-SingleFile-EmbeddedAssets/PenguinTools.exe'
            AssetName  = "PenguinTools.$Tag.exe"
        },
        @{
            SourceFile = 'PenguinTools.CLI/bin/Release/net10.0/publish/WinX64-SelfContained-SingleFile-EmbeddedAssets/PenguinTools.CLI.exe'
            AssetName  = "PenguinTools.CLI.$Tag.exe"
        },
        @{
            Source = 'PenguinTools.CLI/bin/Release/net10.0/publish/WinX64-SelfContained-SingleFile-ExternalAssets'
            AssetName = "PenguinTools.CLI.$Tag.external-assets.zip"
        }
    )

    $assetPaths = foreach ($asset in $assetDefinitions) {
        $destination = Join-Path $artifactRoot $asset.AssetName

        if ($asset.SourceFile) {
            $source = Join-Path $repoRoot $asset.SourceFile
            Write-Host "Copying $($asset.AssetName)..."
            Copy-ReleaseAsset -Source $source -Destination $destination
        }
        else {
            $source = Join-Path $repoRoot $asset.Source
            Write-Host "Compressing $($asset.AssetName)..."
            Compress-PublishOutput -Source $source -Destination $destination
        }

        $destination
    }

    $releaseExists = $false
    gh release view $Tag --repo $Repository *> $null
    if ($LASTEXITCODE -eq 0) {
        $releaseExists = $true
    }

    if ($releaseExists) {
        if (-not $Clobber) {
            throw "GitHub release '$Tag' already exists. Pass -Clobber to replace its assets."
        }

        Write-Host "Uploading assets to existing release $Tag..."
        gh release upload $Tag @assetPaths --repo $Repository --clobber
        if ($LASTEXITCODE -ne 0) {
            throw "Asset upload failed for release '$Tag'."
        }
    }
    else {
        $releaseArgs = @('release', 'create', $Tag)
        $releaseArgs += $assetPaths
        $releaseArgs += @('--repo', $Repository, '--title', $Title)

        if ($NotesFile) {
            $releaseArgs += @('--notes-file', $NotesFile)
        }
        else {
            $releaseArgs += @('--notes', $Notes)
        }

        if ($Draft) {
            $releaseArgs += '--draft'
        }

        if ($Prerelease) {
            $releaseArgs += '--prerelease'
        }

        Write-Host "Creating GitHub release $Tag..."
        gh @releaseArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Release creation failed for '$Tag'."
        }
    }

    Write-Host "Release assets are in $artifactRoot"
}
finally {
    Pop-Location
}

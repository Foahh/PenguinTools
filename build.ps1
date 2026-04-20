$ErrorActionPreference = 'Stop'

$publishTargets = @(
    # Desktop
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-SelfContained-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-EmbeddedAssets' },

    # CLI
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-SelfContained-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-SelfContained-SingleFile-ExternalAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-ExternalAssets' }
)

foreach ($target in $publishTargets) {
    Write-Host "Publishing $($target.Project) [$($target.Profile)]..."
    dotnet publish $target.Project -p:PublishProfile=$($target.Profile)

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for '$($target.Profile)'."
    }
}

if (-not $env:CI) { pause }

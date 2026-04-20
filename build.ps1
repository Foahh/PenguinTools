$ErrorActionPreference = 'Stop'

$publishTargets = @(
    # Desktop (WPF)
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-SelfContained-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-SelfContained-SingleFile-ExternalAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-SelfContained-MultiFile-ExternalAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-ExternalAssets' },
    @{ Project = 'PenguinTools/PenguinTools.csproj'; Profile = 'WinX64-FrameworkDependent-MultiFile-ExternalAssets' },

    # CLI
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-SelfContained-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-SelfContained-SingleFile-ExternalAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-SelfContained-MultiFile-ExternalAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-EmbeddedAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-FrameworkDependent-SingleFile-ExternalAssets' },
    @{ Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'; Profile = 'WinX64-FrameworkDependent-MultiFile-ExternalAssets' }
)

foreach ($target in $publishTargets) {
    Write-Host "Publishing $($target.Project) [$($target.Profile)]..."
    dotnet publish $target.Project -p:PublishProfile=$($target.Profile)

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for '$($target.Profile)'."
    }
}

pause

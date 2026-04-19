$ErrorActionPreference = 'Stop'

$publishTargets = @(
    @{
        Project = 'PenguinTools/PenguinTools.csproj'
        Profile = 'WinX64-Embedded-SelfContained'
    },
    @{
        Project = 'PenguinTools/PenguinTools.csproj'
        Profile = 'WinX64-Embedded-FrameworkDependent'
    },
    @{
        Project = 'PenguinTools/PenguinTools.csproj'
        Profile = 'WinX64-Zip-ExternalAssets'
    },
    @{
        Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'
        Profile = 'WinX64-Zip-SelfContained'
    },
    @{
        Project = 'PenguinTools.CLI/PenguinTools.CLI.csproj'
        Profile = 'LinuxX64-Zip-SelfContained'
    }
)

foreach ($target in $publishTargets) {
    Write-Host "Publishing $($target.Project) with profile $($target.Profile)..."
    dotnet publish $target.Project -p:PublishProfile=$($target.Profile)

    if ($LASTEXITCODE -ne 0) {
        throw "Publish failed for profile '$($target.Profile)'."
    }
}

pause

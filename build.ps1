cd External\muautils
.\build.ps1
cd ..\..\
dotnet publish -c Release -r win-x64 --self-contained false --artifacts-path ./Build
Read-Host -Prompt "Press Enter to continue"
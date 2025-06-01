cargo build --release --target x86_64-pc-windows-msvc --manifest-path External/manipulate-lib/Cargo.toml
dotnet publish -c Release -r win-x64 --self-contained false --artifacts-path ./Build
pause
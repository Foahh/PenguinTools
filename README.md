# PenguinTools

An all-in-one toolbox for converting custom assets for **CHUNITHM** (charts, music, jackets, stages, etc.).

## Usage

- 本工具使用方法请见 [中文 Wiki](https://github.com/Foahh/PenguinTools/wiki/%E4%B8%AD%E6%96%87)

## Disclaimer

This project is created solely for study and self-evaluation purposes.
It does not condone the piracy, operation, modification, or reverse engineering of CHUNITHM arcade games, or any
Sega-licensed games, as these actions may violate Japanese and international laws.
Is also does not condone the modification, redistribution, repurposing, or reverse engineering of UMIGURI and Margrete.

"CHUNITHM" is a trademark of SEGA Corporation. ® SEGA. All rights reserved.

"UMIGURI" and "Margrete" are software by inonote. © inonote.

## Contributing

Issues and pull requests are welcome. A short guide:

### Prerequisites

- **Git** with submodule support.
- **.NET SDK** matching [`global.json`](global.json).
- **Windows** if you want to build or run the **WPF desktop app** (`PenguinTools`). It targets `net10.0-windows`.
- The **CLI** (`PenguinTools.CLI`) targets plain `net10.0` and can be built on Linux and macOS as well.

### Getting the code

Clone the repository **with submodules** (the solution references vendored libraries under `External/`):

```bash
git clone --recurse-submodules https://github.com/Foahh/PenguinTools.git
cd PenguinTools
```

If you already cloned without submodules:

```bash
git submodule update --init --recursive
```

### Build

#### 1. `mua` ([muautils](External/muautils))

[`PenguinTools.Infrastructure`](PenguinTools.Infrastructure/PenguinTools.Infrastructure.csproj) embeds the **muautils** CLI (`mua`) for audio/image work. That binary is **not** checked in: you build it inside the `External/muautils` submodule and install it into the directory this repo expects:

| Platform | Output path (relative to `External/muautils/`) |
|----------|------------------------------------------------|
| Windows  | `cmake-build-vcpkg/Release/mua.exe` |
| Linux / macOS | `cmake-build-vcpkg/mua` |

Use a **recursive** submodule checkout so muautils’s own dependencies (for example **bc7enc_rdo**) are present. Full toolchain requirements (CMake, **vcpkg**, FFmpeg, libvips, OpenMP, etc.) and configure commands are in [`External/muautils/README.md`](External/muautils/README.md). Convenience scripts: [`build.sh`](External/muautils/build.sh) (requires `VCPKG_ROOT`) and [`build.ps1`](External/muautils/build.ps1) on Windows.

#### 2. PenguinTools (.NET)

From the repository root:

```bash
dotnet restore PenguinTools.slnx
dotnet build PenguinTools.slnx -c Release
```

- **CLI output**: `PenguinTools.CLI/bin/<Configuration>/net10.0/` (executable `PenguinTools.CLI` or `PenguinTools.CLI.exe` on Windows).
- **GUI output**: `PenguinTools/bin/<Configuration>/net10.0-windows/` (Windows only).

### Before you open a PR

- Prefer **small, focused changes** with a clear explanation in the PR description.
- Match existing style; the repo includes [`.editorconfig`](.editorconfig) for formatting and naming consistency.
- If you are fixing a bug, mention how to reproduce it or what you verified.
- For larger features, opening an **issue first** helps align on scope and avoid rework.

### Licensing

By contributing, you agree your contributions are under the same license as the project ([MIT](LICENSE)).
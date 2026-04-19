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

Issues and pull requests are welcome.

### Prerequisites

- Git with submodule support
- .NET SDK matching [`global.json`](global.json)
- Windows if you want to build or run the WPF desktop app (`PenguinTools`) — it targets `net10.0-windows`
- The CLI (`PenguinTools.CLI`) targets plain `net10.0` and builds on Linux and macOS too

### Getting the code

Clone with submodules (the solution references vendored libraries under `External/`):

```bash
git clone --recurse-submodules https://github.com/Foahh/PenguinTools.git
cd PenguinTools
```

If you already cloned without them:

```bash
git submodule update --init --recursive
```

### Build

#### 1. `mua` ([muautils](External/muautils))

[`PenguinTools.Infrastructure`](PenguinTools.Infrastructure/PenguinTools.Infrastructure.csproj) embeds the muautils CLI (`mua`) for audio/image work. It's not checked in — build it from the `External/muautils` submodule and install it into the directory the project expects.

#### 2. PenguinTools (.NET)

```bash
dotnet restore PenguinTools.slnx
dotnet build PenguinTools.slnx -c Release
```

Output lands in `PenguinTools.CLI/bin/<Configuration>/net10.0/` for the CLI and `PenguinTools/bin/<Configuration>/net10.0-windows/` for the GUI.

### Before opening a PR

Keep changes small and focused. Match the existing style — there's an [`.editorconfig`](.editorconfig) for formatting and naming. If you're fixing a bug, mention how to reproduce it. For anything larger, open an issue first so we can agree on scope before you put in the work.

### Licensing

Contributions are under the same license as the project ([MIT](LICENSE)).

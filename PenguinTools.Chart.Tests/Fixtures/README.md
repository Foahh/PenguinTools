# Fixtures

Integration tests use paired `.ugc` / `.mgxc` files under `PenguinTools.Chart.Tests/Assets` (same base name, e.g. `Sample.ugc` and `Sample.mgxc`).

Add or replace samples there locally; paths are resolved from the test project directory at runtime (`ChartTestPaths.AssetsDirectory`), not from machine-specific locations.

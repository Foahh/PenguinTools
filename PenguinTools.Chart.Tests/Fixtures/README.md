# Fixtures

Integration tests pull UGC/MGXC sample pairs from `/home/fn/Chunithm/Finished/<song>/MASTER.{ugc,mgxc}`.
They are referenced by absolute path (not copied into the repo) — the files are licensed charts
the developer owns locally. Tests that can't find them are skipped, not failed.

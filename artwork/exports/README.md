# Exports

Final, game-ready PNGs go here, named exactly as in `docs/art-asset-list.md` (e.g. `computer-core.png`).

- Every PNG here is copied into the built mod at `assets/sprites/` automatically. Rebuild after adding one.
- Anything missing shows as a generated placeholder in game.
- Check your exports: run `dotnet test` from the repo root. It fails if a file has the wrong size, frame count, name or colour type.

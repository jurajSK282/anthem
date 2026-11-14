# Project state (update at the end of every work block)

_Last updated: 2026-11-14_

## Current focus
Atmosphere and look of the first area ("Emberwood"), then first-person combat. Iterative, no fixed end scope.

## What exists
- **Emberwood scene** — generated entirely by editor scripts in `Assets/_Project/Editor/Emberwood/`
  (`EmberwoodBuilder` orchestrates; Atmosphere, Meshes, Scatter, Terrain, Textures). Menu `Anthem/Build Emberwood`,
  safe to re-run. Autumn-ember forest: fog, glow bulbs, fireflies, haze/leaf particles, procedural 64-128 px textures
  (placeholders, meant to be replaced by Midjourney tiles keeping the same names).
- `AnthemPipelineSetup.cs` — URP/pipeline setup.
- **PoC** (`Assets/PoC/`) — gnoll model + large Mixamo "sword and shield" animation set, AC_Gnoll, texture
  postprocessor. Not yet wired into Emberwood.
- **Models/Watchtower** — Blender-built tower (`Tools/Blender/build_watchtower.py`) + `WatchtowerSceneSetup.cs`.
- FP controller exists in the earlier scene (`Assets/scene.unity`); not yet in Emberwood.

## Not done yet
Combat, weapon viewmodel, enemies in Emberwood, low-res render texture / posterize shader (verify what's in place),
audio, UI.

## Decisions
- Look: low-poly Gothic 2 style, atmosphere over detail (fog, light, glow, palette).
- Complex assets from user's Midjourney pipeline; Blender only for simple assets.
- Gameplay systems deferred until scene/walk feels right.
- Old Soulslike plan (`guide.txt`) was dropped on 2026-11-14.

## Next
1. Next visual pass on Emberwood (user to steer).
2. Add FP controller + weapon viewmodel to Emberwood, then a grey-box combat test with a dummy enemy.

## Open questions
- Final game type/scope (Gothic-style RPG vs. combat-focused) — undecided on purpose.

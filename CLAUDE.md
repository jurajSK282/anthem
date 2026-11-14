# Anthem — retro first-person dark-fantasy RPG (Unity 6000.6, URP)

Read `docs/STATE.md` first. Do not scan code or assets unless the task needs it.

## Vision
Old-school first-person, Gothic 1/2 + Dark Messiah mood. Atmosphere (light, fog, effects, palette) beats detail.
Scope is open: build the scene, atmosphere and combat iteratively. No end-game plan yet; don't add systems
(inventory, quests, dialogue, multiplayer, open world) unless asked. `.github/copilot-instructions.md` is old
Copilot-era scope; ignore it.

## Workflow rules
- One small pass per turn -> screenshot -> short report -> stop for feedback.
- Keep answers short. Edit only what's needed. Don't re-read files already known from STATE.md.
- Visuals first; gameplay systems wait until the scene/walk feels right.
- Complex assets come from the user (Midjourney -> TRELLIS/Tripo -> Blender cleanup -> Mixamo). I build only simple
  ones (props, architecture, weapons) in Blender.
- End each work block: update `docs/STATE.md`, commit, push (skill: `checkpoint`).

## Editors (both run locally when open)
- Unity: `python Tools/Unity/umcp.py tools|schema <tool>|call <tool> '<json>'` (server :8080).
  Common: `refresh_unity` (then wait ~25 s for domain reload), `execute_menu_item`, `read_console`,
  `execute_code`, `manage_camera` screenshot -> `Assets/Screenshots/` (git-ignored).
  Console `NoSubscription ... generators.ai.unity.com` errors are noise.
- Blender 5.2: socket `127.0.0.1:9876`; scripts live in `Tools/Blender/`.
- `unity` CLI is not on PATH; `gh` is not installed.

## Layout
- `Assets/_Project/Editor/` — editor scripts that generate scenes. Menu `Anthem/Build Emberwood` is re-runnable.
- `Assets/_Project/Art/`, `Scenes/` — generated/hand-made content.
- `Assets/PoC/`, `Assets/Models/`, `Assets/Editor/` — earlier proofs of concept (gnoll + Mixamo anims, watchtower).
- `Tools/` — MCP helper and Blender scripts. `docs/` — STATE.md and notes.

## Visual constraints
- Low poly (~2006): props a few hundred tris, characters 2.5-4k.
- Textures 64-256 px, Point filter, no compression (PoC `TextureImportPostprocessor` enforces this).
- Low-res render (~480x270 / 640x360) point-upscaled; fog, colour grade, limited palette.
- Material defaults for imports: Render Face Both, Smoothness 0.
- Humanoids share one Mixamo Humanoid rig and one AnimatorController. Weapons are separate, attached at runtime.

## Code style
Small single-purpose components (~150 lines max), interfaces like `IDamageable`, ScriptableObjects for data,
null-check serialized refs in `Awake`, no allocations in `Update`.

## Git
Branch `main`, remote `origin` (github.com/jurajSK282/anthem). Small commits, one per visual/gameplay pass.
Binary assets go through Git LFS (`.gitattributes`). Never commit `Library/`, `Temp/`, `Assets/Screenshots/`.

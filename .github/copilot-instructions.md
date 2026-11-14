# Unity Project Instructions (Retro First-Person Soulslike)

- **Engine & Render Pipeline:** Unity 6 / 2022 LTS with Universal Render Pipeline (URP), C#.
- **Architecture:** Single-responsibility micro-components (max 100-150 lines per file). Use Interfaces (`IDamageable`, `IInteractable`), State Pattern for AI, and `ScriptableObject` for data-driven design (`WeaponData`, `EnemyData`).
- **Performance & Style:** Gothic 1/2 + Boltgun retro aesthetic. Point-filtered textures, stepped 12-FPS animations where applicable, zero runtime allocations in `Update()`.
- **Token Saving Rules:**
  - Be concise. Do not write long explanations unless asked.
  - When editing existing scripts, only modify the necessary methods.
  - Always include `RequireComponent` where applicable and null-check serialized references in `Awake()`/`Start()`.

## Scope (Strictly Excluded)
- No open world, no multiplayer, no player armor visuals (first-person), no dialogue trees, no quest logs.
- Story is told through environment and short cryptic lines. Do not add features outside the plan without asking.
- Target: Hub + 3 linked dungeons, 3 weapons, 5 enemy types, 2-3 bosses.

## Project Structure
- `Assets/_Project/Scripts/Core/`: `GameManager.cs`, `SaveSystem.cs` (world state, unlocked shortcuts, souls).
- `Assets/_Project/Scripts/Combat/`: `IDamageable.cs`, `Hitbox.cs`, `ParrySystem.cs`.
- `Assets/_Project/Scripts/Player/`: `FirstPersonMotor.cs`, `PlayerStamina.cs`, `PlayerCombat.cs`.
- `Assets/_Project/Scripts/AI/`: `EnemyStateMachine.cs`, `SteppedAnimator.cs`.
- `Assets/_Project/Scripts/Data/`: `WeaponData.cs`, `EnemyData.cs` (ScriptableObjects).
- `Assets/_PoC/`: temporary proof-of-concept scripts only (`SimpleFPController.cs`, `PoCGnollAI.cs`).

## Gameplay Rules
- Combat: `IDamageable` interface for all damageable objects. Stamina drains on block, parry window opens enemy for critical hit, i-frames on dodge/dash.
- Hit-stop: freeze animation for ~0.06s on hit. Screen shake via Cinemachine Impulse.
- Enemy AI: State Pattern. States: Idle -> Patrol -> Chase -> Telegraph -> Attack -> Recovery -> Staggered. Telegraph must have a clear visual/sound cue.
- Data-driven: new weapon or enemy = new ScriptableObject asset, never new code. Attack speed, stamina cost, poise damage, parry window are Inspector values.
- Soulslike loop: bonfire rests (refill healing, respawn enemies, save). On death, drop souls; recover them at the death location.

## Visual & Animation Rules
- Style: Gothic 1/2 + Boltgun retro. Low-poly, smooth geometry, details painted into textures.
- All textures: `Filter Mode = Point (no filter)`, `Compression = None` (enforce via AssetPostprocessor editor script).
- Stepped animation: 12 FPS update for enemies (`SteppedAnimator.cs`), not 60 FPS.
- Render: low-res render texture (640x360) upscaled with point filter, color quantization/dithering post-process.
- Humanoid characters: all must use the same Mixamo Humanoid rig so one `AnimatorController` serves multiple enemies.
- Weapons and shields are separate models attached at runtime to `RightHand` (weapon) and `LeftForeArm` (shield). Never merged into character meshes.
- Materials for imported models: `Render Face = Both`, `Smoothness = 0`.

## Asset Pipeline (Free-Tier)
1. Midjourney character sheet: A-pose, white background, `--ar 3:4`, no weapon/shield/cape in the character image.
2. Add white padding around the figure before 3D generation.
3. TRELLIS (HuggingFace) or Hunyuan3D-2 -> `.glb`.
4. Blender: weld by distance, `Shade Flat` optional, Decimate ratio ~0.05-0.1 with `Delimit: UVs`, export `.fbx` with embedded textures.
5. Mixamo: auto-rig, `No Fingers (25)` skeleton, download T-pose model and animations `Without Skin`.
6. Unity: set `Animation Type = Humanoid` on all FBX files.

## Editing Rules for Agents
- Touch only the script or method needed. Do not refactor unrelated code.
- Keep each script under 150 lines. Split into micro-components if larger.
- Do not generate markdown documentation unless explicitly requested.

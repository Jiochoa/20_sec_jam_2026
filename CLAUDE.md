# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

**A jam skeleton, not a game.** It is the reusable plumbing extracted from *Crazy Cat Lady*
(Pirate Game Jam) on 2026-07-30: character controller, managers, sound, dialogue, level
management, UI and a working URP + WebGL setup, with that game's cats, enemies and level
content stripped out.

Treat everything here as a starting point to be renamed and rebuilt on, **not** as a codebase
with users. When a jam starts, the first job is usually deleting more, not adding.

### The one scene

`Assets/Scenes/Main.unity` is the working scene and the only one in Build Settings. It carries the
camera, lighting, the post-processing Volume, the manager objects, the canvases and the character —
that wiring is most of this skeleton's value and is tedious to rebuild, so **build on it rather than
starting a new scene**. Reference demos live in `Assets/Scenes/DemoScenes/`.

The scene deliberately keeps some *Crazy Cat Lady* content as walkable placeholder: the house
geometry (`Assets/Art/Models/Blender/Blend_{Entrance,Hallway_1,Kitchen_1}.fbx`), empty `cats` /
`enemies` / `Stage` container objects, and HUD/win-screen labels still named `Cats Rescued` and
`Enemies Defeated` with hardcoded `/24` and `/58` totals. None of it is wired to anything — the
code behind those counters is generic (`GameStats.ItemsCollected`). Replace it as the new game
takes shape. Same for the four placeholder audio files in `Assets/Art/Audio/` and the three
`*Placeholder.mat` materials in `Assets/Materials/Demo 1/`.

**Missing Prefab is not the same as a missing script.** If *every* object in the scene shows
`(Script)` with an unresolvable reference — including ones you never touched, like `GameManager` and
`DefaultCharacter` — that is a **compile error**, not scene damage: the assembly failed to build, so
no MonoBehaviour can load. Read the Console's first error rather than the Inspector. See the deleting-a-type
gotcha below.

## Environment

- **Unity 6000.5.1f1** (Unity 6.5), **Universal Render Pipeline (URP 17.5)**, using
  **`UniversalRendererData`** (`Assets/Settings/PC_Renderer.asset`) — the 3D renderer, deliberately
  *not* the 2D Renderer, because this setup mixes 3D geometry with sprites and the 2D Renderer
  would strip proper 3D lighting. Consequence: **URP 2D Lights are unavailable**; use ordinary
  Point/Spot lights plus Bloom for glow.
- **Glow/post-processing works, and needs all four of these**: HDR on (`m_SupportsHDR: 1`),
  `postProcessData` assigned on `PC_Renderer`, a Volume in the scene using
  `Assets/Plugins/AllIn1SpriteShader/Demo/PostProcessingProfiles/Urp/AllIn1PostProcessing-URP.asset`,
  and `m_RenderPostProcessing: 1` on the camera. The camera tick and the HDR-intensity requirement
  (emissive >1, or AllIn1's glow on sprites) are the two that silently produce "no glow" when missed.
- **WebGL builds must use the `Responsive` template** (`Assets/WebGLTemplates/Responsive/index.html`;
  select it at Player Settings > Player > Resolution and Presentation > WebGL Template, under PROJECT).
  Unity's stock `APPLICATION:Default` hard-codes the canvas to the Player Settings size in *pixels*,
  so an itch.io embed smaller than that **crops the game instead of scaling it** — the symptom is
  seeing only a corner of the screen. This template sizes the canvas at `100%`/`100%` and lets Unity
  match the render target to the DOM size. It also sets `config.devicePixelRatio = 1` (HiDPI shading
  costs frame rate and buys nothing on pixel art) and calls `canvas.focus()` on load, without which
  the player must click the iframe before keyboard input reaches the game.
- **`productName` is what players see**, not the folder name: it feeds `{{{ PRODUCT_NAME }}}` in the
  WebGL template (browser tab title, loading text, and the footer bar in Unity's stock template).
  Set it per jam. It is currently `Jam Skeleton`; `companyName` is `DefaultCompany`, which is harmless
  on web (it only decides `persistentDataPath`).
- **The UI scales with the window already** — all five canvas prefabs (`Assets/Prefabs/Canvases/`)
  carry a `CanvasScaler` set to **Scale With Screen Size** (`m_UiScaleMode: 1`), reference resolution
  **800×600**, Match Width Or Height with **`m_MatchWidthOrHeight: 0`** (width only), and no scene
  instance overrides it. Beware the field that makes this look otherwise: **`m_ScaleFactor: 1` applies
  only in Constant Pixel Size mode**, so it sits at its default under every mode and is not evidence of
  that mode — read `m_UiScaleMode` instead. Practical effects: scale is `screenWidth / 800`, so at a
  960-wide player everything is **1.2×**; and keying on width alone means a wide, short window can push
  centre-anchored elements off the top and bottom, where `m_MatchWidthOrHeight: 0.5` behaves better.
  Set the reference resolution to whatever the new game's design size is, then check the layout at 1.0×.
- **This project deliberately lives outside OneDrive.** The parent project was inside it and paid for
  that in file locks and `Library/` churn. Keep it that way.
- Hot Reload (SingularityGroup) is installed — script changes apply without a domain reload while the
  editor runs. It **cannot introduce a new type** (new class, new enum) and generally cannot add new
  members; it logs something like `Adding enum requires recompiling in unity` and Unity does a forced
  synchronous recompile instead. Transient "type does not exist" errors right after adding a new type
  are usually this, not a real break — refocus the editor.
- **Asset serialization is Force Text** (`m_SerializationMode: 2`). Prefabs, scenes and materials are
  plain YAML: hand-editable, diffable, and Unity will not re-save them as binary behind you.
- **Aseprite source art is gitignored** (`*.aseprite`, `*.ase`, and their `.meta`). Any art added as
  `.aseprite` lives only locally, so a fresh clone will have missing sprite references. Every *other*
  `.meta` under `Assets/` and `Packages/` **must** be committed with its asset — they carry the GUIDs
  all prefab/scene references resolve through.
- Everything builds and runs through the Unity Editor; there is no CLI build script.

## Architecture: Character Editor Package asset

The bulk of `Assets/` is the "Character Editor Package" asset — an extendable 2D/2.5D character
controller. Its code lives directly in `Assets/Code/` (not under `Assets/CharacterEditorPackage/`,
which only holds the ReadMe).

- **Custom physics, not Rigidbody-driven.** Characters use a custom capsule solver built on **3D
  colliders** (`Assets/Code/Physics/`, especially `CapsuleController/`). `ControlledCollider` /
  `ControlledCapsuleCollider` do cast-based movement. Concave mesh colliders can break it.
- **Controller loop:** `CharacterControllerBase` (`Assets/Code/EntityControllers/`) runs everything in
  `FixedUpdate`: updates collision context, then delegates to the highest-priority applicable **ability
  module**, falling back to default movement. `GroundedCharacterController` is the player-style one.
- **Ability modules are prefabs, not components on the character.** Each ability (jump, dash,
  wall-slide… in `Assets/Code/AbilityModules/`) subclasses `AbilityModule`; instances are prefabs under
  `Assets/Prefabs/AbilityModules/` assigned to a character's `AbilityModuleManager` list. Priority ints
  decide which module wins each tick. New abilities follow this pattern.
- **Passive modules** are the trick worth knowing: set `IsApplicable` to always false and do the work in
  `InactiveUpdateModule`, which the manager ticks every FixedUpdate on non-active modules. Such a module
  never competes with movement. The Free/Instant interaction paths use this.
- **Tick order per FixedUpdate:** `UpdateBestApplicableModule()` → `UpdateInactiveModules()` →
  `FixedUpdateCurrentModule()` → `PostFixedUpdateModuleSelection()` (which calls
  `UpdateBestApplicableModule` **again**). So `IsApplicable()` runs **twice per tick** — never consume
  an input inside it. See `InteractingModule`'s frame-stamped consume for the pattern that works.
- **Input wrapper:** modules read input by name via `PlayerInput`/`ButtonInput`/`DirectionInput`
  (`Assets/Code/Input/`). See the Input gotcha below.
- **Character prefabs:** `Assets/Prefabs/CharacterControllers/` (`ClassicCharacter` is the sprite-based
  one). The editor window at **Window > Character Editor** edits tuning, abilities and input bindings;
  changes made there during play mode do not persist.

## The reusable game layer

Everything below was written on top of the asset rather than inside it — the "listen, don't modify"
pattern — so an asset update cannot silently clobber it.

- **Managers** (`Assets/Code/Managers/`, singletons): `GameManager` owns a `GameState` enum state machine
  (Main_Menu → Ready_Game → Intro_Cutscene → Playing ⇄ Pause_Menu; win → Outro_Cutscene → GameOver).
  Menus pause via `Time.timeScale = 0`, which is safe because the character runs in FixedUpdate.
  Escape toggles pause. **Restart / return-to-menu do a full scene reload** (the only reset that restores
  despawned objects), with a static `reloadPending`/`stateAfterReload` carrying the target state across it.
  `MenuManager` toggles the menu canvases (assign in Inspector; the scene needs both managers on *active*
  objects, since `GameManager.Start` calls `MenuManager.Instance`). UI buttons wire to `GameManager.On*`
  hooks (OnStartGame/OnResumeGame/OnRestartGame/OnReturnToMainMenu/OnQuitGame).
- **Run stats:** `GameStats` tracks two counters and a run timer, and is a **self-bootstrapping singleton**
  (`Get()` creates one if the scene has none — nothing to place or assign). It deliberately holds no static
  state, so a scene reload zeroes it, while in-scene level switches carry totals across levels. The counters
  are named `EnemiesDefeated` and `ItemsCollected` as neutral placeholders — **rename them to whatever this
  game counts**, and note `UIManager` reads them by name. `OnStatsChanged` lets a HUD redraw without polling;
  `ElapsedTime` deliberately has no event because it changes every frame, so the HUD polls it instead.
- **`UIManager`** draws the hearts (event-driven off `PlayerLives.OnLivesChanged`), fills win-screen totals
  via `ShowFinalStats()`, and drives the live HUD via `UpdateStatsHUD()`. It has **two independent sets of
  optional text fields** — "Game Over Stats" and "HUD" — and any left unassigned are skipped, so you can ship
  either, both, or neither. These are **legacy `UnityEngine.UI.Text`, not TextMeshPro**: the project's fonts
  are legacy-only, so UI text must use the old Text component. The two item-counter fields carry
  `[FormerlySerializedAs]` from the rename out of cat-specific naming — keep those attributes if you rename again.
- **Sound:** `SoundManager` does music + one-shot SFX, everything addressed by the top-level **`SoundId`** and
  **`MusicTrack`** enums rather than by clip reference, so no caller holds an `AudioClip`.
  `SoundManager.Play(SoundId.Jump)` **no-ops when there is no SoundManager in the scene** and when an id has no
  clip yet — that is what makes it safe to ship with placeholder audio. Unlike the self-bootstrapping singletons
  it carries serialized clip data, so it must be a real object in the scene; it builds its own two `AudioSource`
  children in `Awake`. `Reset()` and a context-menu **"Add Missing Entries"** pre-fill one row per enum value —
  **rerun it after adding an id**. Add new ids at the **END** of the enum: the value is what gets serialized, so
  inserting one in the middle silently re-points every clip after it. Details: a per-id 0.05s repeat guard
  swallows double fires; `m_PitchVariation` keeps repeats from sounding identical; music fades on **unscaled**
  time and stops with the pause menu (`AudioSource` ignores `timeScale`), while SFX are not paused so menu clicks
  still sound. `PlayerSounds.cs` (`Assets/Code/Game/ExtraPlayerScripts/`) does Jump/Land/Footstep by *listening* —
  jump via `GroundedCharacterController.OnJump`, landing by filtering collisions for hard upward-normal impacts,
  and footsteps by accumulating **distance travelled** rather than running a timer, so walking and sprinting step
  at their own rates off one tunable.
- **Interaction & dialogue:** `InteractingModule.cs` + `Interactable.cs` (`Assets/Code/Game/Interaction/`) +
  `DialogueUI.cs`. An `Interactable` (collider + component) has an `InteractionType`: **`Locked`** freezes the
  player for the conversation and is the only type that becomes the active ability; **`Free`** and **`Instant`**
  are handled passively so they never compete with movement. Detection is `OverlapSphereNonAlloc` +
  `GetComponentInParent<Interactable>()` with `QueryTriggerInteraction.Collide` and a `~0` mask — deliberately
  *not* a dedicated layer, since this project has none free. `DialogueUI` is a self-bootstrapping singleton that
  builds its own Canvas + box + text in code when `m_Root` is unassigned, so an `Interactable` works with zero
  scene setup. Since legacy Text has no `maxVisibleCharacters`, the typewriter writes the untyped remainder in
  `<color=#00000000>` instead of truncating — that keeps line breaks from jumping as it types, at the cost of
  not tolerating rich-text tags inside a page. The typewriter runs on **scaled** time so pausing stops it.
  **Prompt:** each `Interactable` shows a two-stage floating sprite — `m_NearbySprite` (a "?") from
  `m_NoticeRadius` in, swapping to `m_InReachSprite` (the interact key) once actually in reach. The switch
  distance is not a field on the Interactable: `InteractingModule` publishes its own `m_DetectionRadius` as the
  static `InteractingModule.ReachRadius`, so the key sprite and the button can never disagree. `Interact` is
  already bound to E in `InputSystem_Actions` from the Unity template.
- **Level management** (`Assets/Code/Game/LevelManagement/`): `DeathTrigger`, `RespawnPointSetter`,
  `InSceneLevel`/`InSceneLevelSwitcher` (teleports the player between in-scene "levels" and snaps the camera),
  and `LevelTransition`. On top of those, `ScreenFader` + `LevelFadeTransition` wrap a switch in a fade:
  a player-filtered trigger that fades to black, switches while hidden, then fades in. `ScreenFader` is
  self-bootstrapping (builds its own overlay canvas in code) and fades on **unscaled** time so it survives a
  paused `timeScale`. Prefab: `Assets/Prefabs/Level/LevelFadeTrigger.prefab`. `GameEndTrigger` fires the win
  state and goes on the final level's end door. `InSceneLevelSwitcher`'s debug R-key/level buttons are
  suppressed unless state == Playing (or there is no GameManager, so demo scenes still work).
- **Player lives:** `PlayerLives.cs` — 3 lives, invulnerability window (which also rate-limits contact damage),
  blink, knockback, and on 0 a respawn at the checkpoint with fresh lives. UI hooks via `OnLivesChanged` /
  `OnAllLivesLost` UnityEvents. Knockback pushes horizontally away from the damage source plus a small upward
  pop via the capsule controller's `SetVelocity` — the airborne moment is what makes the push land, since
  grounded movement would fight it. Must be added to the character object manually.
- **Editor tooling** (`Assets/Code/Editor/`): `StateNameAnimatorEditor` is a reusable base for inspectors that
  map "what am I doing" enums onto Animator state names, giving you dropdowns of the assigned controller's
  actual states plus a "Match States To Controller" keyword-guess button. Subclass it and declare the reference
  fields and keyword table. This exists because the **Aseprite Importer** names generated states after the
  file's *tags* (lowercase, arbitrary), which no prefix+suffix scheme can match. Only layer 0's top-level states
  are offered — `Animator.HasState` cannot match states nested in a sub-state-machine by bare name.
  `ReserializeAssetsTool` (Tools > Reserialize All Assets) rewrites old-format assets.

## Gotchas

- **URP settings live per quality level, not in `PC_RPAsset`.** `Assets/URPDefaultResources/{Fastest,Fast,Default,Good,Beautiful,Fantastic}.asset`
  are one URP asset per quality level, each assigned to its level in `QualitySettings.asset`.
  `GraphicsSettings.m_CustomRenderPipeline` still points at `Assets/Settings/PC_RPAsset.asset`, but a
  **quality-level override wins**, so editing `PC_RPAsset` (HDR, post-processing, shadows, render scale)
  changes nothing at runtime. Edit the asset for the active quality level instead.
  `Assets/Settings/Mobile_RPAsset.asset` + `Mobile_Renderer.asset` are unused template leftovers.
- **Input: the new Input System only** (`activeInputHandler: 1`); `UnityEngine.Input` throws. The asset's
  wrapper was migrated: each `InputElement`'s **name** (e.g. "Jump", "Move", "Dash") resolves at runtime against
  an action of the same name in `Assets/InputSystem_Actions.inputactions`. To add an input: add an action to
  that asset's Player map, then an `InputElement` with the same name on the character's `PlayerInput`; modules
  read it via `GetButtonInput("Name")`. A module can also skip the `PlayerInput` entry entirely and fall back to
  `InputSystem.actions.FindAction(name)` — see `InteractingModule`. The legacy fields still visible in
  `InputElement` (`m_ButtonName`, axis names, `UnityInputType`) are dead, kept only so old serialized data loads.
  Note the asset's `PlayerInput` class **shares its name with `UnityEngine.InputSystem.PlayerInput`** — fully
  qualify Input System types in files that reference the asset's class.
- **Aseprite frames are tight-cropped, so a sprite can look like it "shrinks on Play".** The importer crops every
  frame to its own artwork bounds, so the same character can be 1.59 world units in one frame and 0.97 in
  another. The prefab/scene view shows the one frame baked into the generated prefab; the moment the Animator
  starts you get the clip's frames. **Size sprites against a frame you will actually see in play.** The per-frame
  pivots do resolve to the same canvas origin, so frames *are* aligned — only the drawn size differs.
- **Some materials are on Built-in `Sprites/Default`** (`Assets/Materials/Demo 1/`). They render correctly anyway
  (`Sprites/Default` is unlit and has no Built-in lighting dependency), so this is not a bug to chase.
- **Patched third-party asset:** `Assets/Plugins/PlayModeComponentSaver/` was hand-patched to compile on Unity 6.5
  — `Object.GetInstanceID()` is obsolete-*as-error* (CS0619, so `#pragma warning disable` cannot silence it) and
  its replacement returns an `EntityId` whose conversion to `int` is also obsolete. The fix carries the runtime
  object id as a **`string`** through `GetInstanceId`/`RegisterRuntimeObject`/`GetSavedRuntimeObjectIndex`/`FindRuntimeObject`,
  with EditorPrefs storage moved to `SetString`/`GetString`. **Re-importing or updating the asset overwrites this patch.**
- **Asset setup requirements** (from `Assets/CharacterEditorPackage/ReadMe.txt`): characters live on layer 31
  "Player", and Physics settings must have **"Queries hit triggers" unchecked** — otherwise the player treats
  triggers as solid.
- **Line endings:** Force Text writes LF while git on Windows converts to CRLF on checkout, so git prints
  `LF will be replaced by CRLF` and freshly checked-out files can immediately show as modified. Harmless.
- **Deleting a type breaks the whole assembly, and the symptom points at the wrong place.** This will keep
  happening in a skeleton, because stripping is the main activity here. One unresolved type name means
  `Assembly-CSharp` does not build, and Unity then cannot load **any** MonoBehaviour — so every object in
  every scene shows `(Script)` with a missing reference and it looks like the scenes or prefabs are
  corrupt. They are not. It happened on day one: `UnitManager` was deleted while `GameManager` still
  declared a `[SerializeField] private UnitManager` field it never read (fixed in `fb69872`).
  **Before deleting a script, grep the whole of `Assets/` for its type name** — not just for the things
  it obviously relates to. A serialized field that is never read still counts as a reference, and it will
  not show up if you only search for usages of its *members*. The same applies to enum members
  (`SoundId.X`) and to methods called from another class.

## What was removed when this was stripped

So you know what is *absent* rather than broken: the cat system (`Cat`, `CatAnimator`, `ICatHittable`,
`CatCollectionModule`, `CatTossModule` and their prefabs), the enemy system (`EnemyController`, `EnemyAnimator`
and their prefabs), `UnitManager` (an empty cat/enemy stub), the `CatAnimatorEditor`/`EnemyAnimatorEditor`
inspectors, the cat/enemy `SoundId` entries, the `TossCat` input action, and Unity's `TutorialInfo`/`Readme.asset`
template leftovers. `DeathTrigger.cs` was reverted to stock (it had carried three lines of cat-specific game
code), and `PlayerLives.LoseCarriedCats()` went with it. `GameManager`'s unused
`[SerializeField] private UnitManager unitManager` field was removed alongside `UnitManager` itself — one
commit late, which is what the deleting-a-type gotcha above is about.

`GameStats`/`UIManager` kept their structure but their collectible counter was renamed out of cat-specific
naming (`CatsRescued` → `ItemsCollected`, `AddCatRescued` → `AddItemCollected`, plus a `RemoveItemCollected`
for games where a collectible can be spent again). The `UIManager` text fields carry `[FormerlySerializedAs]`
so the existing scene wiring survived the rename.

The house geometry in `Assets/Art/Models/Blender/` and the placeholder audio in `Assets/Art/Audio/` were
**kept** — delete them when the new game's art arrives.

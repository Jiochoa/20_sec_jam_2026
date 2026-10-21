# Jam Skeleton

A stripped Unity project used as the starting point for game jams. **This is not a game** — it
is the plumbing every jam needs, with the previous jam's content removed.

Derived from *Crazy Cat Lady* (Pirate Game Jam) on 2026-07-30.

## What you get

- **A 2.5D character controller** — the Character Editor Package asset: custom capsule physics
  on 3D colliders, with abilities as *prefabs* rather than components.
- **Interaction + dialogue** — walk up to a thing, press E, read a typewriter dialogue box.
  Self-bootstrapping; no scene setup needed.
- **Managers** — game state machine (menu / playing / pause / win), menu switching, a sound
  system addressed by enum, run statistics, and a screen fader.
- **Level management** — checkpoints, death zones, and in-scene level switching with a fade.
- **Player lives** — hearts, invulnerability blink, knockback, respawn at checkpoint.
- **A responsive WebGL template** — fills any itch.io embed instead of being cropped by it.
- **URP configured** with working bloom/post-processing, and Force Text serialization so
  prefabs and scenes diff properly in git.

## Starting a new jam from this

1. Copy or clone this repo, then `git init` (or use it as a GitHub template repository).
2. Set **Product Name** and **Company Name** in Player Settings. Product Name is what players
   see in the browser tab and on the loading screen.
3. Regenerate `productGUID` in `ProjectSettings/ProjectSettings.asset` if you use Unity Cloud.
4. Rename `JamSkeleton.slnx` and update `.vscode/settings.json`.
5. Build the game in `Assets/Scenes/Main.unity`, which already has the camera, lighting,
   post-processing Volume, managers and canvases set up.

## Controls (as bound in `InputSystem_Actions`)

| Action | Key |
| --- | --- |
| Move | WASD / arrow keys |
| Jump | Space |
| Sprint | Left Shift |
| Dash | Left Alt / right mouse |
| Interact | E |
| Pause | Escape |

## Requirements

**Unity 6000.5.1f1** or matching. Open with Unity Hub; there is no CLI build script.

See [CLAUDE.md](CLAUDE.md) for architecture notes and the gotchas worth knowing before you
start changing things.

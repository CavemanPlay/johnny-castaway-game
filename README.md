# Johnny Castaway Game

A cozy idle game where shipwrecked Johnny generates resources, builds infrastructure, and escapes one island only to crash on a worse one. Built in Unity 6 (URP).

## Prerequisites

- **Unity 6000.3.6f1** (install via Unity Hub — it will prompt you if missing)
- **Git** with LFS support

## Setup

```bash
git clone https://github.com/CavemanPlay/johnny-castaway-game.git
cd johnny-castaway-game
```

Open the project in Unity Hub:

1. Click **Add** → select `UnityProject/JohnnyCastawayGame`
2. Unity Hub installs the correct editor version if needed
3. Open `Assets/Scenes/Bootstrap.unity`
4. Press **Play** to verify the project runs cleanly

## Project Structure

```
johnny-castaway-game/
├── Docs/                        # Architecture, DoD, CI, debug guides
├── ops/issues/                  # Milestone + issue YAML definitions
├── Tools/                       # Editor utilities and scripts
└── UnityProject/
    └── JohnnyCastawayGame/
        └── Assets/
            ├── Art/             # Sprites, animations, materials
            ├── Audio/           # Music and SFX
            ├── Prefabs/         # Reusable GameObjects
            ├── ScriptableObjects/  # Data assets (configs, island rulesets)
            ├── Scenes/          # Unity scenes
            └── Scripts/
                ├── Core/        # Bootstrap, GameRoot, state machine, service interfaces
                ├── Data/        # State models, save schemas, config formats
                ├── Debug/       # DevHUD, logging helpers, snapshot tools
                ├── Events/      # Event director, game event definitions
                ├── Simulation/  # Tick loop, resource management, hazards
                └── UI/          # HUD, upgrade panels, menus
```

## Conventions

- **Branches:** `feat/short-description`, `fix/short-description`, `chore/short-description`
- **PRs:** Must reference an issue (`Closes #N`); CI must pass before merging
- **Commits:** Imperative mood, present tense (`add`, `fix`, `update`)
- **Labels:** `type:*` · `area:*` · `est:xs/s/m/l/xl` · `risk:low/med/high` · `p0–p2`

See [CONTRIBUTING.md](CONTRIBUTING.md) and [Docs/DoD.md](Docs/DoD.md) for full details.

## CI

CI runs on every push and PR to `main`. See [Docs/CI.md](Docs/CI.md) for what must pass to merge.

# Contributing

## Workflow

1. Pick an issue from the board — start with `p0`, then `p1`
2. Create a branch: `feat/short-description`, `fix/short-description`, or `chore/short-description`
3. Do the work; keep PRs small and focused on one issue
4. Open a PR referencing the issue (`Closes #N` in the body)
5. CI must be green before merging
6. Merge to `main` — squash or merge commit (no rebase onto main)

## Code Conventions

- **Naming:** `PascalCase` for types and methods; `camelCase` for fields and locals; `_camelCase` for private fields
- **Files:** One class per file; filename matches class name
- **Editor-only code:** goes in `Editor/` subfolders and is wrapped in `#if UNITY_EDITOR`
- **Logging:** No raw `Debug.Log` calls in committed code — use the project logging helper (`Log.*`)
- **Comments:** Only where the logic isn't self-evident; avoid restating what the code says

## Asset Conventions

- Every asset committed to `Assets/` must have a `.meta` file alongside it
- Art → `Assets/Art/`, Scripts → `Assets/Scripts/`, Audio → `Assets/Audio/`, etc.
- ScriptableObject assets → `Assets/ScriptableObjects/`
- Avoid `Assets/Resources/` unless runtime path-based loading is specifically required

## Issues and Labels

| Label | Meaning |
|---|---|
| `p0` | Blocks progress — fix before anything else |
| `p1` | Important — should land in the next few sessions |
| `p2` | Nice to have — pick up when p0/p1 are clear |
| `est:xs/s/m/l/xl` | Rough effort estimate |
| `risk:low/med/high` | Risk of breakage or needing rework |

## Definition of Done

See [Docs/DoD.md](Docs/DoD.md).

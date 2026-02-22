# CI Expectations

CI runs automatically on every push and every pull request targeting `main`.

## What Must Pass to Merge

| Check | Description |
|---|---|
| **YAML validation** | All files under `ops/issues/` must be valid YAML |
| **Unity meta files** | Every asset in `Assets/` must have a corresponding `.meta` file |

## Workflow File

`.github/workflows/ci.yml`

## Future Checks (planned)

- `dotnet format` style enforcement (requires committed solution file)
- Unity Edit Mode test runner (requires Unity license in CI)
- Build size budget check

## Adding a New Check

1. Add a step to `.github/workflows/ci.yml`
2. Ensure it fails fast (exit code 1 on error)
3. Update this doc with the new row in the table above
4. CI must pass on a green PR before a check is considered "enforced"

# Definition of Done

An issue is **done** when all of the following apply.

## All Issues

- [ ] Change is committed and pushed to the branch
- [ ] No Unity temp/generated files committed (`Library/`, `Temp/`, `Logs/`, `UserSettings/`)
- [ ] CI is green on the PR
- [ ] Issue is closed on GitHub and linked to the PR

## Features

- [ ] Works in Play Mode without errors or new warnings
- [ ] Logged or otherwise observable behaviour confirms the feature is live
- [ ] PR references the issue number

## Bugs

- [ ] Root cause identified and fixed — not worked around
- [ ] No regression in related systems
- [ ] PR references the issue number

## Chores / Tech Debt

- [ ] No unintentional functional changes
- [ ] Old files and dead code fully removed

## Documentation

- [ ] Written in plain English, short and practical
- [ ] Committed alongside the code it describes

## Milestones

A milestone is **done** when:

- All `p0` and `p1` child issues are closed
- At least one manual Play Mode smoke test passes end-to-end
- The exit criteria in the milestone description are met

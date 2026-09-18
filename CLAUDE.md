# Documentation

## README.md

Keep README.md current with every change that affects setup, configuration, 
architecture, or functionality — update it as part of the same change, not 
as a follow-up. Trivial changes (typo fixes, formatting, internal refactors 
with no external effect) don't require an update.

Required sections:
- **Overview** — what the system does and who/what it's for
- **Folder Structure** Overview of the folder structure and organization of the project
- **Setup** — steps to get a dev environment running (prerequisites, install, 
  first run)
- **CodeConventions** - Synopsis of code conventions, fo
- **Configuration** — environment variables, config files, and settings that 
  affect behavior, with defaults noted
- **Architecture** — high-level structure: main components/projects, how 
  they interact, key design decisions

If a change doesn't fit an existing section, extend it rather than adding 
new top-level sections without cause. Keep entries current rather than 
appending a changelog-style history — README.md reflects the system as it 
is now, not what changed when.

## Code Documentation
All public and internal types, methods, properties, and events must have 
C# XML documentation comments (///) directly above their declaration.

Required tags:
- <summary> on every member
- <param name="..."> for every parameter
- <returns> for non-void methods
- <exception cref="..."> for any exception explicitly thrown
- <remarks> for non-obvious design decisions or side effects
- <inheritdoc/> instead of duplicating docs when overriding/implementing

Do not leave placeholder or restated-signature summaries (e.g. 
"Gets or sets the Name" for a property called Name) — write what the 
member actually does or represents in context.

Private members should be documented only when their logic is non-obvious.

# Workflow

## Issue-first workflow

When I ask you to plan, design, or scope a feature/bugfix (rather than
explicitly saying "implement" or "fix it now"):

1. Do NOT edit any files.
2. Investigate the codebase as needed to understand the change.
3. Write up a GitHub issue using `gh issue create` with:
   - A clear title
   - Problem/context section
   - Proposed approach
   - Relevant files/functions you found, with paths
   - Any open questions or risks
4. Print the issue URL when done.

Only start implementing when I explicitly reference an issue number and
ask you to work on it (e.g. "pick up #42").

## Pull requests only on main/master

- Never commit or push directly to `main` (or `master`), for any change,
  no matter how small.
- All changes land on a feature branch, pushed, then opened as a pull
  request (`gh pr create`). Give me the PR URL when it's ready.
- Do not merge, approve, or close the pull request yourself — that's my
  call. Opening it is the end of the task unless I explicitly ask you to
  merge it too.

## Git history

- Never rewrite already-pushed history — `rebase`, `commit --amend` on a
  pushed commit, `filter-branch`/`filter-repo`, resetting a branch backward,
  or any resulting force-push — without asking first and getting my
  explicit confirmation for that specific rewrite.
- Confirming one rewrite does not carry forward to the next one later in
  the same conversation; ask again each time.
- Before doing it, state plainly what will happen: which commits are
  affected, that it requires a force-push, and that the old commits become
  unreachable afterward.

## Other destructive git operations

- Beyond history rewrites, ask first for any other destructive or
  hard-to-reverse git action: deleting a branch or tag, `git clean -fd`,
  or discarding uncommitted changes (`checkout --`/`restore`/`reset --hard`
  over local edits).

## Dependencies

- Flag it before adding a new package or upgrading an existing one's
  version, rather than doing it silently mid-task. Note anything relevant
  (license, major version jump, why it's needed).

## Issues and pull requests

- Never delete or close an issue or pull request without being asked.

## Repository and organization settings

- Changes to GitHub Actions workflows, repository settings, or
  collaborator/permission changes always require explicit confirmation
  before being applied.


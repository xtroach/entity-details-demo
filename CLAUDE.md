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
- **CodeConventions** - Synopsis of code conventions, for quick reference
  without reading `.editorconfig` directly
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

## Scope, implement, or trivial

When a conversation involves talking through a design or approach (not a
simple one-off fix), once we reach a decision, offer me the three ways to
proceed — unless I've already declared one earlier in the conversation
(e.g. by saying "scope this," "implement this," or "this is trivial"):

- **Scope** — write up a GitHub issue (`gh issue create`) capturing the
  decision: title, problem/context, the approach we landed on and the
  reasoning behind it, relevant files/functions with paths, and any open
  questions or risks. Write it with enough detail that implementing from
  the issue alone, with no memory of this conversation, would produce
  essentially the same change. Do not edit any files. Only start
  implementing later when I explicitly reference the issue number.
- **Implement** — write the same kind of issue first, to the same detail
  bar, then continue straight into implementing it in this same workflow
  — no waiting for me to separately reference the issue number.
  Reference the issue from the resulting PR (e.g. "Closes #N") so it
  closes automatically when the PR merges.
- **Trivial** — no issue, just make the change directly. Reserved for
  changes on the same scale as what already counts as "trivial" elsewhere
  in this file (typo fixes, formatting, internal refactors with no
  external effect). For anything bigger, use Scope or Implement instead
  of assuming this is trivial.

Whichever I choose, wait for my answer before acting — don't default to
one.

## Pull requests only on main/master

- Never commit or push directly to `main` (or `master`), for any change,
  no matter how small.
- All changes land on a feature branch, pushed, then opened as a pull
  request (`gh pr create`). Give me the PR URL when it's ready.
- Do not merge, approve, or close the pull request yourself — that's my
  call. Opening it is the end of the task unless I explicitly ask you to
  merge it too.
- When merging is asked for, default to "Create a merge commit." It's the
  only GitHub merge method that never rewrites already-pushed commits —
  squash discards individual commit history, and "Rebase and merge"
  re-creates every commit with a new SHA — consistent with the Git
  history rule below. This still doesn't make a dependent stacked branch
  fast-forwardable afterward; see "Stacked pull requests."

## Verify before calling a PR ready

- Before opening or updating a PR that touches buildable/testable code
  (anything under `src/` or `test/` — not a docs-only change like
  `CLAUDE.md` or `README.md`), run `dotnet build` and `dotnet test`
  locally first. Don't describe a change as ready without having done so.
- State that verification happened, and its result, in the PR description
  or to me directly, rather than leaving it implicit.
- There's no CI configured on this repo yet, so this local check is
  currently the only gate. Once CI exists (see #12), this rule should
  also require it green before a PR is called ready (see #13).

## Stacked pull requests

- If a change depends on content that only exists on another open,
  unmerged PR's branch, branch from that branch and set it as this PR's
  base. State this explicitly in the PR description, along with the
  required merge order (base-most branch merges last).
- The moment a base PR merges, immediately rebase any still-open
  dependent PR branches onto the branch it merged into (or onto `main`,
  if nothing else sits between them) and update the dependent PR's base
  explicitly — don't rely on GitHub's automatic base-retargeting, which
  only fires if the merged branch is deleted, and isn't guaranteed to
  happen.
- After any stacked merge completes, verify the final content actually
  landed in the target branch (e.g. diff it against what the top-most PR
  was supposed to contain) rather than trusting the "Merged" label alone
  — "Merged" only means that PR's diff reached its immediate base, not
  that it survived the rest of the chain.

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


# Documentation

## README.md

Keep README.md current with every change that affects setup, configuration, 
architecture, functionality, or how changes are made and reviewed — 
update it as part of the same change, not as a follow-up. Trivial changes 
(typo fixes, formatting, internal refactors with no external effect) 
don't require an update.

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
- **Development Process** — how changes get made and reviewed: the
  Scope/Implement/Trivial workflow, PR-only-to-main and the GitHub
  settings enforcing it (branch protection, merge method, auto-delete),
  and the reasoning behind them — written for a reader evaluating how
  this project is built, not as agent instructions (that's what this
  file is for)

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

# Code Design

## Data access

- Don't add a generic repository or unit-of-work layer over EF Core.
  `Api` uses `AppDbContext` and EF Core's query API directly. `DbSet<T>`
  and `DbContext` already are those patterns, and wrapping them loses
  projections, `Include`, `AsNoTracking`, SQL-side paging and composable
  queries. README's Architecture section has the full reasoning.
- `Api` may use EF Core, but never provider-specific APIs (e.g. Npgsql's
  `EF.Functions.ILike`, anything under `Npgsql.*`). Those go into `Data`
  behind a provider-neutral helper. The provider is configured only in
  `Data` (`AppDbContextOptions`), and `Api` only passes a connection string
  to `AddEntityDetailsData` and decides when to call
  `MigrateEntityDetailsDatabaseAsync`.
- Shared data-access logic goes into `Data` as helpers that build on EF
  rather than hide it: `IQueryable<T>` extension methods, per-entity
  `IEntityTypeConfiguration<T>`, and `SaveChanges` interceptors.
- If an entity gains rules that every write must enforce, a repository or
  domain service specific to that entity may be warranted. Raise it as a
  design question rather than adding one silently.

# Workflow

## Scope, implement, or trivial

When a conversation involves talking through a design or approach (not a
simple one-off fix), once we reach a decision, offer me the three ways to
proceed — unless I've already declared one earlier in the conversation
(e.g. by saying "scope this," "implement this," or "this is trivial"):

- **Scope** — write up a GitHub issue (`gh issue create`) capturing the
  decision: title, problem/context, the approach we landed on and the
  reasoning behind it, relevant files/functions with paths, and any open
  questions or risks (`.github/ISSUE_TEMPLATE/scope.md` mirrors this
  shape for manual/web-UI issue creation, which `gh issue create --body`
  bypasses). Write it with enough detail that implementing from the
  issue alone, with no memory of this conversation, would produce
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
  request (`gh pr create`) with a summary, a `Closes #N` reference, and
  a test plan (`.github/pull_request_template.md` mirrors this shape for
  manual/web-UI PR creation). Give me the PR URL when it's ready.
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
  (anything under any project's `*/src/` or `*/test/` — not a docs-only
  change like `CLAUDE.md` or `README.md`), run `dotnet build` and
  `dotnet test` locally first. Don't describe a change as ready without
  having done so.
- State that verification happened, and its result, in the PR description
  or to me directly, rather than leaving it implicit.
- Local verification comes first, but a PR is only ready once CI
  (`.github/workflows/ci.yml`) is green on it. Its jobs ("Build and
  test", "Docker build and smoke test") are required status checks on
  `main`, so GitHub blocks merging while either is red or still running.
  If CI fails, fix the cause and push again. Never call a PR ready
  with a red or pending *required* check, and don't work around a
  failing check (skipping tests, loosening a check) without asking
  first.
- Advisory checks are the runs that aren't required status checks —
  currently "Docs coherence" (`.github/workflows/docs-coherence.yml`)
  and CodeQL's default setup ("CodeQL", "Analyze (actions)", "Analyze
  (csharp)"). GitHub won't block a merge on them, but the *run* and its
  *findings* are two different things:
  - **A red, errored or still-pending advisory run means the PR isn't
    ready**, exactly as a required one does. A check that couldn't run
    hasn't passed, and reporting green would hide a broken check behind
    a green tick. Fix the cause so it runs; don't ship past it.
  - **"Docs coherence" absent or skipped is a fourth state, and it is
    not a failure.** It only runs on a PR carrying the
    `docs-coherence-review` label, so on an unlabelled PR there is
    nothing to report and nothing to fix. Don't add the label — asking
    for the audit is the reviewer's call, and adding it also makes
    every later push re-audit. Say plainly that the PR hasn't been
    audited rather than reporting it as clean, and mention the label as
    what would change that.
  - **Its findings are not a merge gate.** A green run that posts a
    non-empty findings comment doesn't make a PR un-ready. Resolving a
    documentation contradiction is a judgment call about which document
    is wrong, and the checker is a model whose findings vary between
    runs over an unchanged tree — so its output is input to my
    judgment, never a blocker. Report what it found and let me decide.
- Either class: don't work around a failing check — skipping tests,
  loosening a check, dropping a step, or removing the
  `docs-coherence-review` label to stop an audit re-running — without
  asking first.

## Tests accompany code changes

- New or changed logic in any project's `*/src/` should come with
  corresponding tests in its `*/test/` in the same PR, not as a
  follow-up. This covers new behavior,
  bug fixes (a regression test reproducing the bug), and non-trivial
  logic changes. Changes on the same scale as what already counts as
  "trivial" elsewhere in this file (typo fixes, formatting, internal
  refactors with no external effect) don't need a dedicated test.
- If it's genuinely unclear whether a change needs a test, flag it and
  ask rather than silently deciding either way.
- This is currently a self-enforced expectation, not a measured gate.
  CI runs every test and collects coverage, but only reports it. Once
  #23 (test coverage gates) lands, this rule should be reconciled with
  whatever gate that issue settles on, rather than left as a separate
  parallel rule.

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
- This applies to direct package additions/upgrades only — not to
  transitive packages that come along with one. A transitive package
  isn't an independent decision; it's a consequence of whatever direct
  package pulled it in.
- Versions are managed centrally: a package's version lives only in the
  root `Directory.Packages.props` (`<PackageVersion>`), and projects
  reference it without a version (`<PackageReference Include="..." />`).
  Adding a package means both; upgrading one means changing only
  `Directory.Packages.props`. Never put a `Version` on a
  `PackageReference`.
- The same applies to the other pinned versions: the SDK in
  `global.json`, base-image digests in the Dockerfiles, and action SHAs
  in `.github/workflows/`.
- Dependabot opens weekly version-update PRs for all of these
  (`.github/dependabot.yml`). Those are the owner's to review and merge.
  Don't merge, close, or rewrite them unasked.

## Secrets and credentials

- Never commit real credentials, API keys, connection strings with
  embedded credentials, or other secrets — not even in
  `appsettings.*.json`, regardless of environment.
- Local secrets go through `dotnet user-secrets` (`Api` already has a
  `UserSecretsId` configured) or environment variables — never into a
  committed file.
- A local-only value that isn't actually sensitive — no embedded
  credentials, e.g. a local file path or a non-secret default — is fine
  to commit as-is. This rule is about actual secrets, not every config
  value near "ConnectionStrings."
- If a real secret is ever found already committed, flag it immediately
  rather than just removing it going forward — a committed secret is
  compromised the moment it's pushed; removing the line doesn't undo
  that, it needs to be rotated/invalidated at the source. Purging it
  from git history entirely is a separate, heavier action, and falls
  under the Git history rule (confirm first).

## Issues and pull requests

- Never delete or close an issue or pull request without being asked.

## Repository and organization settings

- Changes to GitHub Actions workflows, repository settings, or
  collaborator/permission changes always require explicit confirmation
  before being applied.


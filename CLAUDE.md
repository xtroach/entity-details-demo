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

# Openza Reader Tracking

Track product work in GitHub Issues and GitHub Projects so bugs, features, release tasks, PRs, and decisions stay linked to the code.

## Labels

Use one label from each group when possible:

- Type: `type:bug`, `type:feature`, `type:improvement`, `type:tech-debt`, `type:release`, `type:docs`, `type:security`
- Priority: `priority:p0`, `priority:p1`, `priority:p2`, `priority:p3`
- Area: `area:packaging`, `area:markdown-rendering`, `area:webview-security`, `area:file-association`, `area:ui`, `area:performance`, `area:store`, `area:docs`

Priority meanings:

- `priority:p0`: blocks install, launch, security, or data-safety expectations.
- `priority:p1`: important for the next planned release.
- `priority:p2`: useful but not release-blocking.
- `priority:p3`: backlog or later exploration.

## Milestones

Use milestones for release planning:

- `v0.2`: next functional release.
- `v0.3`: follow-up polish and compatibility.
- `v1.0`: stable public baseline.
- `v1.1.0`: next Microsoft Store update with reader modes, settings/recents polish, and packaging validation.

## Project Views

Create a GitHub Project named `Openza Reader Roadmap` with these views:

- `Backlog`: all open issues grouped by priority.
- `vNext`: open issues in the next milestone.
- `Bugs`: open issues with `type:bug`.
- `Packaging & Release`: issues with `area:packaging`, `area:store`, or `type:release`.
- `Roadmap`: issues grouped by milestone.

## Workflow

- Every non-trivial change starts from an issue.
- Branch names should include the issue number when practical, for example `fix/123-self-contained-dotnet`.
- Pull requests should link issues with `Fixes #123`, `Closes #123`, or `Refs #123`.
- Keep issues scoped to one deliverable. Use sub-issues or task lists for larger work.
- Close an issue only after the fix is merged and the relevant verification is recorded.

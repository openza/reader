# Open-Source Readiness

This checklist tracks the repository's public readiness and release hygiene.

## Repository Basics

- `LICENSE` exists and declares the project license.
- `README.md` explains the product, current status, setup, build, test, and security posture.
- `CONTRIBUTING.md` explains setup, scope, and pull request expectations.
- `SECURITY.md` explains private vulnerability reporting.
- `CODE_OF_CONDUCT.md` sets contribution behavior expectations.
- `SUPPORT.md` explains where users should ask for help.
- `THIRD-PARTY-NOTICES.md` lists direct third-party dependencies and bundled assets.

## GitHub Setup

- Enable GitHub private vulnerability reporting.
- Enable Dependabot alerts.
- Confirm issue labels exist for `bug` and `enhancement`.
- Confirm the default branch is protected before accepting outside contributions.
- Confirm CI runs on pull requests.

## Release Setup

- Keep the Microsoft Store listing current: https://apps.microsoft.com/detail/9NNPMN0JSSW5?hl=en-us&gl=IN
- Avoid public self-signed installer distribution.
- Publish winget manifests only after verifying the live Store listing and package identity.
- Use Store signing for public packages.
- Keep local test signing certificates ignored and out of git history.
- Include third-party notices with release artifacts.
- Smoke test install, launch, file association, and uninstall before public release.

## Maintainer Notes

- Keep V1 read-only.
- Keep Markdown input treated as untrusted.
- Keep runtime assets bundled locally; no CDN dependency.
- Add a notice entry if upstream Prism assets replace the current project-owned highlighter.

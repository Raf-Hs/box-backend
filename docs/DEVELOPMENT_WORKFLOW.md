# BOX Development Workflow

This document defines the **mandatory** workflow for every feature, bug fix, refactor,
documentation change, and maintenance task in BOX. It applies to both repositories:

- **This repository:** `Raf-Hs/box-backend`
- **Sibling repository:** [`Raf-Hs/box-frontend`](https://github.com/Raf-Hs/box-frontend)

The same document is kept in both repositories so each one is self-contained. When the
workflow changes, update both.

## Principles

1. **Issue first.** No implementation work begins before a GitHub issue exists.
2. **Branch second.** Every change is developed on a dedicated branch created from the
   latest `develop`.
3. **Validate before review.** Tests and a production build must pass, and the actual
   commands and results must be recorded.
4. **One integration branch.** Completed work lands on `develop`. `main` is production.
5. **Releases are separate.** `develop` is promoted to `main` only through a reviewed
   release pull request.
6. **Never rewrite shared history** and never bypass failing checks.

## Branch model

| Branch | Purpose |
| --- | --- |
| `main` | Production-ready, released code. Deploys run from here. |
| `develop` | Integration branch. All normal pull requests target this. |
| `feature/<issue>-<slug>` | New functionality. |
| `fix/<issue>-<slug>` | Bug fixes. |
| `refactor/<issue>-<slug>` | Internal changes with no behavior change. |
| `docs/<issue>-<slug>` | Documentation only. |
| `chore/<issue>-<slug>` | Tooling, dependencies, configuration. |
| `hotfix/<issue>-<slug>` | Production fixes branched from `main`. |
| `release/<version>` | Optional, for coordinated releases. |

Never commit directly to `main` or `develop`.

## 1. Create a GitHub issue

Before writing code:

- Search the existing issues for duplicates.
- Create an issue describing the problem, motivation, scope, and acceptance criteria.
- Identify the affected repository or repositories (`box-frontend`, `box-backend`, or both).
- Include testing requirements and any security or migration considerations.
- Break large features into small, independently testable issues.
- For work spanning both repositories, create a coordinated issue in each repository and
  cross-link them from the corresponding epics.

## 2. Create a branch from the latest `develop`

```bash
git fetch origin
git checkout develop
git merge --ff-only origin/develop
git checkout -b feature/<issue>-<slug>
```

Use the branch prefixes from the table above. Use a fast-forward-only update of `develop`;
do not reset or force anything.

## 3. Investigate and implement

- Read the relevant code and architecture first.
- Reproduce a bug before fixing it.
- Identify the root cause and the expected behavior.
- Make the smallest reasonable change that satisfies the issue.
- Add or update tests alongside the implementation.
- Preserve established conventions and avoid unrelated modifications.
- Do not expand scope without updating the issue.

## 4. Validate locally

Run the checks relevant to the change and **record the actual commands and results**:

```bash
# Backend (box-backend)
dotnet build
dotnet test

# Frontend (box-frontend)
npm run build
npx ng test --watch=false
```

Also:

- Run formatting, linting, or static analysis where configured.
- Inspect the diff for unrelated changes, secrets, debug code, and configuration drift.
- Review security-sensitive changes and any database migration.

**Never claim a test or build passed unless it was actually executed successfully.** If a
check cannot run, state the reason explicitly.

## 5. Commit with Conventional Commits

Use the `type(scope): summary` form and reference the issue:

```
feat(auth): add user registration endpoint
fix(habits): enforce per-user ownership
test(auth): cover unauthorized access
docs: establish development and release workflow
```

Keep commits focused. Prefer several meaningful commits over one large commit.

## 6. Push and open a pull request targeting `develop`

```bash
git push -u origin <branch>
```

Every pull request must include:

- The issue being addressed (use `Closes #<issue>` **only** when the issue should close on
  merge).
- A concise description of the changes.
- The implementation approach.
- Tests and validation performed, with the real commands and results.
- Database migration or deployment implications.
- Security considerations when relevant.
- Known limitations.
- Screenshots for meaningful frontend changes.

Routine development PRs **never** target `main`.

> **Note:** `develop` is not the default branch, so `Closes #<issue>` does **not** auto-close
> the issue when a PR merges into `develop`. Close the issue explicitly after verifying the
> merge, or let the later release PR close it when it reaches `main`.

## 7. Review and the solo-maintainer self-review policy

Before merging any pull request:

- Review the complete diff.
- Verify the acceptance criteria are met.
- Verify the recorded test and build results.
- Address review comments and fix regressions.
- Confirm the PR targets `develop` and references the correct issue.

A successful local build alone is not sufficient justification to merge.

### Solo-maintainer policy

This project currently has a single maintainer, and GitHub does not allow an author to
approve their own pull request. To keep the workflow enforceable without pretending that
GitHub self-approval exists:

- Every change must have a GitHub issue.
- Every change must use a dedicated branch created from the latest `develop`.
- Every change must be validated before a pull request is opened.
- Every normal development pull request must target `develop`.
- The author must review the PR diff and record the validation results before merging.
- A solo maintainer **may** merge their own pull request after completing and documenting a
  self-review, provided all applicable checks pass.
- Self-review **must never** be represented as approval from another person.
- If a second qualified reviewer is available, request an independent review instead.
- Production releases require a separate `develop` → `main` pull request, a documented
  release checklist, successful checks, and a recorded release decision. A second reviewer
  is recommended but not mandatory under the solo-maintainer workflow.
- Never bypass failing checks, conceal missing validation, or merge changes known to be
  unsafe.
- Production hotfixes must be propagated back to `develop`.

Record the self-review in the PR using this template:

```markdown
### Self-review (solo maintainer)
- Author / reviewer: @<author> (self-review, not an independent approval)
- Diff reviewed: <commit range or PR head>
- Checks: <exact commands and results>
- Acceptance criteria: <met / notes>
- Risks and limitations: <...>
- Decision: merge into develop
```

## 8. Merge into `develop`

Once the PR meets the requirements above:

1. Merge into `develop` (keep the merge strategy consistent across the project).
2. Verify the integration branch still builds and its tests pass.
3. Confirm the linked issue is closed (see the note in step 6).
4. Delete the feature branch when appropriate.
5. Record any remaining deployment requirements.

## 9. Release from `develop` to `main`

Releases are handled separately from ordinary development:

1. Review all changes accumulated since the last release.
2. Identify and resolve outstanding integration failures.
3. Validate the combined release with the full test suite and production builds.
4. Verify migration order, backwards compatibility, configuration, and secrets.
5. Prepare release notes and deployment instructions.
6. Create a release pull request from `develop` into `main`.
7. Review and approve the release PR (a second reviewer is recommended, not mandatory).
8. Merge into `main` only after approval.
9. Verify production deployments and health checks.
10. Document the release and the rollback procedure.

Individual feature branches are never merged directly into `main`.

## 10. Deployment verification and rollback

- **Frontend:** Vercel builds from `main`. Confirm the deployment succeeded and smoke-test
  the running site.
- **Backend:** Render builds the Docker image from `main`. Confirm the deployment and that
  `GET /health` reports healthy.
- **Rollback:** revert the release merge (or redeploy the previous known-good commit/tag),
  verify the health check, and document what happened and why.

## 11. Coordinated changes across both repositories

- Cross-cutting features (for example authentication) have a coordinated issue in each
  repository, cross-linked from the epics.
- Agree the contract **before** implementing: endpoints, request/response shapes, session
  strategy, error handling, and status codes.
- When the frontend depends on a backend contract, land the backend contract first, or
  develop both branches together and merge in a coordinated order.
- Keep both repositories' `develop` branches consistent, and reference both issues in both
  pull requests.
- Never leave the pair in a state where one half expects a contract the other does not
  provide.

## 12. Hotfixes and branch synchronization

- **Production hotfix:** branch from `main` (`hotfix/<issue>-<slug>`), fix, test, open a PR
  targeting `main`, then immediately merge or cherry-pick the fix back into `develop` so the
  branches do not diverge. Document the exception.
- **Synchronization:** update `develop` and `main` from their remotes using fast-forward-only
  merges. If a branch has diverged or local changes prevent a safe update, stop and report
  rather than resetting.
- **Never force-push shared branches** and never rewrite shared history without explicit
  authorization.
- Delete branches once merged.

## Quick checklist

- [ ] Issue exists and is referenced by the branch and PR.
- [ ] Branch created from the latest `develop`.
- [ ] Tests and production build pass; evidence recorded.
- [ ] Conventional commit message(s).
- [ ] PR targets `develop` with a complete description.
- [ ] Diff reviewed; self-review recorded (solo maintainer) or independent review obtained.
- [ ] Merged into `develop`; integration branch healthy; issue closed.
- [ ] Release handled separately through a `develop` → `main` PR.

# Gitflow Workflow Guide

This repository uses the **Gitflow branching model** for managing development, releases, and hotfixes.

> **Versioning: MinVer (tag-driven).** This repo has migrated to **MinVer**
> (`MinVerTagPrefix=v`, prerelease id `rc`; see `public/Directory.Build.props`).
> Nerdbank.GitVersioning / `nbgv` and `version.json` are no longer used.
> To bump a version, **create an annotated `vX.Y.Z[-rc.N]` git tag on the
> release commit** — there is no `version.json` to edit. Read the current
> version with `minver` (or `dotnet minver` / `git describe --tags --match 'v*'`).
> Everything currently targets `v1.3.0-rc.1`.

## Branch Structure

### Main Branches (Permanent)

#### `master`
- **Purpose**: Production-ready code only
- **Protected**: Yes - no direct commits allowed
- **Merges from**: `release/*` and `hotfix/*` branches only
- **Tags**: All releases are tagged here (e.g., `v0.5.0`, `v1.0.0`)

#### `develop`
- **Purpose**: Integration branch for ongoing development
- **Protected**: Yes - no direct commits allowed
- **Merges from**: `feature/*`, `bugfix/*`, `release/*`, and `hotfix/*` branches
- **Default branch**: This is where you start new work

### Supporting Branches (Temporary)

#### `feature/*`
- **Purpose**: Develop new features
- **Branch from**: `develop`
- **Merge to**: `develop`
- **Naming**: `feature/description-of-feature`
- **Example**: `feature/add-authentication`, `feature/implement-caching`

#### `bugfix/*`
- **Purpose**: Fix bugs in `develop` branch
- **Branch from**: `develop`
- **Merge to**: `develop`
- **Naming**: `bugfix/description-of-fix`
- **Example**: `bugfix/fix-null-reference`, `bugfix/fix-connection-leak`

#### `release/*`
- **Purpose**: Prepare for production release
- **Branch from**: `develop`
- **Merge to**: `master` AND `develop`
- **Naming**: `release/version-number`
- **Example**: `release/0.5.0`, `release/1.0.0-beta`

#### `hotfix/*`
- **Purpose**: Emergency fixes for production
- **Branch from**: `master`
- **Merge to**: `master` AND `develop`
- **Naming**: `hotfix/version-number` or `hotfix/description`
- **Example**: `hotfix/0.4.1`, `hotfix/critical-security-fix`

#### `support/*`
- **Purpose**: Maintenance branches for old releases
- **Branch from**: `master` at specific tag
- **Merge to**: Not merged back
- **Naming**: `support/version-number`
- **Example**: `support/1.0.x`

---

## Common Workflows

### Starting New Feature

```bash
# Make sure you're on develop and up to date
git checkout develop
git pull origin develop

# Start new feature (creates feature/my-feature and switches to it)
git flow feature start my-feature

# Work on your feature
# ... make changes, commits, etc.

# Publish feature to remote (optional - for collaboration)
git flow feature publish my-feature

# Finish feature when done (merges to develop and deletes branch)
git flow feature finish my-feature

# Push develop to remote
git push origin develop
```

### Fixing Bug in Development

```bash
# Make sure you're on develop and up to date
git checkout develop
git pull origin develop

# Start bugfix
git flow bugfix start fix-data-leak

# Fix the bug
# ... make changes, commits, etc.

# Finish bugfix (merges to develop and deletes branch)
git flow bugfix finish fix-data-leak

# Push develop to remote
git push origin develop
```

### Creating Release

```bash
# Make sure develop is ready for release
git checkout develop
git pull origin develop

# Start release branch
git flow release start 0.5.0

# Update CHANGELOG.md
# Fix last-minute bugs
# ... commit changes ...
# (No version file to edit — MinVer reads the v0.5.0 tag created below.)

# Finish release (merges to master and develop, creates annotated tag v0.5.0).
# The v-prefixed tag is what MinVer uses to compute 0.5.0.
git flow release finish 0.5.0

# Push everything
git push origin master
git push origin develop
git push origin --tags
```

### Hotfix for Production

```bash
# Make sure master is up to date
git checkout master
git pull origin master

# Start hotfix (version bump, e.g., 0.4.0 -> 0.4.1)
git flow hotfix start 0.4.1

# Fix the critical issue
# ... make changes, commits, etc.
# (No version file to edit — the v0.4.1 tag created on finish drives MinVer.)

# Finish hotfix (merges to master and develop, creates tag v0.4.1)
git flow hotfix finish 0.4.1

# Push everything
git push origin master
git push origin develop
git push origin --tags
```

---

## Pull Request Workflow (Recommended)

While Gitflow can finish branches automatically, we recommend using PRs for code review:

### Feature with PR

```bash
# Start feature
git flow feature start my-feature

# Work on feature
# ... commits ...

# Publish to remote
git flow feature publish my-feature

# Create PR on GitHub/GitLab: feature/my-feature → develop
# Wait for review and approval

# After PR is merged, clean up local branch
git checkout develop
git pull origin develop
git branch -d feature/my-feature
```

### Release with PR

```bash
# Start release
git flow release start 0.5.0

# Prepare release
# ... commits ...

# Push release branch
git push origin release/0.5.0

# Create TWO PRs:
#   1. release/0.5.0 → master
#   2. release/0.5.0 → develop

# After both PRs are merged, create tag manually:
git checkout master
git pull origin master
git tag -a v0.5.0 -m "Release version 0.5.0"
git push origin v0.5.0

# Clean up
git branch -d release/0.5.0
git push origin --delete release/0.5.0
```

---

## Gitflow Configuration

The repository is configured with these settings:

```
Branch names:
  Production:       master
  Integration:      develop

Prefixes:
  Feature:          feature/
  Bugfix:           bugfix/
  Release:          release/
  Hotfix:           hotfix/
  Support:          support/
  Version tag:      v
```

---

## Best Practices

### DO:
✅ Always start features from `develop`
✅ Use descriptive branch names (`feature/add-user-authentication` not `feature/stuff`)
✅ Keep features small and focused
✅ Delete branches after merging
✅ Use semantic versioning for releases (MAJOR.MINOR.PATCH)
✅ Test thoroughly before finishing release branches
✅ Document changes in release branch (CHANGELOG.md)
✅ Create PR for features to enable code review

### DON'T:
❌ Commit directly to `master` or `develop`
❌ Merge features directly without going through `develop`
❌ Leave feature branches open for weeks
❌ Start new features from other feature branches
❌ Forget to merge hotfixes back to `develop`
❌ Use random version numbers
❌ Finish release without testing

---

## Semantic Versioning

This project follows [Semantic Versioning](https://semver.org/):

**Format**: `MAJOR.MINOR.PATCH[-PRERELEASE]`

- **MAJOR**: Breaking changes (e.g., `1.0.0` → `2.0.0`)
- **MINOR**: New features, backward compatible (e.g., `1.0.0` → `1.1.0`)
- **PATCH**: Bug fixes, backward compatible (e.g., `1.0.0` → `1.0.1`)
- **PRERELEASE**: Alpha, beta, rc (e.g., `1.0.0-alpha`, `1.0.0-beta.1`)

**Examples**:
- `0.4.0` → `0.5.0` (new features in pre-release)
- `0.5.0` → `1.0.0` (first stable release)
- `1.0.0` → `1.0.1` (hotfix)
- `1.0.0` → `1.1.0` (new features)
- `1.5.0` → `2.0.0` (breaking changes)

---

## Integration with CI/CD

The GitHub Actions workflow (`.github/workflows/ci.yml`) is configured to:

- **On push to `develop` or `master`**: Build, test, security scan
- **On push to `master`, `develop`, or `release/*`**: Pack NuGet packages
- **On push to `master`, `develop`, or `release/*` (after security scan)**: Publish to NuGet.org

Version numbers are managed by **MinVer** based on:
- The latest reachable `v*` annotated git tag (`MinVerTagPrefix=v`)
- Git height since that tag (commits after a tag get an `-alpha` height suffix)
- The `rc` prerelease id for release candidates (`vX.Y.Z-rc.N`)

There is no `version.json`. Tags created by Gitflow (`git flow release/hotfix finish`)
are what MinVer reads — make sure they are `v`-prefixed.

---

## Quick Reference Card

| Task | Command |
|------|---------|
| Start feature | `git flow feature start <name>` |
| Publish feature | `git flow feature publish <name>` |
| Finish feature | `git flow feature finish <name>` |
| Start bugfix | `git flow bugfix start <name>` |
| Finish bugfix | `git flow bugfix finish <name>` |
| Start release | `git flow release start <version>` |
| Finish release | `git flow release finish <version>` |
| Start hotfix | `git flow hotfix start <version>` |
| Finish hotfix | `git flow hotfix finish <version>` |
| List features | `git flow feature list` |
| Track remote feature | `git flow feature track <name>` |
| Delete feature | `git flow feature delete <name>` |

---

## Troubleshooting

### "Working tree contains unstaged changes"
Gitflow requires clean working directory. Either commit or stash changes:
```bash
git stash
git flow feature start my-feature
git stash pop
```

### "Branch already exists"
If branch exists locally but not in Gitflow:
```bash
git branch -d feature/my-feature  # Delete old branch
git flow feature start my-feature  # Recreate with Gitflow
```

### Forgot to use Gitflow for branch
You can still merge manually:
```bash
git checkout develop
git merge --no-ff feature/my-feature
git branch -d feature/my-feature
```

### Need to abort release/hotfix
```bash
# Delete the release/hotfix branch
git branch -D release/0.5.0
# Or
git branch -D hotfix/0.4.1
```

---

## Additional Resources

- [Gitflow Original Blog Post](https://nvie.com/posts/a-successful-git-branching-model/)
- [Atlassian Gitflow Workflow](https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow)
- [git-flow cheatsheet](https://danielkummer.github.io/git-flow-cheatsheet/)
- [Semantic Versioning](https://semver.org/)

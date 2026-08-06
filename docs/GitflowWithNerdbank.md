# Gitflow + Nerdbank.GitVersioning Integration

**STATUS: HISTORICAL.** FDW no longer uses Nerdbank.GitVersioning. The
current versioning tool is **MinVer** (tag-driven; see
`public/Directory.Build.props`), and there is no `version.json` in the repo.
`nbgv`/Nerdbank.GitVersioning, `GitVersion.MsBuild`, and `version.json` are
all removed. The Gitflow branching guidance below is still broadly accurate,
but every NBGV-specific section (`version.json` schema, `versionHeightOffset`,
`publicReleaseRefSpec`, the per-branch suffix table, every `nbgv` command)
describes a tool that is not configured. For current versioning behaviour see
`GitflowWorkflow.md` and the `<MinVer*>` properties in
`public/Directory.Build.props`.

> **Versioning under MinVer (current).** MinVer derives the version from the
> latest reachable `v*` annotated git tag (`MinVerTagPrefix=v`, default
> prerelease id `rc`). **To bump the version you create an annotated git tag
> on the release commit** — there is no file to edit:
>
> ```bash
> # read the version MinVer would compute right now
> minver            # or: dotnet minver  /  git describe --tags --match 'v*'
>
> # cut a release / prerelease: annotate the release commit with a v-prefixed tag
> git tag -a v1.3.0       -m "Release 1.3.0"
> git tag -a v1.3.0-rc.1  -m "1.3.0 release candidate 1"
> git push origin --tags
> ```
>
> Commits after a tag get that tag's version with a height-based `-alpha`
> suffix until the next tag. Everything currently targets `v1.3.0-rc.1`.
> Wherever the historical text below says "edit `version.json`" or
> "`nbgv prepare-release`", substitute "create an annotated `vX.Y.Z[-rc.N]`
> tag on the release commit."

This document explains how Gitflow branching workflow integrates with Nerdbank.GitVersioning for automatic semantic versioning.

## Overview

**Gitflow** manages your branches, while **Nerdbank.GitVersioning (NBGV)** automatically generates version numbers based on:
- Base version in `version.json`
- Git commit height (number of commits)
- Branch name
- Git tags

Together, they provide a seamless workflow where version numbers are automatically managed based on your Git workflow.

---

## How It Works

### Branch-Based Versioning

Nerdbank.GitVersioning generates different version numbers depending on which branch you're on:

| Branch | Version Format | Example | NuGet Version | Public Release |
|--------|---------------|---------|---------------|----------------|
| `master` | `{version}` | `0.4.1001` | `0.4.1001` | ✅ Yes |
| `develop` | `{version}-alpha.{height}` | `0.4-alpha.1045` | `0.4.0-alpha.1045` | ✅ Yes |
| `release/0.5` | `{version}-rc.{height}` | `0.5.0-rc.3` | `0.5.0-rc.3` | ✅ Yes |
| `feature/auth` | `{version}-alpha.{height}.{commitId}` | `0.4-alpha.1050.abc1234` | `0.4.0-alpha.1050` | ❌ No |
| `hotfix/0.4.1` | `{version}-beta.{height}.{commitId}` | `0.4.1-beta.5.def5678` | `0.4.1-beta.5` | ❌ No |

**Key Points**:
- `{height}` = Number of commits since version was last changed in `version.json`
- `{commitId}` = Short Git commit SHA (included only for non-public releases)
- Public releases (master, develop, release/*) get clean version numbers
- Feature/hotfix branches get commit IDs for traceability

---

## Configuration Explained

Your current `version.json`:

```json
{
  "version": "0.4-alpha.{height}",
  "versionHeightOffset": 1000,
  "nuGetPackageVersion": {
    "semVer": 2
  },
  "publicReleaseRefSpec": [
    "^refs/heads/master$",
    "^refs/heads/develop$",
    "^refs/heads/release/\\d+\\.\\d+",
    "^refs/tags/v\\d+\\.\\d+\\.\\d+"
  ],
  "cloudBuild": {
    "buildNumber": {
      "enabled": true,
      "includeCommitId": {
        "when": "nonPublicReleaseOnly",
        "where": "buildMetadata"
      }
    }
  },
  "release": {
    "branchName": "release/{version}",
    "versionIncrement": "minor",
    "firstUnstableTag": "rc"
  }
}
```

### Settings Breakdown

**`"version": "0.4-alpha.{height}"`**
- Base version: `0.4`
- Default prerelease tag: `alpha`
- `{height}` placeholder replaced with commit count

**`"versionHeightOffset": 1000`**
- Adds 1000 to commit height
- Version looks like `0.4-alpha.1045` instead of `0.4-alpha.45`
- Useful for migrating from other versioning systems

**`"publicReleaseRefSpec"`**
- Defines which branches/tags produce "public release" versions
- Public releases don't include commit SHA in version
- Matches: `master`, `develop`, `release/*`, and tags like `v0.5.0`

**`"release.branchName": "release/{version}"`**
- Template for release branches created by `nbgv prepare-release`
- Matches Gitflow convention: `release/0.5.0`, `release/1.0.0`

**`"release.versionIncrement": "minor"`**
- When creating release, increment MINOR version
- `0.4.x` → `0.5.0`

**`"release.firstUnstableTag": "rc"`**
- Release branches use `-rc` suffix
- `release/0.5.0` → `0.5.0-rc.1`, `0.5.0-rc.2`, etc.

---

## Gitflow + NBGV Workflows

### 1. Feature Development

```bash
# Start feature from develop
git flow feature start add-authentication

# Current version on feature branch
# → 0.4-alpha.1050.abc1234 (includes commit SHA)

# Work on feature, make commits
git commit -m "Add authentication service"
# → 0.4-alpha.1051.def5678

# Finish feature (merges to develop)
git flow feature finish add-authentication

# Back on develop
# → 0.4-alpha.1052 (clean version, no commit SHA)
```

**Version behavior**:
- Feature branches get commit SHA appended (non-public release)
- After merge to `develop`, version is clean (public release)
- Height increases with each commit

### 2. Creating Release

**Option A: Using Gitflow**

```bash
# Make sure develop is ready
git checkout develop
git pull origin develop

# Start release with Gitflow
git flow release start 0.5.0

# NBGV will detect release/0.5.0 branch
# → Version: 0.5.0-rc.1

# Make release commits (update CHANGELOG, fix bugs)
git commit -m "Update CHANGELOG for 0.5.0"
# → Version: 0.5.0-rc.2

# Finish release (merges to master and develop, creates tag v0.5.0)
git flow release finish 0.5.0
# Prompted for tag message - enter "Release 0.5.0"

# On master with tag v0.5.0
# → Version: 0.5.0

# Push everything
git push origin master develop --tags
```

**Option B: Using NBGV (Recommended)**

```bash
# Use NBGV to create release and update version.json
nbgv prepare-release

# This creates release/0.5.0 branch and commits updated version.json
# → Version on release/0.5.0: 0.5.0-rc.1
# → Version on develop bumped to: 0.5-alpha.{height}

# Make any final release commits
git commit -m "Update CHANGELOG for 0.5.0"

# Merge to master manually or with PR
git checkout master
git merge --no-ff release/0.5.0
git tag -a v0.5.0 -m "Release 0.5.0"

# Merge back to develop
git checkout develop
git merge --no-ff release/0.5.0

# Push everything
git push origin master develop --tags

# Delete release branch
git branch -d release/0.5.0
git push origin --delete release/0.5.0
```

### 3. Hotfix for Production

```bash
# Start hotfix from master
git checkout master
git pull origin master
git flow hotfix start 0.5.1

# NBGV detects hotfix branch
# → Version: 0.5.1-beta.1.abc1234 (includes commit SHA, non-public)

# Fix the bug
git commit -m "Fix critical security issue"
# → Version: 0.5.1-beta.2.def5678

# Finish hotfix (merges to master and develop, creates tag v0.5.1)
git flow hotfix finish 0.5.1

# On master with tag v0.5.1
# → Version: 0.5.1

# Push everything
git push origin master develop --tags
```

**Version behavior**:
- Hotfix branches are non-public (include commit SHA)
- After tagging, master gets clean version from tag
- Hotfix changes automatically flow back to develop

---

## Version Number Examples

### Scenario 1: Normal Development Flow

```
master (v0.4.0)
  └─> develop (0.4-alpha.1000)
       └─> feature/auth (0.4-alpha.1005.abc1234)
            [3 commits]
       ←─ merge back
       develop (0.4-alpha.1006)
       └─> feature/cache (0.4-alpha.1008.def5678)
            [2 commits]
       ←─ merge back
       develop (0.4-alpha.1010)
```

### Scenario 2: Release Flow

```
develop (0.4-alpha.1050)
  └─> release/0.5.0 (0.5.0-rc.1)
       [1 commit - update CHANGELOG]
       release/0.5.0 (0.5.0-rc.2)
       ├─> merge to master
       │   master + tag v0.5.0 (0.5.0)
       └─> merge to develop
           develop (0.5-alpha.1052)  # version.json updated by nbgv prepare-release
```

### Scenario 3: Hotfix Flow

```
master (v0.5.0)
  └─> hotfix/0.5.1 (0.5.1-beta.1.abc1234)
       [2 commits - fix bug]
       hotfix/0.5.1 (0.5.1-beta.3.def5678)
       ├─> merge to master
       │   master + tag v0.5.1 (0.5.1)
       └─> merge to develop
           develop (0.5-alpha.1055)
```

---

## NBGV Commands

### Get Current Version
```bash
# Show computed version for current branch
nbgv get-version

# Output:
# Version:                      0.4.1050
# AssemblyVersion:              0.4.0.0
# AssemblyInformationalVersion: 0.4.1050+abc1234
# NuGetPackageVersion:          0.4.0-alpha.1050
```

### Prepare Release (Recommended)
```bash
# Create release branch and update version.json
nbgv prepare-release

# This:
# 1. Creates release/{version} branch
# 2. Commits updated version.json to develop (bumps to next version)
# 3. Leaves you on release branch ready to finalize
```

### Set Version
```bash
# Change base version in version.json
nbgv set-version 1.0

# Result in version.json:
# "version": "1.0-alpha.{height}"
```

### Tag Release
```bash
# Create version tag from current branch
nbgv tag

# Creates tag like v0.5.0 based on current version
```

### Install NBGV CLI
```bash
# Install globally
dotnet tool install -g nbgv

# Or install locally in repo
dotnet tool install nbgv
dotnet nbgv get-version
```

---

## Best Practices

### DO:
✅ Use `nbgv prepare-release` to create releases (handles version.json updates)
✅ Always tag releases on `master` (Gitflow does this automatically)
✅ Let NBGV compute versions - don't hardcode in .csproj files
✅ Keep `version.json` in sync across branches
✅ Use semantic versioning for release names
✅ Check version before publishing: `nbgv get-version`

### DON'T:
❌ Manually edit version numbers in .csproj files (NBGV overwrites them)
❌ Create tags without proper version format (use `v{major}.{minor}.{patch}`)
❌ Forget to update version.json on develop after release
❌ Use Gitflow release names that don't match semantic versioning
❌ Skip tagging releases (NBGV uses tags to compute versions)

---

## Troubleshooting

### Version shows commit SHA on develop/master
**Problem**: Branch not recognized as public release
**Solution**: Check `publicReleaseRefSpec` in version.json includes the branch pattern

### Version not incrementing
**Problem**: `version.json` not committed or height offset issue
**Solution**:
```bash
# Check version.json is committed
git status

# View computed version
nbgv get-version

# Check commit height
git log --oneline | wc -l
```

### Release branch has wrong version
**Problem**: Branch name doesn't match pattern in version.json
**Solution**: Ensure release branch named `release/X.Y.Z` matches `release/{version}` pattern

### Conflict in version.json during merge
**Problem**: Both develop and release branch modified version.json
**Solution**:
```bash
# During merge conflict, keep develop's version (develop should be next version)
git checkout develop version.json
git add version.json
git commit
```

### Want to change version increment (minor vs major)
**Problem**: Need major version bump instead of minor
**Solution**:
```bash
# Edit version.json before creating release
git checkout develop
# Change "versionIncrement": "major" in version.json
git commit -m "Prepare for major version release"

# Then create release
nbgv prepare-release
```

---

## CI/CD Integration

Your GitHub Actions workflow (`.github/workflows/ci.yml`) automatically works with NBGV:

### During Build
```yaml
- name: Restore packages
  run: dotnet restore  # NBGV runs here, computes version

- name: Build
  run: dotnet build --configuration Release  # Uses version from NBGV
```

### Version in Package
```bash
# NBGV automatically sets:
# - AssemblyVersion
# - FileVersion
# - InformationalVersion
# - PackageVersion

# No manual version management needed in .csproj files!
```

### Publish Conditions
```yaml
# Publishes packages only from public release branches
if: github.ref == 'refs/heads/master' ||
    github.ref == 'refs/heads/develop' ||
    startsWith(github.ref, 'refs/heads/release/')
```

---

## Quick Reference

| Task | Command |
|------|---------|
| Check current version | `nbgv get-version` |
| Create release | `nbgv prepare-release` |
| Set base version | `nbgv set-version X.Y` |
| Tag current commit | `nbgv tag` |
| Install NBGV CLI | `dotnet tool install -g nbgv` |
| View version in build | Check `AssemblyInformationalVersion` attribute |

---

## Example: Complete Release Flow

```bash
# 1. Prepare release on develop
git checkout develop
git pull origin develop
nbgv get-version
# → 0.4-alpha.1050

# 2. Create release using NBGV
nbgv prepare-release
# → Creates release/0.5.0
# → Updates version.json on develop to 0.5-alpha.{height}
# → Switches to release/0.5.0

# 3. Finalize release
git checkout release/0.5.0
nbgv get-version
# → 0.5.0-rc.1

# Update CHANGELOG.md
git commit -m "Update CHANGELOG for 0.5.0"

# 4. Merge to master (via PR or direct)
git checkout master
git merge --no-ff release/0.5.0
git tag -a v0.5.0 -m "Release version 0.5.0"

nbgv get-version
# → 0.5.0

# 5. Merge to develop
git checkout develop
git merge --no-ff release/0.5.0

# 6. Push everything
git push origin master develop --tags

# 7. Clean up release branch
git branch -d release/0.5.0
git push origin --delete release/0.5.0

# 8. Verify versions
git checkout master && nbgv get-version  # → 0.5.0
git checkout develop && nbgv get-version  # → 0.5-alpha.1052
```

---

## Summary

**Gitflow** provides the branching structure:
- `master` - Production releases
- `develop` - Integration branch
- `feature/*` - New features
- `release/*` - Release preparation
- `hotfix/*` - Production fixes

**Nerdbank.GitVersioning** automatically generates versions:
- Computes version from Git history
- Uses branch names to apply prerelease tags
- Increments versions on release
- Handles semantic versioning

**Together** they provide:
- Automatic version management
- No manual version editing
- Clear version numbers based on workflow
- Seamless CI/CD integration
- Proper semantic versioning

This configuration means you **never manually edit version numbers** - just follow Gitflow, and NBGV handles versioning automatically!

#!/usr/bin/env bash
# One allocated release (FDW-3350 / FDW-3364 / FDW-4784), in phases, for .github/workflows/publish-nuget.yml on the GitHub
# mirror (the repository root holds src/ and tests/). Each phase is one workflow step; the push itself is the
# workflow's (sign-in rounds, scripts/push-nuget.sh), so a long quota wait never uses a dead key.
#
#   release-allocated.sh plan     plan the release from the baseline and the feed; allocate the bundle's number
#   release-allocated.sh levels   give every package its own version: read each changed package's public surface (and its last
#                                 stable version's), let tools/versioning/release.py decide the lowest level each needs, add the
#                                 packages a major pulls in
#   release-allocated.sh build    prepare the build set, build and test it, pack its members into artifacts/ at their own versions
#   release-allocated.sh bindings prove that a package left as it is still loads with the rebuilt ones: no built assembly is below
#                                 the floor or lower than a version the last release's dlls ask for (tools/versioning/bindcheck.py)
#   release-allocated.sh decide   check-then-push: decide every package before anything is pushed
#   release-allocated.sh bundle   generate and pack the release bundle into bundle/ (pushed last, by the workflow)
#
# Every value comes from the environment; the workflow declares the defaults once. A missing value fails the run and
# names it. Nothing is assumed.
#
#   BRANCH            main or prerelease: main publishes the stable version N, prerelease the candidate N-rc.<k>
#   LINE              the release line, major.minor (for example 1.5)
#   BUMP              patch | minor | major: the mirror has no commits to read it from, so the person cutting the
#                     release states it
#   BUNDLE_ID         the bundle package id (Fdw.Bundle)
#   BASELINE_FILE     the project manifest of the last release tree, for the first release under this scheme; empty
#                     reads the baseline from the latest stable bundle on nuget.org
#   (levels phase)
#   (the bundle's version is not an input: `levels` decides it, N or N-rc.<k> on prerelease, from the highest package level, and prints it)
#   DUMPER_PROJECT    the Fdw.ApiSurface project folder (tools/Fdw.ApiSurface)
#   FIRST_VERSIONS_FILE, SETTINGS_FILE   tools/versioning/first-versions.json and apisurface-settings.json
#   The assembly version floors are not environment values: `plan` reads them from the baseline manifest (assemblyVersionFloors, one per
#   assembly) into plan.json, `levels` works out each package's AssemblyVersion from them (assembly-versions.json), `bindings` checks the
#   built dlls against them, and `bindings` records the floors for the NEXT release in project-manifest.json.
#   LEVELS_MAX_ROUNDS how many packages may join after the first pass before the phase stops
#   MAX_LEVEL         optional: the highest level any package may take in this release (patch, minor or major). Empty = no cap. A package
#                     whose surface says a higher level takes the cap; levels.json keeps what the surface said (capped_from) for the
#                     release notes. It is the release owner's decision, stated by the setting max_level; nothing in the job sets it.
#   WORK_DIR          where the plan files and reports go (not the packages)
#   GITHUB_OUTPUT     GitHub's step output file
#
# Exit 0: the phase ended. Exit 1: the phase failed, and the message says which package or file and why.
set -uo pipefail

TOOLS="tools/versioning"
ARTIFACTS="artifacts"
BUNDLE_OUT="bundle"
# The version a package is packed at only to read its public surface. It is never published and never reaches a manifest.
SURFACE_PACK_VERSION="0.0.0-surface.1"

fail() {
  echo "::error::release-allocated: $*"
  exit 1
}

require() {
  for name in "$@"; do
    [ -n "${!name:-}" ] || fail "environment variable $name is not set."
  done
}

json_get() {
  python3 -c 'import json,sys; d=json.load(open(sys.argv[1])); print(d[sys.argv[2]] if len(sys.argv)==3 else d[sys.argv[2]][sys.argv[3]])' "$@"
}

plan() {
  require BRANCH LINE BUMP BUNDLE_ID WORK_DIR GITHUB_OUTPUT
  [[ "$LINE" =~ ^[0-9]+[.][0-9]+$ ]] || fail "LINE is '$LINE' but must be major.minor, for example 1.5."
  case "$BUMP" in patch|minor|major) ;; *) fail "BUMP is '$BUMP' but must be patch, minor or major." ;; esac
  case "$BRANCH" in main|prerelease) ;; *) fail "BRANCH is '$BRANCH': an allocated release runs on main (stable) or prerelease (candidate) only." ;; esac

  if [ -n "${BASELINE_FILE:-}" ]; then
    [ -f "$BASELINE_FILE" ] || fail "BASELINE_FILE '$BASELINE_FILE' does not exist."
    python3 "$TOOLS/release.py" plan --repo . --line "$LINE" --bundle-id "$BUNDLE_ID" --out "$WORK_DIR" \
      --baseline "$BASELINE_FILE" --bump "$BUMP" || fail "release.py could not plan the release from baseline file $BASELINE_FILE; its message is above."
  else
    python3 "$TOOLS/release.py" plan --repo . --line "$LINE" --bundle-id "$BUNDLE_ID" --out "$WORK_DIR" \
      --baseline-from-bundle --bump "$BUMP" || fail "release.py could not plan the release from the latest stable $BUNDLE_ID; its message is above. For the first release under this scheme set NUGET_BASELINE_FILE."
  fi

  local number members version
  number=$(json_get "$WORK_DIR/plan.json" release) || fail "no release number in $WORK_DIR/plan.json."
  members=$(python3 -c 'import json,sys; print(",".join(json.load(open(sys.argv[1]))["members"]))' "$WORK_DIR/plan.json") \
    || fail "no members in $WORK_DIR/plan.json."
  if [ -z "$members" ]; then
    echo "release-allocated: no package changed since the baseline; there is nothing to release."
    echo "should-release=false" >> "$GITHUB_OUTPUT"
    return 0
  fi

  if [ "$BRANCH" = "main" ]; then
    version="$number"
  else
    # The candidate number k: one above the candidates of this release the bundle already has. The bundle is
    # published for every release, so it counts them for the whole set. A rerun after a candidate that pushed
    # nothing reuses its k; one that pushed some is a new candidate.
    local k
    k=$(PYTHONPATH="$TOOLS" python3 -c '
import sys
from feeds import NugetOrg
bundle, number = sys.argv[1], sys.argv[2]
held = [v for v in NugetOrg().versions(bundle) if v.lower().startswith(number + "-rc.")]
print(len(held) + 1)' "$BUNDLE_ID" "$number") || fail "could not read the candidates of $BUNDLE_ID $number from nuget.org; nothing is assumed."
    version="${number}-rc.${k}"
  fi

  # The manifest carries the version the bundle will have: N, or N-rc.<k>. The unchanged packages keep their stable
  # versions either way.
  python3 -c '
import json,sys
p=sys.argv[1]; m=json.load(open(p)); m["version"]=sys.argv[2]
for k,v in list(m["packages"].items()):
    if v==sys.argv[3]: m["packages"][k]=sys.argv[2]
json.dump(m,open(p,"w"),indent=1,sort_keys=True)' "$WORK_DIR/manifest.json" "$version" "$number" \
    || fail "could not write version $version into $WORK_DIR/manifest.json."

  echo "release-allocated: release $number ($BUMP), version $version, $(echo "$members" | tr ',' '\n' | wc -l) package(s) to build and push."
  {
    echo "should-release=true"
    echo "number=$number"
    echo "version=$version"
    echo "members=$members"
  } >> "$GITHUB_OUTPUT"
}

# Dumps a public surface with Fdw.ApiSurface. Every use of the dumper's command line is on a line marked ASSUMES-DUMPER-CLI, so
# the first run can check exactly those lines against tools/Fdw.ApiSurface/README.md and Program.cs.
dump_surface() {
  # ASSUMES-DUMPER-CLI: `dotnet <dll> --package <file.nupkg> --out <file>` writes the sorted surface records and exits 0.
  dotnet "$WORK_DIR/dumper/Fdw.ApiSurface.dll" --package "$1" --out "$2" || fail "Fdw.ApiSurface could not dump $1; its message is above."
}

make_surface() {
  local side="$1" package="$2" what="$3" nupkg
  local packs="$WORK_DIR/surface-packs"
  case "$side" in
    old)
      mkdir -p "$packs/old"
      python3 "$TOOLS/surfaces.py" fetch --package-id "$package" --version "$what" --out "$packs/old/$package.nupkg" \
        || fail "could not fetch $package $what from nuget.org for its old surface; its message is above."
      dump_surface "$packs/old/$package.nupkg" "$WORK_DIR/surfaces/old/$package.surface"
      ;;
    new)
      rm -rf "$packs/new/$package"
      # Build, then pack with no build: a project that sets GeneratePackageOnBuild (every source generator) is not built by `dotnet pack`,
      # so a pack alone has no dll to put in the package (NU5019). The same properties go to both, or the pack would rebuild.
      dotnet build "$what" -c Release -p:Version="$SURFACE_PACK_VERSION" \
        || fail "dotnet build of $what, to read the surface of $package, failed; its output is above."
      dotnet pack "$what" -c Release --no-build -p:Version="$SURFACE_PACK_VERSION" -o "$packs/new/$package" \
        || fail "dotnet pack of $what, to read the surface of $package, failed; its output is above."
      # dotnet pack of a project also packs the packable projects it references into the same folder, so the package is taken by its
      # id and the stand-in version, never by "the only one".
      nupkg="$packs/new/$package/$package.$SURFACE_PACK_VERSION.nupkg"
      [ -f "$nupkg" ] || fail "packing $what left no $package.$SURFACE_PACK_VERSION.nupkg in $packs/new/$package/; found: $(ls "$packs/new/$package" 2>/dev/null | tr '\n' ' ')."
      dump_surface "$nupkg" "$WORK_DIR/surfaces/new/$package.surface"
      ;;
    *) fail "unknown surface side '$side' for $package." ;;
  esac
}

levels() {
  require WORK_DIR BRANCH BUNDLE_ID DUMPER_PROJECT FIRST_VERSIONS_FILE SETTINGS_FILE LEVELS_MAX_ROUNDS GITHUB_OUTPUT
  [[ "$LEVELS_MAX_ROUNDS" =~ ^[0-9]+$ ]] || fail "LEVELS_MAX_ROUNDS is '$LEVELS_MAX_ROUNDS' but must be a whole number."
  rm -rf "$WORK_DIR/surfaces" "$WORK_DIR/surface-packs" "$WORK_DIR/decided" "$WORK_DIR/dumper"
  mkdir -p "$WORK_DIR/surfaces/old" "$WORK_DIR/surfaces/new"

  case "$BRANCH" in main|prerelease) ;; *) fail "BRANCH is '$BRANCH': an allocated release runs on main (stable) or prerelease (candidate) only." ;; esac
  local cap=()
  if [ -n "${MAX_LEVEL:-}" ]; then
    case "$MAX_LEVEL" in patch|minor|major) cap=(--max-level "$MAX_LEVEL") ;; *) fail "MAX_LEVEL is '$MAX_LEVEL' but must be patch, minor, major or empty." ;; esac
    echo "release-allocated: level cap $MAX_LEVEL: a package whose surface says more takes $MAX_LEVEL, and the release notes keep what the surface said."
  fi

  # ASSUMES-DUMPER-CLI: publishing the project leaves Fdw.ApiSurface.dll (a framework-dependent console app) in the output folder.
  dotnet publish "$DUMPER_PROJECT" -c Release -o "$WORK_DIR/dumper" || fail "Fdw.ApiSurface did not publish; its output is above."
  [ -f "$WORK_DIR/dumper/Fdw.ApiSurface.dll" ] || fail "publishing $DUMPER_PROJECT left no Fdw.ApiSurface.dll in $WORK_DIR/dumper."

  # First pass: every changed package that has a surface of its own. Later passes make only what `decide` reports missing.
  python3 "$TOOLS/surfaces.py" needed --plan-dir "$WORK_DIR" > "$WORK_DIR/surfaces-needed.tsv" \
    || fail "surfaces.py could not list the surfaces to make; its message is above."
  local side package what
  # The list is read on descriptor 3 so a dotnet command inside make_surface cannot swallow it from stdin.
  while IFS=$'\t' read -r side package what <&3; do
    make_surface "$side" "$package" "$what"
  done 3< "$WORK_DIR/surfaces-needed.tsv"

  local code round=0
  while true; do
    python3 "$TOOLS/release.py" decide --plan-dir "$WORK_DIR" --old-surfaces "$WORK_DIR/surfaces/old" --new-surfaces "$WORK_DIR/surfaces/new" \
      --first-versions "$FIRST_VERSIONS_FILE" --settings "$SETTINGS_FILE" --out "$WORK_DIR/decided" \
      --missing-out "$WORK_DIR/missing-surface.json" "${cap[@]}"
    code=$?
    [ "$code" -eq 0 ] && break
    [ "$code" -eq 3 ] || fail "release.py decide could not decide the versions; its message is above. Nothing is assumed."
    round=$((round + 1))
    [ "$round" -le "$LEVELS_MAX_ROUNDS" ] || fail "more than $LEVELS_MAX_ROUNDS surfaces were still missing after the first pass (LEVELS_MAX_ROUNDS); the last was in $WORK_DIR/missing-surface.json."
    package=$(json_get "$WORK_DIR/missing-surface.json" package)
    side=$(json_get "$WORK_DIR/missing-surface.json" side)
    python3 "$TOOLS/surfaces.py" needed --plan-dir "$WORK_DIR" --package "$package" --side "$side" > "$WORK_DIR/surfaces-needed.tsv" \
      || fail "surfaces.py could not say how to make the $side surface of $package; its message is above."
    IFS=$'\t' read -r side package what < "$WORK_DIR/surfaces-needed.tsv"
    make_surface "$side" "$package" "$what"
  done

  # The bundle's number follows the highest package level, so it is known only now. On prerelease the candidate number k counts the
  # candidates of THAT number the bundle already has, and every package takes the same -rc.<k>: decide runs once more with it.
  local number version
  number=$(json_get "$WORK_DIR/decided/plan.json" release) || fail "no release number in $WORK_DIR/decided/plan.json."
  version="$number"
  if [ "$BRANCH" = "prerelease" ]; then
    local k
    k=$(PYTHONPATH="$TOOLS" python3 -c '
import sys
from feeds import NugetOrg
bundle, number = sys.argv[1], sys.argv[2]
held = [v for v in NugetOrg().versions(bundle) if v.lower().startswith(number + "-rc.")]
print(len(held) + 1)' "$BUNDLE_ID" "$number") || fail "could not read the candidates of $BUNDLE_ID $number from nuget.org; nothing is assumed."
    version="${number}-rc.${k}"
    python3 "$TOOLS/release.py" decide --plan-dir "$WORK_DIR" --old-surfaces "$WORK_DIR/surfaces/old" --new-surfaces "$WORK_DIR/surfaces/new" \
      --first-versions "$FIRST_VERSIONS_FILE" --settings "$SETTINGS_FILE" --out "$WORK_DIR/decided" --candidate "$k" "${cap[@]}" \
      || fail "release.py decide could not make the release candidate $version; its message is above."
  fi

  # The decided files replace the planned ones for every later phase; the planned ones are kept for the record.
  mkdir -p "$WORK_DIR/planned"
  local f
  for f in plan versions trees manifest; do
    cp "$WORK_DIR/$f.json" "$WORK_DIR/planned/$f.json"
    cp "$WORK_DIR/decided/$f.json" "$WORK_DIR/$f.json"
  done
  cp "$WORK_DIR/decided/package-versions.json" "$WORK_DIR/decided/assembly-versions.json" "$WORK_DIR/decided/levels.json" "$WORK_DIR"
  python3 -c 'import json,sys; print("\n".join(json.load(open(sys.argv[1]))["report"]))' "$WORK_DIR/levels.json"

  local members
  members=$(python3 -c 'import json,sys; print(",".join(json.load(open(sys.argv[1]))["members"]))' "$WORK_DIR/plan.json") \
    || fail "no members in the decided $WORK_DIR/plan.json."
  [ -n "$members" ] || fail "decide left no package to build, but the plan had members."
  echo "release-allocated: versions decided for $(echo "$members" | tr ',' '\n' | wc -l) package(s); the bundle is $version."
  {
    echo "members=$members"
    echo "number=$number"
    echo "version=$version"
  } >> "$GITHUB_OUTPUT"
}

build() {
  require MEMBERS WORK_DIR
  rm -rf "$ARTIFACTS"
  mkdir -p "$ARTIFACTS"
  python3 "$TOOLS/buildset.py" prepare --repo . --set "$MEMBERS" --versions "$WORK_DIR/versions.json" \
    --trees "$WORK_DIR/trees.json" --package-versions "$WORK_DIR/package-versions.json" --assembly-versions "$WORK_DIR/assembly-versions.json" > "$WORK_DIR/prepare.json" \
    || fail "buildset.py could not prepare the build set; its message is above and nothing was written."
  echo "release-allocated: build set prepared; embeds outside the set: $(python3 -c 'import json,sys; print(json.load(open(sys.argv[1]))["embeds_not_in_set"])' "$WORK_DIR/prepare.json")"

  python3 - "$WORK_DIR/prepare.json" "$ARTIFACTS" <<'PY' || fail "a build, pack or test command of the build set failed; the command and its output are above."
import json, os, subprocess, sys
report = json.load(open(sys.argv[1]))
out = os.path.abspath(sys.argv[2])
run_from = report["run_from"] or "."
for cmd in report["commands"]:
    if cmd[:2] == ["dotnet", "pack"]:
        cmd = cmd + ["-o", out]
    print("::group::" + " ".join(cmd), flush=True)
    code = subprocess.run(cmd, cwd=run_from).returncode
    print("::endgroup::", flush=True)
    if code != 0:
        print(f"command failed with exit code {code}: {' '.join(cmd)}", file=sys.stderr)
        sys.exit(1)
PY
  local packed
  packed=$(find "$ARTIFACTS" -maxdepth 1 -name '*.nupkg' | wc -l)
  [ "$packed" -gt 0 ] || fail "the build set produced no package in $ARTIFACTS/."
  echo "release-allocated: packed $packed package(s)."
}


dump_identity() {
  # ASSUMES-DUMPER-CLI: `dotnet <dll> --package <file.nupkg> --identity --out <file>` writes DEF and REF records of the package's runtime assemblies.
  dotnet "$WORK_DIR/dumper/Fdw.ApiSurface.dll" --package "$1" --identity --out "$2" || fail "Fdw.ApiSurface could not dump the identity of $1; its message is above."
}

bindings() {
  require WORK_DIR
  python3 -c '
import json, sys
json.dump(json.load(open(sys.argv[1]))["assembly_version_floors"], open(sys.argv[2], "w"), indent=1, sort_keys=True)' "$WORK_DIR/plan.json" "$WORK_DIR/floors.json" \
    || fail "no assembly_version_floors in $WORK_DIR/plan.json; the baseline manifest must state them."
  [ -f "$WORK_DIR/dumper/Fdw.ApiSurface.dll" ] || fail "the dumper from the levels phase is not in $WORK_DIR/dumper; run that phase first."
  local ident="$WORK_DIR/identities" packs="$WORK_DIR/identity-packs"
  rm -rf "$ident" "$packs"
  mkdir -p "$ident/new" "$ident/held" "$packs"
  local nupkg
  for nupkg in "$ARTIFACTS"/*.nupkg; do
    [ -e "$nupkg" ] || fail "no package in $ARTIFACTS/ to check; run the build phase first."
    # named by package id (the file name minus .<version>.nupkg), as the held ones are, so `bare` can tell one package from another
    dump_identity "$nupkg" "$ident/new/$(basename "$nupkg" .nupkg | sed -E 's/\.[0-9]+\.[0-9]+\.[0-9]+(-.*)?$//').txt"
  done
  # The dlls that may stay as they are: the last stable version of every package of the last release. A rebuilt dependency has to
  # bind for each of them, so the check does not depend on which packages a given release happens to rebuild.
  python3 -c '
import json, sys
for package, version in sorted(json.load(open(sys.argv[1]))["last_stable"].items()):
    print(package + "\t" + version)' "$WORK_DIR/levels-input.json" > "$WORK_DIR/identities-held.tsv" \
    || fail "could not list the last stable versions from $WORK_DIR/levels-input.json."
  local package version
  while IFS=$'\t' read -r package version <&3; do
    python3 "$TOOLS/surfaces.py" fetch --package-id "$package" --version "$version" --out "$packs/$package.nupkg" \
      || fail "could not fetch $package $version from nuget.org for the binding check; its message is above."
    dump_identity "$packs/$package.nupkg" "$ident/held/$package.txt"
  done 3< "$WORK_DIR/identities-held.tsv"
  python3 "$TOOLS/bindcheck.py" check --new-dir "$ident/new" --held-dir "$ident/held" --floors "$WORK_DIR/floors.json" \
    || fail "a package left as it is would not load with a rebuilt one (listed above); nothing is pushed."
  # The floors for the next release: for each assembly, the higher of the version it carries and the highest any dll of this release asks
  # for it at. They go into the project manifest the bundle carries, which the next release reads as its baseline.
  python3 "$TOOLS/bindcheck.py" floors --dir "$ident/new" --dir "$ident/held" > "$WORK_DIR/next-floors.json" \
    || fail "could not measure the assembly version floors of this release; its message is above."
  python3 "$TOOLS/bindcheck.py" bare --dir "$ident/held" --dir "$ident/new" > "$WORK_DIR/next-bare.json" \
    || fail "could not list the packages without a runtime assembly; its message is above."
  python3 - "$WORK_DIR/project-manifest.json" "$WORK_DIR/next-floors.json" "$WORK_DIR/next-bare.json" <<'PY' || fail "could not record the assembly version floors in $WORK_DIR/project-manifest.json."
import json, sys
path, floors, bare = sys.argv[1], sys.argv[2], sys.argv[3]
manifest = json.load(open(path, encoding="utf-8"))
manifest["assemblyVersionFloors"] = json.load(open(floors, encoding="utf-8"))
manifest["packagesWithoutRuntimeAssembly"] = json.load(open(bare, encoding="utf-8"))
with open(path, "w", encoding="utf-8", newline="\n") as f:
    json.dump(manifest, f, indent=1, sort_keys=True)
    f.write("\n")
PY
  echo "release-allocated: bindings ok; the floors for the next release are in $WORK_DIR/next-floors.json."
}

decide() {
  require WORK_DIR GITHUB_OUTPUT
  python3 "$TOOLS/pushcheck.py" check --packages "$ARTIFACTS" --json > "$WORK_DIR/decide.json"
  local code=$?
  # pushcheck exits 1 for a stop and also when it crashes (nuget.org unreachable); only a readable report is a decision.
  python3 - "$WORK_DIR/decide.json" <<'PY' || fail "pushcheck.py produced no readable decision (its error is above); nothing is assumed and nothing is pushed."
import json, sys
d = json.load(open(sys.argv[1]))
for x in d["decisions"]:
    print(f"  {x['action']:5s} {x['package']} {x['version']}: {x['reason']}")
PY
  if [ "$code" -ne 0 ]; then
    fail "at least one package is a stop (listed above): a version nuget.org already holds has other contents, or a package has no tree marker. Nothing is pushed. Fix the cause; the numbers not pushed are not spent."
  fi
  # A package already on nuget.org with the same contents is not pushed again: move it, with its symbols, out of
  # artifacts/ so the push step sees only what is missing.
  python3 - "$WORK_DIR/decide.json" "$ARTIFACTS" <<'PY' || fail "could not set aside the packages that are already on nuget.org."
import json, os, shutil, sys
d = json.load(open(sys.argv[1]))
skipped = os.path.join(os.path.dirname(os.path.abspath(sys.argv[2])), "artifacts-skipped")
os.makedirs(skipped, exist_ok=True)
for x in d["decisions"]:
    if x["action"] != "skip":
        continue
    for p in (x["path"], x["path"][:-len(".nupkg")] + ".snupkg"):
        if os.path.exists(p):
            shutil.move(p, skipped)
PY
  local left
  left=$(find "$ARTIFACTS" -maxdepth 1 -name '*.nupkg' | wc -l)
  echo "release-allocated: $left package(s) to push."
  echo "to-push=$left" >> "$GITHUB_OUTPUT"
}

bundle() {
  require VERSION WORK_DIR BUNDLE_ID
  rm -rf bundle-src "$BUNDLE_OUT"
  python3 "$TOOLS/hostprops.py" generate --manifest "$WORK_DIR/manifest.json" --out "$WORK_DIR/Fdw.PackageVersions.props" \
    || fail "hostprops.py could not write Fdw.PackageVersions.props from $WORK_DIR/manifest.json; its message is above."
  python3 "$TOOLS/bundle.py" generate --manifest "$WORK_DIR/manifest.json" --out bundle-src \
    --project-manifest "$WORK_DIR/project-manifest.json" --package-versions "$WORK_DIR/Fdw.PackageVersions.props" || fail "bundle.py could not generate the bundle from $WORK_DIR/manifest.json; its message is above."
  dotnet pack "bundle-src/$BUNDLE_ID.csproj" -c Release -o "$BUNDLE_OUT" -p:Version="$VERSION" \
    || fail "dotnet pack of the bundle $BUNDLE_ID $VERSION failed; its output is above."
  [ "$(find "$BUNDLE_OUT" -maxdepth 1 -name '*.nupkg' | wc -l)" -eq 1 ] || fail "expected exactly one bundle package in $BUNDLE_OUT/."
  echo "release-allocated: bundle $BUNDLE_ID $VERSION packed."
}

case "${1:-}" in
  plan|levels|build|bindings|decide|bundle) "$1" ;;
  *) fail "unknown phase '${1:-}': plan, levels, build, bindings, decide or bundle." ;;
esac

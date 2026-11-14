#!/usr/bin/env bash
# One allocated release (FDW-3350 / FDW-3364 / FDW-4784 / FDW-5272), in phases, for .github/workflows/publish-nuget.yml on the GitHub
# mirror (the repository root holds src/ and tests/). A release is ONE real run: there is no dry run. Each phase is one workflow step; the
# push itself is the workflow's (sign-in rounds, scripts/push-nuget.sh), so a long quota wait never uses a dead key.
#
#   release-allocated.sh plan     decide the whole release from the baseline and the feed: which packages changed (anything in a package's
#                                 closure), each package's own next version at the level BUMP states, the AssemblyVersions, the release's
#                                 number. Nothing is compiled and no API is read (tools/versioning/release.py plan)
#   release-allocated.sh build    prepare the build set, build it (and test it only when RUN_TESTS is true), pack its members into artifacts/
#   release-allocated.sh bindings prove that a package left as it is still loads with the rebuilt ones: no built assembly is below the floor
#                                 or lower than a version the last release's dlls ask for (tools/versioning/bindcheck.py, assemblyid.py)
#   release-allocated.sh decide   check-then-push: decide every package against nuget.org before anything is pushed
#   release-allocated.sh record   write the record of this release (release-work/baseline.json): the baseline of the NEXT release, every
#                                 package's exact version in this one (tools/versioning/release.py record); the workflow uploads it, and
#                                 after the green run and the tag the owner commits it as tools/versioning/baseline/release-FDW-<N>.json
#
# Every value comes from the environment; the workflow declares the defaults once. A missing value fails the run and names it. Nothing is
# assumed.
#
#   BRANCH            main or prerelease: main publishes the stable version N, prerelease the candidate N-rc.<k>
#   LINE              the release line, major.minor (for example 1.5)
#   BUMP              patch | minor | major: the level of every package that changed. The mirror has no commits to read it from, so the person
#                     cutting the release states it (the workflow's declared default is patch)
#   BASELINE_FILE     the record of the last release (tools/versioning/baseline/release-FDW-<N>.json); empty takes the record with the highest
#                     number in tools/versioning/baseline and prints which. The plan refuses a record that is not the latest
#   FIRST_VERSIONS_FILE   tools/versioning/first-versions.json: the declared first version of each new package
#   The assembly version floors are not environment values: `plan` reads them from the baseline manifest (assemblyVersionFloors, one per
#   assembly) into plan.json and works out each package's AssemblyVersion from them (assembly-versions.json), `bindings` checks the built
#   dlls against them, and `bindings` records the floors for the NEXT release in project-manifest.json (the `record` phase adds the versions).
#   RUN_TESTS         (build phase) true or false: whether the members' tests run in this job. The real run's gate (stable and candidate)
#                     requires a green CI run of the commit, which runs every test, so the workflow's declared default is false.
#   WORK_DIR          where the plan files and reports go (not the packages)
#   GITHUB_OUTPUT     GitHub's step output file
#
# Exit 0: the phase ended. Exit 1: the phase failed, and the message says which package or file and why.
set -uo pipefail

TOOLS="tools/versioning"
ARTIFACTS="artifacts"

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
  require BRANCH LINE BUMP WORK_DIR FIRST_VERSIONS_FILE GITHUB_OUTPUT
  [[ "$LINE" =~ ^[0-9]+[.][0-9]+$ ]] || fail "LINE is '$LINE' but must be major.minor, for example 1.5."
  case "$BUMP" in patch|minor|major) ;; *) fail "BUMP is '$BUMP' but must be patch, minor or major." ;; esac
  local baseline=() prerelease=()
  case "$BRANCH" in
    main) ;;
    prerelease) prerelease=(--prerelease) ;;
    *) fail "BRANCH is '$BRANCH': an allocated release runs on main (stable) or prerelease (candidate) only." ;;
  esac
  if [ -n "${BASELINE_FILE:-}" ]; then
    [ -f "$BASELINE_FILE" ] || fail "BASELINE_FILE '$BASELINE_FILE' does not exist."
    baseline=(--baseline "$BASELINE_FILE")
  else
    baseline=(--baseline-dir "$TOOLS/baseline")
  fi
  python3 "$TOOLS/release.py" plan --repo . --line "$LINE" --out "$WORK_DIR" "${baseline[@]}" --bump "$BUMP" \
    --first-versions "$FIRST_VERSIONS_FILE" "${prerelease[@]}" \
    || fail "release.py could not plan the release; its message is above. The baseline is the record of the last release (release-FDW-<N>.json in $TOOLS/baseline)."

  local members
  members=$(python3 -c 'import json,sys; print(",".join(json.load(open(sys.argv[1]))["members"]))' "$WORK_DIR/plan.json") \
    || fail "no members in $WORK_DIR/plan.json."
  if [ -z "$members" ]; then
    echo "release-allocated: no package changed since the baseline; there is nothing to release."
    echo "should-release=false" >> "$GITHUB_OUTPUT"
    return 0
  fi
  local number version
  number=$(json_get "$WORK_DIR/plan.json" release) || fail "no release number in $WORK_DIR/plan.json."
  version=$(json_get "$WORK_DIR/plan.json" version) || fail "no version in $WORK_DIR/plan.json."
  {
    echo "should-release=true"
    echo "number=$number"
    echo "version=$version"
    echo "members=$members"
  } >> "$GITHUB_OUTPUT"
}

build() {
  require MEMBERS WORK_DIR RUN_TESTS
  local skip_tests=()
  case "$RUN_TESTS" in
    true) ;;
    false) skip_tests=(--skip-tests); echo "release-allocated: the members' tests are not run in this job (RUN_TESTS=false); the real run requires a green CI run of the commit (RELEASE-UNPROVEN-1/2)." ;;
    *) fail "RUN_TESTS is '$RUN_TESTS' but must be true or false." ;;
  esac
  rm -rf "$ARTIFACTS"
  mkdir -p "$ARTIFACTS"
  python3 "$TOOLS/buildset.py" prepare --repo . --set "$MEMBERS" --versions "$WORK_DIR/versions.json" \
    --trees "$WORK_DIR/trees.json" --package-versions "$WORK_DIR/package-versions.json" --assembly-versions "$WORK_DIR/assembly-versions.json" "${skip_tests[@]}" > "$WORK_DIR/prepare.json" \
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
  python3 "$TOOLS/assemblyid.py" identity --package "$1" --out "$2" || fail "assemblyid.py could not read the identity of $1; its message is above."
}

bindings() {
  require WORK_DIR
  python3 -c '
import json, sys
json.dump(json.load(open(sys.argv[1]))["assembly_version_floors"], open(sys.argv[2], "w"), indent=1, sort_keys=True)' "$WORK_DIR/plan.json" "$WORK_DIR/floors.json" \
    || fail "no assembly_version_floors in $WORK_DIR/plan.json; the baseline manifest must state them."
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
    python3 "$TOOLS/feeds.py" fetch --package-id "$package" --version "$version" --out "$packs/$package.nupkg" \
      || fail "could not fetch $package $version from nuget.org for the binding check; its message is above."
    dump_identity "$packs/$package.nupkg" "$ident/held/$package.txt"
  done 3< "$WORK_DIR/identities-held.tsv"
  python3 "$TOOLS/bindcheck.py" check --new-dir "$ident/new" --held-dir "$ident/held" --floors "$WORK_DIR/floors.json" \
    || fail "a package left as it is would not load with a rebuilt one (listed above); nothing is pushed."
  # The floors for the next release: for each assembly, the higher of the version it carries and the highest any dll of this release asks
  # for it at. They go into the project manifest that the `record` phase completes, which the next release reads as its baseline.
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

record() {
  require WORK_DIR
  python3 "$TOOLS/release.py" record --work "$WORK_DIR" --out "$WORK_DIR/baseline.json" \
    || fail "release.py could not write the record of this release; its message is above. The bindings phase must have run."
  echo "release-allocated: the record of this release is $WORK_DIR/baseline.json; after the green run and the tag it is committed as $TOOLS/baseline/release-FDW-<N>.json."
}

case "${1:-}" in
  plan|build|bindings|decide|record) "$1" ;;
  *) fail "unknown phase '${1:-}': plan, build, bindings, decide or record." ;;
esac

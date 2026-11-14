#!/usr/bin/env python3
"""Plan a release: the one read-only step that turns a baseline, the feed and the level the release's owner states into everything the
later steps read (FDW-4784, FDW-5272). Nothing is compiled and no API is read to decide a version.

  release.py plan --repo R --line 1.4 --out DIR --first-versions FILE --bump patch|minor|major
                  [--ref HEAD] [--prerelease] [--assembly-info FILE]
                  (--base-ref REF | --baseline FILE | --baseline-dir DIR)
  release.py record --work DIR --out FILE

The baseline (what the last release was made from) comes from exactly one of: a git ref such as a release tag (--base-ref), a release
record (--baseline FILE), or the record with the highest release number in a folder (--baseline-dir DIR; the one chosen is printed).
A release record is tools/versioning/baseline/release-FDW-<N>.json: the project manifest of the release's tree (the git tree id of every
project and the hashes of the repo-wide build files, which is the tag's content, the assembly version floors, the packages that carry no
runtime assembly) plus `release` and `packages` ({package id: the exact version it had in that release, tools included}). A record needs
no tag and no history, which is what the GitHub mirror has. `record` writes the record of the release just made (the run's last phase).
The plan refuses, naming the reason and never assuming: a record without `release` or `packages`, a record whose release differs from its
file name, and a record that is not the latest one (a newer record in its folder, or a newer release tag in the checkout).

A package gets a new version when anything in its closure changed since the baseline: its own folder, any project it references (directly
or through others), the repo-wide build files (changeset.py with closure=True). A package whose closure did not change is not built, not
bumped and not pushed: everything it is made from is identical, so it keeps the highest stable version on the feed. The level is stated by
whoever cuts the release (--bump); it is the level of every package that changed. Each takes its OWN last stable version raised by it.
A package with no stable version takes the first version first-versions.json declares for it (level "new", see levelplan.py).

The release has one number, its identity: the number for the stated bump. A new package changes neither it nor any other package's
version (its own version is the first version first-versions.json declares; FDW-5591). With
--prerelease every package is a release candidate, N-rc.<k>, k being one above the highest candidate of N any package already has.

It writes into --out:

  plan.json      the release number, the bump, the build set and why each member is in it, the tests to run, the assembly version floors the
                 baseline recorded (assemblyVersionFloors, and packagesWithoutRuntimeAssembly for the generators and tools that have none; a
                 baseline without them stops the plan) and the release's version
  versions.json  {package id: version} for every packable package OUTSIDE the set: input to `buildset.py prepare`
  trees.json     {project name: git tree id} for the members: input to `buildset.py prepare`
  manifest.json  {release, packages} for the whole release: every package at the version it has in it
  project-manifest.json  the project manifest of the tree planned from; with the floors `bindings` adds and manifest.json's packages it is the
                 release's record (`release.py record`), which the next release reads as its baseline
  levels-input.json  the reference graph, each package's last stable version, the generators, and the names, folders and trees of every
                 packable project: what bindings, the release notes and the plan itself read
  package-versions.json  {project name: version} for every package to build: input to `buildset.py prepare --package-versions`
  assembly-versions.json {project name: x.y.z.w}, each package's AssemblyVersion: the floor the baseline recorded for its assembly, or N.0.0.0
                 for its own major N when that is higher; input to `buildset.py prepare --assembly-versions`
  levels.json    {package id: last, level, next, reasons} and the report lines

An unchanged package keeps the highest stable version on the feed. A package outside the set with no stable version there is an error that
names it: it cannot be shown to be unchanged, and nothing is assumed. Nothing is pushed and the repository is not touched.
"""
import argparse
import json
import os
import re
import sys
from concurrent.futures import ThreadPoolExecutor

import allocate
import bindcheck
import changeset
import levelplan
from feeds import FeedError, NugetOrg
from fdwprojects import git, snapshot


BUMPS = ("patch", "minor", "major")  # in order of size


class ReleaseError(Exception):
    pass


def without_own_versions(held, own_trees, feed, workers=16, baseline_pins=None, baseline_trees=None):
    """`held` ({package id: versions on the feed}) without the versions this release has already pushed itself (FDW-5279).

    own_trees is {package id: the git tree id of the package as it is being released}, for each package the release changes. A package's
    highest stable version on the feed whose tree marker (fdw/tree.txt, the git tree id of its folder) equals that tree was built from
    exactly what is being released: it is this release's own, pushed by an earlier run that stopped part way. The plan must not count it
    when it allocates, or a re-run takes the next numbers (every package one patch higher) and pushes everything again.
    With it dropped the plan comes out as it did on the first run, and `decide` skips what is already there. The commit sha and the
    assemblies' InformationalVersion are not part of it.

    Nothing is guessed. A version the feed lists but whose package cannot be read stops the plan. Once at least one package of the release
    is found already pushed (a resume), any other package of the release whose highest stable version is one of those same versions but
    carries another tree, or no marker, stops the plan too: the release was pushed from different content, and it is never renumbered
    silently. With nothing pushed (a normal first run) a changed package's older version has another tree and an old package may have no
    marker; neither is an error.

    The baseline release is never a resume (FDW-5589). With the closure rule a package is in the release because something below it
    changed, so its own folder may be unchanged since the last release: that release's version then carries the current tree, and would be
    taken for a partial push of this one. So a held version that is the baseline's own version of the package is not this release's own:
    `baseline_pins` is {package id: version}, the exact versions the baseline release shipped, every package and tool among them (the
    `packages` of its record; FDW-5635: with the tree alone a tool whose folder is unchanged had its own held version of this release taken
    for the baseline's, and the plan stopped with a false RESUME_CONFLICT). With the pins every held version is compared exactly, and a
    package the baseline did not ship has no baseline version. Only when the baseline is a git ref (`baseline_pins` is None) there are no
    versions to compare: `baseline_trees` ({package id: the tree in the baseline manifest}) decides, an own folder unchanged since the
    baseline meaning the held version with that tree is the baseline's."""
    def is_baseline_version(pid, version):
        if baseline_pins is not None:
            return baseline_pins.get(pid) == version
        if baseline_trees is not None and baseline_trees.get(pid) is not None:
            return baseline_trees[pid] == own_trees[pid]
        return False
    def read(pid):
        stable = [v for v in held.get(pid, []) if "-" not in v and allocate.triple(v)]
        if not stable:
            return None
        latest = allocate.highest(stable)
        try:
            return latest, feed.readable_tree(pid, latest)
        except FeedError as e:
            raise ReleaseError(str(e))
    pids = sorted(own_trees)
    with ThreadPoolExecutor(max_workers=workers) as pool:
        latest = dict(zip(pids, pool.map(read, pids)))
    own = {pid: found[0] for pid, found in latest.items()
           if found and found[1] is not None and found[1] == own_trees[pid] and not is_baseline_version(pid, found[0])}
    resumed = set(own.values())
    conflicts = sorted(f"{pid} {found[0]}: " + ("no tree marker" if found[1] is None else f"built from tree {found[1]}, not {own_trees[pid]}")
                       for pid, found in latest.items() if found and pid not in own and found[0] in resumed)
    if conflicts:
        raise ReleaseError("RESUME_CONFLICT: this release was already pushed in part (" + ", ".join(sorted(resumed)) + "), but these packages are on the feed at that "
                           "version with other contents, so the release was pushed from different content and a re-run would renumber it: "
                           + "; ".join(conflicts[:10]) + (f"; and {len(conflicts) - 10} more" if len(conflicts) > 10 else "")
                           + ". Re-run from the commit that was pushed, or take the packages that differ out of the release by hand. If an earlier release "
                           "finished and its record is not in the baseline folder, commit it first: the plan compares with the baseline's versions only.")
    return {pid: [v for v in versions if v != own.get(pid)] for pid, versions in held.items()}


def release_plan(current, baseline, feed, tags, line, bump, first_versions, prerelease=False, baseline_pins=None):
    """All the files of a release as a dict. `bump` is the level the release's owner states; `first_versions` is
    levelplan.load_first_versions(); `prerelease` makes every package a candidate; `baseline_pins` is {package id: version}, the exact
    versions of the baseline release (its record's `packages`; None when the baseline is a git ref)."""
    if bump not in BUMPS:
        raise ReleaseError(f"the bump is {bump!r}; it must be patch, minor or major")
    floors, bare = baseline_floors(baseline), baseline_bare(baseline)
    result = changeset.plan(current, baseline, (), True)
    projects = current.projects
    members = list(result["build_set"])  # changeset.plan lists packable projects only
    held = allocate.held_versions(projects, feed)
    held = without_own_versions(held, {projects[n].package_id: current.trees[n] for n in members}, feed, baseline_pins=baseline_pins,
                                baseline_trees={projects[n].package_id: baseline.get("projects", {}).get(n) for n in members})
    in_set = {projects[n].package_id for n in members}
    numbers = {b: allocate.release_number(held, line, tags, b) for b in BUMPS}

    unchanged, missing = {}, []
    for pid, versions in sorted(held.items()):
        if pid in in_set:
            continue
        stable = [v for v in versions if "-" not in v and allocate.triple(v)]
        if stable:
            unchanged[pid] = allocate.highest(stable)
        else:
            missing.append(pid)
    if missing:
        raise ReleaseError("packages outside the build set have no stable version on the feed, so they cannot be shown "
                           f"to be unchanged: {missing[:10]}" + (f" and {len(missing) - 10} more" if len(missing) > 10 else ""))

    info = levels_input(current, result, held)
    base = {"line": line, "assembly_version_floors": floors, "packages_without_runtime_assembly": bare, "built_from_source_only": result["built_from_source_only"],
            "tests": result["tests"], "removed": result["removed"], "bump_stated": bump, "release_numbers": numbers}
    if not members:
        manifest = {"release": numbers[bump], "packages": dict(unchanged)}
        return {"plan": {**base, "release": numbers[bump], "bump": bump, "version": numbers[bump], "members": [], "package_ids": {}, "reasons": {},
                         "unchanged_packages": len(unchanged), "per_package": True, "pulled_in": [], "retired": []},
                "versions": unchanged, "trees": {}, "manifest": manifest, "project-manifest": current.manifest(), "levels-input": info,
                "package-versions": {}, "assembly-versions": {}, "levels": {"packages": {}, "report": levelplan.report({})}}
    names = info["names"]
    first_why = {pid: result["reasons"][names[pid]][0] for pid in info["members"]}
    try:
        rows = levelplan.plan_levels({p: set(v) for p, v in info["depends"].items()}, info["members"], lambda package: bump, info["last_stable"],
                                     first_versions, {g: set(c) for g, c in info["generators"].items()}, first_why)
    except levelplan.LevelPlanError as e:
        raise ReleaseError(str(e))
    joined = sorted(rows)
    assembly_versions = {}
    for p in joined:
        assembly = info["assemblies"][p]
        if assembly not in floors and p in info["last_stable"] and p not in bare:
            raise ReleaseError(f"ASSEMBLY_FLOOR_MISSING: {p} (assembly {assembly}) has a last stable version, but the baseline records no floor for it "
                               "and does not list it as carrying no runtime assembly; nothing is assumed. Measure the baseline again, or fix the assembly name.")
        assembly_versions[names[p]] = assembly_version_of(rows[p]["next"], floors.get(assembly))
    # The release number follows the highest level among the packages that already existed: the stated bump is the least it can be. A new package
    # raises nothing (FDW-5591), so adding one to a release at a stated patch leaves the release at that patch.
    derived = max((r["level"] for r in rows.values() if r["level"] != levelplan.NEW), key=BUMPS.index, default=bump)
    final = max(bump, derived, key=BUMPS.index)
    number = numbers[final]
    candidate = None
    if prerelease:
        candidate = next_candidate(held, number)
    published = {p: rows[p]["next"] + (f"-rc.{candidate}" if candidate else "") for p in rows}
    release_version = f"{number}-rc.{candidate}" if candidate else number
    replaced = levelplan.replaced_packages(first_versions)
    outside = {p: v for p, v in unchanged.items() if p not in rows and p not in replaced}
    manifest = {"release": release_version, "packages": {**outside, **published}}
    problems = check_packages(manifest["packages"])
    if problems:
        raise ReleaseError("the release manifest would not make a valid record: " + "; ".join(problems))
    member_names = sorted(names[p] for p in joined)
    pulled = set(member_names) - set(members)
    reasons = {n: (result["reasons"][n] if n in result["reasons"] else rows[next(p for p in joined if names[p] == n)]["reasons"]) for n in member_names}
    tests = sorted(set(result["tests"]) | {t for t, refs in info["test_references"].items() if pulled & set(refs) or t[:-len(".Tests")] in pulled and t.endswith(".Tests")})
    plan = {**base, "release": number, "bump": final, "version": release_version, "members": member_names, "package_ids": {names[p]: p for p in joined},
            "reasons": reasons, "tests": tests, "unchanged_packages": len(outside), "per_package": True, "pulled_in": sorted(pulled), "retired": replaced}
    return {"plan": plan, "versions": outside, "trees": {names[p]: info["trees"][p] for p in joined}, "manifest": manifest, "project-manifest": current.manifest(),
            "levels-input": info, "package-versions": {names[p]: published[p] for p in joined}, "assembly-versions": assembly_versions,
            "levels": {"packages": rows, "report": levelplan.report(rows)}}


def packable_references(projects, name):
    """The packable src projects `name` depends on: its direct references, and through a project that cannot be a package, whose
    own references become part of the package that builds it."""
    found, seen, stack = set(), {name}, [name]
    while stack:
        for r in projects[stack.pop()].refs:
            target = projects.get(r)
            if target is None or target.kind != "src" or r in seen:
                continue
            seen.add(r)
            if target.packable:
                found.add(r)
            else:
                stack.append(r)
    return found


def levels_input(current, result, held):
    """The facts `decide` reads, keyed by package id (the key of the surfaces and of first-versions.json)."""
    projects = current.projects
    packable = {n: p for n, p in projects.items() if p.kind == "src" and p.packable}
    pid = {n: p.package_id for n, p in packable.items()}
    return {"depends": {pid[n]: sorted(pid[r] for r in packable_references(projects, n)) for n in sorted(packable)},
            "members": sorted(pid[n] for n in result["build_set"]),
            "last_stable": {p: allocate.highest([v for v in vs if "-" not in v and allocate.triple(v)])
                            for p, vs in sorted(held.items()) if any("-" not in v and allocate.triple(v) for v in vs)},
            "generators": {pid[g]: sorted(pid[c] for c in consumers) for g, consumers in result["generators"].items() if g in pid},
            "assemblies": {pid[n]: packable[n].assembly for n in sorted(packable)},
            "names": {pid[n]: n for n in sorted(packable)}, "paths": {pid[n]: f"{p.path}/{n}.csproj" for n, p in sorted(packable.items())}, "trees": {pid[n]: current.trees[n] for n in sorted(packable)},
            "test_references": {n: sorted(p.refs) for n, p in sorted(projects.items()) if p.kind == "tests"}}


def baseline_floors(baseline):
    """{assembly: x.y.z.w}: the assembly version floor of each assembly, as the baseline release recorded it in its manifest. A baseline
    without them stops the plan: they are measured from the release's own dlls (bindcheck.py floors) and never assumed."""
    floors = baseline.get(bindcheck.FLOORS_KEY) if baseline else None
    if floors is None:
        raise ReleaseError(f"BASELINE_FLOORS_MISSING: the baseline manifest does not state {bindcheck.FLOORS_KEY}. Measure them from the baseline release's "
                           "packages (assemblyid.py identity on each, then bindcheck.py floors --dir DIR) and add them to the baseline manifest.")
    if not isinstance(floors, dict):
        raise ReleaseError(f"BASELINE_FLOORS_INVALID: {bindcheck.FLOORS_KEY} must be an object of {{assembly: x.y.z.w}}")
    try:
        return {name: bindcheck.check_floor(v) for name, v in floors.items()}
    except bindcheck.BindError as e:
        raise ReleaseError(f"BASELINE_FLOORS_INVALID: {e}")


def baseline_bare(baseline):
    """The packages the baseline release recorded as carrying no runtime assembly (generators, tools). Required, like the floors."""
    bare = baseline.get(bindcheck.BARE_KEY) if baseline else None
    if not isinstance(bare, list) or not all(isinstance(x, str) for x in bare):
        raise ReleaseError(f"BASELINE_BARE_MISSING: the baseline manifest must state {bindcheck.BARE_KEY} as a list of package ids "
                           "(bindcheck.py bare --dir DIR over the baseline release's identity files).")
    return bare


def assembly_version_of(version, floor):
    """The AssemblyVersion of a package at `version` whose assembly has `floor` (None for a new assembly): the floor, or N.0.0.0 when the
    package is in major N and that is higher. Never lower than the floor, so a dll that asks for it at the floor still binds."""
    base = bindcheck.version_tuple(floor) if floor else (0, 0, 0, 0)
    return ".".join(str(n) for n in max(base, (int(version.split(".")[0]), 0, 0, 0)))


RECORD_FILE = re.compile(r"^release-FDW-(\d+\.\d+\.\d+)\.json$")
PACKAGE_ID = re.compile(r"^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)*$")
SEMVER = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$")
MANIFEST_KEYS = ("projects", "globals", bindcheck.FLOORS_KEY, bindcheck.BARE_KEY)


def check_packages(packages):
    """The problems with {package id: version} as a list of sentences: an id that is not a package id, a version that is not a plain version
    (a range, a wildcard and an empty one are refused: a record names what was shipped, exactly)."""
    if not isinstance(packages, dict) or not packages:
        return ["`packages` is not a non-empty object of {package id: version}"]
    problems = []
    for pid, version in sorted(packages.items()):
        if not PACKAGE_ID.match(str(pid)):
            problems.append(f"{pid!r} is not a package id")
        if not SEMVER.match(str(version)):
            problems.append(f"{pid} has version {version!r}, which is not a plain version")
    return problems


def check_record(record):
    """The problems with a release record, as a list of sentences. Empty means it can be written and read."""
    if not isinstance(record, dict):
        return ["the record is not a JSON object"]
    problems = [f"the record has no {key!r}" for key in ("release", "packages", *MANIFEST_KEYS) if key not in record]
    if problems:
        return problems
    if not SEMVER.match(str(record["release"])):
        problems.append(f"release {record['release']!r} is not a plain version")
    return problems + check_packages(record["packages"])


def read_record(path):
    """The release record at `path`, named release-FDW-<N>.json. Refuses a missing file, a file that is not named so, a record that does not state
    `release`, `packages` and the project manifest, and one whose release differs from its file name: the record of release N is the file of N."""
    name = os.path.basename(path)
    named = RECORD_FILE.match(name)
    if not named:
        raise ReleaseError(f"BASELINE_NAME: {name} is not a release record: a record is named release-FDW-<N>.json (N a plain version such as 1.5.8)")
    try:
        record = read_json(path)
    except FileNotFoundError:
        raise ReleaseError(f"BASELINE_MISSING: there is no baseline file at {path}. The baseline is the record of the last release (release-FDW-<N>.json); "
                           "it is committed after each release, from the run's artifact.")
    problems = check_record(record)
    if problems:
        raise ReleaseError(f"BASELINE_INVALID: {name}: " + "; ".join(problems[:10]))
    if record["release"] != named.group(1):
        raise ReleaseError(f"BASELINE_RELEASE_MISMATCH: {name} states release {record['release']}: the record of release N is the file of N")
    return record


def records_in(directory):
    """{release number: path} of the release records in a folder."""
    if not os.path.isdir(directory):
        raise ReleaseError(f"BASELINE_MISSING: there is no baseline folder at {directory}")
    return {m.group(1): os.path.join(directory, name) for name in sorted(os.listdir(directory)) if (m := RECORD_FILE.match(name))}


def latest_record(directory):
    """The path of the record with the highest release number in a folder; refuses a folder with none."""
    found = records_in(directory)
    if not found:
        raise ReleaseError(f"BASELINE_MISSING: no release record (release-FDW-<N>.json) in {directory}")
    return found[max(found, key=allocate.triple)]


def check_latest(path, tags):
    """Refuses a record that is not the last release: a newer record in its folder, or a newer release tag in the checkout (the GitHub mirror
    has no tags; there the folder is the evidence). The baseline of a release is the release before it, and nothing else."""
    number = RECORD_FILE.match(os.path.basename(path)).group(1)
    newer = sorted((n for n in records_in(os.path.dirname(os.path.abspath(path))) if allocate.triple(n) > allocate.triple(number)), key=allocate.triple)
    if newer:
        raise ReleaseError(f"BASELINE_NOT_LATEST: {os.path.basename(path)} is not the last release's record: release-FDW-{newer[-1]}.json is newer")
    tagged = sorted((n for n in allocate.tag_versions(tags) if allocate.triple(n) > allocate.triple(number)), key=allocate.triple)
    if tagged:
        raise ReleaseError(f"BASELINE_NOT_LATEST: release tag release-FDW-{tagged[-1]} is newer than the baseline record {os.path.basename(path)}; "
                           f"commit the record of release {tagged[-1]} (the artifact of its run)")


def baseline_from_record(path, tags=()):
    """(the project manifest, {package id: version}) of the last release's record: the baseline of the next plan and the exact versions the
    resume rule compares with."""
    check_latest(path, tags)
    record = read_record(path)
    return {k: v for k, v in record.items() if k not in ("release", "packages")}, dict(record["packages"])


def next_candidate(held, number):
    """The candidate number k of a release candidate N-rc.k: one above the highest N-rc.j any package already has."""
    pattern = re.compile(rf"{re.escape(number)}-rc\.(\d+)$")
    return max((int(m.group(1)) for versions in held.values() for v in versions if (m := pattern.match(v))), default=0) + 1


def write_record(work, out):
    """Write the record of the release just made from `work` (the plan's folder after `bindings`): the project manifest with the floors, the
    release's version and every package's exact version. Refuses what the plan would refuse as a baseline."""
    manifest = read_json(os.path.join(work, "manifest.json"))
    record = {**read_json(os.path.join(work, "project-manifest.json")), "release": manifest["release"], "packages": manifest["packages"]}
    problems = check_record(record)
    if problems:
        raise ReleaseError("the release record is incomplete (bindings must run before it): " + "; ".join(problems[:10]))
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    with open(out, "w", encoding="utf-8", newline="\n") as f:
        json.dump(record, f, indent=1, sort_keys=True)
        f.write("\n")
    return record


def read_json(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)


def write(out, files):
    os.makedirs(out, exist_ok=True)
    for name, data in files.items():
        with open(os.path.join(out, name + ".json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(data, f, indent=1, sort_keys=True)
            f.write("\n")


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("plan")
    p.add_argument("--repo", required=True)
    p.add_argument("--line", required=True)
    p.add_argument("--base-ref")
    p.add_argument("--baseline", help="a release record, tools/versioning/baseline/release-FDW-<N>.json; it must be the latest")
    p.add_argument("--baseline-dir", help="a folder of release records: the one with the highest release number is the baseline (printed)")
    p.add_argument("--bump", required=True, choices=["patch", "minor", "major"], help="the level of every package that changed; stated by whoever cuts the release")
    p.add_argument("--first-versions", required=True, help="tools/versioning/first-versions.json: the declared first version of each new package (required, may declare none)")
    p.add_argument("--prerelease", action="store_true", help="every package is a release candidate, N-rc.<k>")
    p.add_argument("--out", required=True)
    p.add_argument("--ref", default="HEAD")
    p.add_argument("--assembly-info", help="only with --base-ref: a git ref holds no dlls, so what is measured from the release's packages is given here (a JSON file "
                                           "with assemblyVersionFloors and packagesWithoutRuntimeAssembly)")
    r = sub.add_parser("record", help="write the record of the release just made, from the plan's folder after `bindings`")
    r.add_argument("--work", required=True, help="the plan's --out folder: manifest.json and project-manifest.json (with the floors)")
    r.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    if a.cmd == "record":
        try:
            record = write_record(a.work, a.out)
        except (ReleaseError, OSError, KeyError, ValueError) as e:
            sys.exit(f"release record not written:\n  {e}")
        print(f"release record {a.out}: release {record['release']}, {len(record['packages'])} packages, {len(record['projects'])} projects")
        return
    if not re.fullmatch(r"\d+\.\d+", a.line):
        ap.error("--line is major.minor, for example 1.4")
    if sum([bool(a.base_ref), bool(a.baseline), bool(a.baseline_dir)]) != 1:
        ap.error("give exactly one of --base-ref, --baseline and --baseline-dir")
    if a.assembly_info and not a.base_ref:
        ap.error("--assembly-info is only for --base-ref; a release record carries its own")
    current = snapshot(a.repo, a.ref)
    tags = git(a.repo, "tag", "-l", "release-*").decode().split()
    feed = NugetOrg()
    try:
        pins = None
        if a.base_ref:
            baseline = changeset.baseline_from_ref(a.repo, a.base_ref)
            if a.assembly_info:
                baseline = baseline | read_json(a.assembly_info)
        else:
            path = a.baseline or latest_record(a.baseline_dir)
            print(f"baseline: {path}")
            baseline, pins = baseline_from_record(path, tags)
        result = release_plan(current, baseline, feed, tags, a.line, a.bump, levelplan.load_first_versions(a.first_versions), a.prerelease, pins)
    except (ReleaseError, levelplan.LevelPlanError) as e:
        sys.exit(f"release not planned; nothing was written:\n  {e}")
    write(a.out, result)
    p = result["plan"]
    print("\n".join(result["levels"]["report"]))
    print(f"release {p['release']} ({p['bump']}), version {p['version']}, line {a.line}: {len(p['members'])} packages to build and push, "
          f"{p['unchanged_packages']} unchanged, {len(p['tests'])} test projects; files in {a.out}")


if __name__ == "__main__":
    main()

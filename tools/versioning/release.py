#!/usr/bin/env python3
"""Plan a release: the one read-only step that turns a baseline, the feed and the level the release's owner states into everything the
later steps read (FDW-4784, FDW-5272). Nothing is compiled and no API is read to decide a version.

  release.py plan --repo R --line 1.4 --bundle-id Fdw.Bundle --out DIR --first-versions FILE --bump patch|minor|major
                  [--ref HEAD] [--prerelease] [--assembly-info FILE]
                  (--base-ref REF | --baseline-from-bundle | --baseline FILE)

The baseline (what the last release was made from) comes from exactly one of: a git ref such as a release tag (--base-ref), the project
manifest inside the latest stable bundle on nuget.org (--baseline-from-bundle), or a manifest file (--baseline). The manifest records the
git tree id of every project at that release, which is the tag's content: the last two need no tags and no history, which is what the
GitHub mirror has. A bundle with no manifest inside stops the plan and says so.

A package gets a new version when anything in its closure changed since the baseline: its own folder, any project it references (directly
or through others), the repo-wide build files (changeset.py with closure=True). A package whose closure did not change is not built, not
bumped and not pushed: everything it is made from is identical, so it keeps the highest stable version on the feed. The level is stated by
whoever cuts the release (--bump); it is the level of every package that changed. Each takes its OWN last stable version raised by it.
A package with no stable version takes the first version first-versions.json declares for it (level "new", see levelplan.py).

The bundle has one number, the release's identity: the number for the stated bump. A new package changes neither it nor any other package's
version (its own version is the first version first-versions.json declares; FDW-5591). With
--prerelease every package and the bundle are release candidates, N-rc.<k>, k being one above the candidates of N the bundle already has.

It writes into --out:

  plan.json      the release number, the bump, the build set and why each member is in it, the tests to run, the assembly version floors the
                 baseline recorded (assemblyVersionFloors, and packagesWithoutRuntimeAssembly for the generators and tools that have none; a
                 baseline without them stops the plan) and the bundle's version
  versions.json  {package id: version} for every packable package OUTSIDE the set: input to `buildset.py prepare`
  trees.json     {project name: git tree id} for the members: input to `buildset.py prepare`
  manifest.json  {id, version, packages} for the whole release: input to `bundle.py generate`
  project-manifest.json  the project manifest of the tree planned from; the bundle carries it, and the next release reads it as its baseline
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
import bundle
import changeset
import levelplan
from feeds import MANIFEST_FILE, NOT_PINNED_FILE, FeedError, NugetOrg
from fdwprojects import git, snapshot


BUMPS = ("patch", "minor", "major")  # in order of size


class ReleaseError(Exception):
    pass


def without_own_versions(held, own_trees, feed, workers=16, baseline_pins=None, baseline_trees=None):
    """`held` ({package id: versions on the feed}) without the versions this release has already pushed itself (FDW-5279).

    own_trees is {package id: the git tree id of the package as it is being released}, for each package the release changes. A package's
    highest stable version on the feed whose tree marker (fdw/tree.txt, the git tree id of its folder) equals that tree was built from
    exactly what is being released: it is this release's own, pushed by an earlier run that stopped part way. The plan must not count it
    when it allocates, or a re-run takes the next numbers (every package one patch higher, the bundle too) and pushes everything again.
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
    `baseline_pins` is {package id: version}, the exact versions the baseline bundle shipped: its pins and, for the packages it does not pin
    (a dotnet tool), the versions in its fdw/not-pinned.json (FDW-5635: with the tree alone a tool whose folder is unchanged had its own
    held version of this release taken for the baseline's, and the plan stopped with a false RESUME_CONFLICT). With the pins every held
    version is compared exactly, and a package the baseline did not ship has no baseline version. Only when the baseline is a file or a git
    ref (`baseline_pins` is None) there are no versions to compare: `baseline_trees` ({package id: the tree in the baseline manifest})
    decides, an own folder unchanged since the baseline meaning the held version with that tree is the baseline's."""
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
                           + ". Re-run from the commit that was pushed, or take the packages that differ out of the release by hand.")
    return {pid: [v for v in versions if v != own.get(pid)] for pid, versions in held.items()}


def release_plan(current, baseline, feed, tags, line, bump, bundle_id, first_versions, prerelease=False, baseline_pins=None):
    """All the files of a release as a dict. `bump` is the level the release's owner states; `first_versions` is
    levelplan.load_first_versions(); `prerelease` makes every package and the bundle a candidate; `baseline_pins` is {package id: version},
    the exact pins of the baseline bundle (None when the baseline is a file or a git ref)."""
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
        manifest = {"id": bundle_id, "version": numbers[bump], "packages": dict(unchanged)}
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
    # The bundle follows the highest level among the packages that already existed: the stated bump is the least it can be. A new package raises
    # nothing (FDW-5591), so adding one to a release at a stated patch leaves the bundle at that patch.
    derived = max((r["level"] for r in rows.values() if r["level"] != levelplan.NEW), key=BUMPS.index, default=bump)
    final = max(bump, derived, key=BUMPS.index)
    number = numbers[final]
    candidate = None
    if prerelease:
        candidate = len([v for v in feed.versions(bundle_id) if v.lower().startswith(number + "-rc.")]) + 1
    published = {p: rows[p]["next"] + (f"-rc.{candidate}" if candidate else "") for p in rows}
    bundle_version = f"{number}-rc.{candidate}" if candidate else number
    replaced = levelplan.replaced_packages(first_versions)
    outside = {p: v for p, v in unchanged.items() if p not in rows and p not in replaced}
    manifest = {"id": bundle_id, "version": bundle_version, "packages": {**outside, **published}}
    problems = bundle.check(manifest)
    if problems:
        raise ReleaseError("the release manifest would not make a valid bundle: " + "; ".join(problems))
    member_names = sorted(names[p] for p in joined)
    pulled = set(member_names) - set(members)
    reasons = {n: (result["reasons"][n] if n in result["reasons"] else rows[next(p for p in joined if names[p] == n)]["reasons"]) for n in member_names}
    tests = sorted(set(result["tests"]) | {t for t, refs in info["test_references"].items() if pulled & set(refs) or t[:-len(".Tests")] in pulled and t.endswith(".Tests")})
    plan = {**base, "release": number, "bump": final, "version": bundle_version, "members": member_names, "package_ids": {names[p]: p for p in joined},
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


def baseline_pins(feed, bundle_id):
    """{package id: version}: the versions the last release shipped, from the latest stable bundle on the feed: its exact pins, and the versions of
    the packages it does not pin (a dotnet tool), which it states in fdw/not-pinned.json. The bundle carries that file exactly when it leaves a
    package out of its pins, so no file means the bundle left none out."""
    stable = [v for v in feed.versions(bundle_id) if "-" not in v and allocate.triple(v)]
    if not stable:
        raise ReleaseError(f"no stable {bundle_id} on the feed to read the baseline versions from")
    version = allocate.highest(stable)
    try:
        shipped = dict(feed.pins(bundle_id, version))
        text = feed.held_file(bundle_id, version, NOT_PINNED_FILE)
    except FeedError as e:
        raise ReleaseError(str(e))
    if text is not None:
        shipped.update({pid: stated["version"] for pid, stated in json.loads(text).items()})
    return shipped


def baseline_from_bundle(feed, bundle_id):
    """The project manifest inside the latest stable bundle on the feed. Stops, naming the reason, when there is none."""
    stable = [v for v in feed.versions(bundle_id) if "-" not in v and allocate.triple(v)]
    if not stable:
        raise ReleaseError(f"no stable {bundle_id} on the feed to take the baseline from. Give --baseline FILE (the project manifest of the "
                           "last release tree) or --base-ref for the first release under this scheme.")
    version = allocate.highest(stable)
    text = feed.held_file(bundle_id, version, MANIFEST_FILE)
    if text is None:
        raise ReleaseError(f"{bundle_id} {version} has no {MANIFEST_FILE}: it was made before bundles carried the project manifest. "
                           "Give --baseline FILE or --base-ref for this release.")
    return json.loads(text)


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
    p.add_argument("--baseline-from-bundle", action="store_true")
    p.add_argument("--baseline")
    p.add_argument("--bump", required=True, choices=["patch", "minor", "major"], help="the level of every package that changed; stated by whoever cuts the release")
    p.add_argument("--bundle-id", required=True)
    p.add_argument("--first-versions", required=True, help="tools/versioning/first-versions.json: the declared first version of each new package (required, may declare none)")
    p.add_argument("--prerelease", action="store_true", help="every package and the bundle are release candidates, N-rc.<k>")
    p.add_argument("--out", required=True)
    p.add_argument("--ref", default="HEAD")
    p.add_argument("--assembly-info", help="only with --base-ref: a git ref holds no dlls, so what is measured from the release's packages is given here (a JSON file "
                                           "with assemblyVersionFloors and packagesWithoutRuntimeAssembly)")
    a = ap.parse_args(argv)
    if not re.fullmatch(r"\d+\.\d+", a.line):
        ap.error("--line is major.minor, for example 1.4")
    if sum([bool(a.base_ref), a.baseline_from_bundle, bool(a.baseline)]) != 1:
        ap.error("give exactly one of --base-ref, --baseline-from-bundle and --baseline")
    if a.assembly_info and not a.base_ref:
        ap.error("--assembly-info is only for --base-ref; a baseline file or bundle manifest carries its own")
    current = snapshot(a.repo, a.ref)
    tags = git(a.repo, "tag", "-l", "release-*").decode().split()
    feed = NugetOrg()
    try:
        if a.base_ref:
            baseline = changeset.baseline_from_ref(a.repo, a.base_ref)
            if a.assembly_info:
                baseline = baseline | read_json(a.assembly_info)
        else:
            baseline = baseline_from_bundle(feed, a.bundle_id) if a.baseline_from_bundle else read_json(a.baseline)
        pins = baseline_pins(feed, a.bundle_id) if a.baseline_from_bundle else None
        result = release_plan(current, baseline, feed, tags, a.line, a.bump, a.bundle_id, levelplan.load_first_versions(a.first_versions), a.prerelease, pins)
    except (ReleaseError, levelplan.LevelPlanError) as e:
        sys.exit(f"release not planned; nothing was written:\n  {e}")
    write(a.out, result)
    p = result["plan"]
    print("\n".join(result["levels"]["report"]))
    print(f"release {p['release']} ({p['bump']}), version {p['version']}, line {a.line}: {len(p['members'])} packages to build and push, "
          f"{p['unchanged_packages']} unchanged, {len(p['tests'])} test projects; files in {a.out}")


if __name__ == "__main__":
    main()

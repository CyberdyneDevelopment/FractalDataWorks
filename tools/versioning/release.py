#!/usr/bin/env python3
"""Plan a release: the one read-only step that turns a baseline and the feed into everything the later steps read.

  release.py plan   --repo R --line 1.4 --bundle-id Fdw.Bundle --out DIR [--ref HEAD] [--api-breaks FILE]
                    (--base-ref REF | --baseline-from-bundle | --baseline FILE --bump patch|minor|major)
  release.py decide --plan-dir DIR --old-surfaces DIR --new-surfaces DIR --first-versions FILE --settings FILE --out DIR
                    [--candidate K] [--missing-out FILE]

The baseline (what the last release was made from) comes from exactly one of: a git ref such as a release tag
(--base-ref; the highest `bump:` trailer since it is read from the commit messages), the project manifest inside the
latest stable bundle on nuget.org (--baseline-from-bundle), or a manifest file (--baseline). The last two need no tags
and no history, which is what the GitHub mirror has; there the bump cannot be read from commits, so --bump is required
and nothing is assumed. A bundle with no manifest inside stops the plan and says so.

It combines changeset.py (which packages changed), allocate.py (the one release number N, the highest declared bump)
and the feed (the version every unchanged package last shipped with), and writes six files into DIR:

  plan.json      the release number, the bump, the build set and why each member is in it, the tests to run, and the assembly version floors
                 the baseline release recorded (assemblyVersionFloors, and packagesWithoutRuntimeAssembly for the generators and tools that have
                 none; a baseline without them stops the plan)
  versions.json  {package id: version} for every packable package OUTSIDE the set: input to `buildset.py prepare`
  trees.json     {project name: git tree id} for the packable members: input to `buildset.py prepare`
  manifest.json  {id, version, packages} for the whole release: input to `bundle.py generate`
  project-manifest.json  the project manifest of the tree planned from: `bundle.py generate --project-manifest`
                 carries it in the bundle, and the next release reads it back as its baseline
  levels-input.json  what `decide` needs to give each package its own version: the reference graph, each package's last stable
                 version, the generators and the packages they caused, and the names and trees of every packable project

An unchanged package keeps the highest stable version on the feed. A package outside the set with no stable version
there is an error that names it: it cannot be shown to be unchanged, and nothing is assumed. Nothing is pushed and the
repository is not touched; the only files written are the six above.

`decide` is the per-package step (FDW-4784). It runs after the members were built once and each package's public surface was
dumped (Fdw.ApiSurface) next to the surface of its last stable version: OLD/<package id>.surface and NEW/<package id>.surface. It asks
levelrun.decide() for each package's own level, adds the packages a major pulls in, gives every package its next version from its
OWN last stable one, and writes into --out the same files `plan` wrote, now with per-package versions, plus:

  package-versions.json  {project name: version} for every package to build: input to `buildset.py prepare --package-versions`
  assembly-versions.json {project name: x.y.z.w}, each package's AssemblyVersion: the floor the baseline recorded for its assembly, or N.0.0.0 for its
                         own major N when that is higher; input to `buildset.py prepare --assembly-versions`
  levels.json            {package id: last, level, next, reasons} and the report lines `levelrun.report` prints

The bundle has one number, the release's identity, and every member of it has its own: `plan` allocated the number for each bump, and
`decide` takes the one for the highest package level (a major in the release makes the bundle a major; the bump stated to `plan` is the
least it can be). With --candidate K the bundle is N-rc.K as well. A package that
a declared first version replaces leaves the manifest. A surface that is missing stops the run and names the package, so the caller
builds and dumps it and runs `decide` again: nothing is assumed. With --missing-out the missing package and side ('old' or 'new') are
written there as JSON and the run exits 3, which is how the job knows to make that surface; any other failure exits 1. With
--candidate K every package is a release candidate: its version is the next one followed by -rc.K, and so is the bundle (N-rc.K).
"""
import argparse
import json
import os
import re
import sys
from concurrent.futures import ThreadPoolExecutor

import allocate
import apisurface
import bindcheck
import bundle
import changeset
import levelplan
import levelrun
from feeds import MANIFEST_FILE, FeedError, NugetOrg
from fdwprojects import git, snapshot


MISSING_SURFACE_EXIT = 3  # `decide` ends with this when a surface is missing, so the job can make it and run decide again


BUMPS = ("patch", "minor", "major")  # in order of size


class ReleaseError(Exception):
    pass


class SurfaceMissing(ReleaseError):
    """A package's surface file is not there: `side` is 'old' (its last stable version) or 'new' (as built now)."""

    def __init__(self, package, side, path):
        super().__init__(f"no {side} surface for {package}: {path} does not exist. Build it, dump its public surface and run decide again.")
        self.package, self.side = package, side


def without_own_versions(held, own_trees, feed, workers=16):
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
    marker; neither is an error."""
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
    own = {pid: found[0] for pid, found in latest.items() if found and found[1] is not None and found[1] == own_trees[pid]}
    resumed = set(own.values())
    conflicts = sorted(f"{pid} {found[0]}: " + ("no tree marker" if found[1] is None else f"built from tree {found[1]}, not {own_trees[pid]}")
                       for pid, found in latest.items() if found and pid not in own and found[0] in resumed)
    if conflicts:
        raise ReleaseError("RESUME_CONFLICT: this release was already pushed in part (" + ", ".join(sorted(resumed)) + "), but these packages are on the feed at that "
                           "version with other contents, so the release was pushed from different content and a re-run would renumber it: "
                           + "; ".join(conflicts[:10]) + (f"; and {len(conflicts) - 10} more" if len(conflicts) > 10 else "")
                           + ". Re-run from the commit that was pushed, or take the packages that differ out of the release by hand.")
    return {pid: [v for v in versions if v != own.get(pid)] for pid, versions in held.items()}


def release_plan(current, baseline, feed, tags, line, bump_of, bundle_id, api_breaks=()):
    """The plan as a dict. `bump_of(directories)` gives the highest bump declared for commits that touched them."""
    floors, bare = baseline_floors(baseline), baseline_bare(baseline)
    result = changeset.plan(current, baseline, api_breaks)
    projects = current.projects
    members = list(result["build_set"])  # changeset.plan lists packable projects only
    held = allocate.held_versions(projects, feed)
    held = without_own_versions(held, {projects[n].package_id: current.trees[n] for n in members}, feed)
    in_set = {projects[n].package_id for n in members}
    bump = bump_of([projects[n].path for n in members]) if members else "patch"
    numbers = {b: allocate.release_number(held, line, tags, b) for b in BUMPS}
    number = numbers[bump]

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

    manifest = {"id": bundle_id, "version": number, "packages": {**unchanged, **{projects[n].package_id: number for n in members}}}
    problems = bundle.check(manifest)
    if problems:
        raise ReleaseError("the release manifest would not make a valid bundle: " + "; ".join(problems))
    return {"plan": {"release": number, "release_numbers": numbers, "bump": bump, "line": line, "members": members, "assembly_version_floors": floors, "packages_without_runtime_assembly": bare,
                     "package_ids": {n: projects[n].package_id for n in members}, "reasons": {n: result["reasons"][n] for n in members},
                     "built_from_source_only": result["built_from_source_only"], "tests": result["tests"],
                     "removed": result["removed"], "unchanged_packages": len(unchanged)},
            "versions": unchanged, "trees": {n: current.trees[n] for n in members}, "manifest": manifest,
            "project-manifest": current.manifest(), "levels-input": levels_input(current, result, held)}


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


def surface_reader(directory, side):
    """old_surface/new_surface for levelrun: the text of DIR/<package id>.surface. A missing file raises SurfaceMissing naming the package."""
    def read(package):
        path = os.path.join(directory, package + ".surface")
        try:
            with open(path, encoding="utf-8") as f:
                return f.read()
        except FileNotFoundError:
            raise SurfaceMissing(package, side, path)
    return read


def decide_release(files, old_surface, new_surface, first_versions, settings, candidate=None, cap=None):
    """The files of a per-package release: `files` is the content of what `plan` wrote, by name ('plan', 'versions', 'trees', 'manifest',
    'levels-input'). Returns the same names with per-package versions, plus 'package-versions', 'levels' and the report lines.
    `candidate` (k) makes every package a release candidate: its version is the next one followed by -rc.k.
    `cap` is the highest level a package may take (a setting of the release; None = none): see levelrun.decide."""
    plan, versions, manifest, info = files["plan"], files["versions"], files["manifest"], files["levels-input"]
    try:
        rows, surface_reasons = levelrun.decide(
            {p: set(v) for p, v in info["depends"].items()}, info["members"], old_surface, new_surface, info["last_stable"], first_versions,
            settings, {g: set(c) for g, c in info["generators"].items()}, cap)
    except (levelplan.LevelPlanError, apisurface.SurfaceError) as e:
        raise ReleaseError(str(e))
    names, joined = info["names"], sorted(rows)
    floors = plan["assembly_version_floors"]
    assembly_versions = {}
    for p in joined:
        assembly = info["assemblies"][p]
        if assembly not in floors and p in info["last_stable"] and p not in plan["packages_without_runtime_assembly"]:
            raise ReleaseError(f"ASSEMBLY_FLOOR_MISSING: {p} (assembly {assembly}) has a last stable version, but the baseline records no floor for it "
                               "and does not list it as carrying no runtime assembly; nothing is assumed. Measure the baseline again, or fix the assembly name.")
        assembly_versions[names[p]] = assembly_version_of(rows[p]["next"], floors.get(assembly))
    published = {p: rows[p]["next"] + (f"-rc.{candidate}" if candidate else "") for p in rows}
    replaced = levelplan.replaced_packages(first_versions)
    unchanged = {p: v for p, v in versions.items() if p not in rows and p not in replaced}
    packages = {**{p: v for p, v in manifest["packages"].items() if p not in rows and p not in replaced}, **published}
    # The bundle's version follows the highest level among the packages (a new package counts as a minor): the bump that was stated is
    # the least it can be, never the most. The number for each bump was allocated by `plan` from the feed and the release tags.
    derived = max(("minor" if r["level"] == levelplan.NEW else r["level"] for r in rows.values()), key=BUMPS.index)
    bump = max(plan["bump"], derived, key=BUMPS.index)
    number = plan["release_numbers"][bump]
    bundle_version = f"{number}-rc.{candidate}" if candidate else number
    new_manifest = {"id": manifest["id"], "version": bundle_version, "packages": packages}
    problems = bundle.check(new_manifest)
    if problems:
        raise ReleaseError("the release manifest would not make a valid bundle: " + "; ".join(problems))
    members = sorted(names[p] for p in joined)
    pulled = set(members) - set(plan["members"])
    reasons = {**{n: plan["reasons"][n] for n in plan["members"] if n in members},
               **{names[p]: rows[p]["reasons"] for p in joined if names[p] in pulled}}
    tests = sorted(set(plan["tests"]) | {t for t, refs in info["test_references"].items() if pulled & set(refs) or t[:-len(".Tests")] in pulled and t.endswith(".Tests")})
    out_plan = {**plan, "members": members, "package_ids": {names[p]: p for p in joined}, "reasons": reasons, "tests": tests,
                "unchanged_packages": len(unchanged), "per_package": True, "pulled_in": sorted(pulled), "retired": replaced,
                "release": number, "bump": bump, "bump_stated": plan["bump"], "version": bundle_version, "level_cap": cap}
    return {"plan": out_plan, "versions": unchanged, "trees": {names[p]: info["trees"][p] for p in joined}, "manifest": new_manifest,
            "package-versions": {names[p]: published[p] for p in joined}, "assembly-versions": assembly_versions,
            "levels": {"packages": rows, "report": levelrun.report(rows, surface_reasons)}}


def baseline_floors(baseline):
    """{assembly: x.y.z.w}: the assembly version floor of each assembly, as the baseline release recorded it in its manifest. A baseline
    without them stops the plan: they are measured from the release's own dlls (bindcheck.py floors) and never assumed."""
    floors = baseline.get(bindcheck.FLOORS_KEY) if baseline else None
    if floors is None:
        raise ReleaseError(f"BASELINE_FLOORS_MISSING: the baseline manifest does not state {bindcheck.FLOORS_KEY}. Measure them from the baseline release's "
                           "packages (Fdw.ApiSurface --identity on each, then bindcheck.py floors --dir DIR) and add them to the baseline manifest.")
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


def decide_main(a):
    files = {name: read_json(os.path.join(a.plan_dir, name + ".json")) for name in ("plan", "versions", "trees", "manifest", "levels-input")}
    try:
        result = decide_release(files, surface_reader(a.old_surfaces, "old"), surface_reader(a.new_surfaces, "new"),
                                levelplan.load_first_versions(a.first_versions), apisurface.load_settings(a.settings), a.candidate, a.max_level)
    except SurfaceMissing as e:
        if a.missing_out:
            with open(a.missing_out, "w", encoding="utf-8", newline="\n") as f:
                json.dump({"package": e.package, "side": e.side}, f)
        print(f"release not decided; nothing was written:\n  {e}", file=sys.stderr)
        sys.exit(MISSING_SURFACE_EXIT)
    except (ReleaseError, levelplan.LevelPlanError, apisurface.SurfaceError) as e:
        sys.exit(f"release not decided; nothing was written:\n  {e}")
    write(a.out, {**{k: v for k, v in files.items() if k not in result}, **result})
    print("\n".join(result["levels"]["report"]))
    print(f"{len(result['plan']['members'])} package(s) to build and push, {len(result['plan']['pulled_in'])} pulled in by a major; files in {a.out}")


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    p = sub.add_parser("plan")
    p.add_argument("--repo", required=True)
    p.add_argument("--line", required=True)
    p.add_argument("--base-ref")
    p.add_argument("--baseline-from-bundle", action="store_true")
    p.add_argument("--baseline")
    p.add_argument("--bump", choices=["patch", "minor", "major"])
    p.add_argument("--bundle-id", required=True)
    p.add_argument("--out", required=True)
    p.add_argument("--ref", default="HEAD")
    p.add_argument("--api-breaks")
    p.add_argument("--assembly-info", help="only with --base-ref: a git ref holds no dlls, so what is measured from the release's packages is given here (a JSON file "
                                           "with assemblyVersionFloors and packagesWithoutRuntimeAssembly)")
    d = sub.add_parser("decide")
    for name in ("plan-dir", "old-surfaces", "new-surfaces", "first-versions", "settings", "out"):
        d.add_argument("--" + name, required=True)
    d.add_argument("--candidate", type=int, help="k: make every package a release candidate, version-rc.k")
    d.add_argument("--max-level", choices=["patch", "minor", "major"], help="the highest level any package may take in this release (the setting max_level); none by default")
    d.add_argument("--missing-out", help="where to write {package, side} when a surface is missing (the run then exits 3)")
    a = ap.parse_args(argv)
    if a.cmd == "decide":
        return decide_main(a)
    if not re.fullmatch(r"\d+\.\d+", a.line):
        ap.error("--line is major.minor, for example 1.4")
    if sum([bool(a.base_ref), a.baseline_from_bundle, bool(a.baseline)]) != 1:
        ap.error("give exactly one of --base-ref, --baseline-from-bundle and --baseline")
    if a.assembly_info and not a.base_ref:
        ap.error("--assembly-info is only for --base-ref; a baseline file or bundle manifest carries its own")
    if a.base_ref and a.bump:
        ap.error("--bump is only for a baseline that is not a git ref; with --base-ref the bump is read from the commits")
    if not a.base_ref and not a.bump:
        ap.error("--bump is required without --base-ref: the commits that would declare it are not available")
    current = snapshot(a.repo, a.ref)
    breaks = read_json(a.api_breaks) if a.api_breaks else []
    tags = git(a.repo, "tag", "-l", "release-*").decode().split()
    feed = NugetOrg()
    try:
        if a.base_ref:
            baseline, bump_of = changeset.baseline_from_ref(a.repo, a.base_ref), lambda dirs: allocate.bumps_in_range(a.repo, dirs, a.base_ref, a.ref)
            if a.assembly_info:
                baseline = baseline | read_json(a.assembly_info)
        else:
            baseline = baseline_from_bundle(feed, a.bundle_id) if a.baseline_from_bundle else read_json(a.baseline)
            bump_of = lambda dirs: a.bump
        result = release_plan(current, baseline, feed, tags, a.line, bump_of, a.bundle_id, breaks)
    except ReleaseError as e:
        sys.exit(f"release not planned; nothing was written:\n  {e}")
    write(a.out, result)
    p = result["plan"]
    print(f"release {p['release']} ({p['bump']}), line {a.line}: {len(p['members'])} packages to build and push, "
          f"{p['unchanged_packages']} unchanged, {len(p['tests'])} test projects; files in {a.out}")


if __name__ == "__main__":
    main()

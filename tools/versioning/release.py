#!/usr/bin/env python3
"""Plan a release: the one read-only step that turns a baseline and the feed into everything the later steps read.

  release.py plan --repo R --line 1.4 --bundle-id Fdw.Bundle --out DIR [--ref HEAD] [--api-breaks FILE]
                  (--base-ref REF | --baseline-from-bundle | --baseline FILE --bump patch|minor|major)

The baseline (what the last release was made from) comes from exactly one of: a git ref such as a release tag
(--base-ref; the highest `bump:` trailer since it is read from the commit messages), the project manifest inside the
latest stable bundle on nuget.org (--baseline-from-bundle), or a manifest file (--baseline). The last two need no tags
and no history, which is what the GitHub mirror has; there the bump cannot be read from commits, so --bump is required
and nothing is assumed. A bundle with no manifest inside stops the plan and says so.

It combines changeset.py (which packages changed), allocate.py (the one release number N, the highest declared bump)
and the feed (the version every unchanged package last shipped with), and writes five files into DIR:

  plan.json      the release number, the bump, the build set and why each member is in it, the tests to run
  versions.json  {package id: version} for every packable package OUTSIDE the set: input to `buildset.py prepare`
  trees.json     {project name: git tree id} for the packable members: input to `buildset.py prepare`
  manifest.json  {id, version, packages} for the whole release: input to `bundle.py generate`
  project-manifest.json  the project manifest of the tree planned from: `bundle.py generate --project-manifest`
                 carries it in the bundle, and the next release reads it back as its baseline

An unchanged package keeps the highest stable version on the feed. A package outside the set with no stable version
there is an error that names it: it cannot be shown to be unchanged, and nothing is assumed. Nothing is pushed and the
repository is not touched; the only files written are the five above.
"""
import argparse
import json
import os
import re
import sys

import allocate
import bundle
import changeset
from feeds import MANIFEST_FILE, NugetOrg
from fdwprojects import git, snapshot


class ReleaseError(Exception):
    pass


def release_plan(current, baseline, feed, tags, line, bump_of, bundle_id, api_breaks=()):
    """The plan as a dict. `bump_of(directories)` gives the highest bump declared for commits that touched them."""
    result = changeset.plan(current, baseline, api_breaks)
    projects = current.projects
    members = list(result["build_set"])  # changeset.plan lists packable projects only
    held = allocate.held_versions(projects, feed)
    in_set = {projects[n].package_id for n in members}
    bump = bump_of([projects[n].path for n in members]) if members else "patch"
    number = allocate.release_number(held, line, tags, bump)

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
    return {"plan": {"release": number, "bump": bump, "line": line, "members": members,
                     "package_ids": {n: projects[n].package_id for n in members}, "reasons": {n: result["reasons"][n] for n in members},
                     "built_from_source_only": result["built_from_source_only"], "tests": result["tests"],
                     "removed": result["removed"], "unchanged_packages": len(unchanged)},
            "versions": unchanged, "trees": {n: current.trees[n] for n in members}, "manifest": manifest,
            "project-manifest": current.manifest()}


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
    ap.add_argument("cmd", choices=["plan"])
    ap.add_argument("--repo", required=True)
    ap.add_argument("--line", required=True)
    ap.add_argument("--base-ref")
    ap.add_argument("--baseline-from-bundle", action="store_true")
    ap.add_argument("--baseline")
    ap.add_argument("--bump", choices=["patch", "minor", "major"])
    ap.add_argument("--bundle-id", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--ref", default="HEAD")
    ap.add_argument("--api-breaks")
    a = ap.parse_args(argv)
    if not re.fullmatch(r"\d+\.\d+", a.line):
        ap.error("--line is major.minor, for example 1.4")
    if sum([bool(a.base_ref), a.baseline_from_bundle, bool(a.baseline)]) != 1:
        ap.error("give exactly one of --base-ref, --baseline-from-bundle and --baseline")
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

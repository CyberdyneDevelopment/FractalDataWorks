#!/usr/bin/env python3
"""Allocate the next version of each package in a build set from what the feed already holds. Read-only.

  allocate.py plan --repo R --line 1.4 (--base-ref REF | --baseline FILE) [--ref HEAD] [--only A,B] [--json]

A release has one number N, shared by every package in it that changed. N is the highest major.minor.patch in the
line among every stable version of every packable package on the feed and every `release-*-x.y.z` tag, plus one. A
version with a tail (`-rc.1`, `-internal.x`) never counts: it does not stop the plain version being published, and
counting it would move the number on with every build. A release that fails after pushing some stable versions has
spent them, and they count. A release is a patch unless a commit between the baseline and the head that touched a package
in the build set carries a `bump: minor` or `bump: major` line; the highest one declared wins. Nothing is pushed and
nothing is written.
"""
import argparse
import json
import re
import sys
from concurrent.futures import ThreadPoolExecutor

import changeset
from feeds import NugetOrg
from fdwprojects import git, snapshot

VERSION = re.compile(r"^(\d+)\.(\d+)\.(\d+)(?:-[0-9A-Za-z.-]+)?(?:\+.*)?$")
TAG_VERSION = re.compile(r"^release-.*?(\d+\.\d+\.\d+)$")
BUMP_LINE = re.compile(r"^bump:\s*(patch|minor|major)\s*$", re.I | re.M)
ORDER = {"patch": 0, "minor": 1, "major": 2}


def triple(version):
    m = VERSION.match(version)
    return tuple(int(x) for x in m.groups()) if m else None


def tag_versions(tags):
    return [m.group(1) for t in tags if (m := TAG_VERSION.match(t))]


def next_version(known, line, bump="patch"):
    """The next free version in `line` ("1.4") given every version already used. Only a version with no tail spends a
    number: 1.4.7-rc.1 and 1.4.7-internal.x do not stop 1.4.7 being published, so they must not move the next number."""
    major, minor = (int(x) for x in line.split("."))
    used = [t for v in known if "-" not in v and (t := triple(v))]
    if bump == "major":
        top = max((t[0] for t in used), default=None)
        return f"{top + 1}.0.0" if top is not None else f"{major}.{minor}.0"
    if bump == "minor":
        top = max((t[1] for t in used if t[0] == major), default=None)
        return f"{major}.{top + 1}.0" if top is not None else f"{major}.{minor}.0"
    top = max((t[2] for t in used if t[:2] == (major, minor)), default=None)
    return f"{major}.{minor}.{top + 1}" if top is not None else f"{major}.{minor}.0"


def declared_bump(messages):
    found = [m.lower() for text in messages for m in BUMP_LINE.findall(text)]
    return max(found, key=ORDER.get, default="patch")


def bumps_in_range(root, directories, base_ref, head_ref):
    """The highest `bump:` declared by a commit between the refs that touched any of the directories."""
    out = git(root, "log", f"{base_ref}..{head_ref}", "--format=%B%x00", "--", *directories).decode("utf-8", "replace")
    return declared_bump(out.split("\0"))


def highest(versions):
    return max(versions, key=lambda v: ((triple(v) or (0, 0, 0)), "-" not in v), default=None)


def held_versions(projects, feed, workers=16):
    """{package id: versions on the feed} for every packable project. One request each, in parallel; any failure raises."""
    ids = sorted({p.package_id for p in projects.values() if p.packable})
    with ThreadPoolExecutor(max_workers=workers) as pool:
        return dict(zip(ids, pool.map(feed.versions, ids)))


def release_number(held, line, tags, bump="patch"):
    """The next free number in `line` given every version held (held: package id -> versions) and every release tag."""
    return next_version([v for vs in held.values() for v in vs] + tag_versions(tags), line, bump)


def release_plan(names, projects, line, feed, tags, bump):
    """One number for the whole release. It is free for every package because it is one above the
    highest version of any packable package, and any release tag, in the line. Only packable names in the build set
    get the number; a non-packable project in the set (an analyzer) is built, not packed."""
    held = held_versions(projects, feed)
    number = release_number(held, line, tags, bump)
    packed = [n for n in names if projects[n].packable]
    return {"release": number, "bump": bump,
            "packages": [{"project": n, "package": projects[n].package_id, "highest_on_feed": highest(held[projects[n].package_id]), "next": number}
                         for n in packed],
            "not_packed": [n for n in names if not projects[n].packable]}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    n = sub.add_parser("number", help="print only the next release number for a line (a patch release)")
    n.add_argument("--repo", required=True)
    n.add_argument("--line", required=True)
    n.add_argument("--ref", default="HEAD")
    p = sub.add_parser("plan")
    p.add_argument("--repo", required=True)
    p.add_argument("--line", required=True)
    p.add_argument("--ref", default="HEAD")
    p.add_argument("--base-ref")
    p.add_argument("--baseline")
    p.add_argument("--only", help="names to allocate for, instead of the build set")
    p.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    if not re.fullmatch(r"\d+\.\d+", a.line):
        ap.error("--line is major.minor, for example 1.4")
    current = snapshot(a.repo, a.ref)
    if a.cmd == "number":
        tags = git(a.repo, "tag", "-l", "release-*").decode().split()
        print(release_number(held_versions(current.projects, NugetOrg()), a.line, tags))
        return
    if a.only:
        names = [n for n in a.only.split(",") if n]
        unknown = [n for n in names if n not in current.projects]
        if unknown:
            sys.exit(f"not projects at {a.ref}: {unknown}")
    else:
        if bool(a.base_ref) == bool(a.baseline):
            ap.error("give exactly one of --base-ref and --baseline, or --only")
        base = changeset.baseline_from_ref(a.repo, a.base_ref) if a.base_ref else json.load(open(a.baseline))
        names = changeset.plan(current, base)["build_set"]
    tags = git(a.repo, "tag", "-l", "release-*").decode().split()
    base_ref = a.base_ref or a.ref
    bump = bumps_in_range(a.repo, [current.projects[n].path for n in names], base_ref, a.ref) if a.base_ref else "patch"
    plan = release_plan(names, current.projects, a.line, NugetOrg(), tags, bump)
    if a.json:
        print(json.dumps(plan, indent=1))
        return
    print(f"release {plan['release']} ({plan['bump']}), line {a.line}; tags counted: {len(tag_versions(tags))}; "
          f"{len(plan['packages'])} packages, {len(plan['not_packed'])} built but not packed")
    for r in plan["packages"]:
        print(f"  {r['package']:50s} on feed {str(r['highest_on_feed']):26s} -> {r['next']}")

if __name__ == "__main__":
    main()

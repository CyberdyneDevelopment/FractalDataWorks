#!/usr/bin/env python3
"""Work out which packages a change needs built.

  changeset.py plan     --repo R [--ref HEAD] (--base-ref REF | --baseline FILE) [--api-breaks FILE] [--json]
  changeset.py manifest --repo R [--ref HEAD]
  changeset.py replay   --repo R [--count 353]

A package has changed when the git tree id of its directory differs from the baseline's. Tree ids depend on the
directory's contents only, so a moved project is unchanged and a squashed history gives the same answer. The build
set is the changed packages, plus more only when one of these holds:

  - a repo-wide build file changed (the version lines of Directory.Build.props are ignored): everything;
  - a generator, analyzer or code-fix project changed: everything above it, because it changes the code compiled
    into each of them and no API check sees that;
  - a package in --api-breaks (a JSON list of package ids from the API check): everything above it;
  - a project packs another project's build output by file path: both go together.

Projects that cannot be packages are built from source whenever a member needs them. Nothing is written but stdout.
"""
import argparse
import json
import subprocess
import sys
from collections import Counter, defaultdict

from fdwprojects import git, snapshot


def dependents(projects, start):
    """Every src project that references `start`, directly or through others."""
    above = defaultdict(set)
    for p in projects.values():
        if p.kind == "src":
            for r in p.refs:
                above[r].add(p.name)
    seen, stack = set(), [start]
    while stack:
        for d in above[stack.pop()]:
            if d not in seen:
                seen.add(d)
                stack.append(d)
    return seen


def plan(current, baseline, api_breaks=()):
    projects = current.projects
    src = {n for n, p in projects.items() if p.kind == "src"}
    base_trees = baseline["projects"] if baseline else {}
    base_globals = baseline["globals"] if baseline else {}
    reasons = defaultdict(list)

    def add(name, why):
        if projects[name].packable and why not in reasons[name]:
            reasons[name].append(why)

    # A project that cannot be a package still counts as changed: it can be a generator, or be packed by file path.
    differs = {n for n in src if n not in base_trees or base_trees[n] != current.trees[n]}
    for n in sorted(differs):
        add(n, "no baseline" if not baseline else "new package" if n not in base_trees else "source changed")

    if baseline:
        for f in sorted(current.globals):
            if base_globals.get(f) != current.globals[f]:
                for n in sorted(src):
                    add(n, f"repo-wide build file changed: {f}")
    generators = {}  # a changed generator that is itself a package -> the packages it caused to be rebuilt
    for b in sorted(differs & current.build_time):
        # Directory.Build.props references these for every project, so no project lists them itself.
        above = src if b in current.always_built else dependents(projects, b)
        for d in sorted(above):
            add(d, f"{b} is a generator, analyzer or code-fix project and changed")
        if projects[b].packable:
            generators[b] = sorted(d for d in above if projects[d].packable and d != b)
    for x in sorted(n for n in differs - current.build_time if not projects[n].packable):
        for d in sorted(dependents(projects, x)):
            add(d, f"{x} cannot be a package and changed")
    by_id = {p.package_id: p.name for p in projects.values() if p.kind == "src"}
    for pid in sorted(api_breaks):
        if pid in by_id:
            add(by_id[pid], f"API break reported in {pid}")
            for d in sorted(dependents(projects, by_id[pid])):
                add(d, f"API break in {pid}")

    # A package that packs another project's output by file path is rebuilt when that project changes,
    # and that project is built whenever the package is.
    grew = True
    while grew:
        grew = False
        for n in sorted(src):
            for e in projects[n].embeds:
                if e not in projects:
                    continue  # the embedded project no longer exists, so the line never matches
                if e in differs and n not in reasons:
                    add(n, f"it packs the build output of {e} by file path"); grew = True
                if n in reasons and e not in reasons:
                    add(e, f"{n} packs its build output by file path"); grew = bool(projects[e].packable) or grew

    members = set(reasons)
    source_only, stack = set(current.always_built), list(members)
    while stack:
        p = projects[stack.pop()]
        for r in p.refs | {e for e in p.embeds if e in projects}:
            if r in projects and projects[r].kind == "src" and not projects[r].packable and r not in source_only:
                source_only.add(r)
                stack.append(r)
    source_only -= members

    def runs(name, project):
        if not baseline:
            return True
        return base_trees.get(name) != current.trees[name] or bool(project.refs & members) \
            or any(name == m + ".Tests" for m in members)

    tests = sorted(n for n, p in projects.items() if p.kind == "tests" and runs(n, p))
    removed = sorted(set(base_trees) - set(projects)) if baseline else []
    return {"build_set": sorted(members), "reasons": {n: reasons[n] for n in sorted(members)},
            "built_from_source_only": sorted(source_only), "tests": tests, "removed": removed,
            "packages": {n: projects[n].package_id for n in sorted(members)}, "generators": generators,
            "total_src_projects": len(src)}


def report(result):
    out = [f"build set: {len(result['build_set'])} of {result['total_src_projects']} src projects, "
           f"plus {len(result['built_from_source_only'])} built from source only, {len(result['tests'])} test projects to run"]
    shown = Counter()
    for n in result["build_set"]:
        shown[tuple(result["reasons"][n])] += 1
    for why, count in shown.most_common():
        names = [n for n in result["build_set"] if tuple(result["reasons"][n]) == why]
        out.append(f"  {count:3d}  {'; '.join(why)}: " + (", ".join(names) if count <= 6 else ", ".join(names[:6]) + ", ..."))
    if result["removed"]:
        out.append("removed since the baseline: " + ", ".join(result["removed"]))
    return "\n".join(out)


def baseline_from_ref(root, ref):
    return snapshot(root, ref).manifest()


def replay(root, count):
    shas = git(root, "log", "--no-merges", "-n", str(count), "--format=%H", "--", "public/src", "public/tests", "src", "tests").decode().split()
    sizes, expanded = [], 0
    for sha in shas:
        base = baseline_from_ref(root, sha + "^")
        result = plan(snapshot(root, sha), base)
        if not result["build_set"]:
            continue
        sizes.append(len(result["build_set"]))
        if any(r != "source changed" and r != "new package" for rs in result["reasons"].values() for r in rs):
            expanded += 1
    sizes.sort()
    print(f"{len(sizes)} commits that change a package")
    print(f"build set per commit: median {sizes[len(sizes) // 2]}, 90th percentile {sizes[int(len(sizes) * .9)]}, max {sizes[-1]}")
    print(f"5 or fewer: {sum(1 for s in sizes if s <= 5) * 100 / len(sizes):.1f}%   "
          f"needed more than the changed packages: {expanded} ({expanded * 100 / len(sizes):.1f}%)")


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("plan", "manifest", "replay"):
        s = sub.add_parser(name)
        s.add_argument("--repo", required=True)
        if name != "replay":
            s.add_argument("--ref", default="HEAD")
    sub.choices["plan"].add_argument("--base-ref")
    sub.choices["plan"].add_argument("--baseline")
    sub.choices["plan"].add_argument("--api-breaks")
    sub.choices["plan"].add_argument("--json", action="store_true")
    sub.choices["replay"].add_argument("--count", type=int, default=353)
    a = ap.parse_args(argv)
    if a.cmd == "manifest":
        print(json.dumps(snapshot(a.repo, a.ref).manifest(), indent=1))
    elif a.cmd == "replay":
        replay(a.repo, a.count)
    else:
        if bool(a.base_ref) == bool(a.baseline):
            ap.error("give exactly one of --base-ref and --baseline")
        base = baseline_from_ref(a.repo, a.base_ref) if a.base_ref else json.load(open(a.baseline))
        breaks = json.load(open(a.api_breaks)) if a.api_breaks else []
        result = plan(snapshot(a.repo, a.ref), base, breaks)
        print(json.dumps(result, indent=1) if a.json else report(result))


if __name__ == "__main__":
    main()

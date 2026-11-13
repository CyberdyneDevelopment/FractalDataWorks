#!/usr/bin/env python3
"""The table for a release's notes, and the checks the release owner asks for (FDW-4784, FDW-5272).

  releasenotes.py --work DIR --repo R --base REF --baseline FILE [--head REF] [--path-prefix public/] [--fallback-ref REF ...]

DIR is the release job's work folder after the `plan` phase (release-work): plan.json, levels.json, levels-input.json, package-versions.json,
assembly-versions.json. For every package that changes version it prints one line: the package, its last and next version, the level,
the first reason (why the package is in the release) and the items that drive it, which are the item keys
(FDW-1234, RUI-12 ...) named in the commits that changed the package's folder since the baseline. Then three checks:

  - no assembly version goes down: each package's AssemblyVersion is not below the floor the baseline recorded for its assembly;
  - the bundle follows the highest package level: its bump is the highest level among the packages that already existed (a new package raises nothing, FDW-5591), and
    never lower than the bump that was stated;
  - the packages that joined only because a package they reference went major are counted apart (none, now that the plan puts every package
    above a changed one in the release).

Which commits are "since the baseline": the release trees are squashed commits on another branch, so BASE..HEAD would list every item ever
merged. Each package's folder in the baseline manifest (--baseline, `projects`) is a git tree id, and nearly always the same tree some internal
commit left that folder in; the commits after that one are the package's changes. A package whose folder was built by cherry-picking
(its state is on no internal commit) is anchored at the same folder in each --fallback-ref in turn (the releases before, newest first; a package
patched in two releases only matches an older one), and the commits that are patch-equivalent to ones on BASE are left out. A package that matches neither stops the run: nothing is guessed.

The release runs on the mirror layout (folders `src/...`) while the history is in the repository (`public/src/...`): `--path-prefix public/`
is put in front of each folder before the history is read. Nothing is assumed: a package with no folder in levels-input.json, or a report line that cannot be read, stops the run.
"""
import argparse
import json
import os
import re
import subprocess
import sys

import levelplan

ITEM = re.compile(r"\b(?:FDW|API|ETL|SCH|RUI|TC|OPERATIONS|MSSQL)-\d+\b")
BUMPS = ("patch", "minor", "major")


class NotesError(Exception):
    pass


def read(work, name):
    path = os.path.join(work, name)
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except (OSError, ValueError) as e:
        raise NotesError(f"{path} cannot be read: {e}")


def reasons_from(report):
    """{package: first reason} from the report lines `release.py decide` writes: PACKAGE LAST LEVEL NEXT WHY, with a header and a total line."""
    out = {}
    for line in report[1:-1]:
        fields = line.split(None, 4)
        if len(fields) < 5:
            raise NotesError(f"cannot read the report line {line!r}")
        out[fields[0]] = fields[4].strip()
    return out


def git(repo, *args):
    return subprocess.run(["git", "-C", repo, *args], capture_output=True, text=True, check=True).stdout


def tree_of(repo, rev, directory):
    done = subprocess.run(["git", "-C", repo, "rev-parse", "--verify", "--quiet", f"{rev}:{directory}"], capture_output=True, text=True)
    return done.stdout.strip() if done.returncode == 0 else None


def commits_after(repo, head, directory, tree):
    """The commits (newest first) that touched the folder after the newest one that left it as `tree`; None when no commit did."""
    shas = git(repo, "log", head, "--format=%H", "--", directory).split()
    for index, sha in enumerate(shas):
        if tree_of(repo, sha, directory) == tree:
            return shas[:index]
    return None


def items_for(repo, base, head, directory, baseline_tree, fallback_refs=()):
    """The item keys named in the commits that changed the folder since the baseline (see the module text for the anchors)."""
    shas = commits_after(repo, head, directory, baseline_tree)
    for fallback_ref in fallback_refs if shas is None else ():
        earlier = tree_of(repo, fallback_ref, directory)
        shas = commits_after(repo, head, directory, earlier) if earlier else None
        if shas is not None:
            shipped = set(git(repo, "log", "--cherry-pick", "--left-only", f"{base}...{head}", "--format=%H", "--", directory).split())
            kept = set(git(repo, "log", "--cherry-pick", "--right-only", f"{base}...{head}", "--format=%H", "--", directory).split())
            shas = [s for s in shas if s in kept and s not in shipped]
            break
    if shas is None:
        raise NotesError(f"{directory} matches neither the baseline's folder state nor the fallback's on any commit of {head}, so its items cannot be attributed")
    if not shas:
        return []
    text = git(repo, "log", "--no-walk=unsorted", "--format=%s%n%b%x00", *shas)
    return sorted({m for m in ITEM.findall(text)}, key=lambda k: (k.split("-")[0], int(k.split("-")[1])))


def build(work, repo, base, head, baseline, prefix="", fallback_refs=()):
    plan, levels, info = read(work, "plan.json"), read(work, "levels.json"), read(work, "levels-input.json")
    assembly_versions = read(work, "assembly-versions.json")
    rows, why = levels["packages"], reasons_from(levels["report"])
    table = []
    for package in sorted(rows, key=lambda p: (BUMPS.index("patch" if rows[p]["level"] == "new" else rows[p]["level"]) * -1, p.lower())):
        row = rows[package]
        path = info["paths"].get(package)
        if path is None:
            raise NotesError(f"levels-input.json has no folder for {package}")
        table.append({"package": package, "last": row["last"], "next": row["next"], "level": row["level"], "why": why.get(package, ""),
                      "joined": any("went major" in r for r in row["reasons"]) and not any("its source changed" in r for r in row["reasons"]),
                      "items": items_for(repo, base, head, prefix + os.path.dirname(path), baseline.get(info["names"][package]), fallback_refs)})
    floors = plan["assembly_version_floors"]
    low = []
    for package, row in rows.items():
        name, assembly = info["names"][package], info["assemblies"][package]
        floor = floors.get(assembly)
        have = assembly_versions[name]
        if floor and tuple(int(x) for x in have.split(".")) < tuple(int(x) for x in floor.split(".")):
            low.append(f"{package}: AssemblyVersion {have} is below the floor {floor}")
    derived = max((r["level"] for r in rows.values() if r["level"] != levelplan.NEW), key=BUMPS.index, default=plan["bump_stated"])
    bundle = {"version": plan["version"], "bump": plan["bump"], "derived": derived, "stated": plan["bump_stated"]}
    return table, low, bundle, len(floors)


def render(table, low, bundle, floors):
    lines = ["| package | last | next | level | why | items |", "|---|---|---|---|---|---|"]
    for r in table:
        lines.append(f"| {r['package']} | {r['last'] or '-'} | {r['next']} | {r['level']}{' (pulled in by a major)' if r['joined'] else ''} | {r['why']} | {', '.join(r['items']) or '-'} |")
    counts = {name: sum(1 for r in table if r["level"] == name) for name in ("major", "minor", "new", "patch")}
    lines += ["", f"{len(table)} package(s) change version: " + ", ".join(f"{n} {k}" for k, n in counts.items() if n) + f"; {sum(1 for r in table if r['joined'])} only because a package they reference went major."]
    lines.append("assembly versions: " + (f"{len(low)} below their floor" if low else f"none below their floor ({floors} floors checked)"))
    lines += [f"  {x}" for x in low]
    follows = BUMPS.index(bundle["bump"]) == max(BUMPS.index(bundle["derived"]), BUMPS.index(bundle["stated"]))
    lines.append(f"bundle {bundle['version']}: bump {bundle['bump']}, highest package level {bundle['derived']}, stated {bundle['stated']}: "
                 + ("follows the highest package" if follows else "DOES NOT follow the highest package"))
    return lines, bool(low) or not follows


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--work", required=True)
    ap.add_argument("--repo", required=True)
    ap.add_argument("--base", required=True)
    ap.add_argument("--head", default="HEAD")
    ap.add_argument("--path-prefix", default="")
    ap.add_argument("--baseline", required=True, help="the baseline manifest the release was planned against (its `projects` are folder tree ids)")
    ap.add_argument("--fallback-ref", action="append", default=[], help="a release before the baseline, newest first (repeat), to anchor a package whose baseline folder state is on no commit")
    a = ap.parse_args(argv)
    try:
        with open(a.baseline, encoding="utf-8") as f:
            baseline = json.load(f)["projects"]
        lines, failed = render(*build(a.work, a.repo, a.base, a.head, baseline, a.path_prefix, a.fallback_ref))
    except (NotesError, subprocess.CalledProcessError, KeyError) as e:
        sys.exit(f"FAIL: {e}")
    print("\n".join(lines))
    if failed:
        sys.exit(1)


if __name__ == "__main__":
    main()

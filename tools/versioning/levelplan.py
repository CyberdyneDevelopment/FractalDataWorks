#!/usr/bin/env python3
"""Per-package versions from per-package levels (FDW-4784).

plan_levels() is the graph rule of the plan, as a pure function so it can be tested without a build:

  - every package whose own source changed (the build set) has a level from its own built public surface, decided by apisurface.py;
  - a package that goes MAJOR pulls each package that references it into the release at PATCH, because a major is the only change
    that can break a binary that was not rebuilt; that dependent's own level (from its own surface) still applies and is never
    lower than patch, and if it goes major in turn its dependents are pulled in too, and so on until nothing new joins;
  - a generator has nothing of its own to compare (its surface is its output), so its level is the highest level among the
    consumers it caused, and never lower than patch;
  - each package's next version is its OWN last stable version raised by its level;
  - a package with no last stable version (new, or the new id of a renamed or split package) takes the first version that
    first-versions.json declares for it, level "new". The file is required, and without an entry the plan stops with
    FIRST_VERSION_NOT_DECLARED naming the entry to add: a first version is stated, never guessed. An entry for a package that
    already has a stable version, or that is not in the release, also stops (a stale or mistyped entry would otherwise be
    ignored in silence). An entry may say which package ids it replaces ({"version": "1.0.0", "replaces": ["Old.Id"]}); those
    ids must exist, must not be changing in the same release, and leave the release as removed, which is how a rename or a split
    (several entries replacing one id) is stated.

The release that ships a new package removes that package's entry in the same release commit, and logs it by name
("first version 1.0.0 of X shipped; entry removed"): `levelplan.py retire --plan FILE --first-versions FILE`, run by the release
procedure on the plan it just published. An entry still there for any other reason (a hand edit, a failed release) keeps stopping
the plan with FIRST_VERSION_FOR_EXISTING_PACKAGE, so nothing is ignored in silence.

Every failure is a LevelPlanError with a code, and its message begins with that code.

The caller supplies the facts and the level of each package as it joins (a package that joins late has to be built before its
level is known), so level_of(package) is called once per package, in a stable order.
"""
import argparse
import json
import re
import sys

PATCH, MINOR, MAJOR, NEW = "patch", "minor", "major", "new"
RANK = {PATCH: 0, MINOR: 1, NEW: 1, MAJOR: 2}  # a new package is an addition: a minor to whatever it is added to
STABLE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")


class LevelPlanError(Exception):
    def __init__(self, code, message):
        super().__init__(f"{code}: {message}")
        self.code = code


def highest(levels):
    return max(levels, key=RANK.get, default=PATCH)


def next_version(last, level):
    """The version after `last` at `level`. `last` must be a plain stable x.y.z."""
    match = STABLE.match(last or "")
    if not match:
        raise LevelPlanError("LAST_VERSION_NOT_PLAIN", f"last stable version {last!r} is not a plain x.y.z")
    major, minor, patch = (int(x) for x in match.groups())
    if level == MAJOR:
        return f"{major + 1}.0.0"
    if level == MINOR:
        return f"{major}.{minor + 1}.0"
    if level == PATCH:
        return f"{major}.{minor}.{patch + 1}"
    raise LevelPlanError("UNKNOWN_LEVEL", f"unknown level {level!r}")


def load_first_versions(path):
    """{package id: {'version', 'replaces'}} from first-versions.json. The file is required; an empty "firstVersions" is a valid answer."""
    try:
        with open(path, encoding="utf-8") as f:
            document = json.load(f)
    except OSError as e:
        raise LevelPlanError("FIRST_VERSIONS_FILE_MISSING", f"{path} cannot be read: {e}. It is required, even when it declares nothing.")
    except ValueError as e:
        raise LevelPlanError("FIRST_VERSIONS_FILE_INVALID", f"{path} is not JSON: {e}")
    declared = document.get("firstVersions") if isinstance(document, dict) else None
    if not isinstance(declared, dict):
        raise LevelPlanError("FIRST_VERSIONS_FILE_INVALID", f"{path} has no object 'firstVersions'")
    return {package: _entry(package, entry) for package, entry in declared.items()}


def _entry(package, entry):
    version, replaces = (entry, []) if isinstance(entry, str) else (entry.get("version"), entry.get("replaces", [])) if isinstance(entry, dict) else (None, [])
    if not isinstance(version, str) or not STABLE.match(version):
        raise LevelPlanError("FIRST_VERSION_INVALID", f"the first version declared for {package} is {version!r}; it must be a plain x.y.z")
    if not isinstance(replaces, list) or not all(isinstance(r, str) for r in replaces):
        raise LevelPlanError("FIRST_VERSION_INVALID", f"'replaces' for {package} must be a list of package ids")
    return {"version": version, "replaces": sorted(replaces)}


def retire_entries(path, plan):
    """Removes from first-versions.json the entry of every package this release introduced (level "new") and returns one log line
    per package. The file is read and checked first, so an entry the plan did not use is left where it is."""
    declared = load_first_versions(path)
    shipped = sorted(package for package, row in plan.items() if row["level"] == NEW)
    missing = [package for package in shipped if package not in declared]
    if missing:
        raise LevelPlanError("FIRST_VERSION_ENTRY_MISSING", f"the plan introduces {', '.join(missing)} but {path} declares no entry for it")
    with open(path, encoding="utf-8") as f:
        document = json.load(f)
    for package in shipped:
        del document["firstVersions"][package]
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(document, f, indent=2, sort_keys=True)
        f.write("\n")
    return [f"first version {plan[package]['next']} of {package} shipped; entry removed" for package in shipped]


def replaced_packages(first_versions):
    """The package ids that leave the release because a declared entry replaces them."""
    return sorted({old for entry in first_versions.values() for old in entry["replaces"]})


def dependents_of(depends):
    """{package: packages that reference it} from {package: packages it references}."""
    reverse = {}
    for package, referenced in depends.items():
        for target in referenced:
            reverse.setdefault(target, set()).add(package)
    return reverse


def plan_levels(depends, members, level_of, last_stable, first_versions, generators=None):
    """{package: {'last', 'level', 'next', 'reasons'}} for every package in the release.

    depends       {package: set of packages it references}
    members       the packages whose own source changed
    level_of      level_of(package) -> the level of the package's own built surface (patch, minor or major)
    last_stable   {package: its last stable version}
    first_versions  load_first_versions() of first-versions.json: the declared first version of each new package (may be empty)
    generators    {generator package: set of the consumers it caused to be rebuilt}
    """
    reverse = dependents_of(depends)
    reasons, level, order = {}, {}, []

    def join(package, why):
        if package in level:
            reasons[package].append(why)
            return False
        if package in last_stable:
            level[package] = level_of(package)
            reasons[package] = [why, f"its own surface is {level[package]}"]
        else:
            level[package] = NEW
            reasons[package] = [why, "it is a new package"]
        order.append(package)
        return True

    pending = sorted(members)
    for package in pending:
        join(package, "its source changed")
    done = set()
    while True:
        majors = [p for p in order if level[p] == MAJOR and p not in done]
        if not majors:
            break
        for base in majors:
            done.add(base)
            for dependent in sorted(reverse.get(base, ())):
                join(dependent, f"{base} went major")
    for generator, consumers in sorted((generators or {}).items()):
        caused = highest(level[c] for c in consumers if c in level)
        caused = MINOR if caused == NEW else caused  # a generator that exists is raised by an addition as by a minor
        if generator not in level:
            level[generator], reasons[generator] = PATCH, ["its source changed"]
            order.append(generator)
        if RANK[caused] > RANK[level[generator]]:
            level[generator] = caused
        reasons[generator].append(f"the consumers it caused are at most {caused}")
    plan = {p: _row(p, level[p], reasons[p], last_stable, first_versions) for p in order}
    _check_declarations(plan, last_stable, first_versions)
    return plan


def _row(package, level, reasons, last_stable, first_versions):
    if package in last_stable:
        if package in first_versions:
            raise LevelPlanError("FIRST_VERSION_FOR_EXISTING_PACKAGE",
                                 f"first-versions.json declares {package}, but it already has stable version {last_stable[package]}; remove the entry")
        return {"last": last_stable[package], "level": level, "next": next_version(last_stable[package], level), "reasons": reasons}
    if package not in first_versions:
        raise LevelPlanError("FIRST_VERSION_NOT_DECLARED",
                             f"{package} has no stable version to raise; add \"{package}\": \"x.y.z\" under firstVersions in first-versions.json "
                             "(for a rename or a split: {\"version\": \"x.y.z\", \"replaces\": [\"Old.Id\"]})")
    entry = first_versions[package]
    note = f"first version declared in first-versions.json" + (f"; replaces {', '.join(entry['replaces'])}" if entry["replaces"] else "")
    return {"last": None, "level": NEW, "next": entry["version"], "reasons": reasons + [note], "replaces": entry["replaces"]}


def _check_declarations(plan, last_stable, first_versions):
    for package in sorted(first_versions):
        if package not in plan:
            raise LevelPlanError("FIRST_VERSION_NOT_IN_RELEASE", f"first-versions.json declares {package}, which is not in this release; fix the id or remove the entry")
    for old in replaced_packages(first_versions):
        if old not in last_stable:
            raise LevelPlanError("REPLACED_PACKAGE_UNKNOWN", f"first-versions.json says a package replaces {old}, which has no stable version on the feed")
        if old in plan:
            raise LevelPlanError("REPLACED_PACKAGE_STILL_CHANGING", f"{old} is replaced by a declared package and is also changing in this release")


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["retire"])
    ap.add_argument("--plan", required=True, help="the plan that was published: release-work/levels.json (its 'packages'), or JSON of {package: row}")
    ap.add_argument("--first-versions", required=True)
    a = ap.parse_args(argv)
    try:
        with open(a.plan, encoding="utf-8") as f:
            plan = json.load(f)
        if isinstance(plan, dict) and isinstance(plan.get("packages"), dict) and "report" in plan:
            plan = plan["packages"]  # levels.json, as release.py decide writes it
        print("\n".join(retire_entries(a.first_versions, plan)) or "no new package in this release; nothing to remove")
    except (LevelPlanError, OSError, ValueError) as e:
        sys.exit(f"FAIL: {e}")


if __name__ == "__main__":
    main()

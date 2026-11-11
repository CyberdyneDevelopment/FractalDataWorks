#!/usr/bin/env python3
"""Decide every package's level from its surface, and print the report (FDW-4784).

decide() is the loop of the plan, with the two things that need a build or a feed handed in, so it can be tested with fakes:

  old_surface(package)  the public surface text of the package's last stable version, or None when it has none (a new package)
  new_surface(package)  the public surface text of the package as built now (the caller builds it, once, when it is first asked)

A generator (a key of `generators`) is never compared: it has no public surface of its own, so no surface is asked for it.
For each other package that joins the release, level_of() compares the two texts with apisurface.compare() and keeps the reasons. A
package that joins because a package it references went major is asked the same way: its own surface decides its level, patch when
nothing in it changed. levelplan.plan_levels() does the graph: dependents of a major, the generator rule, first versions.

report() turns the result into the table the dry run prints: package, last stable, level, next version, and the first reason.
"""
import apisurface
import levelplan


def decide(depends, members, old_surface, new_surface, last_stable, first_versions, settings, generators=None, cap=None):
    """(plan, reasons): the plan of levelplan.plan_levels, and {package: the apisurface reasons for its own change}.

    `cap` (patch, minor or major; None = no cap) is the highest level any package may take in this release: a package whose surface says a
    higher one takes the cap, and its row keeps what the surface said (capped_from). It is a decision of the release's owner, stated as a
    setting; the comparison itself is unchanged, so the breaking changes are still found and listed."""
    if cap is not None and cap not in (levelplan.PATCH, levelplan.MINOR, levelplan.MAJOR):
        raise levelplan.LevelPlanError("LEVEL_CAP_INVALID", f"the level cap is {cap!r}; it must be patch, minor or major")
    reasons, capped = {}, {}

    def level_of(package):
        if package in (generators or {}):
            # A generator has no public surface of its own to compare (what it ships is the code it emits into its consumers), so
            # its own change is a patch; levelplan raises it to the highest level among the consumers it caused.
            reasons[package] = []
            return levelplan.PATCH
        old, new = old_surface(package), new_surface(package)
        if old is None:
            raise levelplan.LevelPlanError("OLD_SURFACE_MISSING", f"{package} has a last stable version but no surface was given for it")
        result = apisurface.compare(old, new, settings)
        reasons[package] = result["reasons"]
        if cap is not None and levelplan.RANK[result["level"]] > levelplan.RANK[cap]:
            capped[package] = result["level"]
            return cap
        return result["level"]

    plan = levelplan.plan_levels(depends, members, level_of, last_stable, first_versions, generators)
    for package, original in capped.items():
        if package in plan:
            plan[package]["capped_from"] = original
            plan[package]["reasons"].append(f"its surface says {original}; capped at {cap} by the release's level cap")
    return plan, reasons


def first_reason(plan_row, surface_reasons):
    if plan_row["level"] == levelplan.NEW:
        return plan_row["reasons"][-1]
    if surface_reasons:
        return apisurface.describe(surface_reasons[0])
    return "; ".join(plan_row["reasons"])


def report(plan, reasons):
    """Lines of the report, most severe level first, then by package id."""
    order = {"major": 0, "minor": 1, "new": 2, "patch": 3}
    lines = [f"{'PACKAGE':40s} {'LAST':12s} {'LEVEL':6s} {'NEXT':12s} WHY"]
    for package in sorted(plan, key=lambda p: (order[plan[p]["level"]], p.lower())):
        row = plan[package]
        mark = f" [surface: {row['capped_from']}, capped at {row['level']}]" if row.get("capped_from") else ""
        lines.append(f"{package:40s} {row['last'] or '-':12s} {row['level']:6s} {row['next']:12s} {first_reason(row, reasons.get(package, []))}{mark}")
    counts = {name: sum(1 for r in plan.values() if r["level"] == name) for name in order}
    lines.append(f"{len(plan)} package(s): " + ", ".join(f"{n} {name}" for name, n in counts.items() if n))
    return lines

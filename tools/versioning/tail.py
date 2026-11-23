#!/usr/bin/env python3
"""The version a build gets on each tier, and the range a tier restores. One rule, written once, for the pipeline to call.

  tail.py version --branch B --number N --date YYYYMMDD --count C
  tail.py version --tier branch --line 1.4 --date YYYYMMDD --count C --sha S
  tail.py range --line 1.4 (--tier T | --env)

N is the release number from allocate.py (plain x.y.z). The tail names the tier, so internal and develop can share one
feed and still never be mistaken for each other:

  internal      N-internal.<yyyyMMdd>.<commit count>
  develop       N-develop.<yyyyMMdd>.<commit count>
  prerelease/*  N-rc.<k>                      (k = --count, the release candidate number)
  stable        N                             (no tail; sorts above every tail)
  branch        L.0-branch.<yyyyMMdd>.<commit count>.g<sha>   (a pack of an unmerged leg, asked for with --tier branch)

A branch pack takes the line L (major.minor) and builds its core as L.0: it spends no release number, so it needs no
look at nuget.org (allocate.py reads it from there; a leg pack must be makeable when the feed is out of reach, and a
lookup that fails stays a loud failure on the line's own path). L.0 is the lowest core in the line, so a branch pack
sorts below every pack of its line whatever release number the line has reached.

A leg has more commits than its base, so a leg pack under the internal label would outrank its line's own pack and be
taken by every consumer floating the line (FDW-6638). The branch label stays out of that float (a restore on
`<line>.*-internal.*` takes only a label that starts with `internal.`) and sorts below `internal` (NuGet compares labels
case-insensitively by ordinal, and b < i; `leg` would sort above it), so where nothing matches the float the line's own
pack still wins. The sha keeps two legs of one day and one commit count from sharing a version, which a push with
--skip-duplicate would otherwise swallow. A branch pack is only ever asked for by name; no git branch maps to it.
Verified with NuGet.Versioning (the .NET 10 SDK's own assembly): FloatRange.Satisfies and VersionComparer.

The commit count is `git rev-list --count HEAD`: it only grows along a lineage, which minver's height does not
(FDW-1035). A tier restores `<line>.*-<tier>.*`; the patch must stay a wildcard, because `1.4.0-internal.*` picks the
stable version (build-agent, 2026-10-30). The tier is never in a tracked file; --env reads FDW_TIER and fails when it
is unset or not a tier. There is no default.
"""
import argparse
import os
import re
import sys

TIERS = ("internal", "develop")
RELEASE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$")
LINE = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)$")
DATE = re.compile(r"^\d{8}$")
SHA = re.compile(r"^[0-9a-f]{7,40}$")
BRANCH_TIER = "branch"
COUNT = re.compile(r"^[1-9]\d*$")
ENV_NAME = "FDW_TIER"


class TailError(Exception):
    pass


def _require(pattern, value, what):
    if not pattern.match(str(value)):
        raise TailError(f"{what} {value!r} is not valid")
    return str(value)


def tier_for_branch(branch):
    """The tier a branch publishes as: 'internal', 'develop', 'rc' (prerelease/*) or 'stable' (main)."""
    if branch in TIERS:
        return branch
    if branch == "main":
        return "stable"
    if branch == "prerelease" or branch.startswith("prerelease/"):
        return "rc"
    raise TailError(f"branch {branch!r} publishes to no tier (internal, develop, prerelease/*, main)")


def version(branch, number, date=None, count=None):
    number = _require(RELEASE, number, "release number")
    tier = tier_for_branch(branch)
    if tier == "stable":
        return number
    count = _require(COUNT, count, "commit count" if tier != "rc" else "release candidate number")
    if tier == "rc":
        return f"{number}-rc.{count}"
    return f"{number}-{tier}.{_require(DATE, date, 'date')}.{count}"


def branch_version(line, date=None, count=None, sha=None):
    """The version of a pack built from an unmerged leg: L.0-branch.<date>.<count>.g<sha>, L the line (major.minor)."""
    line = _require(LINE, line, "line")
    date = _require(DATE, date, "date")
    count = _require(COUNT, count, "commit count")
    return f"{line}.0-{BRANCH_TIER}.{date}.{count}.g{_require(SHA, sha, 'commit sha')}"


def tier_from_env(env=None):
    env = os.environ if env is None else env
    value = env.get(ENV_NAME)
    if not value:
        raise TailError(f"{ENV_NAME} is not set: the tier cannot be assumed. Set it to one of {', '.join(TIERS)}.")
    if value not in TIERS:
        raise TailError(f"{ENV_NAME} is {value!r}, which is not a tier ({', '.join(TIERS)})")
    return value


def float_range(line, tier):
    _require(LINE, line, "line")
    if tier not in TIERS:
        raise TailError(f"{tier!r} is not a tier ({', '.join(TIERS)})")
    return f"{line}.*-{tier}.*"


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    v = sub.add_parser("version")
    v.add_argument("--branch")
    v.add_argument("--tier", help="only 'branch': the pack of an unmerged leg, asked for in place of --branch")
    v.add_argument("--sha", help="with --tier branch: the commit the pack is built from")
    v.add_argument("--number", help="with --branch: the release number from allocate.py")
    v.add_argument("--line", help="with --tier branch: the release line, major.minor")
    v.add_argument("--date")
    v.add_argument("--count")
    r = sub.add_parser("range")
    r.add_argument("--line", required=True)
    r.add_argument("--tier")
    r.add_argument("--env", action="store_true")
    a = ap.parse_args(argv)
    try:
        if a.cmd == "version":
            if bool(a.branch) == bool(a.tier):
                ap.error("give exactly one of --branch and --tier")
            if a.tier is None:
                if a.sha is not None or a.line is not None:
                    ap.error("--sha and --line go with --tier branch")
                if a.number is None:
                    ap.error("--number is required with --branch")
                print(version(a.branch, a.number, a.date, a.count))
            else:
                if a.tier != BRANCH_TIER:
                    ap.error(f"--tier takes only {BRANCH_TIER!r}")
                if a.number is not None:
                    ap.error("a branch pack takes --line, not --number: it spends no release number")
                print(branch_version(a.line, a.date, a.count, a.sha))
        else:
            if bool(a.tier) == a.env:
                ap.error("give exactly one of --tier and --env")
            print(float_range(a.line, a.tier or tier_from_env()))
    except TailError as e:
        sys.exit(f"FAIL: {e}")


if __name__ == "__main__":
    main()

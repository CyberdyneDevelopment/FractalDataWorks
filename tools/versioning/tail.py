#!/usr/bin/env python3
"""The version a build gets on each tier, and the range a tier restores. One rule, written once, for the pipeline to call.

  tail.py version --branch B --number N --date YYYYMMDD --count C
  tail.py range --line 1.4 (--tier T | --env)

N is the release number from allocate.py (plain x.y.z). The tail names the tier, so internal and develop can share one
feed and still never be mistaken for each other:

  internal      N-internal.<yyyyMMdd>.<commit count>
  develop       N-develop.<yyyyMMdd>.<commit count>
  prerelease/*  N-rc.<k>                      (k = --count, the release candidate number)
  stable        N                             (no tail; sorts above every tail)

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
    v.add_argument("--branch", required=True)
    v.add_argument("--number", required=True)
    v.add_argument("--date")
    v.add_argument("--count")
    r = sub.add_parser("range")
    r.add_argument("--line", required=True)
    r.add_argument("--tier")
    r.add_argument("--env", action="store_true")
    a = ap.parse_args(argv)
    try:
        if a.cmd == "version":
            print(version(a.branch, a.number, a.date, a.count))
        else:
            if bool(a.tier) == a.env:
                ap.error("give exactly one of --tier and --env")
            print(float_range(a.line, a.tier or tier_from_env()))
    except TailError as e:
        sys.exit(f"FAIL: {e}")


if __name__ == "__main__":
    main()

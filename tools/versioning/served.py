#!/usr/bin/env python3
"""Wait until nuget.org serves what a release pushed (FDW-5634).

  served.py wait --timeout-minutes N --interval-seconds S --looks K [--manifest FILE] [--package ID VERSION]...

A push is accepted long before a restore can take the package: nuget.org validates and indexes in a queue, and 245 packages took about
twenty minutes (1.5.7), 162 about twenty-five (1.5.8). This waits, so that what comes after it (the release's record and its tag) can rely on
the packages being there. It waits for every package of the release manifest, whatever the run pushed.

A version is served when the flat container's index lists it and its package downloads (feeds.NugetOrg.serves). nuget.org answers from
several edges that are not in step: a package seen served can be missing from the next answer (1.5.8: Fdw.Data.Transformations.Boolean). So
the wait ends only after K consecutive looks that find everything served (--looks); a look that finds one missing starts the count again. The
timeout, the interval and K are given by the caller and have no default here: the release job reads them from its settings, which declare
the defaults. Every line is flushed at once: a step that is cancelled or killed must not take its progress with it (FDW-5708).
At the timeout the exit status is 1 and the ids not served are named, in full: the release is half done, so it is rerun and not tagged.
"""
import argparse
import json
import sys
import time
from concurrent.futures import ThreadPoolExecutor

from feeds import NugetOrg


class ServedError(Exception):
    """The wait was asked for nothing, or with limits that cannot work."""


def unserved(feed, wanted, workers=16):
    """The (id, version) pairs of `wanted` the feed does not serve yet, in the order given."""
    wanted = list(wanted)
    with ThreadPoolExecutor(max_workers=workers) as pool:
        answers = list(pool.map(lambda pair: feed.serves(pair[0], pair[1]), wanted))
    return [pair for pair, served in zip(wanted, answers) if not served]


def say_now(line):
    """Print one progress line and flush it."""
    print(line, flush=True)


def wait(feed, wanted, timeout_seconds, interval_seconds, looks, clock=time.monotonic, sleep=time.sleep, say=say_now):
    """Looks until every (id, version) of `wanted` was found served by `looks` consecutive looks, or `timeout_seconds` have passed; returns the
    ones not served at the last look (empty on success). While something is missing only what is missing is asked again; once everything is
    served the whole list is asked again for each confirming look. The first look is made at once. Raises ServedError when the time ran out
    between two complete looks (everything served, but not for `looks` looks in a row)."""
    wanted = list(wanted)
    if not wanted:
        raise ServedError("nothing to wait for: no package was named")
    if timeout_seconds < 1 or interval_seconds < 1 or looks < 1:
        raise ServedError(f"the timeout ({timeout_seconds} s), the interval ({interval_seconds} s) and the number of looks ({looks}) must each be at least 1")
    deadline = clock() + timeout_seconds
    pending, complete = unserved(feed, wanted), 0
    while True:
        if not pending:
            complete += 1
            if complete >= looks:
                return []
            say(f"served: all {len(wanted)} served at look {complete} of {looks}; looking again in {interval_seconds} s")
            if clock() >= deadline:
                break
            sleep(interval_seconds)
            pending = unserved(feed, wanted)
            continue
        complete = 0
        if clock() >= deadline:
            return pending
        first = ", ".join(f"{pid} {version}" for pid, version in pending[:5]) + (f" and {len(pending) - 5} more" if len(pending) > 5 else "")
        say(f"served: {len(wanted) - len(pending)} of {len(wanted)} served by nuget.org; waiting {interval_seconds} s for {len(pending)} more: {first}")
        sleep(interval_seconds)
        pending = unserved(feed, pending)
        if not pending:
            pending = unserved(feed, wanted)   # the first complete look is a look of the whole list, not of what was left
    raise ServedError(f"every package was served at the last look but not for {looks} consecutive looks before the timeout; the answers of nuget.org are not steady")


def read_manifest(path):
    with open(path, encoding="utf-8") as f:
        return sorted(json.load(f)["packages"].items(), key=lambda kv: kv[0].lower())


def main(argv=None, feed=None, clock=time.monotonic, sleep=time.sleep, say=say_now):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    w = sub.add_parser("wait")
    w.add_argument("--timeout-minutes", type=int, required=True)
    w.add_argument("--interval-seconds", type=int, required=True)
    w.add_argument("--looks", type=int, required=True, help="consecutive looks that must find everything served (no default)")
    w.add_argument("--manifest", help="the release manifest: every package in it, at the version it names")
    w.add_argument("--package", nargs=2, action="append", default=[], metavar=("ID", "VERSION"))
    a = ap.parse_args(argv)
    wanted = (read_manifest(a.manifest) if a.manifest else []) + [tuple(p) for p in a.package]
    started = clock()
    try:
        pending = wait(feed or NugetOrg(), wanted, a.timeout_minutes * 60, a.interval_seconds, a.looks, clock=clock, sleep=sleep, say=say)
    except ServedError as e:
        sys.exit(f"FAIL: {e}")
    if pending:
        names = ", ".join(f"{pid} {version}" for pid, version in pending)
        sys.exit(f"FAIL: nuget.org does not serve {len(pending)} of {len(wanted)} within {a.timeout_minutes} minutes: {names}. What was pushed stays "
                 "on nuget.org: the release is half done. Rerun the workflow (it skips what is there and carries on); do NOT tag.")
    say(f"served: all {len(wanted)} are served by nuget.org after {int(clock() - started)} s.")


if __name__ == "__main__":
    main()

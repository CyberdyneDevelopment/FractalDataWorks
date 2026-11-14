#!/usr/bin/env python3
"""Wait until nuget.org serves what a release pushed (FDW-5634).

  served.py wait --timeout-minutes N --interval-seconds S [--manifest FILE] [--package ID VERSION]...

A push is accepted long before a restore can take the package: nuget.org validates and indexes in a queue, and 245 packages took about
twenty minutes (1.5.7). The bundle's restore ran seconds after the push and failed NU1102 on every pin. This waits, so that the step after
it can rely on the packages being there. It is used twice: before the bundle is packed (every package of the release manifest) and after
the bundle is pushed (the bundle itself). Neither depends on how many packages the run pushed.

A version is served when the flat container's index lists it and its package downloads (feeds.NugetOrg.serves). The timeout and the
interval are given by the caller and have no default here: the release job reads them from its settings, which declare the defaults.
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


def wait(feed, wanted, timeout_seconds, interval_seconds, clock=time.monotonic, sleep=time.sleep, say=print):
    """Polls until every (id, version) of `wanted` is served or `timeout_seconds` have passed; returns the ones still not served (empty on
    success). Only what is not served yet is asked again. The first look is made at once, so something already served costs no wait."""
    wanted = list(wanted)
    if not wanted:
        raise ServedError("nothing to wait for: no package was named")
    if timeout_seconds < 1 or interval_seconds < 1:
        raise ServedError(f"the timeout ({timeout_seconds} s) and the interval ({interval_seconds} s) must each be at least 1 second")
    deadline = clock() + timeout_seconds
    pending = unserved(feed, wanted)
    while pending and clock() < deadline:
        say(f"served: {len(wanted) - len(pending)} of {len(wanted)} served by nuget.org; waiting {interval_seconds} s for {len(pending)} more")
        sleep(interval_seconds)
        pending = unserved(feed, pending)
    return pending


def read_manifest(path):
    with open(path, encoding="utf-8") as f:
        return sorted(json.load(f)["packages"].items(), key=lambda kv: kv[0].lower())


def main(argv=None, feed=None, clock=time.monotonic, sleep=time.sleep, say=print):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    w = sub.add_parser("wait")
    w.add_argument("--timeout-minutes", type=int, required=True)
    w.add_argument("--interval-seconds", type=int, required=True)
    w.add_argument("--manifest", help="the release manifest: every package in it, at the version it names")
    w.add_argument("--package", nargs=2, action="append", default=[], metavar=("ID", "VERSION"))
    a = ap.parse_args(argv)
    wanted = (read_manifest(a.manifest) if a.manifest else []) + [tuple(p) for p in a.package]
    started = clock()
    try:
        pending = wait(feed or NugetOrg(), wanted, a.timeout_minutes * 60, a.interval_seconds, clock=clock, sleep=sleep, say=say)
    except ServedError as e:
        sys.exit(f"FAIL: {e}")
    if pending:
        names = ", ".join(f"{pid} {version}" for pid, version in pending)
        sys.exit(f"FAIL: nuget.org does not serve {len(pending)} of {len(wanted)} within {a.timeout_minutes} minutes: {names}. What was pushed stays "
                 "on nuget.org: the release is half done. Rerun the workflow (it skips what is there and carries on); do NOT tag.")
    say(f"served: all {len(wanted)} are served by nuget.org after {int(clock() - started)} s.")


if __name__ == "__main__":
    main()

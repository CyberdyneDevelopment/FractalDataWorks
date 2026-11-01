#!/usr/bin/env python3
"""Decide, for every built package, whether to push it, skip it or stop, before anything is pushed.

  pushcheck.py check --packages DIR [--json]      (exit status 1 if any package is a stop)

For each .nupkg in DIR the tool reads its id, version and the tree id it was built from (the marker file inside the
package), and asks nuget.org what it holds:
  - the version is not on the feed:           push
  - on the feed with the same tree id:        skip, it is already there
  - on the feed with a different tree id, or with no marker: stop. Pushing would be refused, and skipping would ship
    the old contents under a new build's name. `--skip-duplicate` does exactly that and reports green.
If any package is a stop, nothing is pushed: the release stops with the numbers it has not spent.
"""
import argparse
import glob
import json
import os
import sys
from dataclasses import dataclass

from feeds import NugetOrg, identity_from_nupkg, tree_from_nupkg


@dataclass
class Decision:
    package: str
    version: str
    action: str   # push | skip | stop
    reason: str
    path: str = ""


def decide(built, feed):
    """built: iterable of (package id, version, tree id, path). Returns one Decision per package."""
    out = []
    for pid, version, tree, path in built:
        spelled = {v.lower(): v for v in feed.versions(pid)}
        if tree is None:
            out.append(Decision(pid, version, "stop", "the package has no tree marker, so it cannot be compared with a held version", path))
        elif version.lower() not in spelled:
            out.append(Decision(pid, version, "push", "not on the feed", path))
        else:
            held = feed.held_tree(pid, spelled[version.lower()])
            if held == tree:
                out.append(Decision(pid, version, "skip", "already on the feed with the same contents", path))
            elif held is None:
                out.append(Decision(pid, version, "stop", "on the feed with no tree marker, so it cannot be shown to be the same", path))
            else:
                out.append(Decision(pid, version, "stop", f"on the feed built from tree {held}, but this build is from tree {tree}", path))
    return out


def to_push(decisions):
    """The decisions to act on: all the pushes, or none at all when any package is a stop."""
    return [] if any(d.action == "stop" for d in decisions) else [d for d in decisions if d.action == "push"]


def read_directory(directory):
    built = []
    for path in sorted(glob.glob(os.path.join(directory, "*.nupkg"))):
        if path.endswith(".symbols.nupkg"):
            continue
        with open(path, "rb") as f:
            data = f.read()
        pid, version = identity_from_nupkg(data)
        built.append((pid, version, tree_from_nupkg(data), path))
    return built


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["check"])
    ap.add_argument("--packages", required=True)
    ap.add_argument("--json", action="store_true")
    a = ap.parse_args(argv)
    decisions = decide(read_directory(a.packages), NugetOrg())
    pushes = to_push(decisions)
    if a.json:
        print(json.dumps({"decisions": [d.__dict__ for d in decisions], "push": [d.path for d in pushes]}, indent=1))
    else:
        for d in decisions:
            print(f"  {d.action:5s} {d.package} {d.version}: {d.reason}")
        stops = [d for d in decisions if d.action == "stop"]
        print(f"{len(pushes)} to push, {len(decisions) - len(stops) - len(pushes)} skipped, {len(stops)} stop"
              + (" -- nothing will be pushed" if stops else ""))
    sys.exit(1 if any(d.action == "stop" for d in decisions) else 0)


if __name__ == "__main__":
    main()

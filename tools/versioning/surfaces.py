#!/usr/bin/env python3
"""Which public surfaces the release job has to make, and the last stable package to make an old one from (FDW-4784).

  surfaces.py needed --plan-dir DIR [--package ID --side old|new]
  surfaces.py fetch  --package-id ID --version V --out FILE

`needed` prints one TAB-separated line per surface to make from the files `release.py plan` wrote:

  old <TAB> <package id> <TAB> <last stable version>      dump the surface of that package from nuget.org
  new <TAB> <package id> <TAB> <project file>             pack that project and dump the surface of the package

for every package whose own source changed, except a generator: a generator has no public surface of its own, so none is asked for
(levelrun.decide does not ask either). A package with no last stable version has no old line. With --package and --side only that
one line is printed, which is how the job answers `release.py decide --missing-out` for a package that joined after the first pass.
A package the plan does not know, or one that has no project file recorded, stops the run: nothing is assumed.

`fetch` downloads one held package from nuget.org to FILE and stops, naming it, when nuget.org does not hold it.
"""
import argparse
import json
import os
import sys

from feeds import NugetOrg


class SurfacesError(Exception):
    pass


def needed(info, package=None, side=None):
    """[(side, package id, version or project file)] in a stable order."""
    generators = set(info["generators"])
    members = [p for p in info["members"] if p not in generators]
    if package is not None:
        if package in generators:
            raise SurfacesError(f"{package} is a generator and has no surface of its own; nothing asks for one")
        if package not in info["depends"]:
            raise SurfacesError(f"{package} is not a packable package of this release plan")
        members = [package]
    out = []
    for p in members:
        if side in (None, "old") and p in info["last_stable"]:
            out.append(("old", p, info["last_stable"][p]))
        if side in (None, "new"):
            if p not in info["paths"]:
                raise SurfacesError(f"no project file is recorded for {p}")
            out.append(("new", p, info["paths"][p]))
    return out


def fetch(feed, package_id, version, out):
    data = feed.download(package_id, version)
    if data is None:
        raise SurfacesError(f"nuget.org does not hold {package_id} {version}, so there is no old surface to compare with")
    with open(out, "wb") as f:
        f.write(data)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    n = sub.add_parser("needed")
    n.add_argument("--plan-dir", required=True)
    n.add_argument("--package")
    n.add_argument("--side", choices=["old", "new"])
    f = sub.add_parser("fetch")
    f.add_argument("--package-id", required=True)
    f.add_argument("--version", required=True)
    f.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    try:
        if a.cmd == "fetch":
            fetch(NugetOrg(), a.package_id, a.version, a.out)
            return
        if bool(a.package) != bool(a.side):
            ap.error("--package and --side go together")
        with open(os.path.join(a.plan_dir, "levels-input.json"), encoding="utf-8") as fh:
            info = json.load(fh)
        for line in needed(info, a.package, a.side):
            print("\t".join(line))
    except (SurfacesError, OSError, ValueError) as e:
        sys.exit(f"FAIL: {e}")


if __name__ == "__main__":
    main()

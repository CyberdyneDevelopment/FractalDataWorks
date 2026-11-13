#!/usr/bin/env python3
"""Check that packages which are not rebuilt can still bind to the dependencies that are (FDW-4784, FDW-5272).

  bindcheck.py check --new-dir DIR --held-dir DIR --floors FILE
  bindcheck.py floors --dir DIR [--dir DIR ...]
  bindcheck.py bare --dir DIR [--dir DIR ...]

A package that is not rebuilt keeps its dll, which asks for each dependency at the assembly version it was built against, and the
runtime will not bind a lower one. So with per-package releases a rebuilt dependency must never carry a lower assembly version than
any dll that may stay as it is. assemblyid.py writes, for a package, one DEF record per assembly (its name and version) and
one REF record per assembly it references (the version it asks for). In DIR files of those records:

  --new-dir   the packages built now: their DEF records are what the held dlls will bind to
  --held-dir  the packages that stay as they are (the release checks every package of the last release): their REF records are the demands

The floors are not a setting: each assembly has one, recorded in the baseline release's manifest (assemblyVersionFloors, {assembly: version}),
which `floors` computes from the identity files of a release's packages: for each assembly the higher of the version it carries and the
highest version any assembly of the release asks for it at (references to the framework and to other libraries are left out). The release
that is built records its floors for the next one the same way, in the project manifest the bundle carries. A floor per assembly means a
package that goes to 2.0.0 never raises the floor of a 1.x one.

It stops on any of:
  - a built assembly whose version is below its own floor (an assembly with no floor is a new one and has none);
  - a held dll that references a built assembly at a version above the built one (the lowest version found, when the package ships the
    assembly for several frameworks).
Nothing is assumed: a record that cannot be read, or an empty directory, stops the run.
"""
import argparse
import json
import os
import re
import sys

FLOORS_KEY = "assemblyVersionFloors"
BARE_KEY = "packagesWithoutRuntimeAssembly"
ASSEMBLY_VERSION = re.compile(r"^\d+\.\d+\.\d+\.\d+$")


class BindError(Exception):
    pass


def version_tuple(text):
    try:
        parts = tuple(int(p) for p in text.split("."))
    except ValueError:
        raise BindError(f"{text!r} is not an assembly version")
    if len(parts) != 4:
        raise BindError(f"{text!r} is not an assembly version (four numbers)")
    return parts


def parse(text, origin):
    """([(assembly, version)], [(from, to, version)]) from the records of one file."""
    defs, refs = [], []
    for number, line in enumerate(text.splitlines(), 1):
        if not line.strip():
            continue
        fields = line.split("\t")
        if len(fields) != 4 or fields[1] not in ("DEF", "REF"):
            raise BindError(f"{origin}:{number}: not an identity record: {line!r}")
        if fields[1] == "DEF":
            defs.append((fields[2], version_tuple(fields[3])))
        else:
            source, arrow, target = fields[2].partition("->")
            if not arrow:
                raise BindError(f"{origin}:{number}: a REF record names 'from->to', got {fields[2]!r}")
            refs.append((source, target, version_tuple(fields[3])))
    return defs, refs


def check(new_texts, held_texts, floors):
    """The problems, as sentences. `new_texts` and `held_texts` are {origin: file text}; `floors` is {assembly: x.y.z.w}."""
    floor_v = {name: version_tuple(check_floor(v)) for name, v in floors.items()}
    built = {}
    for origin, text in new_texts.items():
        for name, version in parse(text, origin)[0]:
            built[name] = min(built.get(name, version), version)
    problems = [f"{name} is built at {'.'.join(map(str, v))}, below its floor {'.'.join(map(str, floor_v[name]))}" for name, v in sorted(built.items())
                if name in floor_v and v < floor_v[name]]
    seen = set()
    for origin, text in sorted(held_texts.items()):
        for source, target, version in parse(text, origin)[1]:
            if target in built and version > built[target] and (source, target) not in seen:
                seen.add((source, target))
                problems.append(f"{source} (as it is) asks for {target} {'.'.join(map(str, version))}, but the rebuilt one is {'.'.join(map(str, built[target]))}")
    return problems


def check_floor(text):
    """The floor as given, or an error: it must be x.y.z.w."""
    if not isinstance(text, str) or not ASSEMBLY_VERSION.match(text):
        raise BindError(f"the assembly version floor {text!r} is not x.y.z.w; there is no default")
    return text


def floors(texts):
    """{assembly: floor} for the release whose identity files these are: for each assembly it defines, the higher of the version it carries
    and the highest version any assembly of the release asks for it at. References to the framework and to other libraries do not count."""
    defs, refs = {}, []
    for origin, text in texts.items():
        found_defs, found_refs = parse(text, origin)
        for name, version in found_defs:
            defs[name] = max(defs.get(name, version), version)
        refs += found_refs
    if not defs:
        raise BindError("no assembly definitions in the identity files, so there are no floors to record")
    for _, target, version in refs:
        if target in defs:
            defs[target] = max(defs[target], version)
    return {name: ".".join(str(n) for n in v) for name, v in sorted(defs.items())}


def bare(texts):
    """The packages (identity file names without .txt, which are package ids) that carry no runtime assembly, such as a source generator
    or a tool: nothing binds to them at run time, so they have no floor. A release records them so that a missing floor is otherwise an error."""
    return sorted(os.path.basename(origin)[:-len(".txt")] for origin, text in texts.items() if not parse(text, origin)[0])


def read_dir(path):
    if not os.path.isdir(path):
        raise BindError(f"{path} is not a directory")
    files = sorted(f for f in os.listdir(path) if f.endswith(".txt"))
    if not files:
        raise BindError(f"{path} holds no identity files")
    out = {}
    for name in files:
        with open(os.path.join(path, name), encoding="utf-8") as f:
            out[name] = f.read()
    return out


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["check", "floors", "bare"])
    ap.add_argument("--new-dir")
    ap.add_argument("--held-dir")
    ap.add_argument("--floors", help="a JSON file of {assembly: x.y.z.w}")
    ap.add_argument("--dir", action="append")
    a = ap.parse_args(argv)
    if a.cmd == "bare":
        if not a.dir:
            ap.error("bare needs at least one --dir")
        try:
            by_package = {}
            for d in a.dir:  # a later directory replaces an earlier one's file of the same name: the newest build of a package wins
                for name, text in read_dir(d).items():
                    by_package[name] = (os.path.join(d, name), text)
            print(json.dumps(bare({path: text for path, text in by_package.values()}), indent=1))
        except BindError as e:
            sys.exit(f"FAIL: {e}")
        return
    if a.cmd == "floors":
        if not a.dir:
            ap.error("floors needs at least one --dir")
        try:
            texts = {}
            for d in a.dir:
                texts |= {os.path.join(d, name): text for name, text in read_dir(d).items()}
            print(json.dumps(floors(texts), indent=1, sort_keys=True))
        except BindError as e:
            sys.exit(f"FAIL: {e}")
        return
    if not (a.new_dir and a.held_dir and a.floors):
        ap.error("check needs --new-dir, --held-dir and --floors")
    try:
        with open(a.floors, encoding="utf-8") as f:
            floor_map = json.load(f)
        if not isinstance(floor_map, dict):
            raise BindError(f"{a.floors} is not a JSON object of {{assembly: version}}")
        new, held = read_dir(a.new_dir), read_dir(a.held_dir)
        problems = check(new, held, floor_map)
    except (BindError, OSError, ValueError) as e:
        sys.exit(f"FAIL: {e}")
    if problems:
        print("\n".join(f"  {p}" for p in problems[:50]) + (f"\n  ... and {len(problems) - 50} more" if len(problems) > 50 else ""))
        sys.exit(f"FAIL: {len(problems)} binding problem(s): a package left as it is would not load with the rebuilt one.")
    print(f"bindings ok: {len(held)} held package(s) bind to {len(new)} built package(s), {len(floor_map)} floor(s)")


if __name__ == "__main__":
    main()

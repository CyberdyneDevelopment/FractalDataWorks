#!/usr/bin/env python3
"""Keep a dry run's results so the real allocated release can reuse them (FDW-5175).

A release is two workflow runs on one commit. The dry run does the slow work once (levels, build and pack, bindings, decide) and
keeps the result as one artifact. The real run plans again, checks that the artifact belongs to this exact release, and takes the
packed packages and the decided files from it; it never rebuilds. When it cannot prove the artifact belongs, it stops and says why.

  dryrun.py key     --out FILE --sha S --branch B --line L --bump U --bundle-id I --baseline-file F --max-level M
                    --levels-max-rounds R --run-tests T
        write the key: the commit and every input the release was decided from. Empty values are allowed and are written as they are.
  dryrun.py package --key FILE --work-dir W --artifacts A --out DIR
        stage the dry run's results in DIR: the key, the decided files of W, and the packages (nupkg and snupkg) of A.
  dryrun.py verify  --dir DIR --expect FILE --work-dir W
        DIR is the downloaded artifact. FILE is the key the real run has just written. Every key field must be equal, and the
        planned files kept in DIR (plan, versions, trees, manifest) must equal the plan W holds now: the plan is made from the
        baseline, the feed, the tree and the bump, so equal plans are the same release number, members and candidate number.
        A feed that moved since the dry run makes them differ. Exit 1 and say what differs when they do.
  dryrun.py restore --dir DIR --work-dir W --artifacts A
        put the decided files into W and the packages into A (emptied first). Run verify first.

Nothing is assumed: a file that is missing from the artifact, or a field missing from a key, stops the run and names it.
"""
import argparse
import json
import os
import shutil
import sys

KEY_FILE = "dryrun-key.json"
KEY_FIELDS = ("sha", "branch", "line", "bump", "bundle_id", "baseline_file", "max_level", "levels_max_rounds", "run_tests")
# The decided files the later phases read. plan, versions, trees and manifest are the decided ones (levels replaced the planned ones).
WORK_FILES = ("plan.json", "versions.json", "trees.json", "manifest.json", "package-versions.json", "assembly-versions.json", "levels.json",
              "levels-input.json", "project-manifest.json", "floors.json", "next-floors.json", "next-bare.json", "levels-outputs.env")
# What the plan made before levels decided. The real run's own plan must equal these.
PLANNED_FILES = ("plan.json", "versions.json", "trees.json", "manifest.json")
PLANNED_DIR = "planned"
WORK_IN_ARTIFACT = "work"
PACKAGES_IN_ARTIFACT = "artifacts"


class DryRunError(Exception):
    pass


def write_key(path, values):
    missing = [f for f in KEY_FIELDS if f not in values]
    if missing:
        raise DryRunError(f"no value for key fields: {missing}")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump({k: str(values[k]) for k in KEY_FIELDS}, f, indent=1, sort_keys=True)
        f.write("\n")


def read_key(path):
    if not os.path.isfile(path):
        raise DryRunError(f"{path} does not exist")
    with open(path, encoding="utf-8") as f:
        key = json.load(f)
    missing = [k for k in KEY_FIELDS if k not in key]
    if missing:
        raise DryRunError(f"{path} has no value for: {missing}")
    return key


def _read(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def _need(path, what):
    if not os.path.isfile(path):
        raise DryRunError(f"{what} is missing: {path}")
    return path


def package(key_file, work_dir, artifacts, out):
    read_key(key_file)
    if os.path.exists(out):
        shutil.rmtree(out)
    work_out = os.path.join(out, WORK_IN_ARTIFACT)
    os.makedirs(os.path.join(work_out, PLANNED_DIR))
    pack_out = os.path.join(out, PACKAGES_IN_ARTIFACT)
    os.makedirs(pack_out)
    shutil.copyfile(key_file, os.path.join(out, KEY_FILE))
    for name in WORK_FILES:
        shutil.copyfile(_need(os.path.join(work_dir, name), f"the dry run's {name}"), os.path.join(work_out, name))
    for name in PLANNED_FILES:
        shutil.copyfile(_need(os.path.join(work_dir, PLANNED_DIR, name), f"the planned {name}"), os.path.join(work_out, PLANNED_DIR, name))
    packed = sorted(n for n in os.listdir(artifacts) if n.endswith((".nupkg", ".snupkg")) and os.path.isfile(os.path.join(artifacts, n)))
    if not any(n.endswith(".nupkg") for n in packed):
        raise DryRunError(f"there is no package in {artifacts} to keep")
    for n in packed:
        shutil.copyfile(os.path.join(artifacts, n), os.path.join(pack_out, n))
    return {"packages": sum(1 for n in packed if n.endswith(".nupkg")), "files": len(packed) + len(WORK_FILES) + len(PLANNED_FILES) + 1}


def _same_json(a, b):
    return json.loads(_read(a)) == json.loads(_read(b))


def verify(directory, expect_file, work_dir):
    kept = read_key(_need(os.path.join(directory, KEY_FILE), "the dry run's key"))
    expected = read_key(expect_file)
    problems = [f"{k}: the dry run had '{kept[k]}', this run has '{expected[k]}'" for k in KEY_FIELDS if kept[k] != expected[k]]
    if problems:
        raise DryRunError("the dry run was not made for this release:\n  " + "\n  ".join(problems))
    differing = []
    for name in PLANNED_FILES:
        mine = _need(os.path.join(work_dir, name), f"this run's planned {name}")
        theirs = _need(os.path.join(directory, WORK_IN_ARTIFACT, PLANNED_DIR, name), f"the dry run's planned {name}")
        if not _same_json(mine, theirs):
            differing.append(name)
    if differing:
        raise DryRunError("the plan this run made is not the plan the dry run was made from (" + ", ".join(differing) + " differ). "
                          "The feed or the tree moved since the dry run, so its packages are not this release's. Run the dry run again.")
    for name in WORK_FILES:
        _need(os.path.join(directory, WORK_IN_ARTIFACT, name), f"the dry run's {name}")
    if not [n for n in os.listdir(os.path.join(directory, PACKAGES_IN_ARTIFACT)) if n.endswith(".nupkg")]:
        raise DryRunError("the dry run kept no package")


def restore(directory, work_dir, artifacts):
    for name in WORK_FILES:
        shutil.copyfile(_need(os.path.join(directory, WORK_IN_ARTIFACT, name), f"the dry run's {name}"), os.path.join(work_dir, name))
    os.makedirs(os.path.join(work_dir, PLANNED_DIR), exist_ok=True)
    for name in PLANNED_FILES:
        shutil.copyfile(os.path.join(directory, WORK_IN_ARTIFACT, PLANNED_DIR, name), os.path.join(work_dir, PLANNED_DIR, name))
    if os.path.exists(artifacts):
        shutil.rmtree(artifacts)
    os.makedirs(artifacts)
    source = os.path.join(directory, PACKAGES_IN_ARTIFACT)
    count = 0
    for n in sorted(os.listdir(source)):
        shutil.copyfile(os.path.join(source, n), os.path.join(artifacts, n))
        count += n.endswith(".nupkg")
    return {"packages": count}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    k = sub.add_parser("key")
    k.add_argument("--out", required=True)
    for field in KEY_FIELDS:
        k.add_argument("--" + field.replace("_", "-"), required=True)
    p = sub.add_parser("package")
    p.add_argument("--key", required=True)
    p.add_argument("--work-dir", required=True)
    p.add_argument("--artifacts", required=True)
    p.add_argument("--out", required=True)
    v = sub.add_parser("verify")
    v.add_argument("--dir", required=True)
    v.add_argument("--expect", required=True)
    v.add_argument("--work-dir", required=True)
    r = sub.add_parser("restore")
    r.add_argument("--dir", required=True)
    r.add_argument("--work-dir", required=True)
    r.add_argument("--artifacts", required=True)
    a = ap.parse_args(argv)
    try:
        if a.cmd == "key":
            write_key(a.out, {f: getattr(a, f) for f in KEY_FIELDS})
        elif a.cmd == "package":
            done = package(a.key, a.work_dir, a.artifacts, a.out)
            print(f"dryrun: kept {done['packages']} package(s) and the decided files in {a.out}.")
        elif a.cmd == "verify":
            verify(a.dir, a.expect, a.work_dir)
            print("dryrun: the dry run belongs to this release.")
        else:
            done = restore(a.dir, a.work_dir, a.artifacts)
            print(f"dryrun: restored the decided files and {done['packages']} package(s) from the dry run.")
    except DryRunError as e:
        sys.exit(f"dryrun {a.cmd}: {e}")


if __name__ == "__main__":
    main()

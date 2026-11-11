#!/usr/bin/env python3
"""Decide the lowest version level a package's public API change needs: patch, minor or major.

  apisurface.py diff --old FILE --new FILE --settings FILE [--json] [--max-reasons N]

FILE is the public surface of a package, written by the Fdw.ApiSurface tool from its built assemblies: the old one from the
last stable package, the new one from the build. The rules live here, in Python, so they can be tested without a .NET build;
the .NET tool only lists what is public.

Surface text: one record per line, four TAB-separated fields, sorted; blank lines and lines starting with # are ignored.

  <tfm> TAB <kind> TAB <key> TAB <detail>

  kind    TYPE        key = full type name with its generic parameters      detail = kind and modifiers ("class abstract")
          BASE        key = type name                                       detail = its base type
          IMPL        key = "<type> : <interface>"                          detail = (empty)
          CONSTRAINT  key = "<type> <generic parameter>"                    detail = the constraints
          MEMBER      key = "<type>::<name>(<parameter types>)"             detail = kind, access, modifiers and result type

A record is identified by tfm, kind and key. One that is only in the new surface is ADDED; one only in the old is REMOVED; one in
both with another detail is CHANGED. A package that adds a target framework adds records; one that drops a target framework
removes them, which is therefore a major change.

Level:
  major  a removed record, a changed record, or an added record an implementer must supply (see below)
  minor  an added record that nobody has to supply; a type that stops being sealed
  patch  no difference

An added MEMBER an implementer must supply is one whose detail has the word `abstract` (an abstract member, or an interface member
with no default body) or `required` (a required property or field), on a target framework the old package already had; on a new
target framework nobody can have implemented it, so it is only a minor. Whether such an addition is major or minor is the setting
`mustSupplyAdditionLevel` in the settings file; the file is required and so is the setting, there is no default in code. Strict
SemVer calls it major.

When a change is not on the list of relaxations above, it is major: the tool errs towards the higher level, never the lower.
"""
import argparse
import json
import sys

PATCH, MINOR, MAJOR = "patch", "minor", "major"
RANK = {PATCH: 0, MINOR: 1, MAJOR: 2}
KINDS = ("TYPE", "BASE", "IMPL", "CONSTRAINT", "MEMBER")
SETTING = "mustSupplyAdditionLevel"


class SurfaceError(Exception):
    pass


def load_settings(path):
    """The settings file. Stops, naming the problem, when it or its setting is missing or not a level."""
    try:
        with open(path, encoding="utf-8") as f:
            settings = json.load(f)
    except OSError as e:
        raise SurfaceError(f"settings file {path} cannot be read: {e}")
    except ValueError as e:
        raise SurfaceError(f"settings file {path} is not JSON: {e}")
    if not isinstance(settings, dict) or SETTING not in settings:
        raise SurfaceError(f"settings file {path} does not state {SETTING}; there is no default")
    if settings[SETTING] not in (MINOR, MAJOR):
        raise SurfaceError(f"{SETTING} in {path} is {settings[SETTING]!r}; it must be 'minor' or 'major'")
    return settings


def parse(text, origin="surface"):
    """{(tfm, kind, key): detail}. Stops on a line that is not four fields, an unknown kind or a repeated record."""
    records = {}
    for number, line in enumerate(text.splitlines(), 1):
        if not line.strip() or line.startswith("#"):
            continue
        fields = line.rstrip("\r\n").split("\t")
        if len(fields) != 4:
            raise SurfaceError(f"{origin} line {number}: expected 4 tab-separated fields, found {len(fields)}: {line[:80]}")
        tfm, kind, key, detail = fields
        if kind not in KINDS:
            raise SurfaceError(f"{origin} line {number}: unknown kind {kind!r}")
        if not tfm or not key:
            raise SurfaceError(f"{origin} line {number}: the target framework and the key must not be empty")
        identity = (tfm, kind, key)
        if identity in records:
            raise SurfaceError(f"{origin} line {number}: {kind} {key} for {tfm} appears twice")
        records[identity] = detail
    return records


def must_supply(detail):
    words = detail.split()
    return "abstract" in words or "required" in words


def relaxed(kind, old_detail, new_detail):
    """True for the one change that is not a break: a type that was sealed no longer is."""
    if kind != "TYPE":
        return False
    old_words, new_words = old_detail.split(), new_detail.split()
    return "sealed" in old_words and "sealed" not in new_words and [w for w in old_words if w != "sealed"] == new_words


def compare(old_text, new_text, settings):
    """{'level', 'reasons': [{'level', 'change', 'tfm', 'kind', 'key', 'old', 'new'}], 'counts'}, most severe reasons first."""
    old, new = parse(old_text, "old surface"), parse(new_text, "new surface")
    old_frameworks = {tfm for tfm, _, _ in old}
    reasons = []

    def reason(level, change, identity, old_detail, new_detail):
        tfm, kind, key = identity
        reasons.append({"level": level, "change": change, "tfm": tfm, "kind": kind, "key": key, "old": old_detail, "new": new_detail})

    for identity in sorted(set(old) | set(new)):
        _, kind, _ = identity
        if identity not in new:
            reason(MAJOR, "removed", identity, old[identity], None)
        elif identity not in old:
            # On a target framework the old package did not have, nobody can have implemented anything yet.
            supplied = kind == "MEMBER" and must_supply(new[identity]) and identity[0] in old_frameworks
            reason(settings[SETTING] if supplied else MINOR, "added (an implementer must supply it)" if supplied else "added",
                   identity, None, new[identity])
        elif old[identity] != new[identity]:
            reason(MINOR if relaxed(kind, old[identity], new[identity]) else MAJOR, "changed", identity, old[identity], new[identity])

    reasons.sort(key=lambda r: (-RANK[r["level"]], r["tfm"], r["kind"], r["key"]))
    level = reasons[0]["level"] if reasons else PATCH
    counts = {name: sum(1 for r in reasons if r["level"] == name) for name in (MAJOR, MINOR)}
    return {"level": level, "reasons": reasons, "counts": counts}


def describe(r):
    text = f"{r['level']}: {r['change']} {r['kind']} {r['key']} [{r['tfm']}]"
    if r["old"] is not None and r["new"] is not None:
        text += f" ({r['old']} -> {r['new']})"
    return text


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["diff"])
    ap.add_argument("--old", required=True)
    ap.add_argument("--new", required=True)
    ap.add_argument("--settings", required=True)
    ap.add_argument("--json", action="store_true")
    ap.add_argument("--max-reasons", type=int, help="print only this many reasons; without it all are printed")
    a = ap.parse_args(argv)
    try:
        settings = load_settings(a.settings)
        with open(a.old, encoding="utf-8") as f:
            old_text = f.read()
        with open(a.new, encoding="utf-8") as f:
            new_text = f.read()
        result = compare(old_text, new_text, settings)
    except (SurfaceError, OSError) as e:
        sys.exit(f"FAIL: {e}")
    shown = result["reasons"] if a.max_reasons is None else result["reasons"][:a.max_reasons]
    if a.json:
        print(json.dumps({**result, "reasons": shown}, indent=1))
    else:
        print(f"level: {result['level']} ({result['counts'][MAJOR]} major, {result['counts'][MINOR]} minor change(s))")
        for r in shown:
            print("  " + describe(r))


if __name__ == "__main__":
    main()

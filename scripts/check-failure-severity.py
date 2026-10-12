#!/usr/bin/env python3
"""Fail when a result code with a SUCCESS severity is passed to GenericResult.Failure() or Chain().

A result built from a code takes IsSuccess from the code's severity (GenericResult(IResultCode)), and
Warning, Information, Debug and Trace are success severities. So `GenericResult.Failure(<Warning code>)`
is a SUCCESS: every `IsFailure` gate lets it through with a default value. FDW-904 was a data-route
refusal that read as success and left the read gate open; FDW-914 found 28 more.

Usage:  scripts/check-failure-severity.py <src-dir> [<src-dir> ...]      (exit 1 when it finds any)
        e.g. scripts/check-failure-severity.py src ../../reference-servicetypes/public/src

Sees only codes named by a string literal at the call, as in `Failure(XxxResultCodes.ByName("Name"))`.
A code chosen through a variable (a name passed down to a helper) is not seen.
"""
import re
import sys
from pathlib import Path

SUCCESS_SEVERITIES = {"Warning", "Information", "Debug", "Trace"}
SKIP = ("/obj/", "/bin/", "/tests/", "/Tests/", "/.git/")


def sources(roots):
    for root in roots:
        for path in Path(root).rglob("*.cs"):
            text = str(path).replace("\\", "/")
            if not any(part in text for part in SKIP):
                yield path


def definitions(roots):
    """(collection, code name) -> severity, from files that both declare a [TypeOption] and name a severity."""
    found = {}
    for path in sources(roots):
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
        option = re.search(r'\[TypeOption\(typeof\((\w+)\),\s*"(\w+)"', text)
        severity = re.search(r'ResultSeverities\.ByName\("(\w+)"\)', text)
        if option and severity:
            found[(option.group(1), option.group(2))] = severity.group(1)
    return found


def findings(roots):
    codes = definitions(roots)
    call = re.compile(r"\.(?:Failure|Chain)\(")
    ref = re.compile(r'(\w+)\.ByName\("(\w+)"\)')
    for path in sources(roots):
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
        for match in call.finditer(text):
            window = text[match.end(): match.end() + 400]
            for collection, name in ref.findall(window):
                severity = codes.get((collection, name))
                if severity in SUCCESS_SEVERITIES:
                    line = text.count("\n", 0, match.start()) + 1
                    yield f"{path}:{line}: {collection}.{name} is {severity} severity, so Failure() yields a success"
                    break


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 2
    problems = sorted(set(findings(argv[1:])))
    for problem in problems:
        print(problem)
    print(f"check-failure-severity: {len(problems)} problem(s)")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))

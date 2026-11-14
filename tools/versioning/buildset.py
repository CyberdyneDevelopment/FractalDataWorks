#!/usr/bin/env python3
"""Prepare a throwaway checkout so only a build set is built from source and every other package comes from the feed.

  buildset.py prepare --repo R --set A,B,C --versions FILE --trees FILE (--release N | --package-versions FILE)

  --versions  JSON {package id: version}: the version each swapped package has on the feed (the release manifest)
  --trees     JSON {project name: git tree id} (the `projects` map of `changeset.py manifest`)
  --release   the release number every member is built and packed as, passed to dotnet as -p:Version=N
  --package-versions
              instead of --release: JSON {member: version}, one entry per member, each member built and packed at its own
              version. Passed per project from the generated targets file (a global -p:Version would reach the members a
              pack builds through project references and stamp them with the wrong number). The same file sets each member's
              FileVersion (its x.y.z.0) and AssemblyVersion. An assembly version never goes down, because a package that is
              not rebuilt still references its dependencies at the version it was built against and the runtime will not
              bind a lower one; release.py decide works out each one (assembly-versions.json) from the floor the baseline
              release recorded for that assembly.
  --assembly-versions FILE
              with --package-versions: JSON {member: x.y.z.w}, one AssemblyVersion per member; no default.
  --skip-tests
              leave the members' tests out of the commands: only build and pack. Without it the tests run, as before. The allocated
              release job passes it unless the setting run_tests asks for them (FDW-5175).

For each member's project file, and for the project file of its tests (tests/<Name>.Tests), a <ProjectReference> to a
project outside the set becomes a <PackageReference> to the published package. References to projects in the set, to
projects that are not packable and to anything that is not a src project stay project references. One PackageVersion
per swapped package is added to public/Directory.Packages.props (the mirror keeps it at the root) at that package's own version.

A ProjectReference also carries the referenced project's own private references into the consumer's restore graph, and a published package
cannot (a PrivateAssets=all reference is not a nuspec dependency). Fdw.Workspace.Roslyn holds a private Microsoft.Build.Framework
(ExcludeAssets=runtime, PrivateAssets=all); without it Microsoft.Build.Locator's MSBL001 check fails in whatever reaches Roslyn's own
dependency on the MSBuild family (FDW-5705). So where a ProjectReference is swapped, the consumer also gets the references of the target
that restrict assets and do not flow (PrivateAssets=all with an ExcludeAssets), read from the target's project file and from the
Directory.Build.props groups that apply to it; a consumer that has the package already keeps its own.

Each member also carries the tree id it was built from at fdw/tree.txt inside the package, supplied from outside the
project (CustomAfterMicrosoftCommonTargets), so no project file is edited for it.

It edits the checkout it is pointed at, so it refuses one with uncommitted changes, and it refuses before writing
anything when a version, a tree id or a member is missing. Nothing is guessed.
"""
import argparse
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

from fdwprojects import EMBED, layout_of, place_projects, project_path, prop, target_name

BUILD_DIR = ".fdw-buildset"
TARGETS = "fdw-tree.targets"
REF = re.compile(r'[ \t]*<ProjectReference\b([^>]*?)(/>|>.*?</ProjectReference>)[ \t]*\r?\n?', re.S)
ATTR = re.compile(r'(\w+)="([^"]*)"')


class BuildSetError(Exception):
    pass


def scan(public):
    """Every src and tests project under public/, in any grouping folders: (kind, name) -> {path, rel, text, id, packable}.
    `rel` is the project file relative to public/. The shape and the refusal of a file that does not fit it are
    fdwprojects' (place_projects), so this reads exactly the projects the change set reads."""
    files = []
    for kind in ("src", "tests"):
        root = os.path.join(public, kind)
        for dp, dns, fns in os.walk(root):
            dns[:] = [d for d in dns if d not in ("bin", "obj", "node_modules")]
            files += [os.path.relpath(os.path.join(dp, f), public).replace(os.sep, "/") for f in fns if f.endswith(".csproj")]
    pattern = project_path("")
    found = {}
    for rel in place_projects("", files):
        m = pattern.match(rel)
        kind, name = m.group(1), m.group(3)
        path = os.path.join(public, *rel.split("/"))
        with open(path, encoding="utf-8-sig", newline="") as f:
            text = f.read()
        found[(kind, name)] = {"path": path, "rel": rel, "text": text,
                               "id": prop(text, "PackageId") or prop(text, "AssemblyName") or name,
                               "packable": kind == "src" and (prop(text, "IsPackable") or "true").lower() != "false"}
    return found


PACKAGE_REF = re.compile(r"<PackageReference\b([^>]*?)(/>|>.*?</PackageReference>)", re.S)
ITEM_GROUP = re.compile(r"<ItemGroup\b([^>]*)>(.*?)</ItemGroup>", re.S)
PIN_ATTRS = ("Include", "Version", "IncludeAssets", "ExcludeAssets", "PrivateAssets")
CONDITION_TOKEN = re.compile(r"""\s*(?:(?P<str>'[^']*')
    |\$\((?P<prop>\w+)(?:\.(?P<method>StartsWith|EndsWith|Contains)\((?P<arg>'[^']*')\))?\)
    |(?P<op>==|!=|\(|\)|!)
    |(?P<word>AND|OR)\b)""", re.X | re.I)


def evaluate_condition(condition, properties):
    """The truth of an MSBuild Condition over the forms Directory.Build.props uses: $(Name), $(Name.StartsWith('x')) (also EndsWith and Contains),
    quoted strings that may hold $(Name), == and != (case-insensitive), !, AND, OR and parentheses. `properties` is {name: value}. A property
    that is not in it, or any other syntax, raises BuildSetError naming the condition: nothing is guessed."""
    def refuse(why):
        raise BuildSetError(f"cannot evaluate the Condition {condition!r}: {why}")
    tokens, pos = [], 0
    while pos < len(condition):
        if not condition[pos:].strip():
            break
        m = CONDITION_TOKEN.match(condition, pos)
        if not m or m.end() == pos:
            refuse(f"unknown syntax at {condition[pos:pos + 20]!r}")
        tokens.append(m)
        pos = m.end()

    def expand(text):
        def one(m):
            if m.group(1) not in properties:
                refuse(f"the property {m.group(1)} is not known here")
            return str(properties[m.group(1)])
        return re.sub(r"\$\((\w+)\)", one, text)

    def operand(m):
        if m.group("str"):
            return expand(m.group("str")[1:-1])
        name = m.group("prop")
        if name not in properties:
            refuse(f"the property {name} is not known here")
        value = str(properties[name])
        if not m.group("method"):
            return value
        arg = expand(m.group("arg")[1:-1])
        return {"startswith": value.lower().startswith(arg.lower()), "endswith": value.lower().endswith(arg.lower()),
                "contains": arg.lower() in value.lower()}[m.group("method").lower()]
    at = [0]

    def peek():
        return tokens[at[0]] if at[0] < len(tokens) else None

    def take():
        at[0] += 1
        return tokens[at[0] - 1]

    def truth(v):
        if isinstance(v, bool):
            return v
        if v.lower() in ("true", "false"):
            return v.lower() == "true"
        refuse(f"{v!r} is not a boolean")

    def primary():
        t = peek()
        if t is None:
            refuse("it ends where a value was expected")
        if t.group("op") == "(":
            take()
            v = disjunction()
            if peek() is None or peek().group("op") != ")":
                refuse("a parenthesis is not closed")
            take()
            return v
        if t.group("op") == "!":
            take()
            return not truth(primary())
        if t.group("str") is None and t.group("prop") is None:
            refuse(f"unexpected {t.group(0).strip()!r}")
        left = operand(take())
        c = peek()
        if c is not None and c.group("op") in ("==", "!="):
            take()
            r = peek()
            if r is None or (r.group("str") is None and r.group("prop") is None):
                refuse("a comparison has no right side")
            right = operand(take())
            same = str(left).lower() == str(right).lower()
            return same if c.group("op") == "==" else not same
        return left

    def conjunction():
        v = truth(primary())
        while peek() is not None and (peek().group("word") or "").upper() == "AND":
            take()
            v = truth(primary()) and v
        return v

    def disjunction():
        v = conjunction()
        while peek() is not None and (peek().group("word") or "").upper() == "OR":
            take()
            v = conjunction() or v
        return v
    result = disjunction()
    if peek() is not None:
        refuse(f"unexpected {peek().group(0).strip()!r}")
    return result


def private_pins(text, properties):
    """{package id: ordered attributes} of the PackageReference items of one project file or props file that restrict assets and do not flow to
    a consumer: PrivateAssets contains `all` and ExcludeAssets is set. An item inside an ItemGroup with a Condition, or with a Condition of its
    own, counts when the Condition holds for `properties`."""
    pins = {}
    for group in ITEM_GROUP.finditer(text):
        if "ExcludeAssets" not in group.group(2):
            continue
        gcond = dict(ATTR.findall(group.group(1))).get("Condition")
        for item in PACKAGE_REF.finditer(group.group(2)):
            attrs = dict(ATTR.findall(item.group(1)))
            private = {t.strip().lower() for t in attrs.get("PrivateAssets", "").split(";")}
            if "all" not in private or not attrs.get("ExcludeAssets") or not attrs.get("Include"):
                continue
            conds = [c for c in (gcond, attrs.get("Condition")) if c]
            if all(evaluate_condition(c, properties) for c in conds):
                pins[attrs["Include"]] = {k: attrs[k] for k in PIN_ATTRS if k in attrs}
    return pins


class Masks:
    """What a ProjectReference carried that its package cannot: for a project, the private asset-restricting references it holds itself or gets
    from the repository's Directory.Build.props and .targets (FDW-5705). `central` is the set of package ids the central file gives a version."""

    def __init__(self, public, central, projects=None):
        self.projects = projects or {}
        self.central = {c.lower() for c in central}
        self.shared = []
        for name in ("Directory.Build.props", "Directory.Build.targets"):
            path = os.path.join(public, name)
            if os.path.isfile(path):
                with open(path, encoding="utf-8-sig") as f:
                    self.shared.append(f.read())
        self.cache = {}

    def of(self, project):
        """{id: attributes} for one scanned project."""
        key = project["rel"]
        if key not in self.cache:
            props = {"MSBuildProjectName": os.path.splitext(os.path.basename(project["path"]))[0],
                     "MSBuildProjectDirectory": os.path.dirname(project["path"])}
            pins = {}
            for text in self.shared + [project["text"]]:
                pins.update(private_pins(text, props))
            self.cache[key] = pins
        return self.cache[key]

    def reachable(self, project):
        """{id: attributes} for a project and every src project its references reach, the way a restore graph sees them: a ProjectReference
        brings the references of the project it names, and theirs in turn (not an analyzer reference, and not one marked PrivateAssets=all).
        Once the project is swapped for its package, the package has no such chain below it, so the consumer needs all of it."""
        pins, seen, todo = {}, set(), [project]
        while todo:
            p = todo.pop()
            if p["rel"] in seen:
                continue
            seen.add(p["rel"])
            for pid, attrs in self.of(p).items():
                pins.setdefault(pid, attrs)
            for m in REF.finditer(p["text"]):
                a = dict(ATTR.findall(m.group(1)))
                private = {t.strip().lower() for t in a.get("PrivateAssets", "").split(";")}
                if a.get("OutputItemType") == "Analyzer" or "all" in private:
                    continue
                nxt = self.projects.get(("src", target_name(a.get("Include", ""))))
                if nxt is not None:
                    todo.append(nxt)
        return pins

    def check(self, pins, why):
        """Refuse a pin whose package has no version in the central file and none of its own: the restore would fail with NU1010."""
        missing = sorted(i for i, a in pins.items() if "Version" not in a and i.lower() not in self.central)
        if missing:
            raise BuildSetError(f"{why} give(s) the consumer a reference to {missing}, which have no version in Directory.Packages.props")


def swap(text, members, projects, masks=None, consumer=None, added=None):
    """(new text, {project name: package id swapped}, [(project name, why kept)]) for one project file.

    With `masks` (Masks) and `consumer` (the scanned project the text belongs to), a swapped reference to a project T also brings T's private
    asset-restricting references (FDW-5705), unless the consumer already has that package, from its own text or from the repository's props;
    `added` (a list) receives (package id, project T) for each one written.

    A swapped reference is written once per project file for each identical package id and attributes (FDW-5698). A project file may name
    the same ProjectReference many times (six do, since the 2026-11-01 regroup) and MSBuild accepts that, but NuGet refuses the same
    PackageReference twice (NU1504), so the further identical copies are dropped. Same id with another Condition or other attributes is
    another reference and stays."""
    out, pos, swapped, kept, written, carried = [], 0, {}, [], set(), set()
    owned = None
    for m in REF.finditer(text):
        attrs = dict(ATTR.findall(m.group(1)))
        name = target_name(attrs.get("Include", ""))
        target = projects.get(("src", name))
        why = ("has child elements" if m.group(2) != "/>" else "not a src project" if target is None
               else "in the build set" if name in members else "not packable" if not target["packable"] else None)
        if why:
            kept.append((name, why))
            continue
        extra = f' Condition="{attrs["Condition"]}"' if "Condition" in attrs else ""
        if attrs.get("PrivateAssets"):
            extra += f' PrivateAssets="{attrs["PrivateAssets"]}"'
        elif attrs.get("OutputItemType") == "Analyzer":
            extra += ' PrivateAssets="all"'
        indent = re.match(r"[ \t]*", m.group(0)).group(0)
        newline = "\r\n" if "\r\n" in m.group(0) else ("\n" if m.group(0).endswith("\n") else "")
        out.append(text[pos:m.start()])
        pos = m.end()
        swapped[name] = target["id"]
        if (target["id"].lower(), extra) in written:
            # Another copy of a reference already written: drop it. When it ended a line that still holds other content, the line end stays.
            tail = "".join(out)[-1:]
            if newline and tail and tail not in "\r\n":
                out.append(newline)
            continue
        written.add((target["id"].lower(), extra))
        out.append(f'{indent}<PackageReference Include="{target["id"]}"{extra} />{newline}')
        if masks is not None and name not in carried:
            carried.add(name)
            if owned is None:
                owned = {i.lower() for i in re.findall(r'<PackageReference\b[^>]*?\b(?:Include|Update)="([^"]+)"', text)}
                owned |= {i.lower() for i in masks.of(consumer)}
            pins = {i: a for i, a in masks.reachable(target).items() if i.lower() not in owned}
            masks.check(pins, f"the swap of {name} in {os.path.basename(consumer['path'])}")
            for pid, pin in pins.items():
                owned.add(pid.lower())
                out.append(f'{indent}<PackageReference ' + " ".join(f'{k}="{v}"' for k, v in pin.items()) + f' />{newline}')
                if added is not None:
                    added.append((pid, name))
    out.append(text[pos:])
    return "".join(out), swapped, kept


def _write(path, text):
    """Write text back with the file's own BOM; line endings are already inside `text`."""
    with open(path, "rb") as f:
        bom = f.read(3) == b"\xef\xbb\xbf"
    with open(path, "wb") as f:
        f.write((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


def central_versions(text, versions):
    """The Directory.Packages.props text with one PackageVersion per package id not already listed."""
    have = set(re.findall(r'<PackageVersion\s+Include="([^"]+)"', text))
    add = [(pid, v) for pid, v in sorted(versions.items()) if pid not in have]
    if not add:
        return text
    if text.count("</Project>") != 1:
        raise BuildSetError("Directory.Packages.props does not have exactly one </Project>")
    nl = "\r\n" if "\r\n" in text else "\n"
    lines = nl.join(f'    <PackageVersion Include="{pid}" Version="{v}" />' for pid, v in add)
    return text.replace("</Project>", f'  <ItemGroup Label="build set">{nl}{lines}{nl}  </ItemGroup>{nl}</Project>')


VERSION = re.compile(r"^(\d+)\.(\d+)\.(\d+)(-[0-9A-Za-z][0-9A-Za-z.-]*)?$")
ASSEMBLY_VERSION = re.compile(r"^\d+\.\d+\.\d+\.\d+$")



def file_version(version):
    """The FileVersion of a package at `version`: its x.y.z with a zero fourth part."""
    return ".".join(VERSION.match(version).group(1, 2, 3)) + ".0"


def version_group(package_versions, assembly_versions):
    """The targets-file text that gives each member its own Version, FileVersion and AssemblyVersion, or nothing when one release number is passed instead."""
    if not package_versions:
        return ""
    groups = "".join(f"""  <PropertyGroup Condition="'$(MSBuildProjectName)' == '{name}'">
    <Version>{package_versions[name]}</Version>
    <FileVersion>{file_version(package_versions[name])}</FileVersion>
    <AssemblyVersion>{assembly_versions[name]}</AssemblyVersion>
  </PropertyGroup>
""" for name in sorted(package_versions))
    return "  <!-- Each member's own package, file and assembly version. -->\n" + groups


def tree_targets(package_versions=None, assembly_versions=None):
    return """<Project>
""" + version_group(package_versions, assembly_versions) + """  <!-- Generated by buildset.py. Adds the tree id a member was built from to its package at fdw/tree.txt. -->
  <ItemGroup Condition="Exists('$(FdwBuildSetDir)trees/$(MSBuildProjectName)/tree.txt')">
    <None Include="$(FdwBuildSetDir)trees/$(MSBuildProjectName)/tree.txt" Pack="true" PackagePath="fdw" />
  </ItemGroup>
</Project>
"""


def commands(repo, members, release, tests, projects, run_tests=True):
    """The dotnet commands that build and pack every member, then (unless run_tests is False) test the members' tests. Each is a list of
    arguments, to run from the folder that holds src/ and tests/ (public/ in the repository, the root on the mirror)."""
    work = os.path.join(repo, BUILD_DIR)
    extra = ([f"-p:Version={release}"] if release else []) + [f"-p:CustomAfterMicrosoftCommonTargets={os.path.join(work, TARGETS)}",
                                                               f"-p:FdwBuildSetDir={work}{os.sep}"]
    # Each member is built, then packed with no build. A project that sets GeneratePackageOnBuild (every source generator does) is not built
    # by `dotnet pack` at all, so a pack alone finds no dll to put in the package (NU5019); the build is what makes the dll, and the pack
    # that follows takes it as it is. The same properties go to both, or the pack would rebuild.
    packs = []
    for m in members:
        rel = projects[("src", m)]["rel"]
        packs += [["dotnet", "build", rel, "-c", "Release", *extra], ["dotnet", "pack", rel, "-c", "Release", "--no-build", *extra]]
    if not run_tests:
        return packs
    return packs + [["dotnet", "test", projects[("tests", t)]["rel"], "-c", "Release"] for t in tests]


def dirty(repo):
    """Uncommitted changes to tracked files. Untracked files (python bytecode from running these tools) do not count:
    the tool edits tracked files, so those are the ones that must be clean."""
    out = subprocess.run(["git", "-C", repo, "status", "--porcelain", "--untracked-files=no"], capture_output=True, text=True, check=True).stdout
    return [ln for ln in out.splitlines() if ln.strip()]


def check_package_versions(members, release, package_versions):
    """Exactly one way of naming the version: one release number, or one version per member."""
    if bool(release) == bool(package_versions):
        raise BuildSetError("give either one release number or a version for every member, not both and not neither")
    if not package_versions:
        return
    missing = sorted(set(members) - set(package_versions))
    if missing:
        raise BuildSetError(f"no package version for members: {missing}")
    extra = sorted(set(package_versions) - set(members))
    if extra:
        raise BuildSetError(f"package versions given for names that are not in the set: {extra}")
    bad = sorted(f"{n} {v}" for n, v in package_versions.items() if not VERSION.match(v))
    if bad:
        raise BuildSetError(f"not a version (major.minor.patch with an optional prerelease tail): {bad}")


def prepare(repo, members, versions, trees, release, package_versions=None, assembly_versions=None, run_tests=True):
    """Rewrite the checkout and return a report. Raises BuildSetError, before writing anything, when it cannot be exact.
    The version is `release` (one number for every member) or `package_versions` ({member: version}); exactly one.
    run_tests=False leaves the members' tests out of the commands (the real run requires a green CI run of the commit unless the job tests it)."""
    check_package_versions(members, release, package_versions)
    if package_versions:
        missing = sorted(set(package_versions) - set(assembly_versions or {}))
        if missing:
            raise BuildSetError(f"no assembly version for members: {missing}; there is no default")
        bad = sorted(f"{n} {v}" for n, v in (assembly_versions or {}).items() if n in package_versions and not ASSEMBLY_VERSION.match(str(v)))
        if bad:
            raise BuildSetError(f"not an assembly version (x.y.z.w): {bad}")
    repo = os.path.realpath(repo)
    public = os.path.join(repo, layout_of(lambda p: os.path.isfile(os.path.join(repo, p))).rstrip("/"))
    if changed := dirty(repo):
        raise BuildSetError("the checkout has uncommitted changes, and this tool edits files in place:\n  " + "\n  ".join(changed[:10]))
    projects = scan(public)
    not_src = [m for m in members if ("src", m) not in projects]
    if not_src:
        raise BuildSetError(f"not src projects: {not_src}")
    unpackable = [m for m in members if not projects[("src", m)]["packable"]]
    if unpackable:
        raise BuildSetError(f"set members that are not packable cannot carry a tree id or a release number: {unpackable}")
    no_tree = [m for m in members if m not in trees]
    if no_tree:
        raise BuildSetError(f"no tree id for: {no_tree}")
    tests = [f"{m}.Tests" for m in members if ("tests", f"{m}.Tests") in projects]

    packages = os.path.join(public, "Directory.Packages.props")
    with open(packages, encoding="utf-8-sig", newline="") as f:
        central = re.findall(r'<PackageVersion\s+Include="([^"]+)"', f.read())
    masks = Masks(public, central, projects)
    rewrites, swapped, kept, carried = {}, {}, [], {}
    for key in [("src", m) for m in members] + [("tests", t) for t in tests]:
        added = []
        new, sw, k = swap(projects[key]["text"], set(members), projects, masks, projects[key], added)
        kept += [(key[1], *x) for x in k]
        swapped.update(sw)
        if added:
            carried[key[1]] = sorted({pid for pid, _ in added})
        if new != projects[key]["text"]:
            rewrites[key] = new
    ids = {pid for pid in swapped.values()}
    stable = [v for v in ([release] if release else package_versions.values()) if "-" not in v]
    if stable:
        tails = sorted(pid for pid in ids if pid in versions and "-" in versions[pid])
        if tails:
            raise BuildSetError(f"release {stable[0]} is stable, but these swapped packages are at prerelease versions, which pack refuses (NU5104): "
                                + ", ".join(f"{pid} {versions[pid]}" for pid in tails[:10]) + (" ..." if len(tails) > 10 else ""))
    no_version = sorted(ids - set(versions))
    if no_version:
        raise BuildSetError(f"no version for swapped packages (give each its feed version): {no_version}")
    for key, text in rewrites.items():
        try:
            ET.fromstring(text.encode("utf-8"))
        except ET.ParseError as e:
            raise BuildSetError(f"{key[1]} would not be well-formed after the swap: {e}")

    with open(packages, encoding="utf-8-sig", newline="") as f:
        packages_text = central_versions(f.read(), {pid: versions[pid] for pid in ids})
    ET.fromstring(packages_text.encode("utf-8"))

    for key, text in rewrites.items():
        _write(projects[key]["path"], text)
    _write(packages, packages_text)
    work = os.path.join(repo, BUILD_DIR)
    os.makedirs(work, exist_ok=True)
    with open(os.path.join(work, TARGETS), "w", encoding="utf-8", newline="\n") as f:
        f.write(tree_targets(package_versions, assembly_versions))
    for m in members:
        os.makedirs(os.path.join(work, "trees", m), exist_ok=True)
        with open(os.path.join(work, "trees", m, "tree.txt"), "w", encoding="utf-8", newline="\n") as f:
            f.write(trees[m] + "\n")

    live = {n for (_, n) in projects}
    embeds = {}
    for (kind, n), p in projects.items():
        for x in set(EMBED.findall(p["text"])):
            embeds.setdefault(n, set()).add(x)
    silent = sorted((n, x) for n in members for x in embeds.get(n, ()) if x not in members and x in live)
    return {"members": list(members), "tests": tests, "release": release, "package_versions": dict(sorted((package_versions or {}).items())),
            "rewritten": sorted(k[1] for k in rewrites), "swapped": dict(sorted(swapped.items())), "carried_exclusions": dict(sorted(carried.items())),
            "kept": kept, "versions_added": {pid: versions[pid] for pid in sorted(ids)},
            "embeds_not_in_set": silent, "commands": commands(repo, members, release, tests, projects, run_tests), "run_from": os.path.relpath(public, repo)}


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["prepare"])
    ap.add_argument("--repo", required=True)
    ap.add_argument("--set", required=True)
    ap.add_argument("--versions", required=True)
    ap.add_argument("--trees", required=True)
    ap.add_argument("--release")
    ap.add_argument("--package-versions")
    ap.add_argument("--assembly-versions")
    ap.add_argument("--skip-tests", action="store_true")
    a = ap.parse_args(argv)
    if a.package_versions and not a.assembly_versions:
        sys.exit("build set not prepared; nothing was written:\n  --package-versions needs --assembly-versions FILE: the AssemblyVersion of each member")
    with open(a.versions, encoding="utf-8") as f:
        versions = json.load(f)
    with open(a.trees, encoding="utf-8") as f:
        trees = json.load(f)
    package_versions = assembly_versions = None
    if a.package_versions:
        with open(a.package_versions, encoding="utf-8") as f:
            package_versions = json.load(f)
        with open(a.assembly_versions, encoding="utf-8") as f:
            assembly_versions = json.load(f)
    try:
        report = prepare(a.repo, [m for m in a.set.split(",") if m], versions, trees, a.release, package_versions, assembly_versions, not a.skip_tests)
    except BuildSetError as e:
        sys.exit(f"build set not prepared; nothing was written:\n  {e}")
    print(json.dumps(report, indent=1))


if __name__ == "__main__":
    main()

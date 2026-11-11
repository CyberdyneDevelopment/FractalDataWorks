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


def swap(text, members, projects):
    """(new text, {project name: package id swapped}, [(project name, why kept)]) for one project file."""
    out, pos, swapped, kept = [], 0, {}, []
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
        out += [text[pos:m.start()], f'{indent}<PackageReference Include="{target["id"]}"{extra} />{newline}']
        pos = m.end()
        swapped[name] = target["id"]
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

    rewrites, swapped, kept = {}, {}, []
    for key in [("src", m) for m in members] + [("tests", t) for t in tests]:
        new, sw, k = swap(projects[key]["text"], set(members), projects)
        kept += [(key[1], *x) for x in k]
        swapped.update(sw)
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

    packages = os.path.join(public, "Directory.Packages.props")
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
            "rewritten": sorted(k[1] for k in rewrites), "swapped": dict(sorted(swapped.items())),
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

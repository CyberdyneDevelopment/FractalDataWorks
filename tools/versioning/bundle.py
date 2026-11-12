#!/usr/bin/env python3
"""Generate the release bundle: a metapackage whose dependencies pin every package in a release exactly.

  bundle.py generate --manifest FILE --out DIR --package-dir DIR [--package-dir DIR ...] [--project-manifest FILE] [--package-versions FILE]

The manifest is JSON: {"id": "Fdw.Bundle", "version": "1.4.5", "packages": {"Fdw.Results": "1.4.5", ...}}.
The tool writes DIR/<id>.csproj, plus an empty Directory.Build.props, Directory.Build.targets and Directory.Packages.props beside it, and
nothing else; building and packing it is the pipeline's job. The three empty files are what keeps the bundle a bare metapackage: the
project is generated inside the checkout, and MSBuild reads the nearest of each file upward from the project's folder, so without them it
would import the repo's (FDW-5279: the repo's Directory.Build.props adds analyzer references that take their versions from central package
management, which the bundle turns off, and restore stopped with NU1015). A plain
`Version="1.4.5"` in NuGet is only a minimum, so every dependency is written as `[1.4.5]`. A missing, ranged or
floating version is an error: the tool never picks one for you.

Only an ordinary dependency can be a PackageReference. A package whose nuspec declares a package type other than Dependency (a dotnet tool,
a template, an MSBuild SDK, an MCP server ...) is left out of the pins: NuGet refuses it (a DotnetTool gives NU1212 at restore), and nothing
references it that way. It is stated, never silent: the bundle's description names each one with its type, and the bundle carries them
at fdw/not-pinned.json. The type is read from the package's own nuspec, never from a list of names: from the packed packages in each
--package-dir (the release's artifacts), and from nuget.org for a package that was not built in this run; a package that cannot be read
stops the run.

The project also sets DisableMSBuildAssemblyCopyCheck. Some package of the release brings Microsoft.Build with it, and the buildTransitive
check of Microsoft.Build.Locator (MSBL001) then fires in any project that references it, wanting ExcludeAssets=runtime; the bundle has no
build output and runs nothing, so the check has nothing to protect (found by build-agent's probe pack of the 1.5.6 bundle).

With --package-versions (a Fdw.PackageVersions.props from `hostprops.py generate`) the bundle also carries it at
fdw/Fdw.PackageVersions.props, which `hostprops.py adopt` reads back.

With --project-manifest (the project manifest of the tree the release was planned from, `release.py` writes it as
project-manifest.json) the bundle also carries it at fdw/manifest.json. The next release reads it back as its baseline,
so the baseline is a fact about what was published and needs neither release tags nor git history.
"""
import argparse
import glob
import json
import os
import re
import sys
from xml.sax.saxutils import quoteattr

from feeds import NugetOrg, identity_from_nupkg, package_types

SEMVER = re.compile(r"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$")
PACKAGE_ID = re.compile(r"^[A-Za-z0-9_]+(\.[A-Za-z0-9_]+)*$")


def check(manifest):
    """The problems with a manifest, as a list of sentences. Empty means it can be generated."""
    problems = []
    if not isinstance(manifest, dict):
        return ["the manifest is not a JSON object"]
    for key in ("id", "version", "packages"):
        if key not in manifest:
            problems.append(f"the manifest has no {key!r}")
    if problems:
        return problems
    if not PACKAGE_ID.match(str(manifest["id"])):
        problems.append(f"bundle id {manifest['id']!r} is not a package id")
    if not SEMVER.match(str(manifest["version"])):
        problems.append(f"bundle version {manifest['version']!r} is not a plain version (no range, no wildcard)")
    packages = manifest["packages"]
    if not isinstance(packages, dict) or not packages:
        return problems + ["'packages' is empty or not an object: a bundle of nothing pins nothing"]
    for pid, version in packages.items():
        if not PACKAGE_ID.match(pid):
            problems.append(f"{pid!r} is not a package id")
        if pid == manifest["id"]:
            problems.append(f"{pid!r} lists the bundle itself")
        if not isinstance(version, str) or not SEMVER.match(version):
            problems.append(f"{pid}: {version!r} is not an exact version (no range, no wildcard, not empty)")
    return problems


ORDINARY = "dependency"  # the one package type a PackageReference can point at; a package that declares none is one too


def pinnable(types):
    return all(t.lower() == ORDINARY for t in types)


def classify(manifest, package_dirs, feed):
    """{id: {"version", "types"}} for every package of the manifest that cannot be pinned (its nuspec declares a type other than
    Dependency). Raises ValueError when a package cannot be read."""
    local = {}
    for directory in package_dirs:
        for path in sorted(glob.glob(os.path.join(directory, "*.nupkg"))):
            with open(path, "rb") as f:
                data = f.read()
            pid, version = identity_from_nupkg(data)
            local[(pid.lower(), version.lower())] = package_types(data)
    not_pinned = {}
    for pid, version in sorted(manifest["packages"].items(), key=lambda kv: kv[0].lower()):
        types = local.get((pid.lower(), version.lower()))
        if types is None:
            data = feed.download(pid, version)
            if data is None:
                raise ValueError(f"{pid} {version} is in no --package-dir and nuget.org does not hold it, so its package type cannot be read")
            types = package_types(data)
        if not pinnable(types):
            not_pinned[pid] = {"version": version, "types": sorted(types)}
    return not_pinned


def project_text(manifest, with_project_manifest=False, with_package_versions=False, not_pinned=None):
    """The bundle project. Packages are sorted by id so the same manifest always gives the same bytes."""
    problems = check(manifest)
    if problems:
        raise ValueError("; ".join(problems))
    not_pinned = not_pinned or {}
    pins = {pid: v for pid, v in manifest["packages"].items() if pid not in not_pinned}
    if not pins:
        raise ValueError("every package of the manifest is left out of the pins: a bundle of nothing pins nothing")
    refs = "\n".join(f"    <PackageReference Include={quoteattr(pid)} Version=\"[{v}]\" PrivateAssets=\"none\" />"
                     for pid, v in sorted(pins.items(), key=lambda kv: kv[0].lower()))
    stated = ("" if not not_pinned else " Not pinned, because their package type is not an ordinary dependency: "
              + ", ".join(f"{pid} {x['version']} ({'/'.join(x['types'])})" for pid, x in sorted(not_pinned.items(), key=lambda kv: kv[0].lower())) + ".")
    description = f"Release {manifest['version']}: pins {len(pins)} packages to the exact versions this release shipped.{stated}"
    carried = (["manifest.json"] if with_project_manifest else []) + (["Fdw.PackageVersions.props"] if with_package_versions else []) + (["not-pinned.json"] if not_pinned else [])
    carry = ("\n  <ItemGroup>\n" + "".join(f'    <None Include="{name}" Pack="true" PackagePath="fdw" />\n' for name in carried) + "  </ItemGroup>\n"
             if carried else "")
    return f"""<Project Sdk="Microsoft.NET.Sdk">

  <!-- Generated by public/tools/versioning/bundle.py from the release manifest. Do not edit. -->
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <PackageId>{manifest['id']}</PackageId>
    <Version>{manifest['version']}</Version>
    <Description>{description}</Description>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
    <DisableMSBuildAssemblyCopyCheck>true</DisableMSBuildAssemblyCopyCheck>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <IncludeSymbols>false</IncludeSymbols>
    <SuppressDependenciesWhenPacking>false</SuppressDependenciesWhenPacking>
    <NoWarn>$(NoWarn);NU5128</NoWarn>
  </PropertyGroup>

  <ItemGroup>
{refs}
  </ItemGroup>
{carry}
</Project>
"""


ISOLATION_FILES = ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props")
ISOLATION_TEXT = """<Project>
  <!-- Generated by public/tools/versioning/bundle.py. Empty on purpose: MSBuild uses the nearest file of this name, so the bundle project
       inherits nothing of the repository it is generated in (analyzers, versions, central package management). -->
</Project>
"""


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("cmd", choices=["generate"])
    ap.add_argument("--manifest", required=True)
    ap.add_argument("--out", required=True)
    ap.add_argument("--project-manifest")
    ap.add_argument("--package-versions", help="a Fdw.PackageVersions.props from hostprops.py generate; the bundle carries it at fdw/")
    ap.add_argument("--package-dir", action="append", required=True, help="a folder of the packed packages of this release (repeatable); a package type is read from its nuspec")
    a = ap.parse_args(argv)
    with open(a.manifest, encoding="utf-8") as f:
        manifest = json.load(f)
    problems = check(manifest)
    if problems:
        sys.exit("bundle not generated:\n  " + "\n  ".join(problems))
    project_manifest = None
    if a.project_manifest:
        with open(a.project_manifest, encoding="utf-8") as f:
            project_manifest = json.load(f)  # parsed, so a file that is not JSON stops here
        if not isinstance(project_manifest, dict) or "projects" not in project_manifest:
            sys.exit("bundle not generated:\n  the project manifest has no 'projects'")
    try:
        not_pinned = classify(manifest, a.package_dir, NugetOrg())
        text = project_text(manifest, project_manifest is not None, a.package_versions is not None, not_pinned)
    except ValueError as e:
        sys.exit(f"bundle not generated:\n  {e}")
    os.makedirs(a.out, exist_ok=True)
    path = os.path.join(a.out, manifest["id"] + ".csproj")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    if not_pinned:
        with open(os.path.join(a.out, "not-pinned.json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(not_pinned, f, indent=1, sort_keys=True)
            f.write("\n")
    for name in ISOLATION_FILES:
        with open(os.path.join(a.out, name), "w", encoding="utf-8", newline="\n") as f:
            f.write(ISOLATION_TEXT)
    if a.package_versions is not None:
        with open(a.package_versions, encoding="utf-8") as source, open(os.path.join(a.out, "Fdw.PackageVersions.props"), "w", encoding="utf-8", newline="\n") as f:
            f.write(source.read())
    if project_manifest is not None:
        with open(os.path.join(a.out, "manifest.json"), "w", encoding="utf-8", newline="\n") as f:
            json.dump(project_manifest, f, indent=1, sort_keys=True)
            f.write("\n")
    print(f"{path}: {len(manifest['packages']) - len(not_pinned)} packages pinned for {manifest['id']} {manifest['version']}"
          + (f"; not pinned (package type): {', '.join(sorted(not_pinned))}" if not_pinned else ""))


if __name__ == "__main__":
    main()

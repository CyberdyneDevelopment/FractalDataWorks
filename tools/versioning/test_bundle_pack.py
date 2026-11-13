import io
import json
import os
import shutil
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest import mock

import bundle

# The step of the release that nothing could prove until it ran for real: generating the bundle project and packing it (FDW-5279 found
# three defects there after 289 packages were already on nuget.org). This packs a generated bundle with the real SDK, against a local
# folder feed of tiny fake packages, so it needs no nuget.org and costs a few seconds. It is a test of the generator, not a rehearsal of
# a release. A machine without the .NET SDK fails it and says so; it is not skipped.

NUSPEC = ('<?xml version="1.0"?><package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>'
          "<id>{id}</id><version>{version}</version><authors>fdw</authors><description>fake</description>{types}</metadata></package>")


def fake_package(path, pid, version, types=()):
    declared = "<packageTypes>" + "".join(f'<packageType name="{t}" />' for t in types) + "</packageTypes>" if types else ""
    with zipfile.ZipFile(path, "w") as z:
        z.writestr(f"{pid}.nuspec", NUSPEC.format(id=pid, version=version, types=declared))
        z.writestr("lib/net10.0/_._", b"")


class NoFeed:
    def download(self, pid, version):
        return None


class GeneratedBundlePackTests(unittest.TestCase):
    def setUp(self):
        if shutil.which("dotnet") is None:
            self.fail("dotnet is not on the PATH: this test packs a generated bundle with the real SDK")
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.root = self._tmp.name

    def path(self, *parts):
        return os.path.join(self.root, *parts)

    def write(self, relative, text):
        full = self.path(relative)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)

    def test_a_bundle_generated_inside_a_checkout_that_adds_unversioned_analyzers_packs_and_pins_only_the_libraries(self):
        # The checkout of the release job: a Directory.Build.props that adds an analyzer PackageReference with no version and relies on
        # central package management (NU1015 in the bundle project, FDW-5279), and a bundle project generated inside it.
        self.write("Directory.Build.props", '<Project><ItemGroup><PackageReference Include="AsyncFixer" /></ItemGroup></Project>\n')
        self.write("Directory.Packages.props", '<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup>'
                   '<ItemGroup><PackageVersion Include="AsyncFixer" Version="1.6.0" /></ItemGroup></Project>\n')
        feed = self.path("feed")
        packed = self.path("packed")
        os.makedirs(feed)
        os.makedirs(packed)
        libraries = {"Fdw.A": "1.5.6", "Fdw.B": "1.5.6"}
        for pid, version in libraries.items():
            fake_package(os.path.join(feed, f"{pid}.{version}.nupkg"), pid, version)
            fake_package(os.path.join(packed, f"{pid}.{version}.nupkg"), pid, version)
        # a dotnet tool (NU1212 if it were pinned) is in the packed folder only: it is never restored
        fake_package(os.path.join(packed, "Fdw.Tool.1.5.6.nupkg"), "Fdw.Tool", "1.5.6", ["DotnetTool"])
        self.write("m.json", json.dumps({"id": "Fdw.Bundle", "version": "1.5.6", "packages": {**libraries, "Fdw.Tool": "1.5.6"}}))
        self.write("manifest-in.json", json.dumps({"projects": {}, "globals": {}}))
        self.write("Fdw.PackageVersions.props", "<Project />\n")
        with mock.patch.object(bundle, "NugetOrg", lambda: NoFeed()):
            bundle.main(["generate", "--manifest", self.path("m.json"), "--out", self.path("bundle-src"), "--project-manifest", self.path("manifest-in.json"),
                         "--package-versions", self.path("Fdw.PackageVersions.props"), "--package-dir", packed])

        # A machine's user-level NuGet.Config may map package sources (packageSourceMapping), and then a source given only as RestoreSources is
        # "not considered" (NU1100). The config sits in the temp root, above bundle-src, so the generated project is untouched; it clears the
        # sources and maps every package to the local folder.
        self.write("nuget.config", '<configuration><packageSources><clear /><add key="local" value="' + feed + '" /></packageSources>'
                   '<packageSourceMapping><clear /><packageSource key="local"><package pattern="*" /></packageSource></packageSourceMapping></configuration>\n')
        environment = {**os.environ, "NUGET_PACKAGES": self.path("nuget-cache"), "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1", "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"}
        done = subprocess.run(["dotnet", "pack", self.path("bundle-src", "Fdw.Bundle.csproj"), "-c", "Release", "-o", self.path("out"), "-p:Version=1.5.6",
                               f"-p:RestoreSources={feed}", "-p:NuGetAudit=false", "--nologo"], capture_output=True, text=True, env=environment, cwd=self.root)
        self.assertEqual(done.returncode, 0, done.stdout + done.stderr)

        with zipfile.ZipFile(self.path("out", "Fdw.Bundle.1.5.6.nupkg")) as z:
            names = z.namelist()
            nuspec = next(n for n in names if n.endswith(".nuspec"))
            root = ET.fromstring(z.read(nuspec))
        dependencies = {e.get("id"): e.get("version") for e in root.iter() if e.tag.endswith("}dependency") or e.tag == "dependency"}
        self.assertEqual(dependencies, {"Fdw.A": "[1.5.6]", "Fdw.B": "[1.5.6]"})
        for carried in ("fdw/manifest.json", "fdw/Fdw.PackageVersions.props", "fdw/not-pinned.json"):
            self.assertIn(carried, names)
        description = next(e.text for e in root.iter() if e.tag.endswith("}description") or e.tag == "description")
        self.assertIn("Fdw.Tool 1.5.6 (DotnetTool)", description)


if __name__ == "__main__":
    unittest.main()

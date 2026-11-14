import contextlib
import io
import json
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET

import buildset
from test_changeset import Repo, base_files, proj, src

PACKAGES = '<Project>\n  <ItemGroup>\n    <PackageVersion Include="Existing.Pkg" Version="2.0.0" />\n  </ItemGroup>\n</Project>\n'
TREES = {n: f"tree-{n}" for n in ("A", "B", "C", "D", "E", "G", "H", "Gen.SourceGenerators")}
AV = {"A": "1.4.1.0", "B": "1.4.1.0", "C": "2.0.0.0"}
VERSIONS = {"A": "1.4.4", "B": "1.4.3", "D": "1.4.2", "Gen.SourceGenerators": "1.4.1", "Other.Name": "1.4.0"}


class BuildSetTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.files = base_files() | {"public/Directory.Packages.props": PACKAGES}
        self.files |= src("F", props="<AssemblyName>Other.Name</AssemblyName>")
        self.files |= src("G", refs=["F", "Lint"])
        self.files |= src("H", embeds=["A"])
        self.files |= {"public/tests/B.Tests/B.Tests.csproj": proj(refs=["B", "A"])}

    def commit(self, **over):
        self.repo.commit(self.files | over)

    def prepare(self, members, versions=None, trees=None, release="1.4.5"):
        return buildset.prepare(self.repo.root, members, VERSIONS if versions is None else versions, TREES if trees is None else trees, release)

    def text(self, path):
        with open(os.path.join(self.repo.root, path), "rb") as f:
            return f.read().decode("utf-8-sig")

    def test_a_reference_outside_the_set_becomes_a_package_reference(self):
        self.commit()
        report = self.prepare(["B"])
        self.assertIn('<PackageReference Include="A" />', self.text("public/src/B/B.csproj"))
        self.assertNotIn("ProjectReference", self.text("public/src/B/B.csproj"))
        self.assertEqual(report["swapped"], {"A": "A"})

    def test_a_reference_to_another_member_stays_a_project_reference(self):
        self.commit()
        report = self.prepare(["B", "C"])
        self.assertIn("ProjectReference", self.text("public/src/C/C.csproj"))
        self.assertIn(("C", "B", "in the build set"), report["kept"])

    def test_a_reference_to_a_project_that_is_not_packable_stays(self):
        self.commit()
        report = self.prepare(["G"])
        self.assertIn(("G", "Lint", "not packable"), report["kept"])
        self.assertIn('Include="..\\Lint\\Lint.csproj"', self.text("public/src/G/G.csproj"))

    def test_the_package_id_is_not_always_the_project_name(self):
        self.commit()
        self.assertEqual(self.prepare(["G"])["swapped"], {"F": "Other.Name"})
        self.assertIn('<PackageReference Include="Other.Name" />', self.text("public/src/G/G.csproj"))

    def test_an_analyzer_reference_keeps_private_assets(self):
        self.commit()
        self.prepare(["D"])
        self.assertIn('<PackageReference Include="Gen.SourceGenerators" PrivateAssets="all" />', self.text("public/src/D/D.csproj"))

    def test_the_tests_of_a_member_are_swapped_too(self):
        self.commit()
        report = self.prepare(["B"])
        tests = self.text("public/tests/B.Tests/B.Tests.csproj")
        self.assertIn('<PackageReference Include="A" />', tests)
        self.assertIn("ProjectReference", tests)  # B is in the set
        self.assertEqual(report["tests"], ["B.Tests"])

    def test_each_swapped_package_gets_its_own_central_version(self):
        self.commit()
        self.prepare(["E", "B"])
        central = self.text("public/Directory.Packages.props")
        self.assertIn('<PackageVersion Include="D" Version="1.4.2" />', central)
        self.assertIn('<PackageVersion Include="A" Version="1.4.4" />', central)
        self.assertIn('<PackageVersion Include="Existing.Pkg" Version="2.0.0" />', central)
        ET.fromstring(central.encode())

    def test_a_package_already_listed_centrally_is_not_listed_twice(self):
        self.commit(**{"public/Directory.Packages.props": PACKAGES.replace("Existing.Pkg", "A")})
        self.prepare(["B"])
        self.assertEqual(self.text("public/Directory.Packages.props").count('Include="A"'), 1)

    def test_a_missing_version_refuses_and_writes_nothing(self):
        self.commit()
        with self.assertRaisesRegex(buildset.BuildSetError, "no version.*'D'"):
            self.prepare(["E"], versions={"A": "1.4.4"})
        self.assertEqual(buildset.dirty(self.repo.root), [])

    def test_a_stable_release_with_a_prerelease_dependency_is_refused(self):
        self.commit()
        with self.assertRaisesRegex(buildset.BuildSetError, r"stable.*NU5104.*A 1\.4\.4-rc\.1"):
            self.prepare(["B"], versions=VERSIONS | {"A": "1.4.4-rc.1"})
        self.assertEqual(buildset.dirty(self.repo.root), [])

    def test_a_prerelease_release_may_depend_on_prerelease_versions(self):
        self.commit()
        self.assertEqual(self.prepare(["B"], versions=VERSIONS | {"A": "1.4.4-rc.1"}, release="1.4.5-rc.1")["versions_added"], {"A": "1.4.4-rc.1"})

    def test_untracked_files_do_not_make_the_checkout_dirty(self):
        self.commit()
        os.makedirs(os.path.join(self.repo.root, "public/tools/versioning/__pycache__"))
        with open(os.path.join(self.repo.root, "public/tools/versioning/__pycache__/x.pyc"), "wb") as f:
            f.write(b"x")
        self.assertEqual(self.prepare(["B"])["swapped"], {"A": "A"})

    def test_a_missing_tree_id_refuses(self):
        self.commit()
        with self.assertRaisesRegex(buildset.BuildSetError, "no tree id"):
            self.prepare(["B"], trees={"A": "x"})

    def test_a_dirty_checkout_is_refused(self):
        self.commit()
        with open(os.path.join(self.repo.root, "public/src/A/A.cs"), "a") as f:
            f.write("// edit\n")
        with self.assertRaisesRegex(buildset.BuildSetError, "uncommitted"):
            self.prepare(["B"])

    def test_a_member_that_is_not_a_src_project_is_refused(self):
        self.commit()
        with self.assertRaisesRegex(buildset.BuildSetError, "not src projects"):
            self.prepare(["Nope"])

    def test_a_member_that_is_not_packable_is_refused(self):
        self.commit()
        with self.assertRaisesRegex(buildset.BuildSetError, "not packable"):
            self.prepare(["Lint"])

    # FDW-5698: since the 2026-11-01 regroup six project files repeat the same ProjectReference, often glued on one line (the shape below is the
    # one of Fdw.Services.SecretManagers). MSBuild accepts that; NuGet refuses the same PackageReference twice (NU1504), so the 1.5.8 build set failed.
    A_REF = '<ProjectReference Include="..\\A\\A.csproj" />'
    GEN_REF = '<ProjectReference Include="..\\Gen.SourceGenerators\\Gen.SourceGenerators.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" PrivateAssets="all" />'

    def repeated(self, lines):
        return "<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>\n  <ItemGroup>" + "\n".join(lines) + "\n  </ItemGroup>\n</Project>\n"

    def secret_managers(self):
        glued = "    " + self.A_REF + "    " + self.A_REF + "\n    " + self.GEN_REF + "    " + self.A_REF + "\n"
        return self.repeated(["    " + self.A_REF, glued.rstrip("\n"), "    " + self.GEN_REF, "    " + self.GEN_REF, "    " + self.A_REF])

    def package_references(self, path):
        return [e.attrib for e in ET.fromstring(self.text(path).encode()).iter("PackageReference")]

    def test_a_reference_repeated_in_one_project_file_is_swapped_once(self):
        self.files["public/src/B/B.csproj"] = self.secret_managers()
        self.commit()
        report = self.prepare(["B"])
        text = self.text("public/src/B/B.csproj")
        self.assertEqual(self.package_references("public/src/B/B.csproj"),
                         [{"Include": "A"}, {"Include": "Gen.SourceGenerators", "PrivateAssets": "all"}])
        self.assertEqual(text.count("PackageReference"), 2)
        self.assertNotIn("ProjectReference", text)
        self.assertEqual(report["swapped"], {"A": "A", "Gen.SourceGenerators": "Gen.SourceGenerators"})

    def test_dropping_copies_leaves_no_glued_or_empty_line(self):
        self.files["public/src/B/B.csproj"] = self.secret_managers()
        self.commit()
        self.prepare(["B"])
        lines = self.text("public/src/B/B.csproj").splitlines()
        self.assertFalse([ln for ln in lines if not ln.strip()])
        self.assertTrue(all(ln.count("<PackageReference") <= 1 for ln in lines), lines)

    def test_the_same_package_with_another_condition_stays_distinct(self):
        conditional = '<ProjectReference Include="..\\A\\A.csproj" Condition="\'$(TargetFramework)\' == \'net10.0\'" />'
        self.files["public/src/B/B.csproj"] = self.repeated(["    " + self.A_REF, "    " + conditional, "    " + conditional, "    " + self.A_REF])
        self.commit()
        self.prepare(["B"])
        self.assertEqual(self.package_references("public/src/B/B.csproj"),
                         [{"Include": "A"}, {"Include": "A", "Condition": "'$(TargetFramework)' == 'net10.0'"}])

    def test_the_same_package_with_other_attributes_stays_distinct(self):
        private = '<ProjectReference Include="..\\A\\A.csproj" PrivateAssets="all" />'
        self.files["public/src/B/B.csproj"] = self.repeated(["    " + self.A_REF, "    " + private, "    " + private])
        self.commit()
        self.prepare(["B"])
        self.assertEqual(self.package_references("public/src/B/B.csproj"), [{"Include": "A"}, {"Include": "A", "PrivateAssets": "all"}])

    def test_the_repeats_of_a_reference_to_a_member_are_left_as_they_are(self):
        self.files["public/src/C/C.csproj"] = self.repeated(["    " + self.A_REF, '    <ProjectReference Include="..\\B\\B.csproj" />', '    <ProjectReference Include="..\\B\\B.csproj" />'])
        self.commit()
        self.prepare(["B", "C"])
        text = self.text("public/src/C/C.csproj")
        self.assertEqual(text.count('<ProjectReference Include="..\\B\\B.csproj" />'), 2)
        self.assertEqual(self.package_references("public/src/C/C.csproj"), [{"Include": "A"}])

    def test_a_repeat_with_windows_line_ends_keeps_them(self):
        self.files["public/src/B/B.csproj"] = self.secret_managers().replace("\n", "\r\n")
        self.commit()
        self.prepare(["B"])
        with open(os.path.join(self.repo.root, "public/src/B/B.csproj"), "rb") as f:
            raw = f.read()
        self.assertEqual(raw.count(b"\n"), raw.count(b"\r\n"))
        self.assertEqual(len(self.package_references("public/src/B/B.csproj")), 2)

    def test_the_tests_of_a_member_with_a_repeated_reference_are_swapped_once_too(self):
        self.files["public/tests/B.Tests/B.Tests.csproj"] = self.repeated(["    " + self.A_REF, "    " + self.A_REF, '    <ProjectReference Include="..\\..\\src\\B\\B.csproj" />'])
        self.commit()
        self.prepare(["B"])
        self.assertEqual(self.package_references("public/tests/B.Tests/B.Tests.csproj"), [{"Include": "A"}])

    def test_bom_and_line_endings_are_kept(self):
        crlf = src("B", refs=["A"])["public/src/B/B.csproj"].replace("\n", "\r\n")
        self.files["public/src/B/B.csproj"] = "﻿" + crlf
        self.commit()
        self.prepare(["B"])
        with open(os.path.join(self.repo.root, "public/src/B/B.csproj"), "rb") as f:
            raw = f.read()
        self.assertTrue(raw.startswith(b"\xef\xbb\xbf"))
        self.assertEqual(raw.count(b"\n"), raw.count(b"\r\n"))

    def test_a_member_carries_its_tree_id_through_the_targets_file(self):
        self.commit()
        self.prepare(["B", "C"])
        work = os.path.join(self.repo.root, buildset.BUILD_DIR)
        self.assertEqual(self.text(os.path.join(buildset.BUILD_DIR, "trees/B/tree.txt")), "tree-B\n")
        self.assertEqual(self.text(os.path.join(buildset.BUILD_DIR, "trees/C/tree.txt")), "tree-C\n")
        self.assertFalse(os.path.exists(os.path.join(work, "trees", "A")))
        root = ET.fromstring(self.text(os.path.join(buildset.BUILD_DIR, buildset.TARGETS)).encode())
        none = root.find("ItemGroup/None")
        self.assertEqual((none.get("Pack"), none.get("PackagePath")), ("true", "fdw"))

    def test_every_member_is_packed_at_the_release_number(self):
        self.commit()
        cmds = self.prepare(["B", "C"], release="1.4.9")["commands"]
        packs = [c for c in cmds if c[1] == "pack"]
        self.assertEqual([c[2] for c in packs], ["src/B/B.csproj", "src/C/C.csproj"])
        builds = [c for c in cmds if c[1] == "build"]
        self.assertEqual([c[2] for c in builds], ["src/B/B.csproj", "src/C/C.csproj"])
        self.assertTrue(all("--no-build" in c for c in packs))
        self.assertFalse(any("--no-build" in c for c in builds))
        self.assertEqual([c[3:] for c in builds], [[a for a in c[3:] if a != "--no-build"] for c in packs])
        self.assertLess(cmds.index(builds[0]), cmds.index(packs[0]))
        self.assertTrue(all("-p:Version=1.4.9" in c for c in packs))
        self.assertTrue(all(any(a.startswith("-p:CustomAfterMicrosoftCommonTargets=") for a in c) for c in packs))
        self.assertIn(["dotnet", "test", "tests/B.Tests/B.Tests.csproj", "-c", "Release"], cmds)

    def test_without_tests_only_build_and_pack_remain(self):
        self.commit()
        report = buildset.prepare(self.repo.root, ["B", "C"], VERSIONS, TREES, "1.4.9", run_tests=False)
        self.assertEqual({c[1] for c in report["commands"]}, {"build", "pack"})
        self.assertEqual(report["tests"], ["B.Tests", "C.Tests"])

    def test_the_tests_run_unless_they_are_left_out(self):
        self.commit()
        self.assertIn("test", {c[1] for c in self.prepare(["B"])["commands"]})

    def test_the_command_leaves_the_tests_out_with_skip_tests(self):
        self.commit()
        paths = {}
        for name, data in (("versions", VERSIONS), ("trees", TREES)):
            paths[name] = os.path.join(self._tmp.name, "..", f"{os.path.basename(self._tmp.name)}-{name}.json")
            with open(paths[name], "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, paths[name])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            buildset.main(["prepare", "--repo", self.repo.root, "--set", "B", "--versions", paths["versions"], "--trees", paths["trees"], "--release", "1.4.5", "--skip-tests"])
        self.assertEqual({c[1] for c in json.loads(out.getvalue())["commands"]}, {"build", "pack"})

    def per_package(self, members, package_versions, versions=None, assembly_versions=None):
        return buildset.prepare(self.repo.root, members, VERSIONS if versions is None else versions, TREES, None, package_versions,
                                AV if assembly_versions is None else assembly_versions)

    def test_each_member_gets_its_own_version_from_the_targets_file(self):
        self.commit()
        report = self.per_package(["B", "C"], {"B": "1.4.4", "C": "2.0.0-rc.1"})
        packs = [c for c in report["commands"] if c[1] == "pack"]
        self.assertFalse(any(a.startswith("-p:Version=") for c in packs for a in c))
        self.assertEqual(report["package_versions"], {"B": "1.4.4", "C": "2.0.0-rc.1"})
        root = ET.fromstring(self.text(os.path.join(buildset.BUILD_DIR, buildset.TARGETS)).encode())
        groups = {g.get("Condition"): g.find("Version").text for g in root.findall("PropertyGroup")}
        self.assertEqual(groups, {"'$(MSBuildProjectName)' == 'B'": "1.4.4", "'$(MSBuildProjectName)' == 'C'": "2.0.0-rc.1"})
        files = {g.get("Condition"): (g.find("FileVersion").text, g.find("AssemblyVersion").text) for g in root.findall("PropertyGroup")}
        self.assertEqual(files, {"'$(MSBuildProjectName)' == 'B'": ("1.4.4.0", "1.4.1.0"), "'$(MSBuildProjectName)' == 'C'": ("2.0.0.0", "2.0.0.0")})
        self.assertIsNotNone(root.find("ItemGroup/None"))

    def test_one_release_number_leaves_the_targets_file_without_versions(self):
        self.commit()
        self.prepare(["B"])
        root = ET.fromstring(self.text(os.path.join(buildset.BUILD_DIR, buildset.TARGETS)).encode())
        self.assertEqual(root.findall("PropertyGroup"), [])

    def test_a_member_without_a_version_is_refused_and_nothing_is_written(self):
        self.commit()
        with self.assertRaises(buildset.BuildSetError) as raised:
            self.per_package(["B", "C"], {"B": "1.4.4"})
        self.assertIn("C", str(raised.exception))
        self.assertEqual(self.repo.git("status", "--porcelain", "--untracked-files=no").strip(), "")

    def test_a_version_for_a_name_outside_the_set_is_refused(self):
        self.commit()
        with self.assertRaises(buildset.BuildSetError):
            self.per_package(["B"], {"B": "1.4.4", "C": "1.0.0"})

    def test_a_text_that_is_not_a_version_is_refused(self):
        self.commit()
        with self.assertRaises(buildset.BuildSetError):
            self.per_package(["B"], {"B": "1.4</Version>"})

    def test_both_a_release_and_package_versions_is_refused(self):
        self.commit()
        with self.assertRaises(buildset.BuildSetError):
            buildset.prepare(self.repo.root, ["B"], VERSIONS, TREES, "1.4.5", {"B": "1.4.5"}, AV)
        with self.assertRaises(buildset.BuildSetError):
            buildset.prepare(self.repo.root, ["B"], VERSIONS, TREES, None, None, AV)

    def test_a_stable_member_with_a_prerelease_dependency_is_refused(self):
        self.commit()
        with self.assertRaises(buildset.BuildSetError) as raised:
            self.per_package(["B"], {"B": "1.4.4"}, versions=VERSIONS | {"A": "1.4.4-rc.1"})
        self.assertIn("NU5104", str(raised.exception))

    def test_prerelease_members_may_depend_on_prerelease_versions(self):
        self.commit()
        self.assertEqual(self.per_package(["B"], {"B": "1.4.4-rc.1"}, versions=VERSIONS | {"A": "1.4.4-rc.1"})["versions_added"], {"A": "1.4.4-rc.1"})

    def test_the_file_version_is_the_package_version_with_a_zero_fourth_part(self):
        self.assertEqual(buildset.file_version("1.5.3"), "1.5.3.0")
        self.assertEqual(buildset.file_version("2.0.0-rc.1"), "2.0.0.0")

    def test_package_versions_without_assembly_versions_are_refused(self):
        self.commit()
        for missing in (None, {}, {"A": "1.4.1.0"}):
            with self.assertRaisesRegex(buildset.BuildSetError, "no assembly version"):
                buildset.prepare(self.repo.root, ["B"], VERSIONS, TREES, None, {"B": "1.4.4"}, missing)

    def test_an_assembly_version_that_is_not_x_y_z_w_is_refused(self):
        self.commit()
        for bad in ("1.5", "1.5.1", "x.y.z.w", ""):
            with self.assertRaisesRegex(buildset.BuildSetError, "x.y.z.w", msg=bad):
                self.per_package(["B"], {"B": "1.4.4"}, assembly_versions={"B": bad})

    def test_the_command_needs_the_assembly_versions_with_package_versions(self):
        self.commit()
        with self.assertRaises(SystemExit) as raised:
            buildset.main(["prepare", "--repo", self.repo.root, "--set", "B", "--versions", __file__, "--trees", __file__, "--package-versions", __file__])
        self.assertIn("--assembly-versions", str(raised.exception))

    def test_the_command_takes_a_package_versions_file(self):
        self.commit()
        paths = {}
        for name, data in (("versions", VERSIONS), ("trees", TREES), ("pv", {"B": "1.4.4"}), ("av", {"B": "1.4.1.0"})):
            paths[name] = os.path.join(self._tmp.name, "..", f"{os.path.basename(self._tmp.name)}-{name}.json")
            with open(paths[name], "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, paths[name])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            buildset.main(["prepare", "--repo", self.repo.root, "--set", "B", "--versions", paths["versions"], "--trees", paths["trees"], "--package-versions", paths["pv"], "--assembly-versions", paths["av"]])
        self.assertEqual(json.loads(out.getvalue())["package_versions"], {"B": "1.4.4"})

    def test_an_embed_of_a_sibling_outside_the_set_is_reported(self):
        self.commit()
        self.assertEqual(self.prepare(["H"])["embeds_not_in_set"], [("H", "A")])
        self.assertEqual(self.prepare_again(["H", "A"]), [])

    def prepare_again(self, members):
        self.repo.git("checkout", "-q", "--", ".")
        self.repo.git("clean", "-fdq")
        return self.prepare(members)["embeds_not_in_set"]

    def test_the_command_prints_the_report(self):
        self.commit()
        paths = {}
        for name, data in (("versions", VERSIONS), ("trees", TREES)):
            paths[name] = os.path.join(self._tmp.name, "..", f"{os.path.basename(self._tmp.name)}-{name}.json")
            with open(paths[name], "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, paths[name])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            buildset.main(["prepare", "--repo", self.repo.root, "--set", "B", "--versions", paths["versions"], "--trees", paths["trees"], "--release", "1.4.5"])
        self.assertEqual(json.loads(out.getvalue())["swapped"], {"A": "A"})

    def test_the_command_exits_with_the_reason_when_it_refuses(self):
        self.commit()
        paths = []
        for data in ({}, TREES):
            p = os.path.join(self._tmp.name, "..", f"{os.path.basename(self._tmp.name)}-{len(paths)}.json")
            with open(p, "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, p)
            paths.append(p)
        with self.assertRaises(SystemExit) as raised:
            buildset.main(["prepare", "--repo", self.repo.root, "--set", "B", "--versions", paths[0], "--trees", paths[1], "--release", "1.4.5"])
        self.assertIn("nothing was written", str(raised.exception))



BF_PIN = '<PackageReference Include="Microsoft.Build.Framework" ExcludeAssets="runtime" PrivateAssets="all" />'
ST_PIN = '<PackageReference Include="Microsoft.NET.StringTools" ExcludeAssets="runtime" PrivateAssets="all" />'
PINNED_PROPS = ('<Project>\n  <PropertyGroup>\n    <VersionPrefix>1.4.4</VersionPrefix>\n  </PropertyGroup>\n'
                '  <ItemGroup Condition="$(MSBuildProjectName.StartsWith(\'Host.\')) OR $(MSBuildProjectName) == \'Roslyn.Host\'">\n    ' + BF_PIN + '\n  </ItemGroup>\n'
                '  <ItemGroup Condition="\'$(TargetFramework)\' == \'netstandard2.0\'">\n    <PackageReference Include="PolySharp" PrivateAssets="all" />\n  </ItemGroup>\n</Project>\n')
PINNED_PACKAGES = ('<Project>\n  <ItemGroup>\n    <PackageVersion Include="Microsoft.Build.Framework" Version="18.6.3" />\n'
                   '    <PackageVersion Include="Microsoft.NET.StringTools" Version="18.6.3" />\n  </ItemGroup>\n</Project>\n')


def csproj(refs=(), packages=()):
    items = [f'<ProjectReference Include="..\\{r}\\{r}.csproj" />' for r in refs] + list(packages)
    return ('<Project Sdk="Microsoft.NET.Sdk">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>\n  <ItemGroup>\n    '
            + "\n    ".join(items) + "\n  </ItemGroup>\n</Project>\n")


class CarriedExclusionTests(unittest.TestCase):
    """FDW-5705: a ProjectReference carries the target's private asset exclusions into the consumer's restore graph; a package cannot. The 1.5.8
    build set failed MSBL001 in Fdw.Services.Connections.RoslynWorkspace because Fdw.Workspace.Roslyn (outside the set) lost its Microsoft.Build.Framework."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.files = base_files() | {"public/Directory.Build.props": PINNED_PROPS, "public/Directory.Packages.props": PINNED_PACKAGES}
        self.files |= {"public/src/Roslyn.Host/Roslyn.Host.csproj": csproj(packages=[ST_PIN]), "public/src/Roslyn.Host/H.cs": "class H {}\n"}
        self.files |= {"public/src/Plain/Plain.csproj": csproj(), "public/src/Consumer/Consumer.csproj": csproj(refs=["Roslyn.Host", "Plain"])}
        self.versions = {"Roslyn.Host": "1.4.2", "Plain": "1.4.1", "A": "1.4.4", "Consumer": "1.4.0", "B": "1.4.3"}
        self.trees = {n: f"tree-{n}" for n in ("Consumer", "Roslyn.Host", "Plain", "B", "A")}

    def prepare(self, members, **over):
        self.repo.commit(self.files | over)
        return buildset.prepare(self.repo.root, members, self.versions, self.trees, "1.4.5")

    def text(self, path):
        with open(os.path.join(self.repo.root, path), "rb") as f:
            return f.read().decode("utf-8-sig")

    def refs(self, path):
        return [e.attrib for e in ET.fromstring(self.text(path).encode()).iter("PackageReference")]

    def test_the_consumer_gets_the_exclusion_a_props_group_gave_the_target_by_its_name(self):
        report = self.prepare(["Consumer"])
        self.assertIn(BF_PIN, self.text("public/src/Consumer/Consumer.csproj"))
        self.assertEqual(report["carried_exclusions"]["Consumer"], ["Microsoft.Build.Framework", "Microsoft.NET.StringTools"])

    def test_the_consumer_gets_the_exclusion_the_targets_own_project_file_holds(self):
        self.prepare(["Consumer"])
        self.assertIn(ST_PIN, self.text("public/src/Consumer/Consumer.csproj"))

    def test_the_references_are_written_next_to_the_swapped_one_and_only_once(self):
        self.prepare(["Consumer"])
        self.assertEqual(self.refs("public/src/Consumer/Consumer.csproj"), [
            {"Include": "Roslyn.Host"}, {"Include": "Microsoft.Build.Framework", "ExcludeAssets": "runtime", "PrivateAssets": "all"},
            {"Include": "Microsoft.NET.StringTools", "ExcludeAssets": "runtime", "PrivateAssets": "all"}, {"Include": "Plain"}])

    def test_a_target_with_no_exclusion_brings_nothing(self):
        self.files["public/src/Consumer/Consumer.csproj"] = csproj(refs=["Plain"])
        report = self.prepare(["Consumer"])
        self.assertEqual(report["carried_exclusions"], {})
        self.assertEqual(self.refs("public/src/Consumer/Consumer.csproj"), [{"Include": "Plain"}])

    def test_a_target_in_the_set_stays_a_project_reference_and_brings_nothing(self):
        report = self.prepare(["Consumer", "Roslyn.Host"])
        self.assertIn("ProjectReference", self.text("public/src/Consumer/Consumer.csproj"))
        self.assertEqual(report["carried_exclusions"], {})

    def test_a_consumer_that_has_the_package_itself_keeps_its_own(self):
        own = '<PackageReference Include="Microsoft.NET.StringTools" />'
        self.files["public/src/Consumer/Consumer.csproj"] = csproj(refs=["Roslyn.Host"], packages=[own])
        report = self.prepare(["Consumer"])
        self.assertEqual([r for r in self.refs("public/src/Consumer/Consumer.csproj") if r["Include"] == "Microsoft.NET.StringTools"], [{"Include": "Microsoft.NET.StringTools"}])
        self.assertEqual(report["carried_exclusions"]["Consumer"], ["Microsoft.Build.Framework"])

    def test_a_consumer_the_props_group_already_covers_is_not_given_a_duplicate(self):
        files = {"public/src/Host.Consumer/Host.Consumer.csproj": csproj(refs=["Roslyn.Host"])}
        self.trees["Host.Consumer"] = "t"
        self.versions["Host.Consumer"] = "1.4.0"
        report = self.prepare(["Host.Consumer"], **files)
        self.assertEqual(self.text("public/src/Host.Consumer/Host.Consumer.csproj").count("Microsoft.Build.Framework"), 0)
        self.assertEqual(report["carried_exclusions"]["Host.Consumer"], ["Microsoft.NET.StringTools"])

    def test_two_targets_with_the_same_exclusion_give_it_once(self):
        self.files["public/src/Second/Second.csproj"] = csproj(packages=[ST_PIN])
        self.files["public/src/Consumer/Consumer.csproj"] = csproj(refs=["Roslyn.Host", "Second"])
        self.versions["Second"] = "1.4.0"
        self.trees["Second"] = "t"
        self.prepare(["Consumer"])
        self.assertEqual(self.text("public/src/Consumer/Consumer.csproj").count('Include="Microsoft.NET.StringTools"'), 1)

    def test_a_group_whose_condition_does_not_hold_for_the_target_gives_nothing(self):
        self.files["public/src/Roslyn.Host/Roslyn.Host.csproj"] = csproj()
        self.files["public/Directory.Build.props"] = PINNED_PROPS.replace("== 'Roslyn.Host'", "== 'Something.Else'")
        self.assertEqual(self.prepare(["Consumer"])["carried_exclusions"], {})

    def test_the_tests_project_of_a_member_gets_it_too(self):
        self.files["public/tests/Consumer.Tests/Consumer.Tests.csproj"] = csproj(refs=["Roslyn.Host", "Consumer"])
        report = self.prepare(["Consumer"])
        self.assertIn("Microsoft.Build.Framework", report["carried_exclusions"]["Consumer.Tests"])

    def test_the_exclusion_of_a_project_further_down_the_chain_is_carried_too(self):
        # Consumer -> Middle -> Roslyn.Host: with Middle swapped for its package, nothing below it is a project any more, so the consumer needs
        # what the whole chain gave it in the normal tree (the case of a release that changes only the project above Fdw.Services.Connections.RoslynWorkspace).
        self.files["public/src/Middle/Middle.csproj"] = csproj(refs=["Roslyn.Host"])
        self.files["public/src/Consumer/Consumer.csproj"] = csproj(refs=["Middle"])
        self.versions["Middle"] = "1.4.0"
        self.trees["Middle"] = "t"
        report = self.prepare(["Consumer"])
        self.assertEqual(report["carried_exclusions"]["Consumer"], ["Microsoft.Build.Framework", "Microsoft.NET.StringTools"])

    def test_a_reference_that_does_not_flow_is_not_followed(self):
        self.files["public/src/Middle/Middle.csproj"] = ("<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework></PropertyGroup>\n  <ItemGroup>\n"
                                                         "    <ProjectReference Include=\"..\\Roslyn.Host\\Roslyn.Host.csproj\" OutputItemType=\"Analyzer\" ReferenceOutputAssembly=\"false\" />\n"
                                                         "    <ProjectReference Include=\"..\\Roslyn.Host\\Roslyn.Host.csproj\" PrivateAssets=\"all\" />\n  </ItemGroup>\n</Project>\n")
        self.files["public/src/Consumer/Consumer.csproj"] = csproj(refs=["Middle"])
        self.versions["Middle"] = "1.4.0"
        self.trees["Middle"] = "t"
        self.assertEqual(self.prepare(["Consumer"])["carried_exclusions"], {})

    def test_a_condition_that_cannot_be_evaluated_refuses_naming_it_and_writes_nothing(self):
        self.files["public/Directory.Build.props"] = PINNED_PROPS.replace("OR $(MSBuildProjectName) ==", "OR $([System.String]::new('x')) ==")
        self.repo.commit(self.files)
        with self.assertRaisesRegex(buildset.BuildSetError, r"cannot evaluate the Condition .*System\.String"):
            buildset.prepare(self.repo.root, ["Consumer"], self.versions, self.trees, "1.4.5")
        self.assertNotIn("PackageReference", self.text("public/src/Consumer/Consumer.csproj"))

    def test_an_exclusion_with_no_central_version_refuses_and_writes_nothing(self):
        self.files["public/Directory.Packages.props"] = "<Project>\n  <ItemGroup>\n    <PackageVersion Include=\"Microsoft.NET.StringTools\" Version=\"18.6.3\" />\n  </ItemGroup>\n</Project>\n"
        self.repo.commit(self.files)
        with self.assertRaisesRegex(buildset.BuildSetError, r"Microsoft\.Build\.Framework.*no version in Directory\.Packages\.props"):
            buildset.prepare(self.repo.root, ["Consumer"], self.versions, self.trees, "1.4.5")
        self.assertNotIn("PackageReference", self.text("public/src/Consumer/Consumer.csproj"))


class ConditionTests(unittest.TestCase):
    P = {"MSBuildProjectName": "Fdw.Workspace.Roslyn", "MSBuildProjectDirectory": "/repo/src/tooling/roslyn"}

    def ev(self, condition):
        return buildset.evaluate_condition(condition, self.P)

    def test_the_forms_of_the_real_props_file(self):
        self.assertTrue(self.ev("$(MSBuildProjectName.StartsWith('Fdw.Tools.')) OR $(MSBuildProjectName) == 'Fdw.Workspace.Roslyn'"))
        self.assertFalse(self.ev("$(MSBuildProjectName.StartsWith('Fdw.Tools.')) OR $(MSBuildProjectName) == 'Other'"))
        self.assertTrue(self.ev("!$(MSBuildProjectDirectory.Contains('/samples/')) AND !$(MSBuildProjectDirectory.Contains('\\samples\\'))"))

    def test_comparison_ignores_case_and_expands_properties_in_strings(self):
        self.assertTrue(self.ev("'$(MSBuildProjectName)' == 'fdw.workspace.roslyn'"))
        self.assertTrue(self.ev("'$(MSBuildProjectName)' != 'x'"))

    def test_and_binds_tighter_than_or_and_parentheses_group(self):
        self.assertTrue(self.ev("'a' == 'b' AND 'a' == 'b' OR 'a' == 'a'"))
        self.assertFalse(self.ev("'a' == 'b' AND ('a' == 'b' OR 'a' == 'a')"))

    def test_a_property_that_is_not_known_refuses(self):
        with self.assertRaisesRegex(buildset.BuildSetError, "TargetFramework is not known"):
            self.ev("'$(TargetFramework)' == 'net10.0'")

    def test_syntax_it_does_not_know_refuses_naming_the_condition(self):
        for bad in ("$([MSBuild]::IsOSPlatform('Linux'))", "Exists('x')", "'a' ==", "('a' == 'a'", "'a' == 'a' extra"):
            with self.assertRaisesRegex(buildset.BuildSetError, "cannot evaluate the Condition"):
                self.ev(bad)

    def test_a_pin_is_a_private_reference_that_excludes_assets(self):
        text = ('<Project><ItemGroup><PackageReference Include="A" PrivateAssets="all" />'
                '<PackageReference Include="B" ExcludeAssets="runtime" />'
                '<PackageReference Include="C" ExcludeAssets="runtime" PrivateAssets="All" />'
                '<PackageReference Include="D" Version="2.0" ExcludeAssets="runtime;native" PrivateAssets="contentfiles;all" /></ItemGroup></Project>')
        self.assertEqual(buildset.private_pins(text, self.P), {
            "C": {"Include": "C", "ExcludeAssets": "runtime", "PrivateAssets": "All"},
            "D": {"Include": "D", "Version": "2.0", "ExcludeAssets": "runtime;native", "PrivateAssets": "contentfiles;all"}})


if __name__ == "__main__":
    unittest.main()

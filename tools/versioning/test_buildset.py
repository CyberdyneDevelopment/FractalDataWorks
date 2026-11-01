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
        self.assertTrue(all("-p:Version=1.4.9" in c for c in packs))
        self.assertTrue(all(any(a.startswith("-p:CustomAfterMicrosoftCommonTargets=") for a in c) for c in packs))
        self.assertIn(["dotnet", "test", "tests/B.Tests/B.Tests.csproj", "-c", "Release"], cmds)

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


if __name__ == "__main__":
    unittest.main()

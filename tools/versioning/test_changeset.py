import json
import os
import subprocess
import tempfile
import unittest

import changeset
from fdwprojects import snapshot


def proj(refs=(), analyzer_refs=(), props="", embeds=()):
    items = [f'<ProjectReference Include="..\\{r}\\{r}.csproj" />' for r in refs]
    items += [f'<ProjectReference Include="..\\{r}\\{r}.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />' for r in analyzer_refs]
    items += [f'<None Include="..\\{e}\\bin\\$(Configuration)\\net10.0\\{e}.dll" Pack="true" Condition="Exists(\'..\\{e}\\bin\')" />' for e in embeds]
    return ("<Project Sdk=\"Microsoft.NET.Sdk\">\n  <PropertyGroup><TargetFramework>net10.0</TargetFramework>" + props
            + "</PropertyGroup>\n  <ItemGroup>\n    " + "\n    ".join(items) + "\n  </ItemGroup>\n</Project>\n")


BUILD_PROPS = ('<Project>\n  <PropertyGroup>\n    <VersionPrefix>1.4.4</VersionPrefix>\n  </PropertyGroup>\n'
               '  <ItemGroup>\n    <ProjectReference Include="$(MSBuildThisFileDirectory)src\\Lint\\Lint.csproj" />\n  </ItemGroup>\n</Project>\n')


class Repo:
    def __init__(self, tmp):
        self.root = tmp
        self.git("init", "-q")
        self.git("config", "user.email", "t@example.com")
        self.git("config", "user.name", "t")

    def git(self, *args):
        return subprocess.run(["git", "-C", self.root, *args], capture_output=True, check=True, text=True).stdout.strip()

    def write(self, files):
        for path, content in files.items():
            full = os.path.join(self.root, path)
            if content is None:
                os.remove(full)
                continue
            os.makedirs(os.path.dirname(full), exist_ok=True)
            with open(full, "w", encoding="utf-8", newline="") as f:
                f.write(content)

    def commit(self, files, message="c"):
        self.write(files)
        self.git("add", "-A")
        self.git("commit", "-q", "-m", message)
        return self.git("rev-parse", "HEAD")


def src(name, **kw):
    return {f"public/src/{name}/{name}.csproj": proj(**kw)}


def base_files():
    files = {"public/Directory.Build.props": BUILD_PROPS, "public/Directory.Packages.props": "<Project />\n",
             "public/global.json": "{}\n"}
    files |= src("Lint", props="<IsPackable>false</IsPackable>")
    files |= src("A")
    files |= src("B", refs=["A"])
    files |= src("C", refs=["B"])
    files |= src("Gen.SourceGenerators", props="<DevelopmentDependency>true</DevelopmentDependency>")
    files |= src("D", analyzer_refs=["Gen.SourceGenerators"])
    files |= src("E", refs=["D"])
    files |= {"public/src/A/A.cs": "class A {}\n", "public/src/C/C.cs": "class C {}\n", "public/src/Gen.SourceGenerators/G.cs": "class G {}\n"}
    files |= {"public/tests/A.Tests/A.Tests.csproj": proj(refs=["A"]), "public/tests/C.Tests/C.Tests.csproj": proj(refs=["C"])}
    return files


class ChangeSetTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.before = self.repo.commit(base_files(), "base")

    def plan(self, files, api_breaks=()):
        self.repo.commit(files, "change")
        baseline = snapshot(self.repo.root, self.before).manifest()
        return changeset.plan(snapshot(self.repo.root, "HEAD"), baseline, api_breaks)

    def test_no_change_builds_nothing(self):
        self.repo.commit({"README.md": "x\n"})
        result = changeset.plan(snapshot(self.repo.root), snapshot(self.repo.root, self.before).manifest())
        self.assertEqual(result["build_set"], [])

    def test_leaf_change_builds_only_that_package(self):
        self.assertEqual(self.plan({"public/src/C/C.cs": "class C { int x; }\n"})["build_set"], ["C"])

    def test_dependents_of_a_compatible_change_are_not_pulled_in(self):
        result = self.plan({"public/src/A/A.cs": "class A { int x; }\n"})
        self.assertEqual(result["build_set"], ["A"])
        self.assertIn("A.Tests", result["tests"])

    def test_an_api_break_adds_everything_above_it(self):
        result = self.plan({"public/src/A/A.cs": "class A { }\n"}, api_breaks=["A"])
        self.assertEqual(result["build_set"], ["A", "B", "C"])

    def test_a_changed_generator_adds_everything_above_it(self):
        result = self.plan({"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})
        self.assertEqual(result["build_set"], ["D", "E", "Gen.SourceGenerators"])

    def test_a_changed_generator_that_is_a_package_names_the_packages_it_caused(self):
        result = self.plan({"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})
        self.assertEqual(result["generators"], {"Gen.SourceGenerators": ["D", "E"]})

    def test_a_changed_analyzer_that_cannot_be_a_package_is_not_a_generator_with_a_level(self):
        self.assertEqual(self.plan({"public/src/Lint/Lint.cs": "class L {}\n"})["generators"], {})

    def test_a_changed_style_analyzer_that_cannot_be_a_package_adds_everything(self):
        files = {"public/src/Lint/Lint.cs": "class L {}\n"}
        result = self.plan(files)
        self.assertEqual(result["build_set"], ["A", "B", "C", "D", "E", "Gen.SourceGenerators"])
        self.assertEqual(result["built_from_source_only"], ["Lint"])

    def test_a_repo_wide_build_file_adds_everything(self):
        result = self.plan({"public/Directory.Packages.props": "<Project><ItemGroup /></Project>\n"})
        self.assertEqual(len(result["build_set"]), 6)
        self.assertTrue(all("repo-wide" in r for rs in result["reasons"].values() for r in rs))

    def test_a_base_version_bump_alone_changes_nothing(self):
        bumped = BUILD_PROPS.replace("1.4.4", "1.4.5")
        self.assertEqual(self.plan({"public/Directory.Build.props": bumped})["build_set"], [])

    def test_a_moved_project_folder_is_unchanged(self):
        moves = {}
        for name in ("A", "A.Tests"):
            kind = "tests" if name.endswith(".Tests") else "src"
            old = f"public/{kind}/{name}"
            for fname in ("%s.csproj" % name,) + (("A.cs",) if name == "A" else ()):
                with open(os.path.join(self.repo.root, old, fname), encoding="utf-8") as f:
                    text = f.read()
                moves[f"{old}/{fname}"] = None
                moves[f"public/{kind}/foundation/{name}/{fname}"] = text
        result = self.plan(moves)
        self.assertEqual(result["build_set"], [])

    def test_package_id_comes_from_the_assembly_name_not_the_folder(self):
        files = src("F", props="<AssemblyName>Other.Name</AssemblyName>")
        result = self.plan(files)
        self.assertEqual(result["packages"]["F"], "Other.Name")
        self.assertEqual(result["reasons"]["F"], ["new package"])

    def test_a_package_that_embeds_a_changed_project_is_rebuilt_with_it(self):
        self.repo.commit(src("Tasks", props="<IsPackable>false</IsPackable>") | src("Ddl", embeds=["Tasks"]) | {"public/src/Tasks/T.cs": "x\n"}, "add")
        self.before = self.repo.git("rev-parse", "HEAD")
        result = self.plan({"public/src/Tasks/T.cs": "y\n"})
        self.assertEqual(result["build_set"], ["Ddl"])
        self.assertIn("Tasks", result["built_from_source_only"])

    def test_an_embed_of_a_project_that_does_not_exist_is_ignored(self):
        self.repo.commit(src("Old", embeds=["Gone"]), "add")
        self.before = self.repo.git("rev-parse", "HEAD")
        result = self.plan({"public/src/Old/O.cs": "x\n"})
        self.assertEqual(result["build_set"], ["Old"])

    def test_a_non_packable_project_a_member_references_is_built_from_source(self):
        self.repo.commit(src("Helper", props="<IsPackable>false</IsPackable>") | src("User", refs=["Helper"]), "add")
        self.before = self.repo.git("rev-parse", "HEAD")
        result = self.plan({"public/src/User/U.cs": "x\n"})
        self.assertEqual(result["build_set"], ["User"])
        self.assertEqual(result["built_from_source_only"], ["Helper", "Lint"])

    def test_tests_that_reference_a_member_run(self):
        result = self.plan({"public/src/C/C.cs": "class C { int x; }\n"})
        self.assertEqual(result["tests"], ["C.Tests"])

    def test_a_new_package_is_in_the_set(self):
        self.assertEqual(self.plan(src("Z"))["reasons"]["Z"], ["new package"])

    def test_without_a_baseline_everything_builds(self):
        result = changeset.plan(snapshot(self.repo.root), None)
        self.assertEqual(len(result["build_set"]), 6)

    def test_a_missing_build_props_file_fails_loudly(self):
        self.repo.commit({"public/Directory.Build.props": None}, "remove")
        with self.assertRaises(SystemExit):
            snapshot(self.repo.root)

    def test_the_cli_prints_json(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"})
        path = os.path.join(self._tmp.name, "..", "baseline-for-test.json")
        with open(path, "w") as f:
            json.dump(snapshot(self.repo.root, self.before).manifest(), f)
        self.addCleanup(os.remove, path)
        out = subprocess.run(["python3", changeset.__file__, "plan", "--repo", self.repo.root, "--baseline", path, "--json"],
                             capture_output=True, text=True, check=True).stdout
        self.assertEqual(json.loads(out)["build_set"], ["C"])


if __name__ == "__main__":
    unittest.main()

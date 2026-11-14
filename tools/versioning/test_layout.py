import os
import tempfile
import unittest

import buildset
import changeset
import fdwprojects
import release
from fdwprojects import snapshot
from test_allocate import FakeFeed
from test_changeset import Repo, base_files

ALL = ("A", "B", "C", "D", "E", "Gen.SourceGenerators")


def mirror(files):
    """The same files as the GitHub mirror holds them: the contents of public/ at the root."""
    return {path.removeprefix("public/"): text for path, text in files.items() if path.startswith("public/")}


class LayoutTests(unittest.TestCase):
    def repo(self, files):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        repo = Repo(tmp.name)
        repo.commit(files, "base")
        return repo

    def plans(self, change):
        """The change-set plan for the same change on both layouts."""
        out = []
        for shape in (lambda f: f, mirror):
            repo = self.repo(shape(base_files()))
            before = repo.git("rev-parse", "HEAD")
            repo.commit(shape(change), "change")
            out.append(changeset.plan(snapshot(repo.root), changeset.baseline_from_ref(repo.root, before)))
        return out

    def test_the_layout_is_read_from_the_tree(self):
        self.assertEqual(fdwprojects.layout_of(lambda p: p == "public/Directory.Build.props"), "public/")
        self.assertEqual(fdwprojects.layout_of(lambda p: p == "Directory.Build.props"), "")

    def test_neither_layout_is_an_error(self):
        with self.assertRaisesRegex(SystemExit, "cannot tell the layout.*neither"):
            fdwprojects.layout_of(lambda p: False)

    def test_both_layouts_is_an_error(self):
        with self.assertRaisesRegex(SystemExit, "cannot tell the layout.*both"):
            fdwprojects.layout_of(lambda p: True)

    def test_the_same_projects_are_found_on_both_layouts(self):
        names = [sorted(snapshot(self.repo(shape(base_files())).root).projects) for shape in (lambda f: f, mirror)]
        self.assertEqual(names[0], names[1])
        self.assertIn("C", names[0])

    def test_a_directory_has_the_same_tree_id_on_both_layouts(self):
        trees = [snapshot(self.repo(shape(base_files())).root).trees["C"] for shape in (lambda f: f, mirror)]
        self.assertEqual(trees[0], trees[1])

    def test_a_manifest_made_on_one_layout_is_a_baseline_on_the_other(self):
        internal_root = self.repo(base_files())
        mirror_root = self.repo(mirror(base_files()))
        self.assertEqual(snapshot(internal_root.root).manifest(), snapshot(mirror_root.root).manifest())

    def test_a_leaf_change_gives_the_same_plan_on_both_layouts(self):
        internal_root, mirror_root = self.plans({"public/src/C/C.cs": "class C { int x; }\n"})
        self.assertEqual((internal_root["build_set"], internal_root["reasons"]), (mirror_root["build_set"], mirror_root["reasons"]))
        self.assertEqual(internal_root["build_set"], ["C"])

    def test_a_repo_wide_build_file_change_gives_the_same_plan_on_both_layouts(self):
        internal_root, mirror_root = self.plans({"public/Directory.Packages.props": "<Project><ItemGroup /></Project>\n"})
        self.assertEqual(internal_root["build_set"], mirror_root["build_set"])
        self.assertEqual(len(internal_root["build_set"]), len(ALL))

    def test_a_baseline_from_the_other_layout_gives_no_false_change(self):
        internal_root = self.repo(base_files())
        mirror_root = self.repo(mirror(base_files()))
        result = changeset.plan(snapshot(mirror_root.root), snapshot(internal_root.root).manifest())
        self.assertEqual(result["build_set"], [])

    def test_the_release_plan_is_the_same_on_both_layouts(self):
        held = {n: {"1.4.4": "t"} for n in ALL}
        plans = []
        for shape in (lambda f: f, mirror):
            repo = self.repo(shape(base_files()))
            base = repo.git("rev-parse", "HEAD")
            repo.commit(shape({"public/src/C/C.cs": "class C { int x; }\n"}), "change")
            plans.append(release.release_plan(snapshot(repo.root), changeset.baseline_from_ref(repo.root, base) | {"assemblyVersionFloors": {n: "1.4.1.0" for n in ALL}, "packagesWithoutRuntimeAssembly": []},
                                              FakeFeed(held), [], "1.4", "patch", {}))
        for key in ("versions", "trees", "manifest", "package-versions", "assembly-versions"):
            self.assertEqual(plans[0][key], plans[1][key], key)
        self.assertEqual(plans[0]["plan"]["members"], plans[1]["plan"]["members"])
        self.assertTrue(plans[1]["plan"]["package_ids"])

    def test_buildset_prepares_a_checkout_in_the_mirror_layout(self):
        files = mirror(base_files()) | {"Directory.Packages.props": "<Project>\n</Project>\n"}
        repo = self.repo(files)
        report = buildset.prepare(repo.root, ["B"], {"A": "1.4.4"}, {"B": "tree-B"}, "1.4.5")
        self.assertEqual((report["swapped"], report["run_from"]), ({"A": "A"}, "."))
        with open(os.path.join(repo.root, "src/B/B.csproj")) as f:
            self.assertIn('<PackageReference Include="A" />', f.read())
        with open(os.path.join(repo.root, "Directory.Packages.props")) as f:
            self.assertIn('<PackageVersion Include="A" Version="1.4.4" />', f.read())
        self.assertTrue(os.path.isfile(os.path.join(repo.root, buildset.BUILD_DIR, "trees", "B", "tree.txt")))

    def test_buildset_in_the_repository_layout_runs_from_public(self):
        files = base_files() | {"public/Directory.Packages.props": "<Project>\n</Project>\n"}
        report = buildset.prepare(self.repo(files).root, ["B"], {"A": "1.4.4"}, {"B": "tree-B"}, "1.4.5")
        self.assertEqual(report["run_from"], "public")


if __name__ == "__main__":
    unittest.main()

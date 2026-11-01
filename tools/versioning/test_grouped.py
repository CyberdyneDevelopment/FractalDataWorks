"""The tools on the tree regrouped by domain (FDW-3347): projects under grouping folders are the same projects."""
import os
import tempfile
import unittest

import buildset
import changeset
from fdwprojects import EMBED, snapshot
from test_changeset import Repo, base_files, proj
from test_layout import mirror

# Where each fixture project sits after the regroup: up to two grouping folders, tests that span domains in _cross.
GROUPS = {"Lint": "foundation/analyzers", "A": "foundation/results", "B": "foundation/results", "C": "connections",
          "Gen.SourceGenerators": "foundation/analyzers", "D": "pipelines", "E": "pipelines",
          "A.Tests": "_cross", "C.Tests": "connections"}


def grouped(files):
    """The same files with every project folder moved under its grouping folders. Contents are unchanged."""
    out = {}
    for path, text in files.items():
        parts = path.split("/")
        if len(parts) > 3 and parts[0] == "public" and parts[1] in ("src", "tests") and parts[2] in GROUPS:
            path = "/".join(parts[:2] + GROUPS[parts[2]].split("/") + parts[2:])
        out[path] = text
    return out


def shape(projects):
    return {n: (p.kind, p.package_id, p.packable, sorted(p.refs), sorted(p.analyzer_refs), sorted(p.embeds))
            for n, p in projects.items()}


class GroupedLayoutTests(unittest.TestCase):
    def repo(self, files):
        tmp = tempfile.TemporaryDirectory()
        self.addCleanup(tmp.cleanup)
        repo = Repo(tmp.name)
        repo.commit(files, "base")
        return repo

    def test_grouped_and_flat_read_the_same_projects(self):
        flat = snapshot(self.repo(base_files()).root)
        moved = snapshot(self.repo(grouped(base_files())).root)
        self.assertEqual(shape(flat.projects), shape(moved.projects))
        self.assertEqual(flat.manifest(), moved.manifest())
        self.assertEqual((flat.always_built, flat.build_time), (moved.always_built, moved.build_time))
        self.assertEqual(moved.projects["C"].path, "public/src/connections/C")

    def test_a_cross_domain_test_project_is_read(self):
        moved = snapshot(self.repo(grouped(base_files())).root)
        self.assertEqual(moved.projects["A.Tests"].path, "public/tests/_cross/A.Tests")

    def test_the_mirror_layout_reads_the_grouped_tree_the_same(self):
        internal_root = snapshot(self.repo(grouped(base_files())).root)
        mirror_root = snapshot(self.repo(mirror(grouped(base_files()))).root)
        self.assertEqual(shape(internal_root.projects), shape(mirror_root.projects))
        self.assertEqual(mirror_root.projects["A.Tests"].path, "tests/_cross/A.Tests")

    def test_a_move_without_a_content_change_builds_nothing(self):
        repo = self.repo(base_files())
        before = repo.git("rev-parse", "HEAD")
        repo.write({p: None for p in base_files()})
        repo.commit(grouped(base_files()), "regroup")
        result = changeset.plan(snapshot(repo.root), changeset.baseline_from_ref(repo.root, before))
        self.assertEqual(result["build_set"], [])

    def test_a_project_file_that_cannot_be_placed_stops_the_read(self):
        for stray in ("public/src/Stray.csproj", "public/src/a/b/c/Deep/Deep.csproj", "public/src/Foo/Bar.csproj",
                      "public/tests/x/Name/Other.csproj"):
            with self.subTest(stray=stray):
                repo = self.repo(grouped(base_files()) | {stray: proj()})
                with self.assertRaisesRegex(SystemExit, "cannot place these project files.*" + stray.replace(".", r"\.")):
                    snapshot(repo.root)

    def test_a_project_file_inside_a_project_folder_is_that_projects_data(self):
        fixture = {"public/tests/_cross/A.Tests/_Fixtures/Lib/Lib.csproj": proj(),
                   "public/tests/connections/C.Tests/tests/_Fixtures/Sln/Lib/Lib.csproj": proj()}
        moved = snapshot(self.repo(grouped(base_files()) | fixture).root)
        self.assertNotIn("Lib", moved.projects)
        self.assertIn("A.Tests", moved.projects)

    def test_an_embed_is_found_through_any_number_of_parent_folders(self):
        for include, name in (('..\\X.Tasks\\bin\\Release\\X.Tasks.dll', "X.Tasks"),
                              ('..\\..\\schema\\X.Tasks\\bin\\Release\\X.Tasks.dll', "X.Tasks"),
                              ('../../../foundation/collections/Gen.Registration/bin/$(Configuration)/G.dll', "Gen.Registration")):
            with self.subTest(include=include):
                self.assertEqual(EMBED.findall(f'<None Include="{include}" Pack="true" />'), [name])

    def test_buildset_prepares_the_grouped_tree_with_the_grouped_paths(self):
        files = grouped(base_files()) | {"public/tests/connections/C.Tests/C.Tests.csproj": proj(refs=["C", "B"]),
                                         "public/Directory.Packages.props": "<Project>\n</Project>\n"}
        repo = self.repo(files)
        report = buildset.prepare(repo.root, ["C"], {"B": "1.4.4"}, {"C": "tree-C"}, "1.4.5")
        self.assertEqual(report["swapped"], {"B": "B"})
        packs = [c[2] for c in report["commands"] if c[1] == "pack"]
        tests = [c[2] for c in report["commands"] if c[1] == "test"]
        self.assertEqual((packs, tests), (["src/connections/C/C.csproj"], ["tests/connections/C.Tests/C.Tests.csproj"]))
        with open(os.path.join(repo.root, "public/src/connections/C/C.csproj")) as f:
            self.assertIn('<PackageReference Include="B" />', f.read())

    def test_buildset_refuses_a_project_file_it_cannot_place(self):
        repo = self.repo(grouped(base_files()) | {"public/src/Stray.csproj": proj()})
        with self.assertRaisesRegex(SystemExit, "cannot place these project files"):
            buildset.prepare(repo.root, ["C"], {"B": "1.4.4"}, {"C": "tree-C"}, "1.4.5")


if __name__ == "__main__":
    unittest.main()

import contextlib
import io
import json
import os
import tempfile
import unittest

import releasenotes
from test_changeset import Repo, base_files

REPORT = ["PACKAGE LAST LEVEL NEXT WHY",
          "Fdw.A 1.5.5 major 2.0.0 removed A.Run",
          "Fdw.B 1.5.5 patch 1.5.6 Fdw.A went major",
          "Fdw.C 1.5.5 minor 1.6.0 added C.More",
          "3 package(s): 1 major, 1 minor, 1 patch"]


class NotesTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self._work = tempfile.TemporaryDirectory()
        self.addCleanup(self._work.cleanup)
        self.work = self._work.name
        self.repo = Repo(self._tmp.name)

    def write(self, name, data):
        with open(os.path.join(self.work, name), "w") as f:
            json.dump(data, f)

    def commits(self):
        self.base = self.repo.commit(base_files() | {"public/src/A/A.cs": "class A {}\n"}, "base")
        self.repo.commit({"public/src/A/A.cs": "class A { int x; }\n"}, "FDW-100: change A\n\nalso FDW-7")
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "RUI-5: change C")
        self.baseline = {n: self.repo.git("rev-parse", f"{self.base}:public/src/{n}") for n in ("A", "B", "C")}

    def files(self, floor_a="1.5.1.0", av_a="2.0.0.0", bump="major", stated="patch"):
        rows = {"Fdw.A": {"last": "1.5.5", "level": "major", "next": "2.0.0", "reasons": ["its source changed", "its own surface is major"]},
                "Fdw.B": {"last": "1.5.5", "level": "patch", "next": "1.5.6", "reasons": ["Fdw.A went major", "its own surface is patch"]},
                "Fdw.C": {"last": "1.5.5", "level": "minor", "next": "1.6.0", "reasons": ["its source changed", "its own surface is minor"]}}
        self.write("levels.json", {"packages": rows, "report": REPORT})
        self.write("levels-input.json", {"paths": {"Fdw.A": "public/src/A/A.csproj", "Fdw.B": "public/src/B/B.csproj", "Fdw.C": "public/src/C/C.csproj"},
                                         "names": {"Fdw.A": "A", "Fdw.B": "B", "Fdw.C": "C"}, "assemblies": {"Fdw.A": "Fdw.A", "Fdw.B": "Fdw.B", "Fdw.C": "Fdw.C"}})
        self.write("assembly-versions.json", {"A": av_a, "B": "1.5.1.0", "C": "1.5.1.0"})
        self.write("plan.json", {"assembly_version_floors": {"Fdw.A": floor_a, "Fdw.B": "1.5.1.0", "Fdw.C": "1.5.1.0"}, "version": "2.0.0", "bump": bump, "bump_stated": stated})

    def test_a_new_package_does_not_make_the_release_fail_the_follows_check(self):
        # FDW-5591: a release stated as patch that adds a package is a patch; the notes must not say the release does not follow.
        self.commits()
        self.files(bump="patch", stated="patch")
        with open(os.path.join(self.work, "levels.json")) as f:
            levels = json.load(f)
        levels["packages"] = {"Fdw.A": {"last": "1.5.5", "level": "patch", "next": "1.5.6", "reasons": ["its source changed", "its level is patch"]},
                              "Fdw.B": {"last": "1.5.5", "level": "patch", "next": "1.5.6", "reasons": ["its source changed", "its level is patch"]},
                              "Fdw.C": {"last": None, "level": "new", "next": "1.5.6", "reasons": ["its source changed", "it is a new package", "first version declared in first-versions.json"]}}
        self.write("levels.json", levels)
        with open(os.path.join(self.work, "plan.json")) as f:
            plan = json.load(f)
        self.write("plan.json", plan | {"version": "1.5.6"})
        table, low, number, floors = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline)
        self.assertEqual((number["bump"], number["derived"], number["stated"]), ("patch", "patch", "patch"))
        lines, failed = releasenotes.render(table, low, number, floors)
        self.assertFalse(failed, lines)

    def test_each_package_gets_its_level_reason_and_driving_items(self):
        self.commits()
        self.files()
        table, low, number, floors = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline)
        by = {r["package"]: r for r in table}
        self.assertEqual(by["Fdw.A"]["items"], ["FDW-7", "FDW-100"])
        self.assertEqual(by["Fdw.A"]["why"], "removed A.Run")
        self.assertEqual(by["Fdw.C"]["items"], ["RUI-5"])
        self.assertEqual(by["Fdw.B"]["items"], [])
        self.assertTrue(by["Fdw.B"]["joined"])
        self.assertFalse(by["Fdw.A"]["joined"])
        self.assertEqual([r["package"] for r in table], ["Fdw.A", "Fdw.C", "Fdw.B"])

    def test_the_checks_pass_when_floors_hold_and_the_release_follows(self):
        self.commits()
        self.files()
        lines, failed = releasenotes.render(*releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline))
        self.assertFalse(failed)
        text = "\n".join(lines)
        self.assertIn("none below their floor (3 floors checked)", text)
        self.assertIn("release 2.0.0: bump major, highest package level major, stated patch: follows the highest package", text)
        self.assertIn("1 major, 1 minor, 1 patch", text)

    def test_an_assembly_version_below_its_floor_fails_the_check(self):
        self.commits()
        self.files(floor_a="2.1.0.0")
        lines, failed = releasenotes.render(*releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline))
        self.assertTrue(failed)
        self.assertIn("Fdw.A: AssemblyVersion 2.0.0.0 is below the floor 2.1.0.0", "\n".join(lines))

    def test_a_release_lower_than_the_highest_package_fails_the_check(self):
        self.commits()
        self.files(bump="patch")
        lines, failed = releasenotes.render(*releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline))
        self.assertTrue(failed)
        self.assertIn("DOES NOT follow the highest package", "\n".join(lines))

    def test_a_package_without_a_folder_stops_the_run(self):
        self.commits()
        self.files()
        with open(os.path.join(self.work, "levels-input.json")) as f:
            info = json.load(f)
        info["paths"].pop("Fdw.B")
        self.write("levels-input.json", info)
        with self.assertRaisesRegex(releasenotes.NotesError, "no folder for Fdw.B"):
            releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline)

    def test_a_path_prefix_puts_the_repository_folder_in_front_of_a_mirror_path(self):
        self.commits()
        self.files()
        info_path = os.path.join(self.work, "levels-input.json")
        with open(info_path) as f:
            info = json.load(f)
        info["paths"] = {k: v.removeprefix("public/") for k, v in info["paths"].items()}
        self.write("levels-input.json", info)
        table = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline, "public/")[0]
        self.assertEqual({r["package"]: r["items"] for r in table}["Fdw.A"], ["FDW-7", "FDW-100"])
        with self.assertRaisesRegex(releasenotes.NotesError, "cannot be attributed"):
            releasenotes.build(self.work, self.repo.root, self.base, "HEAD", self.baseline)

    def test_a_package_whose_baseline_state_is_on_no_commit_is_anchored_at_the_fallback_ref(self):
        self.commits()
        self.files()
        baseline = dict(self.baseline, A="0" * 40)
        table = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", baseline, "", [self.base])[0]
        self.assertEqual({r["package"]: r["items"] for r in table}["Fdw.A"], ["FDW-7", "FDW-100"])

    def test_the_fallback_refs_are_tried_in_order(self):
        self.commits()
        self.files()
        baseline = dict(self.baseline, A="0" * 40)
        table = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", baseline, "", ["HEAD", self.base])[0]
        self.assertEqual({r["package"]: r["items"] for r in table}["Fdw.A"], [])
        table = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", baseline, "", ["not-a-ref", self.base])[0]
        self.assertEqual({r["package"]: r["items"] for r in table}["Fdw.A"], ["FDW-7", "FDW-100"])

    def test_a_package_that_matches_no_anchor_stops_the_run(self):
        self.commits()
        self.files()
        baseline = dict(self.baseline, A="0" * 40)
        with self.assertRaisesRegex(releasenotes.NotesError, "cannot be attributed"):
            releasenotes.build(self.work, self.repo.root, self.base, "HEAD", baseline)

    def test_the_items_are_those_since_the_baseline_and_not_every_item_ever_merged(self):
        self.commits()
        self.files()
        self.repo.commit({"public/src/A/A.cs": "class A { int y; }\n"}, "FDW-200: later")
        newer = {n: self.repo.git("rev-parse", f"HEAD~1:public/src/{n}") for n in ("A", "B", "C")}
        table = releasenotes.build(self.work, self.repo.root, self.base, "HEAD", newer)[0]
        self.assertEqual({r["package"]: r["items"] for r in table}["Fdw.A"], ["FDW-200"])

    def test_the_command_prints_the_table(self):
        self.commits()
        self.files()
        baseline = os.path.join(self.work, "baseline.json")
        with open(baseline, "w") as f:
            json.dump({"projects": self.baseline}, f)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            releasenotes.main(["--work", self.work, "--repo", self.repo.root, "--base", self.base, "--baseline", baseline])
        self.assertIn("| Fdw.A | 1.5.5 | 2.0.0 | major | removed A.Run | FDW-7, FDW-100 |", out.getvalue())


if __name__ == "__main__":
    unittest.main()

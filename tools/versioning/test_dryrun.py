import json
import os
import tempfile
import unittest

import dryrun

KEY = {"sha": "abc123", "branch": "main", "line": "1.5", "bump": "patch", "bundle_id": "Fdw.Bundle", "baseline_file": "baseline/release-FDW-1.5.5.json",
       "max_level": "", "levels_max_rounds": "200", "run_tests": "false"}
PLANNED = {"plan.json": {"release": "1.5.6", "members": ["A", "B"]}, "versions.json": {"A": "1.5.5"}, "trees.json": {"A": "t1"},
           "manifest.json": {"id": "Fdw.Bundle", "version": "1.5.6", "packages": {"A": "1.5.6"}}}


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8") as f:
        f.write(text)


def write_json(path, data):
    write(path, json.dumps(data))


class DryRunTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.root = self._tmp.name

    def path(self, *parts):
        return os.path.join(self.root, *parts)

    def dry_run_work(self, decided=None):
        """A release-work folder as the dry run leaves it, and an artifacts folder with two packages and a symbols package."""
        work = self.path("dry", "work")
        for name, data in PLANNED.items():
            write_json(os.path.join(work, "planned", name), data)
            write_json(os.path.join(work, name), (decided or {}).get(name, data))
        for name in dryrun.WORK_FILES:
            if not os.path.exists(os.path.join(work, name)):
                write_json(os.path.join(work, name), {"file": name})
        write(os.path.join(work, "levels-outputs.env"), "members=A,B\nnumber=1.5.6\nversion=1.5.6\n")
        artifacts = self.path("dry", "artifacts")
        for n in ("A.1.5.6.nupkg", "A.1.5.6.snupkg", "B.1.5.7.nupkg"):
            write(os.path.join(artifacts, n), n)
        return work, artifacts

    def key_file(self, name, **over):
        path = self.path(name)
        dryrun.write_key(path, KEY | over)
        return path

    def kept(self, **decided):
        work, artifacts = self.dry_run_work(decided or None)
        out = self.path("kept")
        dryrun.package(self.key_file("dry-key.json"), work, artifacts, out)
        return out

    def real_work(self, planned=None):
        work = self.path("real", "release-work")
        for name, data in (planned or PLANNED).items():
            write_json(os.path.join(work, name), data)
        return work

    def test_a_dry_run_of_the_same_release_is_accepted(self):
        dryrun.verify(self.kept(), self.key_file("real-key.json"), self.real_work())

    def test_every_key_field_must_match(self):
        kept = self.kept()
        work = self.real_work()
        for field, other in (("sha", "def456"), ("branch", "prerelease"), ("line", "1.6"), ("bump", "minor"), ("bundle_id", "Other.Bundle"),
                             ("baseline_file", ""), ("max_level", "minor"), ("levels_max_rounds", "10"), ("run_tests", "true")):
            with self.subTest(field=field):
                with self.assertRaises(dryrun.DryRunError) as raised:
                    dryrun.verify(kept, self.key_file("real-key.json", **{field: other}), work)
                self.assertIn(f"{field}: the dry run had '{KEY[field]}', this run has '{other}'", str(raised.exception))

    def test_all_the_differing_fields_are_listed(self):
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(self.kept(), self.key_file("real-key.json", sha="x", bump="major"), self.real_work())
        self.assertIn("sha:", str(raised.exception))
        self.assertIn("bump:", str(raised.exception))

    def test_a_plan_that_moved_since_the_dry_run_is_refused_and_names_the_file(self):
        moved = PLANNED | {"manifest.json": {"id": "Fdw.Bundle", "version": "1.5.7", "packages": {"A": "1.5.7"}}}
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(self.kept(), self.key_file("real-key.json"), self.real_work(moved))
        self.assertIn("manifest.json differ", str(raised.exception))
        self.assertNotIn("plan.json differ", str(raised.exception))

    def test_the_decided_files_do_not_have_to_equal_the_plan(self):
        decided = {"plan.json": {"release": "1.5.7", "members": ["A", "B", "C"]}}
        dryrun.verify(self.kept(**decided), self.key_file("real-key.json"), self.real_work())

    def test_a_plan_file_this_run_does_not_have_is_refused(self):
        work = self.real_work()
        os.remove(os.path.join(work, "trees.json"))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(self.kept(), self.key_file("real-key.json"), work)
        self.assertIn("trees.json", str(raised.exception))

    def test_an_artifact_without_a_key_is_refused(self):
        kept = self.kept()
        os.remove(os.path.join(kept, dryrun.KEY_FILE))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(kept, self.key_file("real-key.json"), self.real_work())
        self.assertIn("key is missing", str(raised.exception))

    def test_an_artifact_without_a_decided_file_is_refused(self):
        kept = self.kept()
        os.remove(os.path.join(kept, "work", "levels.json"))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(kept, self.key_file("real-key.json"), self.real_work())
        self.assertIn("levels.json", str(raised.exception))

    def test_an_artifact_without_packages_is_refused(self):
        kept = self.kept()
        for n in os.listdir(os.path.join(kept, "artifacts")):
            os.remove(os.path.join(kept, "artifacts", n))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.verify(kept, self.key_file("real-key.json"), self.real_work())
        self.assertIn("no package", str(raised.exception))

    def test_a_key_without_a_field_is_refused(self):
        path = self.path("short.json")
        write_json(path, {"sha": "abc"})
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.read_key(path)
        self.assertIn("branch", str(raised.exception))

    def test_a_key_needs_every_field(self):
        with self.assertRaises(dryrun.DryRunError):
            dryrun.write_key(self.path("k.json"), {"sha": "abc"})

    def test_packaging_with_nothing_packed_is_refused(self):
        work, artifacts = self.dry_run_work()
        for n in os.listdir(artifacts):
            os.remove(os.path.join(artifacts, n))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.package(self.key_file("dry-key.json"), work, artifacts, self.path("kept"))
        self.assertIn("no package", str(raised.exception))

    def test_packaging_without_a_planned_file_is_refused(self):
        work, artifacts = self.dry_run_work()
        os.remove(os.path.join(work, "planned", "plan.json"))
        with self.assertRaises(dryrun.DryRunError) as raised:
            dryrun.package(self.key_file("dry-key.json"), work, artifacts, self.path("kept"))
        self.assertIn("planned plan.json", str(raised.exception))

    def test_packaging_keeps_the_packages_with_their_symbols(self):
        kept = self.kept()
        self.assertEqual(sorted(os.listdir(os.path.join(kept, "artifacts"))), ["A.1.5.6.nupkg", "A.1.5.6.snupkg", "B.1.5.7.nupkg"])

    def test_restore_puts_back_the_decided_files_and_the_packages(self):
        decided = {"plan.json": {"release": "1.5.7", "members": ["A", "B", "C"]}}
        kept = self.kept(**decided)
        work = self.real_work()
        artifacts = self.path("real", "artifacts")
        write(os.path.join(artifacts, "stale.nupkg"), "stale")
        dryrun.restore(kept, work, artifacts)
        with open(os.path.join(work, "plan.json"), encoding="utf-8") as f:
            self.assertEqual(json.load(f)["release"], "1.5.7")
        with open(os.path.join(work, "planned", "plan.json"), encoding="utf-8") as f:
            self.assertEqual(json.load(f)["release"], "1.5.6")
        with open(os.path.join(work, "levels-outputs.env"), encoding="utf-8") as f:
            self.assertIn("version=1.5.6", f.read())
        self.assertEqual(sorted(os.listdir(artifacts)), ["A.1.5.6.nupkg", "A.1.5.6.snupkg", "B.1.5.7.nupkg"])

    def test_the_command_runs_the_whole_round(self):
        work, artifacts = self.dry_run_work()
        key_args = [a for f in dryrun.KEY_FIELDS for a in ("--" + f.replace("_", "-"), KEY[f])]
        dryrun.main(["key", "--out", self.path("k.json"), *key_args])
        dryrun.main(["package", "--key", self.path("k.json"), "--work-dir", work, "--artifacts", artifacts, "--out", self.path("kept")])
        real = self.real_work()
        dryrun.main(["verify", "--dir", self.path("kept"), "--expect", self.path("k.json"), "--work-dir", real])
        dryrun.main(["restore", "--dir", self.path("kept"), "--work-dir", real, "--artifacts", self.path("restored")])
        self.assertEqual(len(os.listdir(self.path("restored"))), 3)

    def test_the_command_exits_with_the_reason(self):
        kept = self.kept()
        with self.assertRaises(SystemExit) as raised:
            dryrun.main(["verify", "--dir", kept, "--expect", self.key_file("real-key.json", sha="zzz"), "--work-dir", self.real_work()])
        self.assertIn("dryrun verify:", str(raised.exception))
        self.assertIn("sha:", str(raised.exception))


if __name__ == "__main__":
    unittest.main()

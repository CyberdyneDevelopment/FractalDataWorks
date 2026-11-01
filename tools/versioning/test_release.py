import contextlib
import io
import json
import os
import tempfile
import unittest
from unittest import mock

import bundle
import changeset
import release
from fdwprojects import snapshot
from test_allocate import FakeFeed
from test_changeset import Repo, base_files

ALL = ("A", "B", "C", "D", "E", "Gen.SourceGenerators")


def feed_at(version="1.4.4", **over):
    held = {n: {version: "t"} for n in ALL}
    held.update(over)
    return FakeFeed(held)


class BaselineFromBundleTests(unittest.TestCase):
    def feed(self, versions, manifest_text=None):
        feed = FakeFeed({"Fdw.Bundle": {v: "t" for v in versions}})
        feed.files = {("Fdw.Bundle", v, "fdw/manifest.json"): manifest_text for v in versions}
        return feed

    def test_the_latest_stable_bundles_manifest_is_the_baseline(self):
        feed = self.feed(["1.4.4", "1.4.5", "1.4.6-rc.1"], '{"projects": {"A": "x"}, "globals": {}}')
        feed.files[("Fdw.Bundle", "1.4.4", "fdw/manifest.json")] = '{"projects": {"A": "old"}, "globals": {}}'
        self.assertEqual(release.baseline_from_bundle(feed, "Fdw.Bundle")["projects"], {"A": "x"})

    def test_no_stable_bundle_stops_and_points_at_the_explicit_baseline(self):
        with self.assertRaisesRegex(release.ReleaseError, "no stable Fdw.Bundle.*--baseline FILE"):
            release.baseline_from_bundle(self.feed(["1.4.6-rc.1"]), "Fdw.Bundle")
        with self.assertRaisesRegex(release.ReleaseError, "no stable Fdw.Bundle"):
            release.baseline_from_bundle(FakeFeed(), "Fdw.Bundle")

    def test_a_bundle_without_a_manifest_stops_and_says_why(self):
        with self.assertRaisesRegex(release.ReleaseError, "1.4.5 has no fdw/manifest.json"):
            release.baseline_from_bundle(self.feed(["1.4.5"], None), "Fdw.Bundle")


class ReleasePlanTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.base = self.repo.commit(base_files(), "base")

    def plan(self, feed=None, tags=(), bump="patch", files=None, bundle_id="Fdw.Bundle"):
        self.repo.commit(files or {"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        return release.release_plan(snapshot(self.repo.root), changeset.baseline_from_ref(self.repo.root, self.base),
                                    feed or feed_at(), list(tags), "1.4", lambda dirs: bump, bundle_id)

    def test_one_package_changed_gives_one_member_and_the_next_number(self):
        p = self.plan()["plan"]
        self.assertEqual((p["release"], p["members"], p["unchanged_packages"]), ("1.4.5", ["C"], len(ALL) - 1))

    def test_unchanged_packages_keep_their_last_stable_version(self):
        feed = feed_at(A={"1.4.1": "t", "1.4.3": "t", "1.4.9-rc.1": "t"}, B={"1.3.0": "t", "1.4.2": "t"})
        r = self.plan(feed)
        self.assertEqual(r["versions"]["A"], "1.4.3")
        self.assertEqual(r["versions"]["B"], "1.4.2")
        self.assertNotIn("C", r["versions"])

    def test_the_number_is_above_every_package_not_only_the_members(self):
        self.assertEqual(self.plan(feed_at(A={"1.4.8": "t"}))["plan"]["release"], "1.4.9")

    def test_the_manifest_pins_members_at_the_release_and_the_rest_at_what_they_shipped(self):
        m = self.plan()["manifest"]
        self.assertEqual((m["id"], m["version"]), ("Fdw.Bundle", "1.4.5"))
        self.assertEqual(m["packages"]["C"], "1.4.5")
        self.assertEqual(m["packages"]["A"], "1.4.4")
        self.assertEqual(sorted(m["packages"]), sorted(ALL))
        self.assertEqual(bundle.check(m), [])

    def test_trees_cover_the_members_only(self):
        r = self.plan()
        self.assertEqual(sorted(r["trees"]), ["C"])
        self.assertEqual(r["trees"]["C"], snapshot(self.repo.root).trees["C"])

    def test_a_package_outside_the_set_with_no_stable_version_stops_the_plan(self):
        with self.assertRaisesRegex(release.ReleaseError, r"no stable version.*'A'"):
            self.plan(feed_at(A={"1.4.4-rc.1": "t"}))

    def test_a_package_never_published_stops_the_plan(self):
        held = {n: {"1.4.4": "t"} for n in ALL if n != "D"}
        with self.assertRaisesRegex(release.ReleaseError, r"'D'"):
            self.plan(FakeFeed(held))

    def test_a_declared_minor_moves_the_release(self):
        self.assertEqual(self.plan(bump="minor")["plan"]["release"], "1.5.0")

    def test_release_tags_spend_numbers(self):
        self.assertEqual(self.plan(tags=["release-FDW-1.4.7"])["plan"]["release"], "1.4.8")

    def test_a_change_to_an_always_built_analyzer_rebuilds_every_package_and_builds_it_from_source_without_packing_it(self):
        r = self.plan(files={"public/src/Lint/L.cs": "class L { int x; }\n"})
        self.assertEqual(r["plan"]["members"], sorted(ALL))
        self.assertEqual(r["plan"]["built_from_source_only"], ["Lint"])
        self.assertEqual(r["versions"], {})

    def test_nothing_changed_gives_an_empty_member_list(self):
        r = self.plan(files={"README.md": "x\n"})
        self.assertEqual((r["plan"]["members"], r["manifest"]["packages"]["C"]), ([], "1.4.4"))

    def test_a_bad_bundle_id_stops_the_plan(self):
        with self.assertRaisesRegex(release.ReleaseError, "valid bundle"):
            self.plan(bundle_id="Fdw Bundle")

    def test_the_command_writes_five_files_and_nothing_else(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-out")
        text = io.StringIO()
        with mock.patch.object(release, "NugetOrg", lambda: feed_at()), contextlib.redirect_stdout(text):
            release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--base-ref", self.base, "--bundle-id", "Fdw.Bundle", "--out", out])
        self.addCleanup(lambda: [os.remove(os.path.join(out, f)) for f in os.listdir(out)] and os.rmdir(out))
        self.assertEqual(sorted(os.listdir(out)), ["manifest.json", "plan.json", "project-manifest.json", "trees.json", "versions.json"])
        with open(os.path.join(out, "plan.json")) as f:
            self.assertEqual(json.load(f)["release"], "1.4.5")
        self.assertIn("release 1.4.5", text.getvalue())

    def command(self, *extra, feed=None):
        if not getattr(self, "_changed", False):
            self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
            self._changed = True
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-cmd")
        self.addCleanup(lambda: os.path.isdir(out) and ([os.remove(os.path.join(out, f)) for f in os.listdir(out)], os.rmdir(out)))
        text = io.StringIO()
        with mock.patch.object(release, "NugetOrg", lambda: feed or feed_at()), contextlib.redirect_stdout(text):
            release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--bundle-id", "Fdw.Bundle", "--out", out, *extra])
        return out, text.getvalue()

    def test_a_baseline_file_needs_an_explicit_bump_and_never_assumes_one(self):
        baseline = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-base.json")
        with open(baseline, "w") as f:
            json.dump(changeset.baseline_from_ref(self.repo.root, self.base), f)
        self.addCleanup(os.remove, baseline)
        with self.assertRaises(SystemExit):
            self.command("--baseline", baseline)  # no --bump: argparse refuses
        out, text = self.command("--baseline", baseline, "--bump", "minor")
        self.assertIn("release 1.5.0 (minor)", text)

    def test_a_bump_with_a_git_ref_baseline_is_refused(self):
        with self.assertRaises(SystemExit):
            self.command("--base-ref", self.base, "--bump", "minor")

    def test_the_baseline_comes_from_the_bundle_on_the_feed(self):
        feed = feed_at()
        feed.held["Fdw.Bundle"] = {"1.4.4": "t"}
        feed.files = {("Fdw.Bundle", "1.4.4", "fdw/manifest.json"): json.dumps(changeset.baseline_from_ref(self.repo.root, self.base))}
        out, text = self.command("--baseline-from-bundle", "--bump", "patch", feed=feed)
        self.assertIn("release 1.4.5 (patch)", text)
        self.assertIn("1 packages to build and push", text)
        with open(os.path.join(out, "project-manifest.json")) as f:
            self.assertEqual(sorted(json.load(f)), ["globals", "projects"])

    def test_exactly_one_baseline_is_required(self):
        with self.assertRaises(SystemExit):
            self.command()
        with self.assertRaises(SystemExit):
            self.command("--base-ref", self.base, "--baseline-from-bundle")

    def test_the_command_writes_nothing_when_it_refuses(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-none")
        with mock.patch.object(release, "NugetOrg", lambda: feed_at(A={"1.4.4-rc.1": "t"})):
            with self.assertRaises(SystemExit) as raised:
                release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--base-ref", self.base, "--bundle-id", "Fdw.Bundle", "--out", out])
        self.assertIn("nothing was written", str(raised.exception))
        self.assertFalse(os.path.exists(out))


if __name__ == "__main__":
    unittest.main()

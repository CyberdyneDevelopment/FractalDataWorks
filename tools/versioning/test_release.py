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
from test_allocate import UNREADABLE, FakeFeed
from test_changeset import Repo, base_files, proj

ALL = ("A", "B", "C", "D", "E", "Gen.SourceGenerators")
FLOORS = {n: "1.4.1.0" for n in ALL}


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

    def floors_file(self):
        path = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-floors.json")
        with open(path, "w") as f:
            json.dump({"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []}, f)
        self.addCleanup(lambda: os.path.exists(path) and os.remove(path))
        return path

    def plan(self, feed=None, tags=(), bump="patch", files=None, bundle_id="Fdw.Bundle"):
        self.repo.commit(files or {"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        return release.release_plan(snapshot(self.repo.root), changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []},
                                    feed or feed_at(), list(tags), "1.4", lambda dirs: bump, bundle_id)

    def test_one_package_changed_gives_one_member_and_the_next_number(self):
        p = self.plan()["plan"]
        self.assertEqual((p["release"], p["members"], p["unchanged_packages"]), ("1.4.5", ["C"], len(ALL) - 1))

    def test_the_plan_carries_the_floors_the_baseline_recorded(self):
        self.assertEqual(self.plan()["plan"]["assembly_version_floors"], FLOORS)

    def test_a_baseline_without_floors_stops_the_plan(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_FLOORS_MISSING"):
            release.release_plan(snapshot(self.repo.root), changeset.baseline_from_ref(self.repo.root, self.base), feed_at(), [], "1.4", lambda dirs: "patch", "Fdw.Bundle")

    def test_a_baseline_without_the_list_of_packages_that_carry_no_runtime_assembly_stops_the_plan(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        baseline = changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS}
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_BARE_MISSING"):
            release.release_plan(snapshot(self.repo.root), baseline, feed_at(), [], "1.4", lambda dirs: "patch", "Fdw.Bundle")

    def test_a_floor_that_is_not_x_y_z_w_stops_the_plan(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        baseline = changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": {"A": "1.4.1"}, "packagesWithoutRuntimeAssembly": []}
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_FLOORS_INVALID"):
            release.release_plan(snapshot(self.repo.root), baseline, feed_at(), [], "1.4", lambda dirs: "patch", "Fdw.Bundle")

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

    def replan(self, feed):
        """The plan again, on the same tree, against a feed that now holds what an earlier run pushed."""
        return release.release_plan(snapshot(self.repo.root), changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []},
                                    feed, [], "1.4", lambda dirs: "patch", "Fdw.Bundle")

    PLAN_FILES = ("plan", "versions", "trees", "manifest", "levels-input")

    def test_a_rerun_after_every_member_was_pushed_makes_the_same_plan(self):
        first = self.plan()
        tree = snapshot(self.repo.root).trees["C"]
        again = self.replan(feed_at(C={"1.4.4": "t", "1.4.5": tree}))
        for key in self.PLAN_FILES:
            self.assertEqual(again[key], first[key], key)
        self.assertEqual((again["plan"]["release"], again["levels-input"]["last_stable"]["C"]), ("1.4.5", "1.4.4"))

    def test_a_rerun_after_some_members_were_pushed_makes_the_same_plan(self):
        first = self.plan(files={"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})
        self.assertEqual(first["plan"]["members"], ["D", "E", "Gen.SourceGenerators"])
        trees = snapshot(self.repo.root).trees
        again = self.replan(feed_at(D={"1.4.4": "t", "1.4.5": trees["D"]}, **{"Gen.SourceGenerators": {"1.4.4": "t", "1.4.5": trees["Gen.SourceGenerators"]}}))
        for key in self.PLAN_FILES:
            self.assertEqual(again[key], first[key], key)

    def test_a_held_version_built_from_another_tree_is_not_this_releases_own(self):
        self.plan()
        again = self.replan(feed_at(C={"1.4.4": "t", "1.4.5": "some other tree"}))
        self.assertEqual(again["plan"]["release"], "1.4.6")

    def test_a_held_version_with_no_tree_marker_is_not_shown_to_be_its_own(self):
        self.plan()
        again = self.replan(feed_at(C={"1.4.4": "t", "1.4.5": None}))
        self.assertEqual(again["plan"]["release"], "1.4.6")

    def test_only_the_highest_stable_version_can_be_the_releases_own(self):
        self.plan()
        tree = snapshot(self.repo.root).trees["C"]
        again = self.replan(feed_at(C={"1.4.3": tree, "1.4.4": "t", "1.4.5-rc.1": tree}))
        self.assertEqual((again["plan"]["release"], again["levels-input"]["last_stable"]["C"]), ("1.4.5", "1.4.4"))

    def test_without_own_versions_drops_only_the_matching_highest_version(self):
        feed = FakeFeed({"A": {"1.0.0": "t0", "1.0.1": "t1"}, "B": {"1.0.0": "t0", "1.0.2": "other"}, "C": {"1.0.0": "t0"}})
        held = {p: feed.versions(p) for p in ("A", "B", "C")}
        self.assertEqual(release.without_own_versions(held, {"A": "t1", "B": "t1", "C": "t1"}, feed),
                         {"A": ["1.0.0"], "B": ["1.0.0", "1.0.2"], "C": ["1.0.0"]})

    def test_a_package_at_the_resumed_version_with_another_tree_stops_the_plan(self):
        feed = FakeFeed({"A": {"1.0.0": "t0", "1.0.1": "t1"}, "B": {"1.0.0": "t0", "1.0.1": "other"}})
        held = {p: feed.versions(p) for p in ("A", "B")}
        with self.assertRaisesRegex(release.ReleaseError, r"RESUME_CONFLICT.*\(1\.0\.1\).*B 1\.0\.1: built from tree other, not t1"):
            release.without_own_versions(held, {"A": "t1", "B": "t1"}, feed)

    def test_a_package_at_the_resumed_version_with_no_marker_stops_the_plan(self):
        feed = FakeFeed({"A": {"1.0.0": "t0", "1.0.1": "t1"}, "B": {"1.0.0": "t0", "1.0.1": None}})
        held = {p: feed.versions(p) for p in ("A", "B")}
        with self.assertRaisesRegex(release.ReleaseError, r"RESUME_CONFLICT.*B 1\.0\.1: no tree marker"):
            release.without_own_versions(held, {"A": "t1", "B": "t1"}, feed)

    def test_a_listed_version_that_cannot_be_read_stops_the_plan(self):
        feed = FakeFeed({"A": {"1.0.0": "t0", "1.0.1": UNREADABLE}})
        with self.assertRaisesRegex(release.ReleaseError, "A 1.0.1 but its package cannot be read"):
            release.without_own_versions({"A": feed.versions("A")}, {"A": "t1"}, feed)

    def test_without_a_resume_an_old_package_with_no_marker_is_not_an_error(self):
        feed = FakeFeed({"A": {"1.0.0": None}, "B": {"1.0.0": "t0"}})
        held = {p: feed.versions(p) for p in ("A", "B")}
        self.assertEqual(release.without_own_versions(held, {"A": "t1", "B": "t1"}, feed), held)

    def test_a_rerun_with_one_member_changed_since_the_push_stops_instead_of_renumbering(self):
        first = self.plan(files={"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})
        trees = snapshot(self.repo.root).trees
        with self.assertRaisesRegex(release.ReleaseError, "RESUME_CONFLICT.*E 1.4.5"):
            self.replan(feed_at(D={"1.4.4": "t", "1.4.5": trees["D"]}, E={"1.4.4": "t", "1.4.5": "another tree"}))

    def test_a_rerun_against_an_unreadable_listed_version_stops(self):
        self.plan()
        with self.assertRaisesRegex(release.ReleaseError, "cannot be read"):
            self.replan(feed_at(C={"1.4.4": "t", "1.4.5": UNREADABLE}))

    def test_a_package_the_release_does_not_change_keeps_every_version(self):
        feed = FakeFeed({"A": {"1.0.0": "t0", "1.0.1": "t1"}})
        held = {"A": feed.versions("A")}
        self.assertEqual(release.without_own_versions(held, {}, feed), held)

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

    def test_the_command_writes_six_files_and_nothing_else(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-out")
        text = io.StringIO()
        with mock.patch.object(release, "NugetOrg", lambda: feed_at()), contextlib.redirect_stdout(text):
            release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--base-ref", self.base, "--assembly-info", self.floors_file(), "--bundle-id", "Fdw.Bundle", "--out", out])
        self.addCleanup(lambda: [os.remove(os.path.join(out, f)) for f in os.listdir(out)] and os.rmdir(out))
        self.assertEqual(sorted(os.listdir(out)), ["levels-input.json", "manifest.json", "plan.json", "project-manifest.json", "trees.json", "versions.json"])
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
            json.dump(changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []}, f)
        self.addCleanup(os.remove, baseline)
        with self.assertRaises(SystemExit):
            self.command("--baseline", baseline)  # no --bump: argparse refuses
        out, text = self.command("--baseline", baseline, "--bump", "minor")
        self.assertIn("release 1.5.0 (minor)", text)

    def test_a_bump_with_a_git_ref_baseline_is_refused(self):
        with self.assertRaises(SystemExit):
            self.command("--base-ref", self.base, "--assembly-info", self.floors_file(), "--bump", "minor")

    def test_the_baseline_comes_from_the_bundle_on_the_feed(self):
        feed = feed_at()
        feed.held["Fdw.Bundle"] = {"1.4.4": "t"}
        feed.files = {("Fdw.Bundle", "1.4.4", "fdw/manifest.json"): json.dumps(changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []})}
        out, text = self.command("--baseline-from-bundle", "--bump", "patch", feed=feed)
        self.assertIn("release 1.4.5 (patch)", text)
        self.assertIn("1 packages to build and push", text)
        with open(os.path.join(out, "project-manifest.json")) as f:
            self.assertEqual(sorted(json.load(f)), ["globals", "projects"])

    def test_exactly_one_baseline_is_required(self):
        with self.assertRaises(SystemExit):
            self.command()
        with self.assertRaises(SystemExit):
            self.command("--base-ref", self.base, "--assembly-info", self.floors_file(), "--baseline-from-bundle")

    def test_the_command_writes_nothing_when_it_refuses(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-none")
        with mock.patch.object(release, "NugetOrg", lambda: feed_at(A={"1.4.4-rc.1": "t"})):
            with self.assertRaises(SystemExit) as raised:
                release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--base-ref", self.base, "--assembly-info", self.floors_file(), "--bundle-id", "Fdw.Bundle", "--out", out])
        self.assertIn("nothing was written", str(raised.exception))
        self.assertFalse(os.path.exists(out))


if __name__ == "__main__":
    unittest.main()


TAB = "\t"
SETTINGS = {"mustSupplyAdditionLevel": "major"}


def surface(*members):
    rows = [("net10.0", "TYPE", "Ns.T", "class"), ("net10.0", "BASE", "Ns.T", "System.Object")]
    rows += [("net10.0", "MEMBER", f"Ns.T::{m}()", "method public -> System.Void") for m in members]
    return "\n".join(TAB.join(r) for r in rows) + "\n"


class DecideTests(unittest.TestCase):
    """C -> B -> A, E -> D, D is built with the generator Gen.SourceGenerators; every package last shipped 1.4.4."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.base = self.repo.commit(base_files(), "base")

    def planned(self, files, bump="patch"):
        self.repo.commit(files, "change")
        r = release.release_plan(snapshot(self.repo.root), changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []},
                                 feed_at(), [], "1.4", lambda dirs: bump, "Fdw.Bundle")
        return {k: r[k] for k in ("plan", "versions", "trees", "manifest", "levels-input")}

    def decide(self, files, old, new, first_versions=None):
        return release.decide_release(self.planned(files), old.get, new.__getitem__, first_versions or {}, SETTINGS)

    def test_plan_states_the_graph_the_last_stable_versions_and_the_generators(self):
        info = self.planned({"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})["levels-input"]
        self.assertEqual(info["depends"]["C"], ["B"])
        self.assertEqual(info["depends"]["A"], [])
        self.assertEqual(info["members"], ["D", "E", "Gen.SourceGenerators"])
        self.assertEqual(info["generators"], {"Gen.SourceGenerators": ["D", "E"]})
        self.assertEqual(info["last_stable"]["A"], "1.4.4")
        self.assertEqual(info["names"]["C"], "C")
        self.assertEqual(info["paths"]["C"], "public/src/C/C.csproj")

    def test_a_change_with_no_surface_change_is_a_patch_of_its_own_last_stable_version(self):
        r = self.decide({"public/src/C/C.cs": "class C { int x; }\n"}, {"C": surface("Run")}, {"C": surface("Run")})
        self.assertEqual(r["package-versions"], {"C": "1.4.5"})
        self.assertEqual(r["manifest"]["packages"]["C"], "1.4.5")
        self.assertEqual(r["manifest"]["packages"]["A"], "1.4.4")
        self.assertEqual(r["manifest"]["version"], "1.4.5")
        self.assertEqual(sorted(r["versions"]), sorted(set(ALL) - {"C"}))
        self.assertEqual(r["plan"]["members"], ["C"])
        self.assertEqual(r["trees"], {"C": snapshot(self.repo.root).trees["C"]})
        self.assertEqual(bundle.check(r["manifest"]), [])

    def test_an_added_member_is_a_minor_and_a_removed_one_a_major_that_pulls_in_its_dependents(self):
        old = {"A": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}
        new = {"A": surface("Run"), "B": surface("X"), "C": surface("Y")}
        r = self.decide({"public/src/A/A.cs": "class A { int x; }\n"}, old, new)
        self.assertEqual(r["package-versions"], {"A": "2.0.0", "B": "1.4.5"})
        self.assertEqual(r["plan"]["pulled_in"], ["B"])
        self.assertEqual(r["manifest"]["packages"]["C"], "1.4.4")
        self.assertIn("A went major", " ".join(r["plan"]["reasons"]["B"]))
        self.assertEqual(r["levels"]["packages"]["A"]["level"], "major")

    def test_a_major_walks_up_the_chain_and_a_pulled_in_package_brings_its_tests(self):
        self.repo.commit({"public/tests/B.Tests/B.Tests.csproj": proj(refs=["B"])}, "tests for B")
        self.base = self.repo.git("rev-parse", "HEAD").strip()
        old = {"A": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}
        new = {"A": surface("Run"), "B": surface("X", "Z"), "C": surface("Y")}
        r = self.decide({"public/src/A/A.cs": "class A { int x; }\n"}, old, new)
        self.assertEqual(r["package-versions"], {"A": "2.0.0", "B": "1.5.0"})
        self.assertIn("B.Tests", r["plan"]["tests"])

    def test_a_changed_generator_takes_the_highest_level_of_the_consumers_it_caused(self):
        old = {"D": surface("P"), "E": surface("Q")}
        new = {"D": surface("P", "Added"), "E": surface("Q")}
        r = self.decide({"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"}, old, new)
        self.assertEqual(r["package-versions"]["D"], "1.5.0")
        self.assertEqual(r["package-versions"]["E"], "1.4.5")
        self.assertEqual(r["package-versions"]["Gen.SourceGenerators"], "1.5.0")

    def test_a_missing_surface_stops_the_run_and_names_the_package(self):
        with self.assertRaisesRegex(release.SurfaceMissing, "no old surface for C"):
            release.decide_release(self.planned({"public/src/C/C.cs": "class C { int x; }\n"}), release.surface_reader(self._tmp.name, "old"),
                                   release.surface_reader(self._tmp.name, "new"), {}, SETTINGS)

    def test_a_package_that_has_no_last_stable_version_needs_its_declared_first_version(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["levels-input"]["last_stable"].pop("C")
        with self.assertRaisesRegex(release.ReleaseError, "FIRST_VERSION_NOT_DECLARED"):
            release.decide_release(files, {}.get, {}.get, {}, SETTINGS)
        r = release.decide_release(files, {}.get, {}.get, {"C": {"version": "1.0.0", "replaces": []}}, SETTINGS)
        self.assertEqual(r["package-versions"], {"C": "1.0.0"})

    def test_a_package_a_declared_entry_replaces_leaves_the_manifest(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["versions"]["Old.Id"] = "1.2.0"
        files["manifest"]["packages"]["Old.Id"] = "1.2.0"
        files["levels-input"]["last_stable"]["Old.Id"] = "1.2.0"
        files["levels-input"]["last_stable"].pop("C")
        r = release.decide_release(files, {}.get, {}.get, {"C": {"version": "1.0.0", "replaces": ["Old.Id"]}}, SETTINGS)
        self.assertNotIn("Old.Id", r["manifest"]["packages"])
        self.assertNotIn("Old.Id", r["versions"])
        self.assertEqual(r["plan"]["retired"], ["Old.Id"])

    def test_a_candidate_gives_every_package_the_rc_suffix_and_leaves_the_bundle_alone(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        r = release.decide_release(files, {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS, candidate=3)
        self.assertEqual(r["package-versions"], {"C": "1.4.5-rc.3"})
        self.assertEqual(r["manifest"]["packages"]["C"], "1.4.5-rc.3")
        self.assertEqual(r["manifest"]["packages"]["A"], "1.4.4")
        self.assertEqual(r["levels"]["packages"]["C"]["next"], "1.4.5")

    def test_the_command_exits_3_and_names_the_missing_surface_for_the_job(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        base = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name))
        dirs = {n: f"{base}-m-{n}" for n in ("plan", "old", "new")}
        for d in dirs.values():
            os.makedirs(d)
            self.addCleanup(lambda d=d: [os.remove(os.path.join(d, f)) for f in os.listdir(d)] and os.rmdir(d) or (os.path.isdir(d) and os.rmdir(d)))
        release.write(dirs["plan"], files)
        with open(os.path.join(dirs["old"], "C.surface"), "w", encoding="utf-8", newline="") as f:
            f.write(surface("Run"))
        extra = {}
        for name, data in (("first-versions", {"firstVersions": {}}), ("settings", SETTINGS)):
            extra[name] = f"{base}-m-{name}.json"
            with open(extra[name], "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, extra[name])
        missing = f"{base}-m-missing.json"
        self.addCleanup(lambda: os.path.exists(missing) and os.remove(missing))
        with self.assertRaises(SystemExit) as raised, contextlib.redirect_stderr(io.StringIO()):
            release.main(["decide", "--plan-dir", dirs["plan"], "--old-surfaces", dirs["old"], "--new-surfaces", dirs["new"],
                          "--first-versions", extra["first-versions"], "--settings", extra["settings"], "--out", f"{base}-m-out", "--missing-out", missing])
        self.assertEqual(raised.exception.code, release.MISSING_SURFACE_EXIT)
        with open(missing) as f:
            self.assertEqual(json.load(f), {"package": "C", "side": "new"})
        self.assertFalse(os.path.exists(f"{base}-m-out"))

    def test_the_plan_allocates_a_bundle_number_for_each_bump(self):
        p = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})["plan"]
        self.assertEqual(p["release_numbers"], {"patch": "1.4.5", "minor": "1.5.0", "major": "2.0.0"})
        self.assertEqual(p["release"], "1.4.5")

    def test_a_major_package_makes_the_bundle_a_major(self):
        old = {"A": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}
        new = {"A": surface("Run"), "B": surface("X"), "C": surface("Y")}
        r = self.decide({"public/src/A/A.cs": "class A { int x; }\n"}, old, new)
        self.assertEqual((r["plan"]["bump"], r["plan"]["bump_stated"], r["plan"]["release"], r["plan"]["version"]), ("major", "patch", "2.0.0", "2.0.0"))
        self.assertEqual((r["manifest"]["version"], r["manifest"]["packages"]["A"]), ("2.0.0", "2.0.0"))

    def test_a_minor_package_makes_the_bundle_a_minor_and_all_patches_leave_it_a_patch(self):
        r = self.decide({"public/src/C/C.cs": "class C { int x; }\n"}, {"C": surface("Run")}, {"C": surface("Run", "More")})
        self.assertEqual((r["plan"]["bump"], r["manifest"]["version"]), ("minor", "1.5.0"))
        r = self.decide({"public/src/C/C.cs": "class C { int y; }\n"}, {"C": surface("Run")}, {"C": surface("Run")})
        self.assertEqual((r["plan"]["bump"], r["manifest"]["version"]), ("patch", "1.4.5"))

    def test_the_stated_bump_is_the_least_the_bundle_can_be(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"}, bump="minor")
        r = release.decide_release(files, {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS)
        self.assertEqual((r["plan"]["bump"], r["plan"]["bump_stated"], r["manifest"]["version"]), ("minor", "minor", "1.5.0"))

    def test_a_new_package_counts_as_a_minor_for_the_bundle(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["levels-input"]["last_stable"].pop("C")
        files["plan"]["assembly_version_floors"].pop("C")
        r = release.decide_release(files, {}.get, {}.get, {"C": {"version": "1.0.0", "replaces": []}}, SETTINGS)
        self.assertEqual((r["plan"]["bump"], r["manifest"]["version"]), ("minor", "1.5.0"))

    def test_a_candidate_makes_the_bundle_n_rc_k_too(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        r = release.decide_release(files, {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS, candidate=3)
        self.assertEqual((r["manifest"]["version"], r["plan"]["version"], r["plan"]["release"]), ("1.4.5-rc.3", "1.4.5-rc.3", "1.4.5"))

    def test_a_level_cap_ships_a_breaking_change_as_a_patch_and_the_bundle_follows(self):
        old = {"A": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}
        new = {"A": surface("Run"), "B": surface("X"), "C": surface("Y")}
        r = release.decide_release(self.planned({"public/src/A/A.cs": "class A { int x; }\n"}), old.get, new.__getitem__, {}, SETTINGS, cap="patch")
        self.assertEqual(r["package-versions"], {"A": "1.4.5"})
        self.assertEqual(r["levels"]["packages"]["A"]["next"], "1.4.5")
        self.assertEqual(r["levels"]["packages"]["A"]["capped_from"], "major")
        self.assertEqual((r["plan"]["bump"], r["plan"]["version"], r["plan"]["level_cap"]), ("patch", "1.4.5", "patch"))
        self.assertEqual(r["assembly-versions"]["A"], "1.4.1.0")
        self.assertIn("[surface: major, capped at patch]", "\n".join(r["levels"]["report"]))

    def test_without_a_cap_the_plan_says_there_was_none(self):
        r = release.decide_release(self.planned({"public/src/C/C.cs": "class C { int x; }\n"}), {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS)
        self.assertIsNone(r["plan"]["level_cap"])

    def test_assembly_versions_follow_each_assemblys_own_floor_and_a_major_from_two(self):
        for version, floor, expected in (("1.4.5", "1.4.1.0", "1.4.1.0"), ("1.9.0", "1.4.1.0", "1.4.1.0"), ("2.0.0", "1.4.1.0", "2.0.0.0"),
                                         ("2.3.1", "2.0.0.0", "2.0.0.0"), ("1.0.0", None, "1.0.0.0"), ("10.1.0", "1.5.4.0", "10.0.0.0"), ("1.5.5", "1.5.4.0", "1.5.4.0")):
            self.assertEqual(release.assembly_version_of(version, floor), expected, (version, floor))

    def test_a_patch_keeps_the_floor_of_its_assembly_and_a_major_in_one_package_restamps_no_other(self):
        old = {"A": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}
        new = {"A": surface("Run"), "B": surface("X"), "C": surface("Y")}
        r = self.decide({"public/src/A/A.cs": "class A { int x; }\n"}, old, new)
        self.assertEqual(r["assembly-versions"], {"A": "2.0.0.0", "B": "1.4.1.0"})

    def test_a_generator_the_baseline_lists_as_carrying_no_runtime_assembly_needs_no_floor(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["plan"]["assembly_version_floors"].pop("C")
        files["plan"]["packages_without_runtime_assembly"].append("C")
        r = release.decide_release(files, {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS)
        self.assertEqual(r["assembly-versions"], {"C": "1.0.0.0"})

    def test_the_plan_names_the_assembly_of_every_package(self):
        info = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})["levels-input"]
        self.assertEqual(info["assemblies"]["Gen.SourceGenerators"], "Gen.SourceGenerators")
        self.assertEqual(info["assemblies"]["C"], "C")

    def test_an_existing_package_whose_assembly_has_no_recorded_floor_stops_the_run(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["plan"]["assembly_version_floors"].pop("C")
        with self.assertRaisesRegex(release.ReleaseError, "ASSEMBLY_FLOOR_MISSING"):
            release.decide_release(files, {"C": surface("Run")}.get, {"C": surface("Run")}.__getitem__, {}, SETTINGS)

    def test_a_new_package_has_no_floor_and_gets_its_own_major(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        files["levels-input"]["last_stable"].pop("C")
        files["plan"]["assembly_version_floors"].pop("C")
        r = release.decide_release(files, {}.get, {}.get, {"C": {"version": "1.0.0", "replaces": []}}, SETTINGS)
        self.assertEqual(r["assembly-versions"], {"C": "1.0.0.0"})

    def test_the_command_reads_a_plan_dir_and_writes_the_decided_files(self):
        files = self.planned({"public/src/C/C.cs": "class C { int x; }\n"})
        base = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name))
        dirs = {n: f"{base}-{n}" for n in ("plan", "old", "new", "out")}
        for d in dirs.values():
            os.makedirs(d)
            self.addCleanup(lambda d=d: [os.remove(os.path.join(d, f)) for f in os.listdir(d)] and os.rmdir(d) or (os.path.isdir(d) and os.rmdir(d)))
        release.write(dirs["plan"], files)
        for d in ("old", "new"):
            with open(os.path.join(dirs[d], "C.surface"), "w", encoding="utf-8", newline="") as f:
                f.write(surface("Run"))
        extra = {}
        for name, data in (("first-versions", {"firstVersions": {}}), ("settings", SETTINGS)):
            extra[name] = f"{base}-{name}.json"
            with open(extra[name], "w") as f:
                json.dump(data, f)
            self.addCleanup(os.remove, extra[name])
        text = io.StringIO()
        with contextlib.redirect_stdout(text):
            release.main(["decide", "--plan-dir", dirs["plan"], "--old-surfaces", dirs["old"], "--new-surfaces", dirs["new"],
                          "--first-versions", extra["first-versions"], "--settings", extra["settings"], "--out", dirs["out"]])
        with open(os.path.join(dirs["out"], "package-versions.json")) as f:
            self.assertEqual(json.load(f), {"C": "1.4.5"})
        self.assertIn("1 package(s)", text.getvalue())
        with open(os.path.join(dirs["out"], "levels-input.json")) as f:
            self.assertIn("depends", json.load(f))

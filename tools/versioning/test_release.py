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


class BaselinePinsTests(unittest.TestCase):
    def feed(self, not_pinned_text):
        feed = FakeFeed({"Fdw.Bundle": {"1.4.4": "t", "1.4.5-rc.1": "t"}})
        feed.pins_of = {("Fdw.Bundle", "1.4.4"): {"A": "1.4.4", "B": "1.4.3"}}
        feed.files = {("Fdw.Bundle", "1.4.4", "fdw/not-pinned.json"): not_pinned_text}
        return feed

    def test_the_versions_of_the_packages_the_bundle_does_not_pin_come_from_its_not_pinned_file(self):
        feed = self.feed('{"Tool": {"types": ["DotnetTool"], "version": "1.4.2"}}')
        self.assertEqual(release.baseline_pins(feed, "Fdw.Bundle"), {"A": "1.4.4", "B": "1.4.3", "Tool": "1.4.2"})

    def test_a_bundle_that_left_no_package_out_of_its_pins_has_no_not_pinned_file(self):
        self.assertEqual(release.baseline_pins(self.feed(None), "Fdw.Bundle"), {"A": "1.4.4", "B": "1.4.3"})


def snapshot_trees(repo, name):
    """The git tree id of a project folder at HEAD, as the plan reads it (the tree a package built from it carries)."""
    return snapshot(repo.root).trees[name]


class ReleasePlanTests(unittest.TestCase):
    """C -> B -> A, E -> D, D is built with the generator Gen.SourceGenerators; every package last shipped 1.4.4."""

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.repo = Repo(self._tmp.name)
        self.base = self.repo.commit(base_files(), "base")

    def tmp_path(self, suffix):
        path = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + suffix)
        self.addCleanup(lambda: os.path.isfile(path) and os.remove(path))
        return path

    def floors_file(self):
        path = self.tmp_path("-floors.json")
        with open(path, "w") as f:
            json.dump({"assemblyVersionFloors": FLOORS, "packagesWithoutRuntimeAssembly": []}, f)
        return path

    def first_versions_file(self, declared=None):
        path = self.tmp_path("-first.json")
        with open(path, "w") as f:
            json.dump({"firstVersions": declared or {}}, f)
        return path

    def baseline(self, **over):
        return changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": dict(FLOORS), "packagesWithoutRuntimeAssembly": []} | over

    def plan(self, feed=None, tags=(), bump="patch", files=None, bundle_id="Fdw.Bundle", first_versions=None, prerelease=False, baseline=None):
        self.repo.commit(files or {"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        return release.release_plan(snapshot(self.repo.root), baseline or self.baseline(), feed or feed_at(), list(tags), "1.4", bump, bundle_id,
                                    first_versions or {}, prerelease)

    def test_one_package_changed_gives_one_member_and_the_next_number(self):
        p = self.plan()["plan"]
        self.assertEqual((p["release"], p["members"], p["unchanged_packages"]), ("1.4.5", ["C"], len(ALL) - 1))

    def test_a_change_to_a_dependency_gives_every_package_above_it_a_new_version(self):
        r = self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})
        self.assertEqual(r["plan"]["members"], ["A", "B", "C"])
        self.assertEqual(r["package-versions"], {"A": "1.4.5", "B": "1.4.5", "C": "1.4.5"})
        self.assertIn("A changed, and C is built from it", r["plan"]["reasons"]["C"])
        self.assertEqual(sorted(r["versions"]), ["D", "E", "Gen.SourceGenerators"])

    def test_a_package_whose_closure_did_not_change_keeps_its_version_and_is_not_pushed(self):
        r = self.plan(files={"public/src/C/C.cs": "class C { int x; }\n"})
        self.assertNotIn("A", r["package-versions"])
        self.assertEqual(r["manifest"]["packages"]["A"], "1.4.4")
        self.assertEqual(r["versions"]["A"], "1.4.4")

    def test_the_plan_carries_the_floors_the_baseline_recorded(self):
        self.assertEqual(self.plan()["plan"]["assembly_version_floors"], FLOORS)

    def test_a_baseline_without_floors_stops_the_plan(self):
        baseline = changeset.baseline_from_ref(self.repo.root, self.base)
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_FLOORS_MISSING"):
            self.plan(baseline=baseline)

    def test_a_baseline_without_the_list_of_packages_that_carry_no_runtime_assembly_stops_the_plan(self):
        baseline = changeset.baseline_from_ref(self.repo.root, self.base) | {"assemblyVersionFloors": FLOORS}
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_BARE_MISSING"):
            self.plan(baseline=baseline)

    def test_a_floor_that_is_not_x_y_z_w_stops_the_plan(self):
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_FLOORS_INVALID"):
            self.plan(baseline=self.baseline(assemblyVersionFloors={"A": "1.4.1"}))

    def test_unchanged_packages_keep_their_last_stable_version(self):
        feed = feed_at(A={"1.4.1": "t", "1.4.3": "t", "1.4.9-rc.1": "t"}, B={"1.3.0": "t", "1.4.2": "t"})
        r = self.plan(feed)
        self.assertEqual(r["versions"]["A"], "1.4.3")
        self.assertEqual(r["versions"]["B"], "1.4.2")
        self.assertNotIn("C", r["versions"])

    def test_the_number_is_above_every_package_not_only_the_members(self):
        self.assertEqual(self.plan(feed_at(A={"1.4.8": "t"}))["plan"]["release"], "1.4.9")

    def test_a_member_is_raised_from_its_own_last_stable_version_not_from_the_bundles_number(self):
        r = self.plan(feed_at(C={"1.4.2": "t"}))
        self.assertEqual(r["package-versions"], {"C": "1.4.3"})
        self.assertEqual(r["plan"]["release"], "1.4.5")

    def test_the_manifest_pins_members_at_their_versions_and_the_rest_at_what_they_shipped(self):
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

    def replan(self, feed, pins=True):
        """The plan again, on the same tree, against a feed that now holds what an earlier run pushed. `pins` are the exact pins of the baseline
        bundle (every package at 1.4.4 here), as the real run reads them; pins=None is a baseline from a file or a git ref."""
        return release.release_plan(snapshot(self.repo.root), self.baseline(), feed, [], "1.4", "patch", "Fdw.Bundle", {},
                                    baseline_pins={n: "1.4.4" for n in ALL} if pins else None)

    PLAN_FILES = ("plan", "versions", "trees", "manifest", "levels-input", "package-versions", "assembly-versions")

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

    def test_after_a_normal_release_the_baseline_versions_are_never_a_resume(self):
        # FDW-5589, the shape of the 1.5.7 plan: A changed; B and C are in the release only because A is below them, so their own folders are
        # unchanged since 1.4.4 and their 1.4.4 versions carry the CURRENT tree. That is the baseline release, not a partial push.
        feed = feed_at(A={"1.4.4": "t"}, B={"1.4.4": snapshot_trees(self.repo, "B")}, C={"1.4.4": snapshot_trees(self.repo, "C")})
        r = self.plan(feed, files={"public/src/A/A.cs": "class A { int x; }\n"})
        self.assertEqual(r["plan"]["members"], ["A", "B", "C"])
        self.assertEqual(r["plan"]["release"], "1.4.5")
        self.assertEqual(r["package-versions"], {"A": "1.4.5", "B": "1.4.5", "C": "1.4.5"})

    def test_the_baseline_pins_decide_when_a_closure_only_member_carries_the_current_tree(self):
        self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})
        trees = snapshot(self.repo.root).trees
        feed = feed_at(A={"1.4.4": "t", "1.4.5": trees["A"]}, B={"1.4.4": trees["B"]}, C={"1.4.4": trees["C"]})
        again = self.replan(feed)   # A already pushed at 1.4.5 by an earlier run, B and C not yet
        self.assertEqual(again["plan"]["release"], "1.4.5")
        self.assertEqual(again["package-versions"], {"A": "1.4.5", "B": "1.4.5", "C": "1.4.5"})

    def test_a_closure_only_member_pushed_by_this_release_is_a_resume_when_the_baseline_pins_say_so(self):
        self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})
        trees = snapshot(self.repo.root).trees
        feed = feed_at(A={"1.4.4": "t", "1.4.5": trees["A"]}, B={"1.4.4": trees["B"], "1.4.5": trees["B"]}, C={"1.4.4": trees["C"]})
        again = self.replan(feed)
        self.assertEqual(again["plan"]["release"], "1.4.5")
        self.assertEqual(again["package-versions"]["B"], "1.4.5")

    def test_an_unpinned_package_pushed_by_this_release_is_a_resume_not_a_conflict(self):
        # FDW-5635, 1.5.7 run 43516432943: B is a dotnet tool. The baseline bundle does not pin it (it states it in fdw/not-pinned.json), its folder is
        # unchanged since the baseline, and this release already pushed it at 1.4.5. Judged by the tree alone, its own 1.4.5 was taken for the
        # baseline's and the plan stopped with RESUME_CONFLICT, 'built from tree X, not X'.
        self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})
        trees = snapshot(self.repo.root).trees
        feed = feed_at(A={"1.4.4": "t", "1.4.5": trees["A"]}, B={"1.4.4": trees["B"], "1.4.5": trees["B"]}, C={"1.4.4": trees["C"], "1.4.5": trees["C"]})
        pins = {n: "1.4.4" for n in ALL if n != "B"}   # what baseline_pins gave before: the exact pins, which leave the tool out
        again = release.release_plan(snapshot(self.repo.root), self.baseline(), feed, [], "1.4", "patch", "Fdw.Bundle", {}, baseline_pins=pins)
        self.assertEqual(again["plan"]["release"], "1.4.5")
        self.assertEqual(again["package-versions"], {"A": "1.4.5", "B": "1.4.5", "C": "1.4.5"})
        # with the tool's own baseline version stated (not-pinned.json), its baseline 1.4.4 is still the baseline's, not a resume
        baseline_only = feed_at(A={"1.4.4": "t", "1.4.5": trees["A"]}, B={"1.4.4": trees["B"]}, C={"1.4.4": trees["C"]})
        stated = release.release_plan(snapshot(self.repo.root), self.baseline(), baseline_only, [], "1.4", "patch", "Fdw.Bundle", {}, baseline_pins=pins | {"B": "1.4.4"})
        self.assertEqual(stated["plan"]["release"], "1.4.5")

    def test_without_pins_an_unchanged_own_folder_is_taken_for_the_baseline_version(self):
        self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})
        trees = snapshot(self.repo.root).trees
        feed = feed_at(A={"1.4.4": "t"}, B={"1.4.4": trees["B"]}, C={"1.4.4": trees["C"]})
        self.assertEqual(self.replan(feed, pins=None)["plan"]["release"], "1.4.5")

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

    def test_nothing_changed_gives_an_empty_member_list_and_no_levels(self):
        r = self.plan(files={"README.md": "x\n"})
        self.assertEqual((r["plan"]["members"], r["manifest"]["packages"]["C"], r["package-versions"]), ([], "1.4.4", {}))
        self.assertEqual(r["levels"]["packages"], {})

    def test_a_bad_bundle_id_stops_the_plan(self):
        with self.assertRaisesRegex(release.ReleaseError, "valid bundle"):
            self.plan(bundle_id="Fdw Bundle")

    def test_a_bump_that_is_not_one_of_the_three_stops_the_plan(self):
        with self.assertRaisesRegex(release.ReleaseError, "must be patch, minor or major"):
            self.plan(bump="huge")

    def test_the_stated_level_is_the_level_of_every_package_that_changed(self):
        for bump, version, assembly, bundle_number in (("patch", "1.4.5", "1.4.1.0", "1.4.5"), ("minor", "1.5.0", "1.4.1.0", "1.5.0"), ("major", "2.0.0", "2.0.0.0", "2.0.0")):
            with self.subTest(bump=bump):
                self.setUp()
                r = self.plan(bump=bump, files={"public/src/A/A.cs": "class A { int x; }\n"})
                self.assertEqual(set(r["package-versions"].values()), {version})
                self.assertEqual(set(r["assembly-versions"].values()), {assembly})
                self.assertEqual((r["plan"]["bump"], r["plan"]["release"], r["manifest"]["version"]), (bump, bundle_number, bundle_number))
                self.assertEqual({row["level"] for row in r["levels"]["packages"].values()}, {bump})

    def test_the_plan_allocates_a_bundle_number_for_each_bump(self):
        p = self.plan()["plan"]
        self.assertEqual(p["release_numbers"], {"patch": "1.4.5", "minor": "1.5.0", "major": "2.0.0"})
        self.assertEqual((p["bump"], p["bump_stated"]), ("patch", "patch"))

    def test_a_changed_generator_gives_the_packages_it_caused_a_new_version_with_it(self):
        r = self.plan(files={"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})
        self.assertEqual(r["package-versions"], {"D": "1.4.5", "E": "1.4.5", "Gen.SourceGenerators": "1.4.5"})

    def test_a_package_that_has_no_last_stable_version_needs_its_declared_first_version(self):
        feed = feed_at()
        del feed.held["C"]
        with self.assertRaisesRegex(release.ReleaseError, "no stable version"):
            self.plan(feed)  # C is a member here, so it is not asked of the feed; it is C's own first version that is missing
        self.setUp()
        feed = feed_at()
        del feed.held["C"]
        with self.assertRaisesRegex(release.ReleaseError, "FIRST_VERSION_NOT_DECLARED"):
            self.plan(feed)

    def test_a_new_package_takes_its_declared_first_version_and_leaves_the_bundle_at_the_stated_level(self):
        # FDW-5591: adding a package to a release stated as a patch is still that patch.
        feed = feed_at()
        del feed.held["C"]
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        r = self.plan(feed, first_versions={"C": {"version": "1.0.0", "replaces": []}}, baseline=self.baseline(assemblyVersionFloors=floors))
        self.assertEqual(r["package-versions"], {"C": "1.0.0"})
        self.assertEqual((r["plan"]["bump"], r["manifest"]["version"]), ("patch", "1.4.5"))
        self.assertEqual(r["assembly-versions"], {"C": "1.0.0.0"})

    def test_a_new_package_declaring_the_release_number_keeps_exactly_that_version_and_raises_no_other(self):
        # The shape of 1.5.7: new packages declare the release's own number; the changed ones stay at the stated patch.
        feed = feed_at()
        del feed.held["C"]
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        r = self.plan(feed, files={"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n", "public/src/C/C.cs": "class C { int y; }\n"},
                      first_versions={"C": {"version": "1.4.5", "replaces": []}}, baseline=self.baseline(assemblyVersionFloors=floors))
        self.assertEqual(r["package-versions"]["C"], "1.4.5")
        self.assertEqual(r["package-versions"]["Gen.SourceGenerators"], "1.4.5")
        self.assertEqual({row["level"] for p, row in r["levels"]["packages"].items() if p != "C"}, {"patch"})
        self.assertEqual((r["plan"]["release"], r["manifest"]["version"]), ("1.4.5", "1.4.5"))

    def test_a_new_package_in_a_release_that_states_a_minor_leaves_it_a_minor(self):
        feed = feed_at()
        del feed.held["C"]
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        r = self.plan(feed, bump="minor", first_versions={"C": {"version": "1.0.0", "replaces": []}}, baseline=self.baseline(assemblyVersionFloors=floors))
        self.assertEqual((r["plan"]["bump"], r["manifest"]["version"]), ("minor", "1.5.0"))

    def test_a_package_a_declared_entry_replaces_leaves_the_manifest(self):
        feed = feed_at()
        del feed.held["C"]
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        r = self.plan(feed, first_versions={"C": {"version": "1.0.0", "replaces": ["E"]}}, baseline=self.baseline(assemblyVersionFloors=floors))
        self.assertNotIn("E", r["manifest"]["packages"])
        self.assertNotIn("E", r["versions"])
        self.assertEqual(r["plan"]["retired"], ["E"])

    def test_a_prerelease_makes_every_package_and_the_bundle_a_candidate_one_above_those_the_bundle_has(self):
        feed = feed_at(**{"Fdw.Bundle": {"1.4.4": "t", "1.4.5-rc.1": "t", "1.4.5-rc.2": "t", "1.4.6-rc.1": "t"}})
        r = self.plan(feed, prerelease=True)
        self.assertEqual(r["package-versions"], {"C": "1.4.5-rc.3"})
        self.assertEqual((r["manifest"]["version"], r["plan"]["version"], r["plan"]["release"]), ("1.4.5-rc.3", "1.4.5-rc.3", "1.4.5"))
        self.assertEqual(r["manifest"]["packages"]["C"], "1.4.5-rc.3")
        self.assertEqual(r["manifest"]["packages"]["A"], "1.4.4")
        self.assertEqual(r["levels"]["packages"]["C"]["next"], "1.4.5")

    def test_the_first_candidate_of_a_release_is_rc_1(self):
        self.assertEqual(self.plan(prerelease=True)["manifest"]["version"], "1.4.5-rc.1")

    def test_assembly_versions_follow_each_assemblys_own_floor_and_a_major_from_two(self):
        for version, floor, expected in (("1.4.5", "1.4.1.0", "1.4.1.0"), ("1.9.0", "1.4.1.0", "1.4.1.0"), ("2.0.0", "1.4.1.0", "2.0.0.0"),
                                         ("2.3.1", "2.0.0.0", "2.0.0.0"), ("1.0.0", None, "1.0.0.0"), ("10.1.0", "1.5.4.0", "10.0.0.0"), ("1.5.5", "1.5.4.0", "1.5.4.0")):
            self.assertEqual(release.assembly_version_of(version, floor), expected, (version, floor))

    def test_a_patch_keeps_the_floor_of_its_assembly(self):
        self.assertEqual(self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})["assembly-versions"], {"A": "1.4.1.0", "B": "1.4.1.0", "C": "1.4.1.0"})

    def test_a_generator_the_baseline_lists_as_carrying_no_runtime_assembly_needs_no_floor(self):
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        r = self.plan(baseline=self.baseline(assemblyVersionFloors=floors, packagesWithoutRuntimeAssembly=["C"]))
        self.assertEqual(r["assembly-versions"], {"C": "1.0.0.0"})

    def test_an_existing_package_whose_assembly_has_no_recorded_floor_stops_the_plan(self):
        floors = {k: v for k, v in FLOORS.items() if k != "C"}
        with self.assertRaisesRegex(release.ReleaseError, "ASSEMBLY_FLOOR_MISSING"):
            self.plan(baseline=self.baseline(assemblyVersionFloors=floors))

    def test_the_plan_names_the_graph_the_last_stable_versions_and_the_generators(self):
        info = self.plan(files={"public/src/Gen.SourceGenerators/G.cs": "class G { int x; }\n"})["levels-input"]
        self.assertEqual(info["depends"]["C"], ["B"])
        self.assertEqual(info["depends"]["A"], [])
        self.assertEqual(info["members"], ["D", "E", "Gen.SourceGenerators"])
        self.assertEqual(info["generators"], {"Gen.SourceGenerators": ["D", "E"]})
        self.assertEqual(info["last_stable"]["A"], "1.4.4")
        self.assertEqual(info["names"]["C"], "C")
        self.assertEqual(info["paths"]["C"], "public/src/C/C.csproj")
        self.assertEqual(info["assemblies"]["Gen.SourceGenerators"], "Gen.SourceGenerators")

    def test_the_report_lists_each_package_with_its_last_next_and_why(self):
        report = "\n".join(self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})["levels"]["report"])
        self.assertRegex(report, r"\nA\s+1\.4\.4\s+patch\s+1\.4\.5\s+source changed")
        self.assertRegex(report, r"C\s+1\.4\.4\s+patch\s+1\.4\.5\s+A changed, and C is built from it")
        self.assertIn("3 package(s): 3 patch", report)

    def test_a_package_a_dependency_change_adds_brings_its_tests(self):
        self.repo.commit({"public/tests/B.Tests/B.Tests.csproj": proj(refs=["B"])}, "tests for B")
        self.base = self.repo.git("rev-parse", "HEAD").strip()
        self.assertIn("B.Tests", self.plan(files={"public/src/A/A.cs": "class A { int x; }\n"})["plan"]["tests"])

    def command(self, *extra, feed=None, declared=None):
        if not getattr(self, "_changed", False):
            self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
            self._changed = True
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-cmd")
        self.addCleanup(lambda: os.path.isdir(out) and ([os.remove(os.path.join(out, f)) for f in os.listdir(out)], os.rmdir(out)))
        text = io.StringIO()
        with mock.patch.object(release, "NugetOrg", lambda: feed or feed_at()), contextlib.redirect_stdout(text):
            release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--bundle-id", "Fdw.Bundle", "--first-versions", self.first_versions_file(declared), "--out", out, *extra])
        return out, text.getvalue()

    def test_the_command_writes_the_files_the_later_steps_read_and_nothing_else(self):
        out, text = self.command("--base-ref", self.base, "--assembly-info", self.floors_file(), "--bump", "patch")
        self.assertEqual(sorted(os.listdir(out)), ["assembly-versions.json", "levels-input.json", "levels.json", "manifest.json", "package-versions.json", "plan.json",
                                                   "project-manifest.json", "trees.json", "versions.json"])
        with open(os.path.join(out, "plan.json")) as f:
            self.assertEqual(json.load(f)["release"], "1.4.5")
        with open(os.path.join(out, "package-versions.json")) as f:
            self.assertEqual(json.load(f), {"C": "1.4.5"})
        self.assertIn("release 1.4.5 (patch), version 1.4.5", text)

    def test_a_bump_is_always_required_and_never_assumed(self):
        with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
            self.command("--base-ref", self.base, "--assembly-info", self.floors_file())

    def test_a_baseline_file_with_a_minor_bump(self):
        baseline = self.tmp_path("-base.json")
        with open(baseline, "w") as f:
            json.dump(self.baseline(), f)
        out, text = self.command("--baseline", baseline, "--bump", "minor")
        self.assertIn("release 1.5.0 (minor)", text)

    def test_the_prerelease_flag_makes_candidates(self):
        out, text = self.command("--base-ref", self.base, "--assembly-info", self.floors_file(), "--bump", "patch", "--prerelease")
        self.assertIn("version 1.4.5-rc.1", text)

    def test_the_baseline_comes_from_the_bundle_on_the_feed(self):
        feed = feed_at()
        feed.held["Fdw.Bundle"] = {"1.4.4": "t"}
        feed.files = {("Fdw.Bundle", "1.4.4", "fdw/manifest.json"): json.dumps(self.baseline())}
        feed.pins_of = {("Fdw.Bundle", "1.4.4"): {n: "1.4.4" for n in ALL}}
        out, text = self.command("--baseline-from-bundle", "--bump", "patch", feed=feed)
        self.assertIn("release 1.4.5 (patch)", text)
        self.assertIn("1 packages to build and push", text)
        with open(os.path.join(out, "project-manifest.json")) as f:
            self.assertEqual(sorted(json.load(f)), ["globals", "projects"])

    def test_exactly_one_baseline_is_required(self):
        with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
            self.command("--bump", "patch")
        with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
            self.command("--base-ref", self.base, "--assembly-info", self.floors_file(), "--baseline-from-bundle", "--bump", "patch")

    def test_the_command_writes_nothing_when_it_refuses(self):
        self.repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "change")
        out = os.path.join(self._tmp.name, "..", os.path.basename(self._tmp.name) + "-none")
        with mock.patch.object(release, "NugetOrg", lambda: feed_at(A={"1.4.4-rc.1": "t"})):
            with self.assertRaises(SystemExit) as raised:
                release.main(["plan", "--repo", self.repo.root, "--line", "1.4", "--base-ref", self.base, "--assembly-info", self.floors_file(), "--bundle-id", "Fdw.Bundle",
                              "--first-versions", self.first_versions_file(), "--bump", "patch", "--out", out])
        self.assertIn("nothing was written", str(raised.exception))
        self.assertFalse(os.path.exists(out))


if __name__ == "__main__":
    unittest.main()

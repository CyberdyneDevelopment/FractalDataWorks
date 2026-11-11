import contextlib
import io
import json
import os
import tempfile
import unittest

import levelplan

LAST = {"Base": "1.5.2", "B": "1.5.2", "C": "1.5.2", "D": "1.5.2", "Gen": "1.5.2", "Solo": "2.3.4", "User": "1.5.2"}
DEPENDS = {"B": {"Base"}, "C": {"Base"}, "D": {"B"}, "Solo": set(), "Base": set()}


def levels(**by_package):
    return lambda package: by_package.get(package, "patch")


class NextVersionTests(unittest.TestCase):
    def test_each_level_raises_the_right_part(self):
        self.assertEqual(levelplan.next_version("1.5.2", "patch"), "1.5.3")
        self.assertEqual(levelplan.next_version("1.5.2", "minor"), "1.6.0")
        self.assertEqual(levelplan.next_version("1.5.2", "major"), "2.0.0")

    def test_a_version_that_is_not_plain_stable_stops(self):
        for bad in ("1.5.2-rc.1", "1.5", "", None):
            with self.assertRaises(levelplan.LevelPlanError):
                levelplan.next_version(bad, "patch")

    def test_an_unknown_level_stops(self):
        with self.assertRaises(levelplan.LevelPlanError):
            levelplan.next_version("1.0.0", "huge")


class OwnLevelTests(unittest.TestCase):
    def test_a_package_takes_its_own_last_version_raised_by_its_own_level(self):
        plan = levelplan.plan_levels(DEPENDS, {"Solo"}, levels(Solo="minor"), LAST, {})
        self.assertEqual(plan["Solo"]["next"], "2.4.0")
        self.assertEqual(plan["Solo"]["last"], "2.3.4")

    def test_packages_in_one_release_get_different_numbers(self):
        plan = levelplan.plan_levels(DEPENDS, {"Solo", "Base"}, levels(Solo="patch", Base="minor"), LAST, {})
        self.assertEqual((plan["Solo"]["next"], plan["Base"]["next"]), ("2.3.5", "1.6.0"))

    def test_a_package_that_did_not_change_is_not_in_the_plan(self):
        plan = levelplan.plan_levels(DEPENDS, {"Solo"}, levels(), LAST, {})
        self.assertEqual(list(plan), ["Solo"])

    def test_a_package_with_no_last_stable_version_stops_and_is_named(self):
        with self.assertRaisesRegex(levelplan.LevelPlanError, "FIRST_VERSION_NOT_DECLARED.*NewOne"):
            levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, {})


class DependentTests(unittest.TestCase):
    def test_a_major_pulls_each_dependent_in_at_patch(self):
        plan = levelplan.plan_levels(DEPENDS, {"Base"}, levels(Base="major"), LAST, {})
        self.assertEqual({p: plan[p]["level"] for p in plan}, {"Base": "major", "B": "patch", "C": "patch"})
        self.assertEqual(plan["B"]["next"], "1.5.3")
        self.assertIn("Base went major", plan["B"]["reasons"])

    def test_a_minor_or_patch_pulls_nothing_in(self):
        for level in ("minor", "patch"):
            plan = levelplan.plan_levels(DEPENDS, {"Base"}, levels(Base=level), LAST, {})
            self.assertEqual(list(plan), ["Base"])

    def test_a_dependent_that_is_not_a_direct_dependent_stays_out_of_a_patch_chain(self):
        plan = levelplan.plan_levels(DEPENDS, {"Base"}, levels(Base="major"), LAST, {})
        self.assertNotIn("D", plan)

    def test_a_dependent_whose_own_surface_is_minor_keeps_that_level(self):
        plan = levelplan.plan_levels(DEPENDS, {"Base"}, levels(Base="major", B="minor"), LAST, {})
        self.assertEqual((plan["B"]["level"], plan["B"]["next"]), ("minor", "1.6.0"))

    def test_a_dependent_that_goes_major_in_turn_pulls_in_its_dependents(self):
        plan = levelplan.plan_levels(DEPENDS, {"Base"}, levels(Base="major", B="major"), LAST, {})
        self.assertEqual(set(plan), {"Base", "B", "C", "D"})
        self.assertEqual(plan["D"]["level"], "patch")
        self.assertIn("B went major", plan["D"]["reasons"])

    def test_a_package_already_a_member_is_not_joined_twice(self):
        plan = levelplan.plan_levels(DEPENDS, {"Base", "B"}, levels(Base="major", B="minor"), LAST, {})
        self.assertEqual(plan["B"]["level"], "minor")
        self.assertIn("Base went major", plan["B"]["reasons"])

    def test_the_level_of_each_package_is_asked_once(self):
        asked = []

        def level_of(package):
            asked.append(package)
            return "major" if package == "Base" else "patch"

        levelplan.plan_levels(DEPENDS, {"Base", "B"}, level_of, LAST, {})
        self.assertEqual(sorted(asked), sorted(set(asked)))


class GeneratorTests(unittest.TestCase):
    def test_a_generator_takes_the_highest_level_among_the_consumers_it_caused(self):
        depends = {"User": set(), "B": set()}
        plan = levelplan.plan_levels(depends, {"User", "B", "Gen"}, levels(User="minor", B="patch"), LAST, {}, {"Gen": {"User", "B"}})
        self.assertEqual((plan["Gen"]["level"], plan["Gen"]["next"]), ("minor", "1.6.0"))

    def test_a_generator_whose_consumers_are_all_patch_is_a_patch(self):
        plan = levelplan.plan_levels({"User": set()}, {"User", "Gen"}, levels(), LAST, {}, {"Gen": {"User"}})
        self.assertEqual(plan["Gen"]["level"], "patch")

    def test_a_generator_is_in_the_plan_even_if_it_was_not_a_member(self):
        plan = levelplan.plan_levels({"User": set()}, {"User"}, levels(User="major"), LAST, {}, {"Gen": {"User"}})
        self.assertEqual(plan["Gen"]["level"], "major")
        self.assertIn("its source changed", plan["Gen"]["reasons"])


def declared(**entries):
    return {package: {"version": v, "replaces": []} if isinstance(v, str) else v for package, v in entries.items()}


class NewPackageTests(unittest.TestCase):
    def test_a_new_package_takes_the_first_version_that_is_declared_for_it(self):
        plan = levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, declared(NewOne="1.0.0"))
        self.assertEqual((plan["NewOne"]["level"], plan["NewOne"]["next"], plan["NewOne"]["last"]), ("new", "1.0.0", None))

    def test_a_declared_first_version_is_not_changed_by_the_level_function(self):
        asked = []
        levelplan.plan_levels({}, {"NewOne"}, lambda p: asked.append(p) or "major", LAST, declared(NewOne="0.1.0"))
        self.assertEqual(asked, [])

    def test_a_release_that_adds_a_package_still_plans_the_others(self):
        plan = levelplan.plan_levels({}, {"NewOne", "Solo"}, levels(Solo="minor"), LAST, declared(NewOne="1.0.0"))
        self.assertEqual((plan["NewOne"]["next"], plan["Solo"]["next"]), ("1.0.0", "2.4.0"))

    def test_the_missing_entry_is_named_in_the_failure_with_its_code(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, {})
        self.assertEqual(caught.exception.code, "FIRST_VERSION_NOT_DECLARED")
        self.assertIn('"NewOne": "x.y.z"', str(caught.exception))
        self.assertIn("first-versions.json", str(caught.exception))

    def test_an_entry_for_a_package_that_already_has_a_stable_version_stops(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"Solo"}, levels(), LAST, declared(Solo="1.0.0"))
        self.assertEqual(caught.exception.code, "FIRST_VERSION_FOR_EXISTING_PACKAGE")

    def test_an_entry_for_a_package_that_is_not_in_the_release_stops(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"Solo"}, levels(), LAST, declared(Typo="1.0.0"))
        self.assertEqual(caught.exception.code, "FIRST_VERSION_NOT_IN_RELEASE")

    def test_a_new_package_counts_as_an_addition_for_the_generator_rule(self):
        plan = levelplan.plan_levels({}, {"NewOne", "Gen"}, levels(), LAST, declared(NewOne="1.0.0"), {"Gen": {"NewOne"}})
        self.assertEqual((plan["Gen"]["level"], plan["Gen"]["next"]), ("minor", "1.6.0"))


class RenameAndSplitTests(unittest.TestCase):
    def test_a_rename_declares_the_new_id_and_the_old_one_it_replaces(self):
        first = declared(NewName={"version": "1.0.0", "replaces": ["Solo"]})
        plan = levelplan.plan_levels({}, {"NewName"}, levels(), LAST, first)
        self.assertEqual(plan["NewName"]["replaces"], ["Solo"])
        self.assertEqual(levelplan.replaced_packages(first), ["Solo"])
        self.assertIn("replaces Solo", plan["NewName"]["reasons"][-1])

    def test_a_split_has_several_new_ids_replacing_one(self):
        first = declared(PartA={"version": "1.0.0", "replaces": ["Solo"]}, PartB={"version": "1.0.0", "replaces": ["Solo"]})
        plan = levelplan.plan_levels({}, {"PartA", "PartB"}, levels(), LAST, first)
        self.assertEqual({plan["PartA"]["next"], plan["PartB"]["next"]}, {"1.0.0"})
        self.assertEqual(levelplan.replaced_packages(first), ["Solo"])

    def test_a_replaced_package_that_has_no_stable_version_stops(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"NewName"}, levels(), LAST, declared(NewName={"version": "1.0.0", "replaces": ["Never.Existed"]}))
        self.assertEqual(caught.exception.code, "REPLACED_PACKAGE_UNKNOWN")

    def test_a_replaced_package_that_is_also_changing_stops(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"NewName", "Solo"}, levels(), LAST, declared(NewName={"version": "1.0.0", "replaces": ["Solo"]}))
        self.assertEqual(caught.exception.code, "REPLACED_PACKAGE_STILL_CHANGING")


class LoadTests(unittest.TestCase):
    def write(self, content):
        handle, path = tempfile.mkstemp(suffix=".json")
        self.addCleanup(os.remove, path)
        with os.fdopen(handle, "w", encoding="utf-8") as f:
            f.write(content)
        return path

    def code(self, content):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.load_first_versions(self.write(content))
        return caught.exception.code

    def test_both_forms_of_an_entry_load(self):
        loaded = levelplan.load_first_versions(self.write(json.dumps({"firstVersions": {"A": "1.0.0", "B": {"version": "2.0.0", "replaces": ["Z", "Y"]}}})))
        self.assertEqual(loaded, {"A": {"version": "1.0.0", "replaces": []}, "B": {"version": "2.0.0", "replaces": ["Y", "Z"]}})

    def test_an_empty_declaration_is_valid(self):
        self.assertEqual(levelplan.load_first_versions(self.write('{"firstVersions": {}}')), {})

    def test_a_missing_file_stops_with_its_code(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.load_first_versions(os.path.join(tempfile.gettempdir(), "no-such-first-versions.json"))
        self.assertEqual(caught.exception.code, "FIRST_VERSIONS_FILE_MISSING")

    def test_a_file_without_the_object_stops(self):
        self.assertEqual(self.code("{}"), "FIRST_VERSIONS_FILE_INVALID")
        self.assertEqual(self.code("not json"), "FIRST_VERSIONS_FILE_INVALID")

    def test_a_version_that_is_not_plain_stops(self):
        for bad in ('"1.0"', '"1.0.0-rc.1"', '"1.*"', '{"version": 3}', "7"):
            self.assertEqual(self.code('{"firstVersions": {"A": %s}}' % bad), "FIRST_VERSION_INVALID", bad)

    def test_the_file_in_the_repository_loads_and_declares_nothing(self):
        here = os.path.dirname(os.path.abspath(__file__))
        self.assertEqual(levelplan.load_first_versions(os.path.join(here, "first-versions.json")), {})


class RetireTests(unittest.TestCase):
    def file(self, document):
        handle, path = tempfile.mkstemp(suffix=".json")
        self.addCleanup(os.remove, path)
        with os.fdopen(handle, "w", encoding="utf-8") as f:
            json.dump(document, f)
        return path

    def entries(self, path):
        with open(path, encoding="utf-8") as f:
            return json.load(f)["firstVersions"]

    def test_the_command_reads_the_levels_file_the_release_job_writes(self):
        first = self.file({"firstVersions": {"Fresh": "1.0.0", "Other": "2.0.0"}})
        levels = self.file({"packages": {"Fresh": {"last": None, "level": "new", "next": "1.0.0", "reasons": []},
                                         "Base": {"last": "1.5.2", "level": "patch", "next": "1.5.3", "reasons": []}}, "report": ["PACKAGE ..."]})
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            levelplan.main(["retire", "--plan", levels, "--first-versions", first])
        self.assertIn("first version 1.0.0 of Fresh shipped; entry removed", out.getvalue())
        self.assertEqual(sorted(self.entries(first)), ["Other"])

    def test_the_release_that_ships_a_new_package_removes_its_entry_and_logs_it_by_name(self):
        path = self.file({"firstVersions": {"NewOne": "1.0.0"}})
        plan = levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, levelplan.load_first_versions(path))
        log = levelplan.retire_entries(path, plan)
        self.assertEqual(log, ["first version 1.0.0 of NewOne shipped; entry removed"])
        self.assertEqual(self.entries(path), {})

    def test_the_next_release_runs_clean_once_the_package_has_a_stable_version(self):
        path = self.file({"firstVersions": {"NewOne": "1.0.0"}})
        first = levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, levelplan.load_first_versions(path))
        levelplan.retire_entries(path, first)
        shipped = {**LAST, "NewOne": first["NewOne"]["next"]}
        second = levelplan.plan_levels({}, {"NewOne"}, levels(NewOne="minor"), shipped, levelplan.load_first_versions(path))
        self.assertEqual((second["NewOne"]["last"], second["NewOne"]["level"], second["NewOne"]["next"]), ("1.0.0", "minor", "1.1.0"))

    def test_without_the_removal_the_next_release_stops_on_the_stale_entry(self):
        path = self.file({"firstVersions": {"NewOne": "1.0.0"}})
        shipped = {**LAST, "NewOne": "1.0.0"}
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.plan_levels({}, {"NewOne"}, levels(), shipped, levelplan.load_first_versions(path))
        self.assertEqual(caught.exception.code, "FIRST_VERSION_FOR_EXISTING_PACKAGE")

    def test_an_entry_the_release_did_not_use_stays_and_keeps_stopping_a_later_plan(self):
        path = self.file({"firstVersions": {"NewOne": "1.0.0", "HandEdit": "9.0.0"}})
        plan = levelplan.plan_levels({}, {"NewOne"}, levels(), LAST, {"NewOne": {"version": "1.0.0", "replaces": []}})
        levelplan.retire_entries(path, plan)
        self.assertEqual(self.entries(path), {"HandEdit": "9.0.0"})

    def test_a_rename_and_a_split_retire_every_new_id_they_introduced(self):
        path = self.file({"firstVersions": {"PartA": {"version": "1.0.0", "replaces": ["Solo"]}, "PartB": {"version": "1.0.0", "replaces": ["Solo"]}}})
        plan = levelplan.plan_levels({}, {"PartA", "PartB"}, levels(), LAST, levelplan.load_first_versions(path))
        log = levelplan.retire_entries(path, plan)
        self.assertEqual(len(log), 2)
        self.assertEqual(self.entries(path), {})

    def test_a_release_with_no_new_package_changes_nothing(self):
        path = self.file({"firstVersions": {"Later": "1.0.0"}})
        plan = levelplan.plan_levels({}, {"Solo"}, levels(), LAST, {})
        self.assertEqual(levelplan.retire_entries(path, plan), [])
        self.assertEqual(self.entries(path), {"Later": "1.0.0"})

    def test_a_new_package_in_the_plan_with_no_entry_stops_the_removal(self):
        path = self.file({"firstVersions": {}})
        plan = {"NewOne": {"level": "new", "next": "1.0.0"}}
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelplan.retire_entries(path, plan)
        self.assertEqual(caught.exception.code, "FIRST_VERSION_ENTRY_MISSING")

    def test_the_command_prints_the_log_lines(self):
        path = self.file({"firstVersions": {"NewOne": "1.0.0"}})
        plan_path = self.file({"NewOne": {"level": "new", "next": "1.0.0"}})
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            levelplan.main(["retire", "--plan", plan_path, "--first-versions", path])
        self.assertIn("first version 1.0.0 of NewOne shipped; entry removed", out.getvalue())
        self.assertEqual(self.entries(path), {})


if __name__ == "__main__":
    unittest.main()

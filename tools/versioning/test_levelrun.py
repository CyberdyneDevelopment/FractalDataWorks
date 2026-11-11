import unittest

import levelplan
import levelrun

TAB = "\t"
SETTINGS = {"mustSupplyAdditionLevel": "major"}
LAST = {"Base": "1.5.2", "B": "1.5.2", "C": "1.5.2", "D": "1.5.2"}
DEPENDS = {"B": {"Base"}, "C": {"Base"}, "D": {"B"}, "Base": set()}


def surface(*members):
    rows = [("net10.0", "TYPE", "Ns.T", "class"), ("net10.0", "BASE", "Ns.T", "System.Object")]
    rows += [("net10.0", "MEMBER", f"Ns.T::{m}()", "method public -> System.Void") for m in members]
    return "\n".join(TAB.join(r) for r in rows) + "\n"


class Surfaces:
    """Old and new surface by package; counts how many times each new surface was asked for."""

    def __init__(self, old, new):
        self.old, self.new, self.asked = old, new, []

    def old_surface(self, package):
        return self.old.get(package)

    def new_surface(self, package):
        self.asked.append(package)
        return self.new[package]


def run(surfaces, members, first_versions=None, generators=None, last=None):
    return levelrun.decide(DEPENDS, members, surfaces.old_surface, surfaces.new_surface, last or LAST, first_versions or {}, SETTINGS, generators)


class DecideTests(unittest.TestCase):
    def test_a_removed_member_makes_the_package_major_and_its_dependents_patch(self):
        s = Surfaces({"Base": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")},
                     {"Base": surface("Run"), "B": surface("X"), "C": surface("Y")})
        plan, reasons = run(s, {"Base"})
        self.assertEqual({p: plan[p]["level"] for p in plan}, {"Base": "major", "B": "patch", "C": "patch"})
        self.assertEqual(plan["Base"]["next"], "2.0.0")
        self.assertEqual(plan["B"]["next"], "1.5.3")
        self.assertEqual(reasons["Base"][0]["change"], "removed")

    def test_an_added_member_makes_the_package_minor_and_pulls_nobody_in(self):
        s = Surfaces({"Base": surface("Run")}, {"Base": surface("Run", "Extra")})
        plan, _ = run(s, {"Base"})
        self.assertEqual(list(plan), ["Base"])
        self.assertEqual((plan["Base"]["level"], plan["Base"]["next"]), ("minor", "1.6.0"))

    def test_a_change_with_no_surface_difference_is_a_patch(self):
        s = Surfaces({"Base": surface("Run")}, {"Base": surface("Run")})
        plan, reasons = run(s, {"Base"})
        self.assertEqual((plan["Base"]["level"], plan["Base"]["next"]), ("patch", "1.5.3"))
        self.assertEqual(reasons["Base"], [])

    def test_a_dependent_that_joins_late_is_built_and_asked_once(self):
        s = Surfaces({"Base": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")},
                     {"Base": surface("Run"), "B": surface("X"), "C": surface("Y")})
        run(s, {"Base"})
        self.assertEqual(sorted(s.asked), ["B", "Base", "C"])

    def test_a_dependent_whose_own_surface_changed_takes_that_level(self):
        s = Surfaces({"Base": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")},
                     {"Base": surface("Run"), "B": surface("X", "More"), "C": surface("Y")})
        plan, _ = run(s, {"Base"})
        self.assertEqual((plan["B"]["level"], plan["B"]["next"]), ("minor", "1.6.0"))

    def test_a_new_package_is_not_asked_for_a_surface_and_takes_its_declared_version(self):
        s = Surfaces({}, {})
        plan, _ = run(s, {"NewOne"}, first_versions={"NewOne": {"version": "1.0.0", "replaces": []}})
        self.assertEqual(plan["NewOne"]["next"], "1.0.0")
        self.assertEqual(s.asked, [])

    def test_a_package_with_a_stable_version_but_no_old_surface_stops_with_its_code(self):
        s = Surfaces({}, {"Base": surface("Run")})
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            run(s, {"Base"})
        self.assertEqual(caught.exception.code, "OLD_SURFACE_MISSING")

    def test_a_generator_takes_the_highest_level_of_its_consumers(self):
        s = Surfaces({"B": surface("X"), "C": surface("Y"), "Gen": surface()}, {"B": surface("X", "Added"), "C": surface("Y"), "Gen": surface()})
        plan, _ = run(s, {"B", "C"}, generators={"Gen": {"B", "C"}}, last={**LAST, "Gen": "1.5.2"})
        self.assertEqual((plan["Gen"]["level"], plan["Gen"]["next"]), ("minor", "1.6.0"))


class ReportTests(unittest.TestCase):
    def setUp(self):
        s = Surfaces({"Base": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")},
                     {"Base": surface("Run"), "B": surface("X"), "C": surface("Y")})
        self.plan, self.reasons = run(s, {"Base"})

    def test_the_report_lists_the_most_severe_first_with_last_level_next_and_why(self):
        lines = levelrun.report(self.plan, self.reasons)
        self.assertTrue(lines[1].startswith("Base"))
        self.assertIn("1.5.2", lines[1])
        self.assertIn("major", lines[1])
        self.assertIn("2.0.0", lines[1])
        self.assertIn("removed MEMBER Ns.T::Stop()", lines[1])

    def test_a_dependent_says_which_package_pulled_it_in(self):
        lines = levelrun.report(self.plan, self.reasons)
        row = next(l for l in lines if l.startswith("B "))
        self.assertIn("Base went major", row)

    def test_the_last_line_counts_the_levels(self):
        self.assertEqual(levelrun.report(self.plan, self.reasons)[-1], "3 package(s): 1 major, 2 patch")

    def test_a_new_package_row_says_its_version_was_declared(self):
        plan, reasons = run(Surfaces({}, {}), {"NewOne"}, first_versions={"NewOne": {"version": "1.0.0", "replaces": []}})
        self.assertIn("declared", levelrun.report(plan, reasons)[1])


if __name__ == "__main__":
    unittest.main()


class GeneratorTests(unittest.TestCase):
    def test_a_generator_is_never_asked_for_a_surface_and_takes_its_consumers_level(self):
        s = Surfaces({"B": surface("X")}, {"B": surface("X", "Added")})
        plan, _ = run(s, {"Base", "B"}, generators={"Base": {"B"}})
        self.assertEqual(s.asked, ["B"])
        self.assertEqual((plan["Base"]["level"], plan["Base"]["next"]), ("minor", "1.6.0"))


class CapTests(unittest.TestCase):
    def surfaces(self):
        return Surfaces({"Base": surface("Run", "Stop"), "B": surface("X"), "C": surface("Y")}, {"Base": surface("Run"), "B": surface("X", "Z"), "C": surface("Y")})

    def test_a_cap_lowers_what_the_surface_says_and_records_it(self):
        plan, _ = levelrun.decide(DEPENDS, {"Base", "B", "C"}, self.surfaces().old_surface, self.surfaces().new_surface, LAST, {}, SETTINGS, None, "patch")
        self.assertEqual([(plan[p]["level"], plan[p]["next"]) for p in ("Base", "B", "C")], [("patch", "1.5.3")] * 3)
        self.assertEqual((plan["Base"]["capped_from"], plan["B"]["capped_from"]), ("major", "minor"))
        self.assertNotIn("capped_from", plan["C"])
        self.assertIn("capped at patch", plan["Base"]["reasons"][-1])

    def test_a_cap_above_a_level_leaves_it_alone(self):
        plan, _ = levelrun.decide(DEPENDS, {"Base", "B"}, self.surfaces().old_surface, self.surfaces().new_surface, LAST, {}, SETTINGS, None, "minor")
        self.assertEqual((plan["Base"]["level"], plan["Base"]["capped_from"], plan["B"]["level"]), ("minor", "major", "minor"))
        self.assertNotIn("capped_from", plan["B"])

    def test_a_capped_major_pulls_no_dependent_and_the_report_says_so(self):
        plan, reasons = levelrun.decide(DEPENDS, {"Base"}, self.surfaces().old_surface, self.surfaces().new_surface, LAST, {}, SETTINGS, None, "patch")
        self.assertEqual(sorted(plan), ["Base"])
        self.assertIn("[surface: major, capped at patch]", "\n".join(levelrun.report(plan, reasons)))

    def test_no_cap_is_the_old_behaviour(self):
        plan, _ = levelrun.decide(DEPENDS, {"Base"}, self.surfaces().old_surface, self.surfaces().new_surface, LAST, {}, SETTINGS)
        self.assertEqual((plan["Base"]["level"], plan["Base"]["next"]), ("major", "2.0.0"))

    def test_a_cap_that_is_not_a_level_stops(self):
        with self.assertRaises(levelplan.LevelPlanError) as caught:
            levelrun.decide(DEPENDS, {"Base"}, self.surfaces().old_surface, self.surfaces().new_surface, LAST, {}, SETTINGS, None, "huge")
        self.assertEqual(caught.exception.code, "LEVEL_CAP_INVALID")

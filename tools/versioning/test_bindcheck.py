import contextlib
import io
import json
import os
import tempfile
import unittest

import bindcheck

TAB = "\t"


def rec(framework, kind, key, detail):
    return TAB.join((framework, kind, key, detail)) + "\n"


NEW = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.5.1.0") + rec("net10.0", "REF", "Fdw.Types.Abstractions->System.Runtime", "10.0.0.0")
FLOORS = {"Fdw.Types.Abstractions": "1.5.1.0"}
HELD = rec("net10.0", "DEF", "Fdw.Results", "1.5.1.0") + rec("net10.0", "REF", "Fdw.Results->Fdw.Types.Abstractions", "1.5.1.0")


class CheckTests(unittest.TestCase):
    def test_a_held_dll_binds_when_the_rebuilt_assembly_is_not_lower(self):
        self.assertEqual(bindcheck.check({"a.txt": NEW}, {"b.txt": HELD}, FLOORS), [])

    def test_a_rebuilt_assembly_at_a_higher_version_binds(self):
        new = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "2.0.0.0")
        self.assertEqual(bindcheck.check({"a.txt": new}, {"b.txt": HELD}, FLOORS), [])

    def test_a_rebuilt_assembly_lower_than_a_held_demand_is_named(self):
        new = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.0.0.0")
        problems = bindcheck.check({"a.txt": new}, {"b.txt": HELD}, {})
        self.assertEqual(len(problems), 1)
        self.assertIn("Fdw.Results (as it is) asks for Fdw.Types.Abstractions 1.5.1.0, but the rebuilt one is 1.0.0.0", problems[0])

    def test_a_built_assembly_below_the_floor_is_named_even_when_nothing_is_held(self):
        new = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.0.0.0")
        problems = bindcheck.check({"a.txt": new}, {"b.txt": rec("net10.0", "DEF", "X", "1.5.1.0")}, FLOORS)
        self.assertEqual(problems, ["Fdw.Types.Abstractions is built at 1.0.0.0, below its floor 1.5.1.0"])

    def test_an_assembly_without_a_floor_is_a_new_one_and_has_none(self):
        new = rec("net10.0", "DEF", "Fdw.Brand.New", "1.0.0.0")
        self.assertEqual(bindcheck.check({"a.txt": new}, {"b.txt": rec("net10.0", "DEF", "X", "1.5.1.0")}, FLOORS), [])

    def test_a_floor_of_one_assembly_does_not_apply_to_another(self):
        new = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.5.1.0") + rec("net10.0", "DEF", "Other", "1.0.0.0")
        self.assertEqual(bindcheck.check({"a.txt": new}, {"b.txt": HELD}, FLOORS), [])

    def test_the_lowest_framework_build_counts(self):
        new = rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.5.1.0") + rec("netstandard2.0", "DEF", "Fdw.Types.Abstractions", "1.4.0.0")
        self.assertEqual(len(bindcheck.check({"a.txt": new}, {"b.txt": HELD}, {})), 1)

    def test_a_reference_to_an_assembly_that_is_not_built_is_not_checked(self):
        held = rec("net10.0", "REF", "Fdw.Results->Fdw.Other", "9.0.0.0")
        self.assertEqual(bindcheck.check({"a.txt": NEW}, {"b.txt": held}, FLOORS), [])

    def test_a_record_that_cannot_be_read_stops_the_run(self):
        for bad in ("net10.0\tTYPE\tx\ty\n", "net10.0\tREF\tno-arrow\t1.0.0.0\n", "net10.0\tDEF\tX\t1.0\n", "only\ttwo\n"):
            with self.assertRaises(bindcheck.BindError, msg=bad):
                bindcheck.check({"a.txt": bad}, {"b.txt": HELD}, FLOORS)


class FloorsTests(unittest.TestCase):
    def test_each_assembly_floor_is_the_higher_of_what_it_carries_and_what_the_release_asks_for_it_at(self):
        a = rec("net10.0", "DEF", "A", "1.5.4.0") + rec("net10.0", "REF", "A->System.Runtime", "10.0.0.0")
        b = rec("net10.0", "DEF", "B", "1.5.1.0") + rec("net10.0", "REF", "B->A", "1.5.4.0") + rec("net10.0", "REF", "B->Newtonsoft.Json", "13.0.0.0")
        self.assertEqual(bindcheck.floors({"a.txt": a, "b.txt": b}), {"A": "1.5.4.0", "B": "1.5.1.0"})

    def test_a_demand_higher_than_the_definition_raises_that_assemblys_floor_only(self):
        a = rec("net10.0", "DEF", "A", "1.5.1.0")
        b = rec("net10.0", "DEF", "B", "1.5.1.0") + rec("net10.0", "REF", "B->A", "1.5.3.0")
        self.assertEqual(bindcheck.floors({"a.txt": a, "b.txt": b}), {"A": "1.5.3.0", "B": "1.5.1.0"})

    def test_a_2_0_package_does_not_raise_the_floor_of_a_1_x_one(self):
        a = rec("net10.0", "DEF", "A", "2.0.0.0")
        b = rec("net10.0", "DEF", "B", "1.5.4.0") + rec("net10.0", "REF", "B->System.Runtime", "10.0.0.0")
        self.assertEqual(bindcheck.floors({"a.txt": a, "b.txt": b}), {"A": "2.0.0.0", "B": "1.5.4.0"})

    def test_no_definitions_stops_the_run(self):
        with self.assertRaises(bindcheck.BindError):
            bindcheck.floors({"a.txt": rec("net10.0", "REF", "A->B", "1.0.0.0")})

    def test_a_floor_must_be_x_y_z_w(self):
        self.assertEqual(bindcheck.check_floor("1.5.4.0"), "1.5.4.0")
        for bad in ("1.5.4", "", None, "a.b.c.d"):
            with self.assertRaises(bindcheck.BindError):
                bindcheck.check_floor(bad)


class BareTests(unittest.TestCase):
    def test_a_package_with_no_assembly_definition_is_listed_by_its_package_id(self):
        texts = {"/x/Fdw.A.txt": rec("net10.0", "DEF", "Fdw.A", "1.5.4.0"), "/x/Fdw.Gen.SourceGenerators.txt": "", "/x/Fdw.Tool.txt": rec("net10.0", "REF", "X->Y", "1.0.0.0")}
        self.assertEqual(bindcheck.bare(texts), ["Fdw.Gen.SourceGenerators", "Fdw.Tool"])


    def test_a_later_directory_replaces_the_file_of_the_same_package(self):
        with tempfile.TemporaryDirectory() as root:
            old, new = os.path.join(root, "old"), os.path.join(root, "new")
            os.makedirs(old)
            os.makedirs(new)
            for d, text in ((old, rec("net10.0", "DEF", "Fdw.G", "1.5.4.0")), (new, "")):
                with open(os.path.join(d, "Fdw.G.txt"), "w") as f:
                    f.write(text)
            with open(os.path.join(new, "Fdw.H.txt"), "w") as f:
                f.write("")
            out = io.StringIO()
            with contextlib.redirect_stdout(out):
                bindcheck.main(["bare", "--dir", old, "--dir", new])
            self.assertEqual(json.loads(out.getvalue()), ["Fdw.G", "Fdw.H"])


class CommandTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.dirs = {}
        for name in ("new", "held"):
            self.dirs[name] = os.path.join(self._tmp.name, name)
            os.makedirs(self.dirs[name])
        self.floors = os.path.join(self._tmp.name, "floors.json")
        with open(self.floors, "w") as f:
            json.dump(FLOORS, f)

    def put(self, which, text):
        with open(os.path.join(self.dirs[which], "p.txt"), "w", encoding="utf-8", newline="") as f:
            f.write(text)

    def run_main(self):
        args = ["check", "--new-dir", self.dirs["new"], "--held-dir", self.dirs["held"], "--floors", self.floors]
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            bindcheck.main(args)
        return out.getvalue()

    def test_the_command_says_ok_when_everything_binds(self):
        self.put("new", NEW)
        self.put("held", HELD)
        self.assertIn("bindings ok: 1 held package(s) bind to 1 built package(s), 1 floor(s)", self.run_main())

    def test_the_command_fails_and_lists_the_problem(self):
        self.put("new", rec("net10.0", "DEF", "Fdw.Types.Abstractions", "1.0.0.0"))
        self.put("held", HELD)
        with self.assertRaises(SystemExit) as raised, contextlib.redirect_stdout(io.StringIO()):
            self.run_main()
        self.assertIn("binding problem", str(raised.exception))

    def test_the_floors_command_prints_the_floors_as_json(self):
        self.put("new", rec("net10.0", "DEF", "A", "1.5.4.0"))
        self.put("held", HELD)
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            bindcheck.main(["floors", "--dir", self.dirs["new"], "--dir", self.dirs["held"]])
        self.assertEqual(json.loads(out.getvalue()), {"A": "1.5.4.0", "Fdw.Results": "1.5.1.0"})

    def test_an_empty_directory_stops_the_run(self):
        self.put("new", NEW)
        with self.assertRaises(SystemExit) as raised:
            self.run_main()
        self.assertIn("holds no identity files", str(raised.exception))


if __name__ == "__main__":
    unittest.main()

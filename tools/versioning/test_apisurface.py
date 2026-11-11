import contextlib
import io
import json
import os
import tempfile
import unittest

import apisurface

MAJOR_SETTINGS = {"mustSupplyAdditionLevel": "major"}
MINOR_SETTINGS = {"mustSupplyAdditionLevel": "minor"}
TAB = "\t"


def surface(*records, tfm="net10.0"):
    """Surface text from (kind, key, detail) records."""
    return "\n".join(TAB.join((tfm, kind, key, detail)) for kind, key, detail in records) + "\n"


BASE = [
    ("TYPE", "Ns.Widget", "class"),
    ("BASE", "Ns.Widget", "System.Object"),
    ("MEMBER", "Ns.Widget::Name", "property public get set -> System.String"),
    ("MEMBER", "Ns.Widget::Run(System.Int32)", "method public -> System.Boolean"),
    ("TYPE", "Ns.IPlugin", "interface"),
    ("MEMBER", "Ns.IPlugin::Start()", "method public abstract -> System.Void"),
]


def level(old, new, settings=MAJOR_SETTINGS):
    return apisurface.compare(old, new, settings)["level"]


def with_extra(*extra, drop=()):
    return surface(*[r for r in BASE if (r[0], r[1]) not in drop], *extra)


class LevelTests(unittest.TestCase):
    def test_identical_surfaces_are_a_patch(self):
        self.assertEqual(level(surface(*BASE), surface(*BASE)), "patch")

    def test_an_added_type_is_a_minor(self):
        new = with_extra(("TYPE", "Ns.Extra", "class"), ("BASE", "Ns.Extra", "System.Object"))
        self.assertEqual(level(surface(*BASE), new), "minor")

    def test_an_added_member_is_a_minor(self):
        new = with_extra(("MEMBER", "Ns.Widget::Stop()", "method public -> System.Void"))
        self.assertEqual(level(surface(*BASE), new), "minor")

    def test_an_added_interface_on_a_type_is_a_minor(self):
        new = with_extra(("IMPL", "Ns.Widget : Ns.IPlugin", ""))
        self.assertEqual(level(surface(*BASE), new), "minor")

    def test_a_removed_member_is_a_major(self):
        new = surface(*[r for r in BASE if r[1] != "Ns.Widget::Run(System.Int32)"])
        self.assertEqual(level(surface(*BASE), new), "major")

    def test_a_removed_type_is_a_major(self):
        new = surface(*[r for r in BASE if not r[1].startswith("Ns.IPlugin")])
        self.assertEqual(level(surface(*BASE), new), "major")

    def test_a_changed_result_type_is_a_major(self):
        new = surface(*[(k, key, d.replace("System.Boolean", "System.Int32")) for k, key, d in BASE])
        self.assertEqual(level(surface(*BASE), new), "major")

    def test_a_changed_parameter_list_is_a_removal_and_so_a_major(self):
        new = surface(*[(k, key.replace("Run(System.Int32)", "Run(System.Int64)"), d) for k, key, d in BASE])
        result = apisurface.compare(surface(*BASE), new, MAJOR_SETTINGS)
        self.assertEqual(result["level"], "major")
        self.assertIn("removed", [r["change"] for r in result["reasons"]])

    def test_a_removed_interface_from_a_type_is_a_major(self):
        old = with_extra(("IMPL", "Ns.Widget : Ns.IPlugin", ""))
        self.assertEqual(level(old, surface(*BASE)), "major")

    def test_a_type_that_becomes_sealed_is_a_major(self):
        new = surface(*[(k, key, "class sealed" if key == "Ns.Widget" and k == "TYPE" else d) for k, key, d in BASE])
        self.assertEqual(level(surface(*BASE), new), "major")

    def test_a_type_that_stops_being_sealed_is_a_minor(self):
        old = surface(*[(k, key, "class sealed" if key == "Ns.Widget" and k == "TYPE" else d) for k, key, d in BASE])
        self.assertEqual(level(old, surface(*BASE)), "minor")

    def test_a_type_that_stops_being_sealed_and_changes_otherwise_is_a_major(self):
        old = surface(*[(k, key, "class sealed" if key == "Ns.Widget" and k == "TYPE" else d) for k, key, d in BASE])
        new = surface(*[(k, key, "class abstract" if key == "Ns.Widget" and k == "TYPE" else d) for k, key, d in BASE])
        self.assertEqual(level(old, new), "major")

    def test_a_dropped_target_framework_is_a_major_and_an_added_one_a_minor(self):
        both = surface(*BASE, tfm="net10.0") + surface(*BASE, tfm="netstandard2.0")
        self.assertEqual(level(both, surface(*BASE, tfm="net10.0")), "major")
        self.assertEqual(level(surface(*BASE, tfm="net10.0"), both), "minor")


class MustSupplyTests(unittest.TestCase):
    ABSTRACT = ("MEMBER", "Ns.IPlugin::Stop()", "method public abstract -> System.Void")
    REQUIRED = ("MEMBER", "Ns.Widget::Id", "property public required get init -> System.Guid")
    DEFAULT_BODY = ("MEMBER", "Ns.IPlugin::Describe()", "method public virtual -> System.String")

    def test_an_added_abstract_member_is_a_major_by_default_setting_of_major(self):
        self.assertEqual(level(surface(*BASE), with_extra(self.ABSTRACT)), "major")

    def test_an_added_required_property_is_a_major(self):
        self.assertEqual(level(surface(*BASE), with_extra(self.REQUIRED)), "major")

    def test_the_setting_decides_the_level_of_such_an_addition(self):
        self.assertEqual(level(surface(*BASE), with_extra(self.ABSTRACT), MINOR_SETTINGS), "minor")
        self.assertEqual(level(surface(*BASE), with_extra(self.REQUIRED), MINOR_SETTINGS), "minor")

    def test_an_interface_member_with_a_default_body_is_only_a_minor(self):
        self.assertEqual(level(surface(*BASE), with_extra(self.DEFAULT_BODY)), "minor")

    def test_the_setting_never_lowers_a_removal(self):
        new = surface(*[r for r in BASE if r[1] != "Ns.Widget::Name"])
        self.assertEqual(level(surface(*BASE), new, MINOR_SETTINGS), "major")

    def test_the_reason_says_an_implementer_must_supply_it(self):
        result = apisurface.compare(surface(*BASE), with_extra(self.ABSTRACT), MAJOR_SETTINGS)
        self.assertIn("implementer must supply", result["reasons"][0]["change"])


class ReasonTests(unittest.TestCase):
    def test_reasons_come_most_severe_first_and_are_counted(self):
        new = with_extra(("MEMBER", "Ns.Widget::Stop()", "method public -> System.Void"), drop=[("MEMBER", "Ns.Widget::Name")])
        result = apisurface.compare(surface(*BASE), new, MAJOR_SETTINGS)
        self.assertEqual([r["level"] for r in result["reasons"]], ["major", "minor"])
        self.assertEqual(result["counts"], {"major": 1, "minor": 1})
        self.assertEqual(result["reasons"][0]["key"], "Ns.Widget::Name")

    def test_a_patch_has_no_reasons(self):
        self.assertEqual(apisurface.compare(surface(*BASE), surface(*BASE), MAJOR_SETTINGS)["reasons"], [])


class ParseTests(unittest.TestCase):
    def test_a_line_that_is_not_four_fields_stops_with_its_number(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "line 2: expected 4"):
            apisurface.parse("net10.0\tTYPE\tNs.A\tclass\nbroken line\n")

    def test_an_unknown_kind_stops(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "unknown kind"):
            apisurface.parse("net10.0\tWHAT\tNs.A\tclass\n")

    def test_a_repeated_record_stops(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "appears twice"):
            apisurface.parse(surface(("TYPE", "Ns.A", "class"), ("TYPE", "Ns.A", "class")))

    def test_blank_and_comment_lines_are_ignored(self):
        self.assertEqual(len(apisurface.parse("# header\n\n" + surface(("TYPE", "Ns.A", "class")))), 1)


class SettingsTests(unittest.TestCase):
    def write(self, content):
        handle, path = tempfile.mkstemp(suffix=".json")
        self.addCleanup(os.remove, path)
        with os.fdopen(handle, "w", encoding="utf-8") as f:
            f.write(content)
        return path

    def test_a_valid_settings_file_loads(self):
        self.assertEqual(apisurface.load_settings(self.write('{"mustSupplyAdditionLevel": "major"}')), MAJOR_SETTINGS)

    def test_a_missing_setting_stops_and_says_there_is_no_default(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "no default"):
            apisurface.load_settings(self.write("{}"))

    def test_a_setting_that_is_not_a_level_stops(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "must be 'minor' or 'major'"):
            apisurface.load_settings(self.write('{"mustSupplyAdditionLevel": "patch"}'))

    def test_a_missing_file_stops(self):
        with self.assertRaisesRegex(apisurface.SurfaceError, "cannot be read"):
            apisurface.load_settings(os.path.join(tempfile.gettempdir(), "no-such-settings-file.json"))


class DeclaredSettingsTests(unittest.TestCase):
    def test_the_settings_file_in_the_repository_loads_and_states_the_level(self):
        here = os.path.dirname(os.path.abspath(__file__))
        settings = apisurface.load_settings(os.path.join(here, "apisurface-settings.json"))
        self.assertEqual(settings["mustSupplyAdditionLevel"], "major")


class CommandTests(unittest.TestCase):
    def test_the_command_prints_the_level_and_the_reasons(self):
        directory = tempfile.mkdtemp()
        paths = {}
        for name, text in (("old", surface(*BASE)), ("new", with_extra(("MEMBER", "Ns.Widget::Stop()", "method public -> System.Void"))),
                           ("settings", json.dumps(MAJOR_SETTINGS))):
            paths[name] = os.path.join(directory, name)
            with open(paths[name], "w", encoding="utf-8") as f:
                f.write(text)
            self.addCleanup(os.remove, paths[name])
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            apisurface.main(["diff", "--old", paths["old"], "--new", paths["new"], "--settings", paths["settings"]])
        self.assertIn("level: minor", out.getvalue())
        self.assertIn("added MEMBER Ns.Widget::Stop()", out.getvalue())


if __name__ == "__main__":
    unittest.main()

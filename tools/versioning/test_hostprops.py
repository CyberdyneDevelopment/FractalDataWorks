import contextlib
import io
import json
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET

import bundle
import hostprops


def manifest(**packages):
    return {"id": "Fdw.Bundle", "version": "2.0.0", "packages": packages or {"Fdw.Results": "2.0.0", "Fdw.Data": "1.5.3", "Fdw.Services": "1.6.0"}}


def release_of(text):
    return hostprops.read_release(text)


class RenderTests(unittest.TestCase):
    def test_the_release_group_holds_the_exact_versions(self):
        self.assertEqual(release_of(hostprops.render(manifest())),
                         {"Fdw.Data": "1.5.3", "Fdw.Results": "2.0.0", "Fdw.Services": "1.6.0"})

    def test_the_development_group_floats_on_the_line_of_the_bundle_for_every_package(self):
        root = ET.fromstring(hostprops.render(manifest()))
        dev = next(g for g in root.findall("ItemGroup") if "internal" in g.get("Condition"))
        floats = {i.get("Include"): i.get("Version") for i in dev.findall("PackageVersion")}
        self.assertEqual(floats, {"Fdw.Data": "2.0.*-$(FDW_TIER).*", "Fdw.Results": "2.0.*-$(FDW_TIER).*", "Fdw.Services": "2.0.*-$(FDW_TIER).*"})

    def test_the_line_is_the_major_and_minor_of_the_bundle_and_a_candidate_has_the_same_line(self):
        for version, line in (("2.0.0", "2.0"), ("1.5.6", "1.5"), ("2.1.3-rc.2", "2.1")):
            text = hostprops.render({"id": "Fdw.Bundle", "version": version, "packages": {"Fdw.A": version.split("-")[0]}})
            self.assertIn(f'Version="{line}.*-$(FDW_TIER).*"', text, version)

    def test_a_development_float_does_not_name_each_packages_own_major(self):
        text = hostprops.render(manifest(**{"Fdw.A": "3.2.1"}))
        self.assertNotIn('Version="3.*-', text)
        self.assertIn('Version="2.0.*-$(FDW_TIER).*"', text)

    def test_the_same_manifest_gives_the_same_bytes_whatever_its_order(self):
        a = manifest(**{"Fdw.B": "1.0.0", "Fdw.A": "1.0.0"})
        b = manifest(**{"Fdw.A": "1.0.0", "Fdw.B": "1.0.0"})
        self.assertEqual(hostprops.render(a), hostprops.render(b))

    def test_the_output_is_xml_and_its_comment_has_no_double_hyphen(self):
        text = hostprops.render(manifest())
        ET.fromstring(text)
        for comment in text.split("<!--")[1:]:
            self.assertNotIn("--", comment.split("-->")[0])

    def test_an_invalid_manifest_stops(self):
        with self.assertRaisesRegex(hostprops.HostPropsError, "not a valid bundle manifest"):
            hostprops.render(manifest(**{"Fdw.Results": "1.*"}))


class RowsTests(unittest.TestCase):
    OLD = {"Fdw.Results": "1.9.0", "Fdw.Data": "1.5.3", "Fdw.Gone": "1.0.0", "Fdw.Same": "1.0.0", "Fdw.Tail": "1.0.0-rc.1"}
    NEW = {"Fdw.Results": "2.0.0", "Fdw.Data": "1.6.0", "Fdw.Same": "1.0.0", "Fdw.New": "1.0.0", "Fdw.Tail": "1.0.0-rc.2"}

    def test_each_package_that_moved_has_a_level(self):
        found = {r[0]: r[3] for r in hostprops.rows(self.OLD, self.NEW)}
        self.assertEqual(found, {"Fdw.Results": "major", "Fdw.Data": "minor", "Fdw.Gone": "removed", "Fdw.New": "added", "Fdw.Tail": "changed"})

    def test_a_package_that_did_not_move_has_no_row(self):
        self.assertNotIn("Fdw.Same", [r[0] for r in hostprops.rows(self.OLD, self.NEW)])

    def test_a_patch_move_is_a_patch(self):
        self.assertEqual(hostprops.rows({"A": "1.2.3"}, {"A": "1.2.4"})[0][3], "patch")

    def test_the_most_severe_rows_come_first(self):
        self.assertEqual(hostprops.rows(self.OLD, self.NEW)[0][3], "major")

    def test_the_table_marks_a_major_and_a_removal_as_breaking(self):
        lines = hostprops.table(hostprops.rows(self.OLD, self.NEW))
        self.assertTrue(any("MAJOR" in l and "BREAKING" in l and "1.9.0 -> 2.0.0" in l for l in lines))
        self.assertTrue(any("REMOVED" in l and "BREAKING" in l for l in lines))
        self.assertFalse(any("MINOR" in l and "BREAKING" in l for l in lines))


class AdoptTests(unittest.TestCase):
    def setUp(self):
        self.dir = tempfile.mkdtemp()
        self.props = os.path.join(self.dir, "Fdw.PackageVersions.props")
        self.addCleanup(lambda: [os.remove(os.path.join(self.dir, f)) for f in os.listdir(self.dir)] and os.rmdir(self.dir))

    def test_the_first_adoption_adds_every_package_and_writes_the_file(self):
        text = hostprops.render(manifest())
        found = hostprops.adopt(self.props, text, dry_run=False)
        self.assertEqual({r[3] for r in found}, {"added"})
        with open(self.props, encoding="utf-8") as f:
            self.assertEqual(f.read(), text)

    def test_a_later_adoption_lists_only_what_moved(self):
        hostprops.adopt(self.props, hostprops.render(manifest()), dry_run=False)
        newer = manifest(**{"Fdw.Results": "3.0.0", "Fdw.Data": "1.5.3", "Fdw.Services": "1.6.1"})
        found = hostprops.adopt(self.props, hostprops.render(newer), dry_run=False)
        self.assertEqual({r[0]: r[3] for r in found}, {"Fdw.Results": "major", "Fdw.Services": "patch"})

    def test_a_dry_run_writes_nothing(self):
        hostprops.adopt(self.props, hostprops.render(manifest()), dry_run=True)
        self.assertFalse(os.path.exists(self.props))

    def test_a_file_without_a_release_group_stops(self):
        with self.assertRaisesRegex(hostprops.HostPropsError, "no ItemGroup with Condition"):
            hostprops.adopt(self.props, "<Project></Project>", dry_run=True)

    def test_the_command_prints_the_summary_and_the_table(self):
        source = os.path.join(self.dir, "new.props")
        with open(source, "w", encoding="utf-8") as f:
            f.write(hostprops.render(manifest()))
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            hostprops.main(["adopt", "--props", self.props, "--from", source, "--dry-run"])
        self.assertIn("3 package(s) moved", out.getvalue())
        self.assertIn("ADDED", out.getvalue())


class BundleCarriesTheFileTests(unittest.TestCase):
    def test_the_bundle_project_packs_the_file_under_fdw_when_asked(self):
        root = ET.fromstring(bundle.project_text(manifest(), with_package_versions=True))
        packed = [(n.get("Include"), n.get("PackagePath")) for n in root.iter("None")]
        self.assertEqual(packed, [("Fdw.PackageVersions.props", "fdw")])

    def test_the_bundle_can_carry_both_files(self):
        root = ET.fromstring(bundle.project_text(manifest(), with_project_manifest=True, with_package_versions=True))
        self.assertEqual([n.get("Include") for n in root.iter("None")], ["manifest.json", "Fdw.PackageVersions.props"])

    def test_without_the_option_the_bundle_is_as_before(self):
        self.assertNotIn("<None", bundle.project_text(manifest()))

    def test_the_command_copies_the_file_next_to_the_project(self):
        d = tempfile.mkdtemp()
        m = os.path.join(d, "m.json")
        p = os.path.join(d, "p.props")
        with open(m, "w", encoding="utf-8") as f:
            json.dump(manifest(), f)
        with open(p, "w", encoding="utf-8") as f:
            f.write(hostprops.render(manifest()))
        out = os.path.join(d, "out")
        bundle.main(["generate", "--manifest", m, "--out", out, "--package-versions", p])
        self.assertTrue(os.path.exists(os.path.join(out, "Fdw.PackageVersions.props")))
        with open(os.path.join(out, "Fdw.Bundle.csproj"), encoding="utf-8") as f:
            self.assertIn("Fdw.PackageVersions.props", f.read())


if __name__ == "__main__":
    unittest.main()

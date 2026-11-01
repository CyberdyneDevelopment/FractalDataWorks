import json
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET

import bundle


def manifest(**over):
    m = {"id": "Fdw.Bundle", "version": "1.4.5", "packages": {"Fdw.Results": "1.4.5", "Fdw.Abstractions": "1.4.2"}}
    m.update(over)
    return m


class BundleTests(unittest.TestCase):
    def refs(self, m):
        root = ET.fromstring(bundle.project_text(m))
        return {r.get("Include"): r.get("Version") for r in root.iter("PackageReference")}

    def test_every_package_is_pinned_as_an_exact_range(self):
        self.assertEqual(self.refs(manifest()), {"Fdw.Results": "[1.4.5]", "Fdw.Abstractions": "[1.4.2]"})

    def test_the_bundle_takes_its_id_and_version_from_the_manifest(self):
        props = ET.fromstring(bundle.project_text(manifest())).find("PropertyGroup")
        self.assertEqual((props.findtext("PackageId"), props.findtext("Version")), ("Fdw.Bundle", "1.4.5"))

    def test_the_bundle_ships_no_assembly_and_ignores_central_versions(self):
        props = ET.fromstring(bundle.project_text(manifest())).find("PropertyGroup")
        self.assertEqual((props.findtext("IncludeBuildOutput"), props.findtext("IncludeSymbols"), props.findtext("ManagePackageVersionsCentrally")),
                         ("false", "false", "false"))

    def test_the_same_manifest_gives_the_same_bytes_whatever_its_order(self):
        a = manifest(packages={"B": "1.0.0", "a": "1.0.0", "C": "2.0.0"})
        b = manifest(packages={"C": "2.0.0", "a": "1.0.0", "B": "1.0.0"})
        self.assertEqual(bundle.project_text(a), bundle.project_text(b))
        self.assertEqual(list(self.refs(a)), ["a", "B", "C"])

    def test_a_release_candidate_bundle_pins_release_candidate_versions(self):
        m = manifest(version="1.4.5-rc.2", packages={"Fdw.Results": "1.4.5-rc.2"})
        self.assertEqual(self.refs(m), {"Fdw.Results": "[1.4.5-rc.2]"})

    def test_a_missing_version_is_refused(self):
        for bad in (None, "", 5):
            self.assertTrue(bundle.check(manifest(packages={"Fdw.Results": bad})), bad)

    def test_a_range_or_floating_version_is_refused(self):
        for bad in ("[1.4.5]", "1.4.*", "*", "1.4", "(,1.5)", "1.4.5 ", "v1.4.5", "01.4.5"):
            self.assertTrue(bundle.check(manifest(packages={"Fdw.Results": bad})), bad)

    def test_a_bad_bundle_version_or_id_is_refused(self):
        self.assertTrue(bundle.check(manifest(version="1.4.*")))
        self.assertTrue(bundle.check(manifest(id="Fdw Bundle")))

    def test_a_manifest_with_no_packages_is_refused(self):
        self.assertTrue(bundle.check(manifest(packages={})))

    def test_a_bundle_cannot_list_itself(self):
        self.assertTrue(bundle.check(manifest(packages={"Fdw.Bundle": "1.4.5"})))

    def test_missing_keys_are_named(self):
        self.assertTrue(any("'version'" in p for p in bundle.check({"id": "X", "packages": {"A": "1.0.0"}})))

    def test_generating_from_a_bad_manifest_raises(self):
        with self.assertRaises(ValueError):
            bundle.project_text(manifest(packages={"Fdw.Results": "1.4.*"}))

    def test_the_command_writes_the_project_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "m.json")
            with open(path, "w") as f:
                json.dump(manifest(), f)
            bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out")])
            self.assertTrue(os.path.exists(os.path.join(tmp, "out", "Fdw.Bundle.csproj")))

    def test_the_project_manifest_is_carried_at_fdw_manifest_json(self):
        root = ET.fromstring(bundle.project_text(manifest(), with_project_manifest=True))
        none = [n for n in root.iter("None")]
        self.assertEqual([(n.get("Include"), n.get("Pack"), n.get("PackagePath")) for n in none], [("manifest.json", "true", "fdw")])
        self.assertEqual(list(ET.fromstring(bundle.project_text(manifest())).iter("None")), [])

    def test_the_command_writes_the_project_manifest_beside_the_project(self):
        with tempfile.TemporaryDirectory() as tmp:
            for name, data in (("m.json", manifest()), ("p.json", {"projects": {"A": "x"}, "globals": {}})):
                with open(os.path.join(tmp, name), "w") as f:
                    json.dump(data, f)
            bundle.main(["generate", "--manifest", os.path.join(tmp, "m.json"), "--out", os.path.join(tmp, "out"),
                         "--project-manifest", os.path.join(tmp, "p.json")])
            with open(os.path.join(tmp, "out", "manifest.json")) as f:
                self.assertEqual(json.load(f)["projects"], {"A": "x"})

    def test_a_project_manifest_without_projects_is_refused(self):
        with tempfile.TemporaryDirectory() as tmp:
            for name, data in (("m.json", manifest()), ("p.json", {"nothing": 1})):
                with open(os.path.join(tmp, name), "w") as f:
                    json.dump(data, f)
            with self.assertRaises(SystemExit):
                bundle.main(["generate", "--manifest", os.path.join(tmp, "m.json"), "--out", os.path.join(tmp, "out"),
                             "--project-manifest", os.path.join(tmp, "p.json")])
            self.assertFalse(os.path.exists(os.path.join(tmp, "out")))

    def test_the_command_writes_nothing_for_a_bad_manifest(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "m.json")
            with open(path, "w") as f:
                json.dump(manifest(packages={"Fdw.Results": "1.4.*"}), f)
            with self.assertRaises(SystemExit):
                bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out")])
            self.assertFalse(os.path.exists(os.path.join(tmp, "out")))


if __name__ == "__main__":
    unittest.main()

import io
import json
import os
import tempfile
import unittest
import xml.etree.ElementTree as ET
import zipfile
from unittest import mock

import bundle


def manifest(**over):
    m = {"id": "Fdw.Bundle", "version": "1.4.5", "packages": {"Fdw.Results": "1.4.5", "Fdw.Abstractions": "1.4.2"}}
    m.update(over)
    return m


def nupkg(pid, version, types=()):
    """The bytes of a package whose nuspec declares the given package types (none = an ordinary dependency)."""
    declared = "<packageTypes>" + "".join(f'<packageType name="{t}" />' for t in types) + "</packageTypes>" if types else ""
    nuspec = ('<?xml version="1.0"?><package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata>'
              f"<id>{pid}</id><version>{version}</version>{declared}</metadata></package>")
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w") as z:
        z.writestr(f"{pid}.nuspec", nuspec)
    return buffer.getvalue()


class Feed:
    """nuget.org as the bundle sees it: download(id, version) -> bytes, or None when it holds no such package."""
    def __init__(self, held):
        self.held = held

    def download(self, pid, version):
        return self.held.get((pid, version))


LIBRARIES = {("Fdw.Results", "1.4.5"): nupkg("Fdw.Results", "1.4.5"), ("Fdw.Abstractions", "1.4.2"): nupkg("Fdw.Abstractions", "1.4.2")}


class BundleTests(unittest.TestCase):
    def setUp(self):
        patcher = mock.patch.object(bundle, "NugetOrg", lambda: Feed(LIBRARIES))
        patcher.start()
        self.addCleanup(patcher.stop)

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
            bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out"), "--package-dir", tmp])
            self.assertTrue(os.path.exists(os.path.join(tmp, "out", "Fdw.Bundle.csproj")))

    def test_the_bundle_inherits_nothing_of_the_repo_it_is_generated_in(self):
        # FDW-5279: MSBuild imports the nearest Directory.Build.props, Directory.Build.targets and Directory.Packages.props upward from the
        # project's folder. The repo's Directory.Build.props adds analyzer references without a version and relies on central package
        # management, which the bundle turns off, so restore stopped with NU1015. Three empty files beside the project stop the inheritance.
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "m.json")
            with open(path, "w") as f:
                json.dump(manifest(), f)
            out = os.path.join(tmp, "out")
            bundle.main(["generate", "--manifest", path, "--out", out, "--package-dir", tmp])
            self.assertEqual(sorted(os.listdir(out)), ["Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "Fdw.Bundle.csproj"])
            for name in bundle.ISOLATION_FILES:
                root = ET.parse(os.path.join(out, name)).getroot()
                self.assertEqual((root.tag, len(list(root))), ("Project", 0), name)

    def test_the_isolation_files_are_the_three_msbuild_looks_for_upward(self):
        self.assertEqual(bundle.ISOLATION_FILES, ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props"))

    def test_the_only_package_references_of_the_project_are_the_pins(self):
        self.assertEqual(sorted(self.refs(manifest())), ["Fdw.Abstractions", "Fdw.Results"])
        self.assertTrue(all(v.startswith("[") and v.endswith("]") for v in self.refs(manifest()).values()))

    def packages(self, tmp, types_by_id, feed_only=()):
        """Write packages of the given types into tmp/pk (those not in feed_only), and return the manifest for them."""
        directory = os.path.join(tmp, "pk")
        os.makedirs(directory, exist_ok=True)
        held = {}
        for pid, types in types_by_id.items():
            data = nupkg(pid, "1.5.6", types)
            if pid in feed_only:
                held[(pid, "1.5.6")] = data
            else:
                with open(os.path.join(directory, f"{pid}.1.5.6.nupkg"), "wb") as f:
                    f.write(data)
        patcher = mock.patch.object(bundle, "NugetOrg", lambda: Feed(held))
        patcher.start()
        self.addCleanup(patcher.stop)
        path = os.path.join(tmp, "m.json")
        with open(path, "w") as f:
            json.dump(manifest(version="1.5.6", packages={pid: "1.5.6" for pid in types_by_id}), f)
        return path, directory

    def generate(self, tmp, types_by_id, feed_only=()):
        path, directory = self.packages(tmp, types_by_id, feed_only)
        out = os.path.join(tmp, "out")
        bundle.main(["generate", "--manifest", path, "--out", out, "--package-dir", directory])
        return out

    def pins(self, out):
        return sorted(r.get("Include") for r in ET.parse(os.path.join(out, "Fdw.Bundle.csproj")).getroot().iter("PackageReference"))

    def test_only_an_ordinary_dependency_is_pinned_whatever_other_type_a_package_declares(self):
        # NU1212: NuGet refuses a DotnetTool as a PackageReference. The type is read from the package, never from a list of names.
        types = {"A": [], "B": ["DotnetTool"], "C": ["Template"], "D": ["MSBuildSdk"], "E": ["McpServer"], "F": ["Dependency"], "G": ["Dependency", "DotnetTool"], "H": ["dotnettool"]}
        with tempfile.TemporaryDirectory() as tmp:
            out = self.generate(tmp, types)
            self.assertEqual(self.pins(out), ["A", "F"])
            with open(os.path.join(out, "not-pinned.json")) as f:
                stated = json.load(f)
            self.assertEqual(sorted(stated), ["B", "C", "D", "E", "G", "H"])
            self.assertEqual(stated["G"], {"version": "1.5.6", "types": ["Dependency", "DotnetTool"]})

    def test_what_is_left_out_is_stated_in_the_description_and_carried_in_the_bundle(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = self.generate(tmp, {"A": [], "B": ["DotnetTool"], "C": ["McpServer"]})
            root = ET.parse(os.path.join(out, "Fdw.Bundle.csproj")).getroot()
            description = root.find("PropertyGroup").findtext("Description")
            self.assertIn("pins 1 packages", description)
            self.assertIn("B 1.5.6 (DotnetTool)", description)
            self.assertIn("C 1.5.6 (McpServer)", description)
            self.assertIn("fdw", [n.get("PackagePath") for n in root.iter("None")])
            self.assertIn("not-pinned.json", [n.get("Include") for n in root.iter("None")])

    def test_with_nothing_to_leave_out_nothing_is_stated(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = self.generate(tmp, {"A": [], "B": []})
            self.assertFalse(os.path.exists(os.path.join(out, "not-pinned.json")))
            root = ET.parse(os.path.join(out, "Fdw.Bundle.csproj")).getroot()
            self.assertNotIn("Not pinned", root.find("PropertyGroup").findtext("Description"))

    def test_a_package_not_built_in_this_run_is_read_from_the_feed(self):
        with tempfile.TemporaryDirectory() as tmp:
            out = self.generate(tmp, {"A": [], "B": ["DotnetTool"]}, feed_only=("B",))
            self.assertEqual(self.pins(out), ["A"])

    def test_a_package_found_nowhere_stops_the_run(self):
        with tempfile.TemporaryDirectory() as tmp:
            path, directory = self.packages(tmp, {"A": []})
            with open(path, "w") as f:
                json.dump(manifest(version="1.5.6", packages={"A": "1.5.6", "Ghost": "1.5.6"}), f)
            with self.assertRaises(SystemExit) as raised:
                bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out"), "--package-dir", directory])
            self.assertIn("Ghost 1.5.6", str(raised.exception))
            self.assertFalse(os.path.exists(os.path.join(tmp, "out")))

    def test_a_bundle_of_only_left_out_packages_is_refused(self):
        with tempfile.TemporaryDirectory() as tmp:
            path, directory = self.packages(tmp, {"B": ["DotnetTool"]})
            with self.assertRaises(SystemExit) as raised:
                bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out"), "--package-dir", directory])
            self.assertIn("pins nothing", str(raised.exception))

    def test_the_package_dirs_are_required(self):
        with tempfile.TemporaryDirectory() as tmp:
            path, _ = self.packages(tmp, {"A": []})
            with self.assertRaises(SystemExit), mock.patch("sys.stderr", io.StringIO()):
                bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out")])

    def test_the_project_turns_off_the_assembly_copy_check_because_it_has_no_build_output(self):
        # MSBL001: a package of the release brings Microsoft.Build, and Microsoft.Build.Locator's buildTransitive check then fires.
        props = ET.fromstring(bundle.project_text(manifest())).find("PropertyGroup")
        self.assertEqual((props.findtext("DisableMSBuildAssemblyCopyCheck"), props.findtext("IncludeBuildOutput")), ("true", "false"))

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
                         "--project-manifest", os.path.join(tmp, "p.json"), "--package-dir", tmp])
            with open(os.path.join(tmp, "out", "manifest.json")) as f:
                self.assertEqual(json.load(f)["projects"], {"A": "x"})

    def test_a_project_manifest_without_projects_is_refused(self):
        with tempfile.TemporaryDirectory() as tmp:
            for name, data in (("m.json", manifest()), ("p.json", {"nothing": 1})):
                with open(os.path.join(tmp, name), "w") as f:
                    json.dump(data, f)
            with self.assertRaises(SystemExit):
                bundle.main(["generate", "--manifest", os.path.join(tmp, "m.json"), "--out", os.path.join(tmp, "out"),
                             "--project-manifest", os.path.join(tmp, "p.json"), "--package-dir", tmp])
            self.assertFalse(os.path.exists(os.path.join(tmp, "out")))

    def test_the_command_writes_nothing_for_a_bad_manifest(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "m.json")
            with open(path, "w") as f:
                json.dump(manifest(packages={"Fdw.Results": "1.4.*"}), f)
            with self.assertRaises(SystemExit):
                bundle.main(["generate", "--manifest", path, "--out", os.path.join(tmp, "out"), "--package-dir", tmp])
            self.assertFalse(os.path.exists(os.path.join(tmp, "out")))


if __name__ == "__main__":
    unittest.main()

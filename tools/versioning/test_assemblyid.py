import glob
import os
import tempfile
import unittest
import zipfile

import assemblyid


def framework_dlls():
    """Real managed assemblies from the installed .NET shared framework: nothing is faked, so a table the reader sizes wrongly shows up here.
    A machine without .NET cannot run these tests, and they say so instead of passing."""
    roots = [os.environ.get("DOTNET_ROOT", ""), "/usr/share/dotnet", "/usr/lib/dotnet", "/usr/local/share/dotnet", r"C:\Program Files\dotnet"]
    for root in filter(None, roots):
        found = sorted(glob.glob(os.path.join(root, "shared", "Microsoft.NETCore.App", "*", "*.dll")))
        if found:
            return found
    raise AssertionError("no .NET shared framework found to read real assemblies from; set DOTNET_ROOT")


def named(dlls, stem):
    return next(d for d in dlls if os.path.basename(d) == stem + ".dll")


def read(path):
    with open(path, "rb") as f:
        return f.read()


class AssemblyIdTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.dlls = framework_dlls()

    def test_a_facade_names_itself_and_the_assembly_it_forwards_to(self):
        name, version, refs = assemblyid.read_assembly(read(named(self.dlls, "System.Diagnostics.Tools")))
        self.assertEqual(name, "System.Diagnostics.Tools")
        self.assertEqual(version[0], 10)
        self.assertEqual([r[0] for r in refs], ["System.Runtime"])

    def test_an_assembly_lists_every_assembly_it_references_with_the_version_it_asks_for(self):
        name, version, refs = assemblyid.read_assembly(read(named(self.dlls, "System.Runtime")))
        self.assertEqual(name, "System.Runtime")
        self.assertEqual({r[0] for r in refs}, {"System.Private.CoreLib", "System.Private.Uri"})
        self.assertTrue(all(len(v) == 4 and v[0] == 10 for _, v in refs))

    def test_the_largest_assembly_has_tables_wider_than_65535_rows_and_is_still_read(self):
        name, version, refs = assemblyid.read_assembly(read(named(self.dlls, "System.Private.CoreLib")))
        self.assertEqual((name, version[0], refs), ("System.Private.CoreLib", 10, []))

    def test_every_assembly_of_the_framework_is_read_and_is_named_by_its_file(self):
        for path in self.dlls:
            with self.subTest(path=os.path.basename(path)):
                try:
                    name, version, refs = assemblyid.read_assembly(read(path))
                except assemblyid.IdentityError as e:
                    if "managed" in str(e):
                        continue  # a native library in the framework folder
                    raise
                self.assertEqual(name, os.path.basename(path)[:-4])
                self.assertEqual(len(version), 4)

    def test_a_file_that_is_not_a_pe_image_is_refused(self):
        with self.assertRaisesRegex(assemblyid.IdentityError, "not a PE image"):
            assemblyid.read_assembly(b"hello, this is not an assembly" * 10)

    def test_a_truncated_image_is_refused_and_does_not_crash(self):
        data = read(named(self.dlls, "System.Diagnostics.Tools"))
        for cut in (100, 400, 1000):
            with self.subTest(cut=cut), self.assertRaises((assemblyid.IdentityError, ValueError)):
                assemblyid.read_assembly(data[:cut])

    def test_the_records_are_four_tab_separated_fields(self):
        lines = assemblyid.records("net10.0", read(named(self.dlls, "System.Diagnostics.Tools")))
        self.assertEqual(lines[0].split("\t")[:3], ["net10.0", "DEF", "System.Diagnostics.Tools"])
        self.assertEqual(lines[1].split("\t")[:3], ["net10.0", "REF", "System.Diagnostics.Tools->System.Runtime"])
        self.assertTrue(all(len(line.split("\t")) == 4 for line in lines))


class PackageIdentityTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.dll = read(named(framework_dlls(), "System.Diagnostics.Tools"))

    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)

    def package(self, entries):
        path = os.path.join(self._tmp.name, "p.nupkg")
        with zipfile.ZipFile(path, "w") as z:
            for name, data in entries.items():
                z.writestr(name, data)
        return path

    def test_only_the_runtime_assemblies_under_lib_are_read_each_with_its_framework(self):
        path = self.package({"lib/net10.0/A.dll": self.dll, "lib/netstandard2.0/A.dll": self.dll, "ref/net10.0/A.dll": self.dll,
                             "analyzers/dotnet/cs/G.dll": self.dll, "A.nuspec": b"<package/>", "lib/net10.0/readme.txt": b"x"})
        lines = assemblyid.package_identity(path)
        self.assertEqual(len(lines), 4)
        self.assertEqual({line.split("\t")[0] for line in lines}, {"net10.0", "netstandard2.0"})

    def test_the_records_are_sorted(self):
        path = self.package({"lib/net10.0/A.dll": self.dll, "lib/netstandard2.0/A.dll": self.dll})
        lines = assemblyid.package_identity(path)
        self.assertEqual(lines, sorted(lines))

    def test_a_package_with_no_runtime_assembly_gives_no_records(self):
        self.assertEqual(assemblyid.package_identity(self.package({"A.nuspec": b"<package/>", "analyzers/dotnet/cs/G.dll": self.dll})), [])

    def test_a_dll_directly_under_lib_is_refused(self):
        with self.assertRaisesRegex(assemblyid.IdentityError, "not under a target framework folder"):
            assemblyid.package_identity(self.package({"lib/A.dll": self.dll}))

    def test_a_dll_that_cannot_be_read_stops_the_run_and_names_the_entry(self):
        with self.assertRaisesRegex(assemblyid.IdentityError, r"lib/net10.0/Bad.dll"):
            assemblyid.package_identity(self.package({"lib/net10.0/Bad.dll": b"not an assembly"}))

    def test_the_command_writes_the_file(self):
        out = os.path.join(self._tmp.name, "out.txt")
        assemblyid.main(["identity", "--package", self.package({"lib/net10.0/A.dll": self.dll}), "--out", out])
        with open(out, encoding="utf-8") as f:
            text = f.read()
        self.assertTrue(text.endswith("\n"))
        self.assertEqual(text.splitlines()[0].split("\t")[:2], ["net10.0", "DEF"])

    def test_the_command_exits_with_the_reason_for_a_bad_package(self):
        with self.assertRaises(SystemExit) as raised:
            assemblyid.main(["identity", "--package", self.package({"lib/net10.0/Bad.dll": b"x"}), "--out", os.path.join(self._tmp.name, "o.txt")])
        self.assertIn("Bad.dll", str(raised.exception))

    def test_the_output_is_what_bindcheck_reads(self):
        import bindcheck
        defs, refs = bindcheck.parse("\n".join(assemblyid.package_identity(self.package({"lib/net10.0/A.dll": self.dll}))), "p")
        self.assertEqual(defs[0][0], "System.Diagnostics.Tools")
        self.assertEqual(refs[0][:2], ("System.Diagnostics.Tools", "System.Runtime"))


if __name__ == "__main__":
    unittest.main()

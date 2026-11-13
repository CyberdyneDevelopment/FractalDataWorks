import io
import os
import tempfile
import unittest
import zipfile

import feeds


class Feed:
    def __init__(self, held):
        self.held = held

    def download(self, package_id, version):
        return self.held.get((package_id, version))


class FetchTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.out = os.path.join(self._tmp.name, "A.nupkg")

    def test_a_held_package_is_saved(self):
        feeds.fetch(Feed({("A", "1.4.4"): b"zip"}), "A", "1.4.4", self.out)
        with open(self.out, "rb") as f:
            self.assertEqual(f.read(), b"zip")

    def test_a_package_the_feed_does_not_hold_stops_the_run_and_names_it(self):
        with self.assertRaisesRegex(feeds.FeedError, "does not hold A 1.4.5"):
            feeds.fetch(Feed({}), "A", "1.4.5", self.out)
        self.assertFalse(os.path.exists(self.out))


def package(nuspec):
    buffer = io.BytesIO()
    with zipfile.ZipFile(buffer, "w") as z:
        z.writestr("A.nuspec", nuspec)
    return buffer.getvalue()


NS = 'xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"'


class PackageTypesTests(unittest.TestCase):
    def test_a_package_that_declares_none_is_an_ordinary_dependency(self):
        self.assertEqual(feeds.package_types(package(f'<package {NS}><metadata><id>A</id><version>1.0.0</version></metadata></package>')), [])

    def test_the_declared_types_are_listed_in_order(self):
        text = f'<package {NS}><metadata><id>A</id><version>1.0.0</version><packageTypes><packageType name="DotnetTool" /><packageType name="McpServer" /></packageTypes></metadata></package>'
        self.assertEqual(feeds.package_types(package(text)), ["DotnetTool", "McpServer"])

    def test_a_nuspec_without_a_namespace_is_read_too(self):
        self.assertEqual(feeds.package_types(package('<package><metadata><id>A</id><packageTypes><packageType name="Template" /></packageTypes></metadata></package>')), ["Template"])


class PinsTests(unittest.TestCase):
    def nuspec(self, *dependencies):
        deps = "".join(f'<dependency id="{i}" version="{v}" />' for i, v in dependencies)
        return package(f'<package {NS}><metadata><id>Fdw.Bundle</id><version>1.5.6</version><dependencies><group targetFramework="net10.0">{deps}</group></dependencies></metadata></package>')

    def test_exact_pins_are_read_in_both_spellings(self):
        data = self.nuspec(("Fdw.A", "[1.5.6]"), ("Fdw.B", "[1.5.6, 1.5.6]"))
        self.assertEqual(feeds.pins_from_nupkg(data), {"Fdw.A": "1.5.6", "Fdw.B": "1.5.6"})

    def test_a_range_is_not_a_pin(self):
        data = self.nuspec(("Fdw.A", "[1.5.0, 2.0.0)"), ("Fdw.B", "1.5.0"), ("Fdw.C", "[1.5.6]"))
        self.assertEqual(feeds.pins_from_nupkg(data), {"Fdw.C": "1.5.6"})

    def test_a_package_without_dependencies_pins_nothing(self):
        self.assertEqual(feeds.pins_from_nupkg(package(f'<package {NS}><metadata><id>A</id><version>1.0.0</version></metadata></package>')), {})

    def test_the_feed_reads_the_pins_of_a_held_package(self):
        feed = feeds.NugetOrg()
        feed._package = lambda pid, version: self.nuspec(("Fdw.A", "[1.5.6]"))
        self.assertEqual(feed.pins("Fdw.Bundle", "1.5.6"), {"Fdw.A": "1.5.6"})

    def test_a_listed_version_that_cannot_be_read_is_an_error(self):
        feed = feeds.NugetOrg()
        feed._package = lambda pid, version: None
        with self.assertRaisesRegex(feeds.FeedError, "cannot be read"):
            feed.pins("Fdw.Bundle", "1.5.6")


class ReadableTreeTests(unittest.TestCase):
    def feed(self, body):
        feed = feeds.NugetOrg()
        feed._package = lambda pid, version: body
        return feed

    def test_a_package_without_a_marker_gives_none(self):
        self.assertIsNone(self.feed(package("<package/>")).readable_tree("A", "1.0.0"))

    def test_a_package_with_a_marker_gives_its_tree(self):
        buffer = io.BytesIO()
        with zipfile.ZipFile(buffer, "w") as z:
            z.writestr("fdw/tree.txt", "abc123\n")
        self.assertEqual(self.feed(buffer.getvalue()).readable_tree("A", "1.0.0"), "abc123")

    def test_a_listed_version_that_cannot_be_read_is_an_error(self):
        with self.assertRaisesRegex(feeds.FeedError, "A 1.0.0 but its package cannot be read"):
            self.feed(None).readable_tree("A", "1.0.0")


if __name__ == "__main__":
    unittest.main()

import io
import unittest
import zipfile

import feeds


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

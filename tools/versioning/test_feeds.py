import io
import json
import os
import tempfile
import unittest
import urllib.error
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


class ServesTests(unittest.TestCase):
    """FDW-5634: a push is accepted well before a restore can take the package; `serves` is true only when the index lists the version and
    its package downloads."""

    class Response:
        def __init__(self, body=b""):
            self.body = body

        def __enter__(self):
            return self

        def __exit__(self, *exc):
            return False

        def read(self):
            return self.body

    def opener(self, index, package_status):
        calls = []

        def open_(request, timeout=None):
            url = getattr(request, "full_url", request)
            calls.append((getattr(request, "method", None) or "GET", url))
            if url.endswith("/index.json"):
                if index is None:
                    raise urllib.error.HTTPError(url, 404, "not found", {}, None)
                return self.Response(json.dumps({"versions": index}).encode())
            if package_status == 200:
                return self.Response()
            raise urllib.error.HTTPError(url, package_status, "status", {}, None)
        open_.calls = calls
        return open_

    def test_a_listed_version_whose_package_downloads_is_served(self):
        open_ = self.opener(["1.5.6", "1.5.7"], 200)
        self.assertTrue(feeds.NugetOrg(open_).serves("Fdw.A", "1.5.7"))
        self.assertEqual(open_.calls[-1], ("HEAD", "https://api.nuget.org/v3-flatcontainer/fdw.a/1.5.7/fdw.a.1.5.7.nupkg"))

    def test_a_version_the_index_does_not_list_yet_is_not_served_and_asks_nothing_more(self):
        open_ = self.opener(["1.5.6"], 200)
        self.assertFalse(feeds.NugetOrg(open_).serves("Fdw.A", "1.5.7"))
        self.assertEqual(len(open_.calls), 1)

    def test_a_package_the_index_has_never_heard_of_is_not_served(self):
        self.assertFalse(feeds.NugetOrg(self.opener(None, 200)).serves("Fdw.A", "1.5.7"))

    def test_a_listed_version_whose_package_is_not_there_yet_is_not_served(self):
        self.assertFalse(feeds.NugetOrg(self.opener(["1.5.7"], 404)).serves("Fdw.A", "1.5.7"))

    def test_the_version_is_compared_without_regard_to_case(self):
        self.assertTrue(feeds.NugetOrg(self.opener(["1.5.7-RC.1"], 200)).serves("Fdw.A", "1.5.7-rc.1"))

    def test_any_other_failure_is_raised_not_read_as_not_served(self):
        with self.assertRaises(urllib.error.HTTPError):
            feeds.NugetOrg(self.opener(["1.5.7"], 500)).serves("Fdw.A", "1.5.7")

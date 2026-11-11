import contextlib
import io
import json
import os
import tempfile
import unittest

import surfaces

INFO = {"depends": {"A": [], "B": ["A"], "Gen": []}, "members": ["A", "B", "Gen", "New"],
        "last_stable": {"A": "1.4.4", "B": "1.4.3", "Gen": "1.4.1"}, "generators": {"Gen": ["B"]},
        "paths": {"A": "public/src/A/A.csproj", "B": "public/src/B/B.csproj", "Gen": "public/src/Gen/Gen.csproj", "New": "public/src/New/New.csproj"}}


class Feed:
    def __init__(self, held):
        self.held = held

    def download(self, package_id, version):
        return self.held.get((package_id, version))


class NeededTests(unittest.TestCase):
    def test_every_changed_package_has_an_old_and_a_new_line_except_generators_and_new_packages_have_no_old(self):
        self.assertEqual(surfaces.needed(INFO), [("old", "A", "1.4.4"), ("new", "A", "public/src/A/A.csproj"),
                                                 ("old", "B", "1.4.3"), ("new", "B", "public/src/B/B.csproj"),
                                                 ("new", "New", "public/src/New/New.csproj")])

    def test_one_package_and_side_gives_that_line_only(self):
        self.assertEqual(surfaces.needed(INFO, "B", "new"), [("new", "B", "public/src/B/B.csproj")])
        self.assertEqual(surfaces.needed(INFO, "B", "old"), [("old", "B", "1.4.3")])

    def test_a_generator_or_an_unknown_package_is_refused(self):
        with self.assertRaisesRegex(surfaces.SurfacesError, "generator"):
            surfaces.needed(INFO, "Gen", "new")
        with self.assertRaisesRegex(surfaces.SurfacesError, "not a packable package"):
            surfaces.needed(INFO, "Nope", "new")

    def test_a_package_without_a_recorded_project_file_stops(self):
        with self.assertRaisesRegex(surfaces.SurfacesError, "no project file"):
            surfaces.needed(INFO | {"paths": {}, "members": ["A"]})


class FetchTests(unittest.TestCase):
    def test_a_held_package_is_written_and_a_missing_one_is_named(self):
        with tempfile.TemporaryDirectory() as d:
            out = os.path.join(d, "x.nupkg")
            surfaces.fetch(Feed({("A", "1.4.4"): b"zip"}), "A", "1.4.4", out)
            with open(out, "rb") as f:
                self.assertEqual(f.read(), b"zip")
            with self.assertRaisesRegex(surfaces.SurfacesError, "A 1.4.5"):
                surfaces.fetch(Feed({}), "A", "1.4.5", out)


class CommandTests(unittest.TestCase):
    def test_the_command_prints_tab_separated_lines(self):
        with tempfile.TemporaryDirectory() as d:
            with open(os.path.join(d, "levels-input.json"), "w") as f:
                json.dump(INFO, f)
            out = io.StringIO()
            with contextlib.redirect_stdout(out):
                surfaces.main(["needed", "--plan-dir", d, "--package", "A", "--side", "old"])
            self.assertEqual(out.getvalue(), "old\tA\t1.4.4\n")


if __name__ == "__main__":
    unittest.main()

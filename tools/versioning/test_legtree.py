"""The --leg dirty-tree check of pack-push-internal.sh (FDW-6638).

The check refuses a tree with anything git reports, because anything untracked that can enter a pack is compiled into it
under a sha that does not name it. The suite itself leaves Python's __pycache__ behind, and that must not trip it: the
directory is ignored in the repo's .gitignore (the check stays plain `git status --porcelain`), so a stray source file
still refuses and Python's own cache does not.
"""
import os
import subprocess
import tempfile
import unittest

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", "..", ".."))
GITIGNORE = os.path.join(REPO, ".gitignore")
SCRIPT = os.path.join(REPO, "pack-push-internal.sh")


def git(cwd, *args):
    return subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True, check=True).stdout


def touch(root, relative):
    path = os.path.join(root, relative)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w") as handle:
        handle.write("x")


class LegTreeTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        git(self.tmp.name, "init", "-q")

    def porcelain(self):
        """What the script's check sees: its own command, with this repo's .gitignore as the ignore rules."""
        return git(self.tmp.name, "-c", f"core.excludesFile={GITIGNORE}", "status", "--porcelain").strip()

    def test_the_script_checks_with_plain_status_that_counts_untracked_files(self):
        text = open(SCRIPT).read()
        self.assertIn('git status --porcelain', text)
        self.assertNotIn("--untracked-files=no", text)
        self.assertNotIn("-uno", text)

    def test_pythons_cache_directory_is_ignored(self):
        self.assertEqual(subprocess.run(["git", "check-ignore", "-q", "public/tools/versioning/__pycache__/tail.cpython-314.pyc"], cwd=REPO).returncode, 0)

    def test_a_tree_holding_only_pythons_cache_is_clean(self):
        touch(self.tmp.name, "public/tools/versioning/__pycache__/tail.cpython-314.pyc")
        touch(self.tmp.name, "__pycache__/x.cpython-314.pyc")
        self.assertEqual(self.porcelain(), "")

    def test_a_stray_untracked_source_file_still_refuses(self):
        touch(self.tmp.name, "public/tools/versioning/__pycache__/tail.cpython-314.pyc")
        touch(self.tmp.name, "public/Fdw.Something/Stray.cs")
        self.assertNotEqual(self.porcelain(), "")
        self.assertIn("Stray.cs", git(self.tmp.name, "-c", f"core.excludesFile={GITIGNORE}", "status", "--porcelain", "-uall"))

    def test_a_stray_untracked_file_beside_the_cache_is_not_hidden(self):
        touch(self.tmp.name, "public/tools/versioning/__pycache__/tail.cpython-314.pyc")
        touch(self.tmp.name, "public/tools/versioning/Stray.cs")
        self.assertNotEqual(self.porcelain(), "")


class LegVersionIsOfflineTests(unittest.TestCase):
    """--leg must not ask nuget.org for a release number (FDW-6638): allocate.py reads it from the network, and a leg pack
    is built from the line in the checkout and from git. The script's text is checked, never run (packs are the build owner's)."""

    def setUp(self):
        self.text = open(SCRIPT).read()
        self.leg_start = self.text.index('if [ "$LEG" = "1" ]')
        self.else_at = self.text.index("\nelse\n", self.leg_start)
        self.leg = self.text[self.leg_start:self.else_at]

    def test_the_release_number_lookup_is_only_on_the_line_s_own_path(self):
        self.assertEqual(self.text.count('allocate.py" number'), 1)
        self.assertGreater(self.text.index('allocate.py" number'), self.else_at)

    def test_the_leg_path_builds_its_version_from_the_line_and_names_no_release_number(self):
        self.assertIn('tail.py" version --tier branch --line "$LINE"', self.leg)
        self.assertNotIn("--number", self.leg)
        self.assertNotIn("NUMBER", self.leg)

    def test_the_line_is_read_from_the_props_before_either_path(self):
        self.assertLess(self.text.index('LINE="${PREFIX%.*}"'), self.leg_start)


if __name__ == "__main__":
    unittest.main()

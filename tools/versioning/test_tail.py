import contextlib
import io
import unittest

import tail


def semver_key(version):
    """SemVer 2 precedence: the core numerically, a version with a tail below the same core without, identifiers one by one."""
    core, _, pre = version.partition("-")
    nums = tuple(int(x) for x in core.split("."))
    if not pre:
        return nums, (1,)
    ids = tuple((0, int(i), "") if i.isdigit() else (1, 0, i) for i in pre.split("."))
    return nums, (0, ids)


class VersionTests(unittest.TestCase):
    def test_internal_and_develop_name_their_tier(self):
        self.assertEqual(tail.version("internal", "1.4.7", "20261030", "3480"), "1.4.7-internal.20261030.3480")
        self.assertEqual(tail.version("develop", "1.4.7", "20261030", "812"), "1.4.7-develop.20261030.812")

    def test_prerelease_branches_make_a_release_candidate(self):
        self.assertEqual(tail.version("prerelease", "1.4.7", count="2"), "1.4.7-rc.2")
        self.assertEqual(tail.version("prerelease/1.4.7", "1.4.7", count="1"), "1.4.7-rc.1")

    def test_main_is_the_plain_number(self):
        self.assertEqual(tail.version("main", "1.4.7"), "1.4.7")

    def test_a_later_commit_in_a_tier_always_sorts_higher(self):
        older = tail.version("internal", "1.4.7", "20261030", "999")
        newer = tail.version("internal", "1.4.7", "20261031", "1000")
        same_day = tail.version("internal", "1.4.7", "20261031", "1001")
        self.assertLess(semver_key(older), semver_key(newer))
        self.assertLess(semver_key(newer), semver_key(same_day))
        self.assertLess(semver_key(tail.version("internal", "1.4.7", "20261030", "99")), semver_key(tail.version("internal", "1.4.7", "20261030", "100")))

    def test_the_two_tiers_never_produce_an_equal_version(self):
        for count in ("1", "50", "3480"):
            self.assertNotEqual(tail.version("internal", "1.4.7", "20261030", count), tail.version("develop", "1.4.7", "20261030", count))

    def test_the_stable_version_sorts_above_every_tail(self):
        stable = semver_key(tail.version("main", "1.4.7"))
        for branch in ("internal", "develop", "prerelease"):
            self.assertLess(semver_key(tail.version(branch, "1.4.7", "20261030", "5")), stable)

    def test_the_next_release_sorts_above_every_tail_of_this_one(self):
        self.assertLess(semver_key(tail.version("internal", "1.4.7", "20261030", "9999")), semver_key(tail.version("internal", "1.4.8", "20261001", "1")))

    def test_a_bad_input_is_refused_not_repaired(self):
        for args in (("internal", "1.4", "20261030", "5"), ("internal", "1.4.7", "2026-10-30", "5"), ("internal", "1.4.7", None, "5"),
                     ("internal", "1.4.7", "20261030", "0"), ("internal", "1.4.7", "20261030", None), ("internal", "1.4.7", "20261030", "05"),
                     ("feature/x", "1.4.7", "20261030", "5"), ("internal", "1.4.7-rc.1", "20261030", "5")):
            with self.assertRaises(tail.TailError, msg=args):
                tail.version(*args)


class RangeTests(unittest.TestCase):
    def test_a_tier_floats_its_own_label_with_a_wildcard_patch(self):
        self.assertEqual(tail.float_range("1.4", "internal"), "1.4.*-internal.*")
        self.assertEqual(tail.float_range("1.4", "develop"), "1.4.*-develop.*")

    def test_the_tier_is_read_from_the_environment(self):
        self.assertEqual(tail.tier_from_env({"FDW_TIER": "develop"}), "develop")

    def test_an_unset_tier_fails_and_names_the_setting(self):
        with self.assertRaisesRegex(tail.TailError, "FDW_TIER is not set"):
            tail.tier_from_env({})
        with self.assertRaisesRegex(tail.TailError, "FDW_TIER is not set"):
            tail.tier_from_env({"FDW_TIER": ""})

    def test_an_unknown_tier_fails(self):
        with self.assertRaisesRegex(tail.TailError, "not a tier"):
            tail.tier_from_env({"FDW_TIER": "prod"})
        with self.assertRaises(tail.TailError):
            tail.float_range("1.4", "rc")

    def test_a_bad_line_fails(self):
        for line in ("1", "1.4.0", "1.*", ""):
            with self.assertRaises(tail.TailError, msg=line):
                tail.float_range(line, "internal")


class CommandTests(unittest.TestCase):
    def run_main(self, *argv):
        out = io.StringIO()
        with contextlib.redirect_stdout(out):
            tail.main(list(argv))
        return out.getvalue().strip()

    def test_version_prints_the_version(self):
        self.assertEqual(self.run_main("version", "--branch", "internal", "--number", "1.4.7", "--date", "20261030", "--count", "5"), "1.4.7-internal.20261030.5")

    def test_range_prints_the_range(self):
        self.assertEqual(self.run_main("range", "--line", "1.4", "--tier", "develop"), "1.4.*-develop.*")

    def test_a_refusal_exits_with_the_reason(self):
        with self.assertRaises(SystemExit) as raised:
            self.run_main("version", "--branch", "feature/x", "--number", "1.4.7")
        self.assertIn("publishes to no tier", str(raised.exception))


if __name__ == "__main__":
    unittest.main()

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


def floats(line, tier, version):
    """Whether a restore floating `<line>.*-<tier>.*` takes this version: the same line, and a label that starts with `<tier>.`.

    Checked against NuGet.Versioning (FloatRange.Satisfies, .NET 10 SDK, FDW-6638): 1.6.0-internal.* is taken by
    1.6.*-internal.*, and 1.6.0-leg.* and 1.6.0-branch.* are not; 1.5.9-internal.* is taken by 1.5.* and not by 1.6.*.
    """
    core, _, pre = version.partition("-")
    return core.split(".")[:2] == line.split(".") and pre.startswith(tier + ".")


class BranchPackTests(unittest.TestCase):
    """A pack built from an unmerged leg (FDW-6638): a leg has more commits than its base, so under the internal label
    its count outranked the line's own pack and every consumer floating the line took it."""

    def test_a_branch_pack_names_its_label_and_its_commit(self):
        self.assertEqual(tail.branch_version("1.6", "20261117", "5371", "14fc4f9"), "1.6.0-branch.20261117.5371.g14fc4f9")

    def test_a_branch_pack_is_built_from_the_line_alone_its_core_is_the_lines_zero(self):
        # No release number: allocate.py reads nuget.org for it, and a leg pack must not need the network (FDW-6638).
        self.assertEqual(tail.branch_version("1.5", "20261117", "100", "14fc4f9"), "1.5.0-branch.20261117.100.g14fc4f9")
        self.assertEqual(tail.branch_version("2.0", "20261117", "100", "14fc4f9"), "2.0.0-branch.20261117.100.g14fc4f9")

    def test_a_branch_pack_sorts_below_every_internal_pack_of_its_line_whatever_release_number_it_has_reached(self):
        pack = semver_key(tail.branch_version("1.6", "20261117", "99999", "14fc4f9"))
        for number in ("1.6.0", "1.6.1", "1.6.7"):
            self.assertLess(pack, semver_key(tail.version("internal", number, "20261101", "1")), number)

    def test_a_branch_pack_sorts_below_the_lines_own_internal_pack_however_many_commits_it_has(self):
        own = tail.version("internal", "1.6.0", "20261117", "5362")
        for date, count in (("20261117", "5371"), ("20261117", "99999"), ("20261118", "5371")):
            self.assertLess(semver_key(tail.branch_version("1.6", date, count, "14fc4f9")), semver_key(own), (date, count))

    def test_a_branch_pack_is_not_taken_by_the_float_a_consumer_restores_on(self):
        pack = tail.branch_version("1.6", "20261117", "5371", "14fc4f9")
        self.assertFalse(floats("1.6", "internal", pack))
        self.assertTrue(floats("1.6", "internal", tail.version("internal", "1.6.0", "20261117", "5362")))
        self.assertFalse(floats("1.6", "develop", pack))

    def test_the_float_range_still_has_no_branch_tier(self):
        with self.assertRaisesRegex(tail.TailError, "not a tier"):
            tail.float_range("1.6", "branch")
        with self.assertRaisesRegex(tail.TailError, "not a tier"):
            tail.tier_from_env({"FDW_TIER": "branch"})

    def test_a_git_branch_never_publishes_as_a_branch_pack(self):
        for name in ("branch", "leg", "feature/FDW-6638"):
            with self.assertRaisesRegex(tail.TailError, "publishes to no tier"):
                tail.tier_for_branch(name)

    def test_a_later_commit_of_one_leg_sorts_higher_and_two_legs_never_share_a_version(self):
        a = tail.branch_version("1.6", "20261117", "5371", "14fc4f9")
        self.assertLess(semver_key(a), semver_key(tail.branch_version("1.6", "20261117", "5372", "0123abc")))
        self.assertNotEqual(a, tail.branch_version("1.6", "20261117", "5371", "f8d0fb5"))

    def test_a_branch_pack_sorts_below_the_stable_version_of_its_number(self):
        self.assertLess(semver_key(tail.branch_version("1.6", "20261117", "5371", "14fc4f9")), semver_key(tail.version("main", "1.6.0")))

    def test_a_bad_input_is_refused_not_repaired(self):
        for args in (("1.6.0", "20261117", "5371", "14fc4f9"), ("1", "20261117", "5371", "14fc4f9"), ("1.*", "20261117", "5371", "14fc4f9"),
                     ("1.6", "2026-11-17", "5371", "14fc4f9"), ("1.6", None, "5371", "14fc4f9"),
                     ("1.6", "20261117", "0", "14fc4f9"), ("1.6", "20261117", None, "14fc4f9"), ("1.6", "20261117", "5371", None),
                     ("1.6", "20261117", "5371", ""), ("1.6", "20261117", "5371", "14FC4F9"), ("1.6", "20261117", "5371", "14fc4"),
                     ("1.6", "20261117", "5371", "14fc4f9.x"), (None, "20261117", "5371", "14fc4f9")):
            with self.assertRaises(tail.TailError, msg=args):
                tail.branch_version(*args)


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

    def test_version_prints_a_branch_pack_only_when_the_tier_is_asked_for(self):
        self.assertEqual(self.run_main("version", "--tier", "branch", "--line", "1.6", "--date", "20261117", "--count", "5371", "--sha", "14fc4f9"),
                         "1.6.0-branch.20261117.5371.g14fc4f9")

    def test_a_branch_pack_takes_no_release_number_and_the_other_tiers_no_line(self):
        for argv in (("--tier", "branch", "--line", "1.6", "--number", "1.6.0", "--date", "20261117", "--count", "5", "--sha", "14fc4f9"),
                     ("--tier", "branch", "--number", "1.6.0", "--date", "20261117", "--count", "5", "--sha", "14fc4f9"),
                     ("--branch", "internal", "--line", "1.6", "--number", "1.6.0", "--date", "20261117", "--count", "5")):
            with self.assertRaises(SystemExit, msg=argv):
                self.run_main("version", *argv)

    def test_version_takes_a_branch_or_the_branch_tier_never_both_or_neither(self):
        for argv in (("--number", "1.6.0"), ("--branch", "internal", "--tier", "branch", "--line", "1.6", "--date", "20261117", "--count", "5", "--sha", "14fc4f9")):
            with self.assertRaises(SystemExit, msg=argv):
                self.run_main("version", *argv)

    def test_the_branch_tier_is_the_only_tier_version_takes_and_it_needs_a_sha(self):
        for argv in (("--tier", "internal", "--line", "1.6", "--date", "20261117", "--count", "5", "--sha", "14fc4f9"),
                     ("--tier", "branch", "--line", "1.6", "--date", "20261117", "--count", "5")):
            with self.assertRaises(SystemExit, msg=argv):
                self.run_main("version", *argv)


if __name__ == "__main__":
    unittest.main()

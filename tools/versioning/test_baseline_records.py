import os
import unittest

import release

BASELINE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "baseline")


class RepoRecordTests(unittest.TestCase):
    """The records committed in tools/versioning/baseline are what the next plan reads, so a malformed or misnamed one must fail here, not in a release."""

    def test_the_latest_record_of_the_repo_is_a_valid_baseline(self):
        path = release.latest_record(BASELINE)
        baseline, pins = release.baseline_from_record(path)
        self.assertGreater(len(baseline["projects"]), 0)
        self.assertGreater(len(pins), 0)
        self.assertEqual(release.check_packages(pins), [])

    def test_every_record_that_states_a_release_is_named_for_it(self):
        for number, path in release.records_in(BASELINE).items():
            record = release.read_json(path)
            if "release" in record:
                self.assertEqual(record["release"], number, path)

    def test_the_1_5_5_manifest_is_history_and_a_plan_refuses_it_as_a_baseline(self):
        with self.assertRaisesRegex(release.ReleaseError, "BASELINE_INVALID.*no 'release'"):
            release.read_record(os.path.join(BASELINE, "release-FDW-1.5.5.json"))


if __name__ == "__main__":
    unittest.main()

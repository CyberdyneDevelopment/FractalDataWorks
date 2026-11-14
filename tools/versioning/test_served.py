import json
import os
import tempfile
import unittest

import served


class Feed:
    """serves(id, version) is true once `after` polls of that pair have been made; never=True means never."""
    def __init__(self, after=None):
        self.after = after or {}
        self.asked = {}

    def serves(self, pid, version):
        n = self.asked[(pid, version)] = self.asked.get((pid, version), 0) + 1
        after = self.after.get((pid, version), 0)
        return after is not None and n > after


class Clock:
    def __init__(self):
        self.now = 0

    def __call__(self):
        return self.now

    def sleep(self, seconds):
        self.now += seconds


WANTED = [("Fdw.A", "1.5.7"), ("Fdw.B", "1.5.7"), ("Fdw.Bundle", "1.5.7")]


class WaitTests(unittest.TestCase):
    def wait(self, feed, wanted=WANTED, timeout=600, interval=30):
        clock, said = Clock(), []
        pending = served.wait(feed, wanted, timeout, interval, clock=clock, sleep=clock.sleep, say=said.append)
        return pending, clock.now, said

    def test_what_is_already_served_costs_no_wait(self):
        pending, spent, said = self.wait(Feed())
        self.assertEqual((pending, spent, said), ([], 0, []))

    def test_it_waits_until_the_last_package_is_served(self):
        feed = Feed({("Fdw.A", "1.5.7"): 2, ("Fdw.B", "1.5.7"): 1})
        pending, spent, said = self.wait(feed)
        self.assertEqual((pending, spent), ([], 60))
        self.assertEqual(len(said), 2)

    def test_only_what_is_not_served_is_asked_again(self):
        feed = Feed({("Fdw.B", "1.5.7"): 3})
        self.wait(feed)
        self.assertEqual(feed.asked, {("Fdw.A", "1.5.7"): 1, ("Fdw.B", "1.5.7"): 4, ("Fdw.Bundle", "1.5.7"): 1})

    def test_at_the_timeout_the_ones_not_served_are_returned_in_the_order_given(self):
        feed = Feed({("Fdw.A", "1.5.7"): None, ("Fdw.Bundle", "1.5.7"): None})
        pending, spent, _ = self.wait(feed, timeout=100, interval=30)
        self.assertEqual(pending, [("Fdw.A", "1.5.7"), ("Fdw.Bundle", "1.5.7")])
        self.assertGreaterEqual(spent, 100)

    def test_nothing_to_wait_for_stops_the_run(self):
        with self.assertRaisesRegex(served.ServedError, "nothing to wait for"):
            self.wait(Feed(), wanted=[])

    def test_a_timeout_or_an_interval_below_one_second_stops_the_run(self):
        with self.assertRaisesRegex(served.ServedError, "at least 1 second"):
            self.wait(Feed(), timeout=0)
        with self.assertRaisesRegex(served.ServedError, "at least 1 second"):
            self.wait(Feed(), interval=0)


class MainTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.manifest = os.path.join(self._tmp.name, "manifest.json")
        with open(self.manifest, "w") as f:
            json.dump({"packages": {"Fdw.B": "1.5.7", "Fdw.A": "1.5.2"}}, f)

    def test_the_manifest_and_the_named_packages_are_all_waited_for(self):
        feed, clock = Feed(), Clock()
        served.main(["wait", "--timeout-minutes", "1", "--interval-seconds", "1", "--manifest", self.manifest,
                     "--package", "Fdw.Bundle", "1.5.7"], feed=feed, clock=clock, sleep=clock.sleep, say=lambda line: None)
        self.assertEqual(sorted(feed.asked), [("Fdw.A", "1.5.2"), ("Fdw.B", "1.5.7"), ("Fdw.Bundle", "1.5.7")])

    def test_a_timeout_fails_naming_every_package_not_served(self):
        feed = Feed({("Fdw.B", "1.5.7"): None, ("Fdw.Bundle", "1.5.7"): None})
        clock = Clock()
        with self.assertRaises(SystemExit) as stopped:
            served.main(["wait", "--timeout-minutes", "1", "--interval-seconds", "60", "--manifest", self.manifest,
                         "--package", "Fdw.Bundle", "1.5.7"], feed=feed, clock=clock, sleep=clock.sleep, say=lambda line: None)
        message = str(stopped.exception)
        self.assertIn("Fdw.B 1.5.7, Fdw.Bundle 1.5.7", message)
        self.assertIn("2 of 3", message)
        self.assertIn("half done", message)
        self.assertIn("do NOT tag", message)

    def test_the_limits_have_no_default(self):
        with self.assertRaises(SystemExit):
            served.main(["wait", "--manifest", self.manifest], feed=Feed())


if __name__ == "__main__":
    unittest.main()

import contextlib
import io
import json
import os
import tempfile
import unittest

import served


class Feed:
    """serves(id, version) is true from the poll after `after` polls of that pair; None means never. `flaps` {pair: [poll numbers that answer no]} makes a
    pair answer no at those polls even when it is served (an edge that is not in step)."""
    def __init__(self, after=None, flaps=None):
        self.after = after or {}
        self.flaps = flaps or {}
        self.asked = {}

    def serves(self, pid, version):
        n = self.asked[(pid, version)] = self.asked.get((pid, version), 0) + 1
        after = self.after.get((pid, version), 0)
        return after is not None and n > after and n not in self.flaps.get((pid, version), ())


class Clock:
    def __init__(self):
        self.now = 0

    def __call__(self):
        return self.now

    def sleep(self, seconds):
        self.now += seconds


WANTED = [("Fdw.A", "1.5.8"), ("Fdw.B", "1.5.8"), ("Fdw.C", "1.5.8")]


class WaitTests(unittest.TestCase):
    def wait(self, feed, wanted=WANTED, timeout=600, interval=30, looks=1):
        clock, said = Clock(), []
        pending = served.wait(feed, wanted, timeout, interval, looks, clock=clock, sleep=clock.sleep, say=said.append)
        return pending, clock.now, said

    def test_what_is_already_served_costs_no_wait_with_one_look(self):
        pending, spent, said = self.wait(Feed())
        self.assertEqual((pending, spent, said), ([], 0, []))

    def test_it_waits_until_the_last_package_is_served(self):
        feed = Feed({("Fdw.A", "1.5.8"): 2, ("Fdw.B", "1.5.8"): 1})
        pending, spent, said = self.wait(feed)
        self.assertEqual((pending, spent), ([], 60))
        self.assertEqual(len(said), 2)

    def test_while_something_is_missing_only_what_is_missing_is_asked_again(self):
        feed = Feed({("Fdw.B", "1.5.8"): 3})
        self.wait(feed)
        # the first look asks all; B is asked three more times; once B is served the whole list is looked at again (the first complete look)
        self.assertEqual(feed.asked, {("Fdw.A", "1.5.8"): 2, ("Fdw.B", "1.5.8"): 5, ("Fdw.C", "1.5.8"): 2})

    def test_each_progress_line_names_the_first_ids_still_missing(self):
        feed = Feed({("Fdw.B", "1.5.8"): 1, ("Fdw.C", "1.5.8"): 1})
        _, _, said = self.wait(feed)
        self.assertEqual(len(said), 1)
        self.assertIn("1 of 3 served", said[0])
        self.assertIn("Fdw.B 1.5.8, Fdw.C 1.5.8", said[0])

    def test_a_long_list_of_missing_ids_is_cut_after_five(self):
        wanted = [(f"Fdw.P{i}", "1.5.8") for i in range(8)]
        feed = Feed({pair: 1 for pair in wanted})
        _, _, said = self.wait(feed, wanted=wanted)
        self.assertIn("and 3 more", said[0])

    def test_with_two_looks_everything_is_looked_at_twice_in_a_row(self):
        feed = Feed()
        pending, spent, said = self.wait(feed, looks=2)
        self.assertEqual((pending, spent), ([], 30))
        self.assertEqual(set(feed.asked.values()), {2})
        self.assertIn("look 1 of 2", said[0])

    def test_a_package_that_goes_missing_again_starts_the_count_again(self):
        # Fdw.B answers no at its second poll, as a stale edge did for Fdw.Data.Transformations.Boolean (1.5.8)
        feed = Feed(flaps={("Fdw.B", "1.5.8"): [2]})
        pending, spent, said = self.wait(feed, looks=2)
        self.assertEqual(pending, [])
        self.assertEqual(spent, 90)   # look 1 complete, look 2 not, then the missing one again, then two complete looks
        self.assertEqual(feed.asked[("Fdw.A", "1.5.8")], 4)

    def test_at_the_timeout_the_ones_not_served_are_returned_in_the_order_given(self):
        feed = Feed({("Fdw.A", "1.5.8"): None, ("Fdw.C", "1.5.8"): None})
        pending, spent, _ = self.wait(feed, timeout=100, interval=30)
        self.assertEqual(pending, [("Fdw.A", "1.5.8"), ("Fdw.C", "1.5.8")])
        self.assertGreaterEqual(spent, 100)

    def test_a_package_that_keeps_going_missing_is_named_at_the_timeout(self):
        feed = Feed(flaps={("Fdw.B", "1.5.8"): [2, 4, 6, 8, 10, 12]})
        pending, _, _ = self.wait(feed, timeout=100, interval=30, looks=2)
        self.assertEqual(pending, [("Fdw.B", "1.5.8")])

    def test_everything_served_at_the_last_look_but_for_too_few_looks_stops_the_run(self):
        with self.assertRaisesRegex(served.ServedError, "not for 3 consecutive looks"):
            self.wait(Feed(), timeout=30, interval=30, looks=3)

    def test_nothing_to_wait_for_stops_the_run(self):
        with self.assertRaisesRegex(served.ServedError, "nothing to wait for"):
            self.wait(Feed(), wanted=[])

    def test_a_timeout_an_interval_or_a_number_of_looks_below_one_stops_the_run(self):
        for kwargs in ({"timeout": 0}, {"interval": 0}, {"looks": 0}):
            with self.assertRaisesRegex(served.ServedError, "at least 1"):
                self.wait(Feed(), **kwargs)


class FlushRecorder(io.StringIO):
    def __init__(self):
        super().__init__()
        self.flushes = 0

    def flush(self):
        self.flushes += 1
        super().flush()


class MainTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self._tmp.cleanup)
        self.manifest = os.path.join(self._tmp.name, "manifest.json")
        with open(self.manifest, "w") as f:
            json.dump({"release": "1.5.8", "packages": {"Fdw.B": "1.5.8", "Fdw.A": "1.5.2"}}, f)

    def run_main(self, feed, *extra, looks="1"):
        clock = Clock()
        served.main(["wait", "--timeout-minutes", "1", "--interval-seconds", "1", "--looks", looks, "--manifest", self.manifest, *extra],
                    feed=feed, clock=clock, sleep=clock.sleep, say=lambda line: None)

    def test_the_manifest_and_the_named_packages_are_all_waited_for(self):
        feed = Feed()
        self.run_main(feed, "--package", "Fdw.C", "1.5.8")
        self.assertEqual(sorted(feed.asked), [("Fdw.A", "1.5.2"), ("Fdw.B", "1.5.8"), ("Fdw.C", "1.5.8")])

    def test_a_timeout_fails_naming_every_package_not_served(self):
        feed = Feed({("Fdw.B", "1.5.8"): None, ("Fdw.C", "1.5.8"): None})
        clock = Clock()
        with self.assertRaises(SystemExit) as stopped:
            served.main(["wait", "--timeout-minutes", "1", "--interval-seconds", "60", "--looks", "1", "--manifest", self.manifest,
                         "--package", "Fdw.C", "1.5.8"], feed=feed, clock=clock, sleep=clock.sleep, say=lambda line: None)
        message = str(stopped.exception)
        self.assertIn("Fdw.B 1.5.8, Fdw.C 1.5.8", message)
        self.assertIn("2 of 3", message)
        self.assertIn("half done", message)
        self.assertIn("do NOT tag", message)

    def test_the_limits_and_the_looks_have_no_default(self):
        for missing in ("--timeout-minutes", "--interval-seconds", "--looks"):
            args = ["wait", "--manifest", self.manifest]
            for flag, value in (("--timeout-minutes", "1"), ("--interval-seconds", "1"), ("--looks", "1")):
                if flag != missing:
                    args += [flag, value]
            with self.assertRaises(SystemExit), contextlib.redirect_stderr(io.StringIO()):
                served.main(args, feed=Feed())

    def test_every_progress_line_is_flushed_when_it_is_printed(self):
        # FDW-5708: the 1.5.8 wait logged nothing for 19 minutes because Python buffers stdout when it is not a terminal
        stream = FlushRecorder()
        with contextlib.redirect_stdout(stream):
            served.say_now("served: 1 of 3")
            served.say_now("served: 2 of 3")
        self.assertEqual(stream.flushes, 2)

    def test_the_default_say_of_the_command_is_the_flushing_one(self):
        self.assertIs(served.main.__defaults__[-1], served.say_now)


if __name__ == "__main__":
    unittest.main()

import io
import json
import os
import tempfile
import unittest
import urllib.error
import zipfile
from unittest import mock

import allocate
import feeds
import pushcheck
from fdwprojects import snapshot
from test_changeset import Repo, base_files


def nupkg(pid, version, tree=None):
    buf = io.BytesIO()
    with zipfile.ZipFile(buf, "w") as z:
        z.writestr(f"{pid}.nuspec", '<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">'
                   f"<metadata><id>{pid}</id><version>{version}</version></metadata></package>")
        if tree:
            z.writestr(feeds.TREE_MARKER, tree + "\n")
    return buf.getvalue()


class FakeFeed:
    """A feed that remembers what was pushed to it: package id -> {version: tree}."""
    def __init__(self, held=None):
        self.held = {pid: dict(v) for pid, v in (held or {}).items()}

    def versions(self, pid):
        return list(self.held.get(pid, {}))

    def held_tree(self, pid, version):
        return self.held[pid][version]

    def held_file(self, pid, version, path):
        return getattr(self, "files", {}).get((pid, version, path))

    def push(self, decision, tree):
        self.held.setdefault(decision.package, {})[decision.version] = tree


class NextVersionTests(unittest.TestCase):
    def test_a_patch_is_one_above_the_highest_in_the_line(self):
        self.assertEqual(allocate.next_version(["1.4.3", "1.4.4"], "1.4"), "1.4.5")

    def test_a_version_with_a_tail_does_not_spend_its_number(self):
        self.assertEqual(allocate.next_version(["1.4.4", "1.4.5-rc.1", "1.4.5-internal.20261030.g1a2b3c"], "1.4"), "1.4.5")

    def test_a_partial_stable_push_spends_its_number(self):
        self.assertEqual(allocate.next_version(["1.4.4", "1.4.5"], "1.4"), "1.4.6")

    def test_release_tags_spend_their_number(self):
        tags = allocate.tag_versions(["release-FDW-1.4.4", "release-1.5.0", "release-RUI-1.4.9", "v1.0.0", "release-notes"])
        self.assertEqual(tags, ["1.4.4", "1.5.0", "1.4.9"])
        self.assertEqual(allocate.next_version(["1.4.4"] + tags, "1.4"), "1.4.10")

    def test_versions_in_other_lines_do_not_count_for_a_patch(self):
        self.assertEqual(allocate.next_version(["1.3.9", "1.5.2", "2.0.0"], "1.4"), "1.4.0")

    def test_a_package_with_no_versions_starts_the_line(self):
        self.assertEqual(allocate.next_version([], "1.4"), "1.4.0")

    def test_a_minor_bump_goes_above_the_highest_minor_of_the_major(self):
        self.assertEqual(allocate.next_version(["1.4.4", "1.6.1"], "1.4", "minor"), "1.7.0")

    def test_a_major_bump_goes_above_the_highest_major(self):
        self.assertEqual(allocate.next_version(["1.4.4", "2.1.0"], "1.4", "major"), "3.0.0")

    def test_text_that_is_not_a_version_is_ignored(self):
        self.assertEqual(allocate.next_version(["latest", "1.4.x", "1.4.2"], "1.4"), "1.4.3")

    def test_build_metadata_does_not_hide_a_number(self):
        self.assertEqual(allocate.next_version(["1.4.7+abc"], "1.4"), "1.4.8")

    def test_the_highest_declared_bump_wins(self):
        self.assertEqual(allocate.declared_bump(["fix\n", "x\n\nbump: minor\n", "y\nBump: patch\n"]), "minor")
        self.assertEqual(allocate.declared_bump(["fix\n"]), "patch")
        self.assertEqual(allocate.declared_bump(["a\nbump: minor\n", "b\nbump: major\n"]), "major")
        self.assertEqual(allocate.declared_bump(["we should bump: major someday, maybe\n"]), "patch")


class BumpFromCommitsTests(unittest.TestCase):
    def test_only_commits_that_touched_the_package_declare_for_it(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = Repo(tmp)
            base = repo.commit(base_files(), "base")
            repo.commit({"public/src/A/A.cs": "class A { int x; }\n"}, "A change\n\nbump: minor")
            repo.commit({"public/src/C/C.cs": "class C { int x; }\n"}, "C change")
            self.assertEqual(allocate.bumps_in_range(tmp, ["public/src/A"], base, "HEAD"), "minor")
            self.assertEqual(allocate.bumps_in_range(tmp, ["public/src/C"], base, "HEAD"), "patch")


class FeedTests(unittest.TestCase):
    def opener(self, routes):
        def open_(url, timeout=0):
            if url not in routes:
                raise urllib.error.HTTPError(url, 404, "no", {}, io.BytesIO())
            body = routes[url]
            if isinstance(body, int):
                raise urllib.error.HTTPError(url, body, "server", {}, io.BytesIO())
            return _Response(body)
        return open_

    def test_versions_and_the_held_tree_are_read(self):
        base = feeds.NugetOrg.base
        routes = {f"{base}/fdw.x/index.json": json.dumps({"versions": ["1.4.4", "1.4.5-rc.1"]}).encode(),
                  f"{base}/fdw.x/1.4.4/fdw.x.1.4.4.nupkg": nupkg("Fdw.X", "1.4.4", "abc123")}
        feed = feeds.NugetOrg(self.opener(routes))
        self.assertEqual(feed.versions("Fdw.X"), ["1.4.4", "1.4.5-rc.1"])
        self.assertEqual(feed.held_tree("Fdw.X", "1.4.4"), "abc123")

    def test_a_file_inside_a_held_package_is_read(self):
        base = feeds.NugetOrg.base
        data = nupkg("Fdw.Bundle", "1.4.5", "t")
        buf = io.BytesIO(data)
        with zipfile.ZipFile(buf, "a") as z:
            z.writestr(feeds.MANIFEST_FILE, '{"projects": {}}')
        routes = {f"{base}/fdw.bundle/1.4.5/fdw.bundle.1.4.5.nupkg": buf.getvalue()}
        feed = feeds.NugetOrg(self.opener(routes))
        self.assertEqual(feed.held_file("Fdw.Bundle", "1.4.5", feeds.MANIFEST_FILE), '{"projects": {}}')
        self.assertIsNone(feed.held_file("Fdw.Bundle", "1.4.5", "fdw/nothing.json"))
        self.assertIsNone(feed.held_file("Fdw.Bundle", "9.9.9", feeds.MANIFEST_FILE))

    def test_a_package_that_does_not_exist_has_no_versions(self):
        self.assertEqual(feeds.NugetOrg(self.opener({})).versions("Fdw.Nope"), [])

    def test_any_other_failure_raises(self):
        base = feeds.NugetOrg.base
        feed = feeds.NugetOrg(self.opener({f"{base}/fdw.x/index.json": 503}))
        with self.assertRaises(urllib.error.HTTPError):
            feed.versions("Fdw.X")


class _Response:
    def __init__(self, body): self._b = body
    def read(self): return self._b
    def __enter__(self): return self
    def __exit__(self, *a): return False


class CheckThenPushTests(unittest.TestCase):
    def built(self, trees, version="1.4.5"):
        return [(pid, version, tree, f"/p/{pid}.{version}.nupkg") for pid, tree in trees.items()]

    def test_a_version_that_is_not_on_the_feed_is_pushed(self):
        d = pushcheck.decide(self.built({"A": "t1"}), FakeFeed())
        self.assertEqual([x.action for x in d], ["push"])

    def test_a_version_already_there_with_the_same_contents_is_skipped(self):
        d = pushcheck.decide(self.built({"A": "t1"}), FakeFeed({"A": {"1.4.5": "t1"}}))
        self.assertEqual([x.action for x in d], ["skip"])

    def test_a_version_there_with_different_contents_is_a_stop(self):
        d = pushcheck.decide(self.built({"A": "t2"}), FakeFeed({"A": {"1.4.5": "t1"}}))
        self.assertEqual(d[0].action, "stop")
        self.assertIn("t1", d[0].reason)
        self.assertIn("t2", d[0].reason)

    def test_a_held_version_with_no_marker_is_a_stop(self):
        d = pushcheck.decide(self.built({"A": "t1"}), FakeFeed({"A": {"1.4.5": None}}))
        self.assertEqual(d[0].action, "stop")

    def test_a_built_package_with_no_marker_is_a_stop(self):
        d = pushcheck.decide(self.built({"A": None}), FakeFeed())
        self.assertEqual(d[0].action, "stop")

    def test_one_stop_means_nothing_is_pushed(self):
        d = pushcheck.decide(self.built({"A": "t2", "B": "t1"}), FakeFeed({"A": {"1.4.5": "t1"}}))
        self.assertEqual(pushcheck.to_push(d), [])

    def test_a_partial_push_then_a_fix_stops_and_the_same_rerun_finishes(self):
        feed = FakeFeed()
        first = pushcheck.decide(self.built({"A": "tA", "B": "tB", "C": "tC"}), feed)
        pushed = []
        for d in pushcheck.to_push(first):
            if d.package == "C":
                break  # the run dies before C is pushed
            feed.push(d, {"A": "tA", "B": "tB"}[d.package]); pushed.append(d.package)
        self.assertEqual(pushed, ["A", "B"])

        fixed = pushcheck.decide(self.built({"A": "tA2", "B": "tB", "C": "tC"}), feed)
        self.assertEqual({d.package: d.action for d in fixed}, {"A": "stop", "B": "skip", "C": "push"})
        self.assertEqual(pushcheck.to_push(fixed), [])  # no mixed release

        unfixed = pushcheck.decide(self.built({"A": "tA", "B": "tB", "C": "tC"}), feed)
        self.assertEqual([d.package for d in pushcheck.to_push(unfixed)], ["C"])

    def test_version_case_does_not_hide_a_held_version(self):
        d = pushcheck.decide(self.built({"A": "t1"}, "1.4.5-RC.1"), FakeFeed({"A": {"1.4.5-rc.1": "t1"}}))
        self.assertEqual(d[0].action, "skip")

    def test_packages_are_read_from_a_directory(self):
        with tempfile.TemporaryDirectory() as tmp:
            for name, data in (("A.1.4.5.nupkg", nupkg("A", "1.4.5", "t1")), ("A.1.4.5.symbols.nupkg", nupkg("A", "1.4.5", "t1")),
                               ("B.1.4.5.nupkg", nupkg("B", "1.4.5"))):
                with open(os.path.join(tmp, name), "wb") as f:
                    f.write(data)
            built = pushcheck.read_directory(tmp)
        self.assertEqual([(p, v, t) for p, v, t, _ in built], [("A", "1.4.5", "t1"), ("B", "1.4.5", None)])

    def test_the_command_exits_1_on_a_stop(self):
        with tempfile.TemporaryDirectory() as tmp:
            with open(os.path.join(tmp, "A.1.4.5.nupkg"), "wb") as f:
                f.write(nupkg("A", "1.4.5", "t2"))
            with mock.patch.object(pushcheck, "NugetOrg", lambda: FakeFeed({"A": {"1.4.5": "t1"}})):
                with self.assertRaises(SystemExit) as raised:
                    pushcheck.main(["check", "--packages", tmp])
        self.assertEqual(raised.exception.code, 1)


class ReleaseNumberTests(unittest.TestCase):
    def test_held_versions_are_read_for_every_packable_package_once(self):
        calls = []

        class Counting(FakeFeed):
            def versions(self, pid):
                calls.append(pid)
                return super().versions(pid)
        with tempfile.TemporaryDirectory() as tmp:
            Repo(tmp).commit(base_files())
            projects = snapshot(tmp).projects
        held = allocate.held_versions(projects, Counting({"A": {"1.4.4": "t"}}), workers=4)
        self.assertEqual(sorted(calls), sorted(held))
        self.assertNotIn("Lint", held)  # not packable
        self.assertEqual(held["A"], ["1.4.4"])

    def test_the_number_comes_from_every_package_and_the_tags(self):
        held = {"A": ["1.4.4"], "B": ["1.4.6", "1.4.7-rc.1"]}
        self.assertEqual(allocate.release_number(held, "1.4", []), "1.4.7")
        self.assertEqual(allocate.release_number(held, "1.4", ["release-FDW-1.4.9"]), "1.4.10")

    def test_a_failed_read_in_the_pool_stops_the_number(self):
        class Broken(FakeFeed):
            def versions(self, pid):
                if pid == "B":
                    raise urllib.error.URLError("down")
                return []
        with tempfile.TemporaryDirectory() as tmp:
            Repo(tmp).commit(base_files())
            projects = snapshot(tmp).projects
        with self.assertRaises(urllib.error.URLError):
            allocate.held_versions(projects, Broken())

    def test_the_number_command_prints_only_the_number(self):
        import contextlib
        with tempfile.TemporaryDirectory() as tmp:
            repo = Repo(tmp)
            repo.commit(base_files())
            repo.git("tag", "release-FDW-1.4.8")
            out = io.StringIO()
            with mock.patch.object(allocate, "NugetOrg", lambda: FakeFeed({"A": {"1.4.4": "t"}})), contextlib.redirect_stdout(out):
                allocate.main(["number", "--repo", tmp, "--line", "1.4"])
        self.assertEqual(out.getvalue().strip(), "1.4.9")


class ReleasePlanTests(unittest.TestCase):
    def projects(self, extra=None):
        with tempfile.TemporaryDirectory() as tmp:
            repo = Repo(tmp)
            repo.commit(base_files() | (extra or {}))
            return snapshot(tmp).projects

    def plan(self, names, held, tags=(), bump="patch", extra=None):
        return allocate.release_plan(names, self.projects(extra), "1.4", FakeFeed(held), list(tags), bump)

    def test_every_package_in_the_set_gets_the_same_number(self):
        p = self.plan(["A", "B"], {"A": {"1.4.4": "t"}, "B": {"1.4.4": "t"}})
        self.assertEqual(p["release"], "1.4.5")
        self.assertEqual({r["package"]: r["next"] for r in p["packages"]}, {"A": "1.4.5", "B": "1.4.5"})

    def test_a_package_far_behind_does_not_lower_the_number(self):
        p = self.plan(["A"], {"A": {"1.4.1": "t"}, "B": {"1.4.9": "t"}})
        self.assertEqual(p["release"], "1.4.10")

    def test_a_package_outside_the_set_still_spends_numbers(self):
        p = self.plan(["A"], {"A": {"1.4.4": "t"}, "E": {"1.4.6": "t"}})
        self.assertEqual(p["release"], "1.4.7")

    def test_release_tags_spend_numbers(self):
        self.assertEqual(self.plan(["A"], {"A": {"1.4.4": "t"}}, ["release-FDW-1.4.8"])["release"], "1.4.9")

    def test_versions_with_a_tail_do_not_move_the_number(self):
        held = {"A": {"1.4.4": "t", "1.4.5-rc.1": "t", "1.4.5-internal.20261030120000.gabc": "t"}}
        self.assertEqual(self.plan(["A"], held)["release"], "1.4.5")

    def test_a_declared_minor_moves_the_whole_release(self):
        p = self.plan(["A", "B"], {"A": {"1.4.4": "t"}, "B": {"1.6.2": "t"}}, bump="minor")
        self.assertEqual((p["release"], {r["next"] for r in p["packages"]}), ("1.7.0", {"1.7.0"}))

    def test_a_project_that_is_not_packed_is_built_but_gets_no_number(self):
        p = self.plan(["A", "Lint"], {"A": {"1.4.4": "t"}})
        self.assertEqual(([r["project"] for r in p["packages"]], p["not_packed"]), (["A"], ["Lint"]))

    def test_a_package_id_that_differs_from_its_project_name_is_used(self):
        renamed = base_files()["public/src/A/A.csproj"].replace("</PropertyGroup>", "<AssemblyName>Other.Name</AssemblyName></PropertyGroup>")
        p = self.plan(["F"], {"Other.Name": {"1.4.2": "t"}}, extra={"public/src/F/F.csproj": renamed})
        self.assertEqual([(r["package"], r["next"]) for r in p["packages"]], [("Other.Name", "1.4.3")])

    def test_a_failing_feed_read_stops_the_plan(self):
        class Broken(FakeFeed):
            def versions(self, pid):
                raise urllib.error.URLError("down")
        with self.assertRaises(urllib.error.URLError):
            allocate.release_plan(["A"], self.projects(), "1.4", Broken(), [], "patch")


if __name__ == "__main__":
    unittest.main()

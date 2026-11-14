"""Read what a package feed holds: the versions of a package, and the tree id a held version was built from.

Only nuget.org is read here. The internal registries of the dev tiers follow with the per-branch number forms.
A request that fails for any reason other than "no such package" raises, so a plan cannot print numbers
from an incomplete picture of the feed.

  feeds.py fetch --package-id ID --version V --out FILE    save a held package (the release's binding check reads the last stable ones)
"""
import argparse
import io
import json
import re
import sys
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

class FeedError(Exception):
    """The feed does not answer as it should for a request that must work (a listed version whose package cannot be read)."""


TREE_MARKER = "fdw/tree.txt"  # where a package records the git tree id of the directory it was built from


def file_from_nupkg(data, path):
    """The text of one file inside a package, or None when the package does not hold it."""
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        return z.read(path).decode("utf-8-sig") if path in z.namelist() else None


def tree_from_nupkg(data):
    text = file_from_nupkg(data, TREE_MARKER)
    return text.strip() if text is not None else None


def identity_from_nupkg(data):
    """(package id, version) from the nuspec inside a package."""
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        name = next(n for n in z.namelist() if n.endswith(".nuspec") and "/" not in n)
        root = ET.fromstring(z.read(name))
    ns = re.match(r"\{.*\}", root.tag)
    meta = root.find(f"{ns.group(0) if ns else ''}metadata")
    get = lambda tag: meta.find(f"{ns.group(0) if ns else ''}{tag}").text.strip()
    return get("id"), get("version")


class NugetOrg:
    base = "https://api.nuget.org/v3-flatcontainer"

    def __init__(self, opener=urllib.request.urlopen):
        self._open = opener

    def _get(self, url):
        try:
            with self._open(url, timeout=60) as response:
                return response.read()
        except urllib.error.HTTPError as e:
            e.close()
            if e.code == 404:
                return None
            raise

    def versions(self, package_id):
        body = self._get(f"{self.base}/{package_id.lower()}/index.json")
        return json.loads(body)["versions"] if body else []

    def _package(self, package_id, version):
        pid, ver = package_id.lower(), version.lower()
        return self._get(f"{self.base}/{pid}/{ver}/{pid}.{ver}.nupkg")

    def serves(self, package_id, version):
        """True when a restore can take the version now: the flat container's index lists it AND its package downloads (FDW-5634). A push
        is accepted well before that: nuget.org validates and indexes in a queue, minutes to an hour later. A request that fails for any
        reason other than 'not there yet' raises."""
        if version.lower() not in (v.lower() for v in self.versions(package_id)):
            return False
        pid, ver = package_id.lower(), version.lower()
        request = urllib.request.Request(f"{self.base}/{pid}/{ver}/{pid}.{ver}.nupkg", method="HEAD")
        try:
            with self._open(request, timeout=60):
                return True
        except urllib.error.HTTPError as e:
            e.close()
            if e.code == 404:
                return False
            raise

    def download(self, package_id, version):
        """The bytes of a held package, or None when the package or version is not there."""
        return self._package(package_id, version)

    def held_tree(self, package_id, version):
        body = self._package(package_id, version)
        return tree_from_nupkg(body) if body else None

    def readable_tree(self, package_id, version):
        """The tree id a held version was built from; None when its package carries no marker. Raises FeedError when the feed lists the
        version but its package cannot be read, which is not the same as having no marker."""
        body = self._package(package_id, version)
        if body is None:
            raise FeedError(f"nuget.org lists {package_id} {version} but its package cannot be read")
        return tree_from_nupkg(body)



def fetch(feed, package_id, version, out):
    """Saves the held package to `out`. Raises FeedError when the feed does not hold it."""
    data = feed.download(package_id, version)
    if data is None:
        raise FeedError(f"nuget.org does not hold {package_id} {version}")
    with open(out, "wb") as f:
        f.write(data)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    f = sub.add_parser("fetch")
    f.add_argument("--package-id", required=True)
    f.add_argument("--version", required=True)
    f.add_argument("--out", required=True)
    a = ap.parse_args(argv)
    try:
        fetch(NugetOrg(), a.package_id, a.version, a.out)
    except (FeedError, OSError, urllib.error.URLError) as e:
        sys.exit(f"FAIL: {e}")


if __name__ == "__main__":
    main()

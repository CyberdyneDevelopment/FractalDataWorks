"""Read what a package feed holds: the versions of a package, and the tree id a held version was built from.

Only nuget.org is read here. The internal registries of the dev tiers follow with the per-branch number forms.
A request that fails for any reason other than "no such package" raises, so a dry run cannot print numbers
from an incomplete picture of the feed.
"""
import io
import json
import re
import urllib.error
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

class FeedError(Exception):
    """The feed does not answer as it should for a request that must work (a listed version whose package cannot be read)."""


TREE_MARKER = "fdw/tree.txt"  # where a package records the git tree id of the directory it was built from


MANIFEST_FILE = "fdw/manifest.json"  # where a bundle records the project manifest its release was planned from


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


def package_types(data):
    """The package types the nuspec inside a package declares: [] when it declares none, which means an ordinary dependency."""
    with zipfile.ZipFile(io.BytesIO(data)) as z:
        name = next(n for n in z.namelist() if n.endswith(".nuspec") and "/" not in n)
        root = ET.fromstring(z.read(name))
    return [e.get("name") for e in root.iter() if e.tag.split("}")[-1] == "packageType" and e.get("name")]


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

    def held_file(self, package_id, version, path):
        """The text of `path` inside a held package. None when the package or the file is not there."""
        body = self._package(package_id, version)
        return file_from_nupkg(body, path) if body else None

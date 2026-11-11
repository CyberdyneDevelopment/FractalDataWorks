"""Read the projects of an FDW-style checkout from a git ref: names, package ids, references and roles.

Everything is read from git (tree ids and file contents at a ref), never from the working tree, so the answer
for a commit does not depend on what happens to be checked out. A project is `public/{src,tests}/[a/[b/]]Name/Name.csproj`;
folders between `src` and the project are grouping only (any name, `_cross` included), so a project that moves between
them is the same project. A `.csproj` inside another project's folder is that project's data (a test fixture), not a
project. Any other `.csproj` under `src` or `tests` that does not fit the shape stops the read, naming its path: a
project the tools cannot see would silently never be built, tested or released.

The GitHub mirror holds the contents of `public/` at its root, so there a project is `{src,tests}/.../Name.csproj`.
The layout is read from the tree itself (`public/Directory.Build.props` or `Directory.Build.props`), never passed in.
"""
import hashlib
import re
import subprocess
from dataclasses import dataclass, field

GLOBAL_FILES = ("Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json")
PREFIXES = ("public/", "")  # the repository layout, and the mirror layout


def project_path(prefix):
    return re.compile(rf"^{re.escape(prefix)}(src|tests)/((?:[^/]+/){{0,2}})([^/]+)/\3\.csproj$")


def place_projects(prefix, paths):
    """The project files among `paths` (every file at a ref), by the shape above.

    A matching file inside another matching project's folder is that project's data. A `.csproj` under `src` or
    `tests` that is neither a project nor inside one is an error naming every such path."""
    pattern = project_path(prefix)
    candidates = sorted(p for p in paths if pattern.match(p))
    dirs = {p.rsplit("/", 1)[0] for p in candidates}

    def inside_another(path):
        d = path.rsplit("/", 1)[0]
        return any(d.startswith(o + "/") for o in dirs if o != d)

    projects = [p for p in candidates if not inside_another(p)]
    placed = {p.rsplit("/", 1)[0] for p in projects}
    roots = (prefix + "src/", prefix + "tests/")
    stray = sorted(p for p in paths
                   if p.endswith(".csproj") and p.startswith(roots) and p not in projects
                   and not any(p.startswith(d + "/") for d in placed))
    if stray:
        raise SystemExit("cannot place these project files (expected {src,tests}/[a/[b/]]Name/Name.csproj): "
                         + ", ".join(stray))
    return projects


def layout_of(has):
    """The prefix of the layout, given `has(path)`: public/ or the mirror's root. Neither or both is an error."""
    found = [p for p in PREFIXES if has(p + "Directory.Build.props")]
    if len(found) != 1:
        raise SystemExit("cannot tell the layout: " + ("neither public/Directory.Build.props nor Directory.Build.props is there"
                                                      if not found else "both public/Directory.Build.props and Directory.Build.props are there"))
    return found[0]
VERSION_LINES = re.compile(r"<(VersionPrefix|VersionSuffix|AssemblyVersion|FileVersion|FdwVersion|FdwReferenceVersion)>")
BUILD_TIME_NAME = re.compile(r"\.(SourceGenerators|Analyzers|CodeFixes)(\.|$)")

_REF = re.compile(r"<ProjectReference\b([^>]*?)/?>", re.S)
_ATTR = re.compile(r'([\w.]+)="([^"]*)"')
# A sibling's build output packed by file path: `..\X\bin\...`, or through grouping folders after a move,
# `..\..\domain\X\bin\...`. The name is the folder right before `bin`.
EMBED = re.compile(r'Include="(?:\.\.[\\/])+(?:[A-Za-z0-9_.]+[\\/])*?([A-Za-z0-9_.]+)[\\/]bin[\\/]')
_blobs = {}  # blob sha -> text, shared so a replay over many commits reads each file once


def git(root, *args):
    return subprocess.run(["git", "-C", str(root), *args], capture_output=True, check=True).stdout


def prop(text, name):
    m = re.search(rf"<{name}>\s*([^<]*?)\s*</{name}>", text)
    return m.group(1) if m else None


def target_name(include):
    return include.replace("\\", "/").rsplit("/", 1)[-1].removesuffix(".csproj")


@dataclass
class Project:
    name: str
    kind: str                 # "src" or "tests"
    path: str                 # directory, relative to the repo root
    package_id: str
    assembly: str             # the assembly name it builds (its AssemblyName, or the project name)
    packable: bool
    refs: set = field(default_factory=set)           # project names referenced
    analyzer_refs: set = field(default_factory=set)  # referenced with OutputItemType="Analyzer"
    embeds: set = field(default_factory=set)         # names whose build output this project packs by file path


@dataclass
class Snapshot:
    projects: dict            # name -> Project
    trees: dict               # name -> git tree id of the project directory
    globals: dict             # repo-wide build file -> hash with its version lines removed
    always_built: set         # projects Directory.Build.props references for every project
    build_time: set           # names of generator, analyzer and code-fix projects

    def manifest(self):
        return {"projects": dict(sorted(self.trees.items())), "globals": dict(sorted(self.globals.items()))}


def _source_files(root, ref):
    blobs, trees = {}, {}
    for entry in git(root, "ls-tree", "-r", "-t", "-z", ref).split(b"\0"):
        if not entry:
            continue
        meta, path = entry.split(b"\t", 1)
        _, kind, sha = meta.split()
        (trees if kind == b"tree" else blobs)[path.decode("utf-8", "surrogateescape")] = sha.decode()
    return blobs, trees


def _text(root, blobs, path):
    sha = blobs.get(path)
    if sha is None:
        return None
    if sha not in _blobs:
        _blobs[sha] = git(root, "cat-file", "blob", sha).decode("utf-8-sig", "replace")
    return _blobs[sha]


def _normalise(text):
    kept = [ln for ln in text.replace("\r", "").split("\n") if not VERSION_LINES.search(ln)]
    return hashlib.sha1("\n".join(kept).encode("utf-8")).hexdigest()


def snapshot(root, ref="HEAD"):
    blobs, trees = _source_files(root, ref)
    prefix = layout_of(lambda p: p in blobs)
    projects, tree_ids = {}, {}
    pattern = project_path(prefix)
    for path in place_projects(prefix, blobs):
        m = pattern.match(path)
        kind, name = m.group(1), m.group(3)
        text = _text(root, blobs, path)
        directory = path.rsplit("/", 1)[0]
        refs, analyzer = set(), set()
        for tag in _REF.finditer(text):
            attrs = dict(_ATTR.findall(tag.group(1)))
            if "Include" in attrs:
                refs.add(target_name(attrs["Include"]))
                if attrs.get("OutputItemType") == "Analyzer":
                    analyzer.add(target_name(attrs["Include"]))
        projects[name] = Project(
            name=name, kind=kind, path=directory,
            package_id=prop(text, "PackageId") or prop(text, "AssemblyName") or name,
            assembly=prop(text, "AssemblyName") or name,
            packable=kind == "src" and (prop(text, "IsPackable") or "true").lower() != "false",
            refs=refs, analyzer_refs=analyzer, embeds=set(EMBED.findall(text)))
        tree_ids[name] = trees[directory]

    props = _text(root, blobs, prefix + "Directory.Build.props")
    always = {target_name(t) for t in re.findall(r'ProjectReference\s+Include="\$\(MSBuildThisFileDirectory\)([^"]+)"', props)}
    build_time = set(always)
    for p in projects.values():
        build_time |= p.analyzer_refs
    build_time |= {n for n in projects if BUILD_TIME_NAME.search(n)}
    globals_ = {f: _normalise(t) for f in GLOBAL_FILES if (t := _text(root, blobs, prefix + f)) is not None}
    src_names = {n for n, p in projects.items() if p.kind == "src"}
    return Snapshot(projects, tree_ids, globals_, always & src_names, build_time & src_names)

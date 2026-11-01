#!/usr/bin/env python3
"""Write each fractaldataworks domain folder's CLAUDE.md: the guide to that domain for any session working there.

  gen_domain_guides.py --repo FDW --rst RST [--anatomy DIR]

For each src domain folder (public/src/[<group>/]<domain>/) the guide says what the domain is, its projects in
fractaldataworks and reference-servicetypes (with each project's own <Description>), its seams (the public interfaces
and abstract bases its .Abstractions projects declare, most used first), the rules that matter there, its tests and
the command that runs each, and which domains it depends on and which depend on it.

Projects and references are read from git with tools/versioning/fdwprojects.py; the domain and group of every project
are the rules in domains.py, so reference-servicetypes is matched to the same domains. Source text is read from the
checkouts' working trees. Also keeps the block in public/.gitignore that un-ignores exactly these CLAUDE.md files
(the repository root .gitignore ignores every CLAUDE.md). Writes nothing else. A domain with no summary below is an
error: add one.
"""
import argparse, collections, os, re, subprocess, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.join(HERE, "..", "versioning"))
import domains  # noqa: E402
from fdwprojects import prop, snapshot  # noqa: E402

SUMMARY = {
 "abstractions": "The base contracts every other domain builds on: the core FDW abstractions (`IGenericResult` among them) and the shared type abstractions.",
 "aegis": "Aegis: its abstractions, configuration and MCP server.",
 "agents": "Agents and the agent UI (AUI): agent services, their clients and the intelligence abstractions.",
 "analyzers": "The FDW Roslyn analyzers, code fixes and convention checks that every project builds with, and the shared source generators.",
 "authentication": "Signing in: authentication services and endpoints, external identity providers, identity (client credentials, JWT assertion), session state and HTTP authentication.",
 "authorization": "Who may do what: roles, permissions, the authorization service, its endpoints and clients.",
 "calculations": "Calculations: calculation kinds, aggregations, their services, endpoints, SignalR hub and clients.",
 "catalog": "The data catalog and lineage: catalog endpoints, clients and models, and data lineage.",
 "collections": "TypeCollections and state collections: the collection base types, their source generators, analyzers and code fixes.",
 "commands": "Commands: the command abstractions, data commands and their fluent builders, and development commands.",
 "configuration": "Configuration: the configuration abstractions, managed configuration and its source generators, the settings service and the configuration endpoints.",
 "connections": "Connections: the connection service and its abstractions, the connection types (SQL Server, PostgreSQL, SQLite, HTTP, Roslyn workspace), their data dialects, SQL commands and DDL.",
 "credentials": "Credentials: credential stores, the data vault and token managers.",
 "data": "Data: the data abstractions and DataGateway, data services and endpoints, data containers, nodes, paths, builders, execution, serialization and the data source generators.",
 "datasets": "Data sets and data stores: data set types and the REST and SQL Server data stores.",
 "devsession": "Development sessions: the DevSession service, its abstractions and MCP server.",
 "documents": "Documents: the document abstractions.",
 "endpoints": "HTTP endpoints: the web API base, endpoint and REST endpoint bases, HTTP abstractions and web clients.",
 "hosts": "Hosts: host services and abstractions, hosting, the CLI host and SignalR.",
 "mcp": "MCP: the MCP bus and hosting, and WebMCP (abstractions, hosting and its MCP surface).",
 "messagelogging": "MessageLogging: messages, the MessageLogging abstractions and their source generators.",
 "multitenancy": "Multitenancy: tenants and tenant scope, the multitenancy service, endpoints and clients.",
 "notifications": "Notifications and messaging: notification rules and channels, messaging, their endpoints and clients.",
 "pipelines": "Pipelines and ETL: orchestration, operations, ETL services and projects, mappers, job trigger sources, the ETL gateway and the pipeline UI clients.",
 "quality": "Data quality: quality rules, their service, endpoints and clients.",
 "registration": "Service registration: the three-phase ServiceTypeCollection registration and its source generators.",
 "resiliency": "Resiliency and rate limiting: retry, Polly, primary/backup, retry-notify and the rate limiter.",
 "results": "Results: `GenericResult`, the result every operation returns, and the result codes, severities, categories and statuses it carries. The `IGenericResult` interface is in `abstractions`.",
 "roslyn": "Roslyn tooling: Roslyn commands, the code builder and analysis, the SQL workspace and workspace management.",
 "rowsources": "Row sources: reading rows from delimited, fixed-width, JSON, XML, HTTP and data reader sources.",
 "scheduling": "Scheduling: schedules, the scheduling service, endpoints and clients.",
 "schema": "Schema: schema abstractions, DDL generation and its MSBuild tasks, schema clients and endpoints, JSON schema.",
 "secretmanagers": "Secret managers: the secret manager service and abstractions, its endpoints, clients and test double.",
 "security": "Security primitives: encryption, hashing and claims abstractions.",
 "services": "The service base: `ServiceTypeBase` and the service and processor abstractions every domain service builds on, and `Fdw.Services`.",
 "shells": "Shells: the terminal UI management, the VS Code shell and terminal abstractions.",
 "telemetry": "Telemetry and logging: the telemetry and logging services and web analytics clients.",
 "transformations": "Transformations: field transformations (boolean, conditional, date-time, injection, numeric, text) and the transformation service abstractions.",
 "ui": "UI: the UI abstractions and services, components (Razor console, TUI), Spectre rendering, themes, and the lineage and schema UI client abstractions.",
 "users": "Users: the user service, its endpoints and clients.",
 "validation": "Validation: the validation abstractions and the FastEndpoints validation integration.",
 "workspaces": "Workspaces: the workspace service and endpoints, and the TDS protocol and endpoints that serve them over SQL Server's wire protocol.",
}
RULES = [
 "No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.",
 "Log through MessageLogging (source-generated messages), never raw `ILogger` calls.",
 "Return `IGenericResult` with a ResultCode; a failure never carries a plain string.",
 "All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.",
 "The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.",
 "Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.",
 "Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.",
 "One type per file. Before adding a type, search the repo for one with the same name.",
]
EXTRA = {
 "connections": ["Connection-type-specific code (ADO.NET, dialect SQL, authentication types) belongs in the connection projects here and nowhere above them."],
 "data": ["`IDataGateway` is the only data path. Containers abstract physical storage: never pass raw schema and table strings above the connection layer."],
 "configuration": ["Top-level configurations are written through their domain provider's `Save()`/`Delete()`; `ConfigurationSaveCommand<T>` only for child records. A provider with a parent names the key type it joins on (Logical or Physical); never a default."],
 "collections": ["A TypeCollection collects and dedupes its options; a new constructor parameter on an option base must be defaultable, or the generated not-found sentinel fails every host."],
 "analyzers": ["Every project except tests and the analyzers themselves builds with these analyzers (Directory.Build.props). A change here changes every package's build."],
 "registration": ["Registration is three-phase: AddFrameworkServiceTypes, the `.AddXxx()` chain, InitializeFrameworkServiceTypes."],
 "endpoints": ["An endpoint is one class per file and a pure HTTP orchestrator: validate, call a service, map, send. It never holds `IDataGateway` or business logic."],
 "hosts": ["An endpoint class is mapped by a host only when an endpoints option names it."],
 "secretmanagers": ["Secrets are written and deleted only through the connection credential store, inside creating, updating or deleting a connection."],
}
ANATOMY = {
 "results": ["results"], "collections": ["collections", "collections-domain"],
 "analyzers": ["analyzers", "conventions"], "registration": ["registration"],
 "abstractions": ["principles", "composition"], "services": ["principles"],
 "security": ["security"], "commands": ["commands", "builders"],
 "connections": ["connections", "axes", "gateways", "catalog-conn"],
 "hosts": ["host-endpoint", "middleware"], "endpoints": ["endpoints", "middleware"],
 "secretmanagers": ["secretmanagers", "catalog-sec", "access-resources"], "credentials": ["catalog-cred"],
 "authentication": ["authentication", "catalog-auth"],
 "authorization": ["access-model", "access-roles", "access-gaps", "catalog-authz"],
 "multitenancy": ["multitenancy", "access-tenancy", "catalog-tenant"], "users": ["catalog-usr"],
 "configuration": ["configuration", "catalog-settings"], "data": ["gateways", "catalog-data"],
 "datasets": ["datasets", "datastores"], "transformations": ["catalog-transform"],
 "pipelines": ["pipelines", "catalog-pipe", "catalog-workflow"], "scheduling": ["scheduling", "catalog-sched"],
 "calculations": ["catalog-calc"], "notifications": ["catalog-notify", "catalog-msg"],
 "telemetry": ["catalog-otel", "catalog-log"], "resiliency": ["catalog-rsl"], "quality": ["catalog-quality"],
 "catalog": ["catalog-catalog"], "workspaces": ["catalog-dataverse", "access-dataverses"],
 "agents": ["catalog-agent"], "aegis": ["catalog-aegis"], "roslyn": ["roslyn-mcp"], "mcp": ["webmcp"], "ui": ["ui-host"],
}
FOUNDATION = {"abstractions", "results", "collections", "messagelogging", "analyzers", "validation", "security", "registration", "services"}
BEGIN, END = "# BEGIN domain guides (written by tools/reorg/gen_domain_guides.py)", "# END domain guides"
DECL = re.compile(r"^\s*public\s+(?:sealed\s+|static\s+|partial\s+)*(?:(interface)\s+(I[A-Z]\w*)|abstract\s+(?:partial\s+)?class\s+(\w+Base)\b)", re.M)


def place(repo_key, name, kind):
    d = domains.domain_of(repo_key, name)
    if d is None and kind == "tests":
        d = "_cross"
    if d is None:
        raise SystemExit(f"no domain rule for {name}: add one to domains.py")
    return d


def where(d):
    return "/".join(x for x in (domains.GROUP.get(d), d) if x)


def description(root, project):
    path = os.path.join(root, project.path, project.name + ".csproj")
    with open(path, encoding="utf-8-sig") as f:
        return " ".join((prop(f.read(), "Description") or "").split())


def uses(repo):
    """How many times each I* / *Base identifier appears across the repo's C# source, as a rough measure of use."""
    out = subprocess.run(["git", "-C", repo, "grep", "-ohE", r"\b(I[A-Z][A-Za-z0-9]+|[A-Z][A-Za-z0-9]+Base)\b", "--", "*.cs"],
                         capture_output=True, text=True, check=True).stdout.split()
    return collections.Counter(out)


def seams(root, projects, count):
    found = []
    for p in projects:
        if not p.name.endswith(".Abstractions"):
            continue
        for dp, dns, fns in os.walk(os.path.join(root, p.path)):
            dns[:] = [x for x in dns if x not in ("bin", "obj")]
            for fn in fns:
                if fn.endswith(".cs"):
                    with open(os.path.join(dp, fn), encoding="utf-8-sig", errors="replace") as f:
                        for m in DECL.finditer(f.read()):
                            found.append((m.group(2) or m.group(3), p.name))
    return sorted(set(found), key=lambda x: (-count[x[0]], x[0]))


def gitignore_block(path, folders):
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    nl = "\r\n" if "\r\n" in text else "\n"
    text = re.sub(r"(\r?\n)?# BEGIN domain experts \(written by tools/reorg/gen_experts\.py\).*?# END domain experts\r?\n?", "", text, flags=re.S)
    block = nl.join([BEGIN, "# The root .gitignore ignores CLAUDE.md; each domain folder's guide is tracked."]
                    + [f"!/src/{w}/CLAUDE.md" for w in sorted(folders)] + [END])
    pattern = re.compile(re.escape(BEGIN) + r".*?" + re.escape(END), re.S)
    text = pattern.sub(lambda m: block, text) if pattern.search(text) else text.rstrip("\r\n") + nl + nl + block + nl
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def main():
    a = argparse.ArgumentParser()
    a.add_argument("--repo", required=True); a.add_argument("--rst", required=True); a.add_argument("--anatomy")
    args = a.parse_args()
    repo, rst_root = os.path.realpath(args.repo), os.path.realpath(args.rst)
    fdw, rst = snapshot(repo), snapshot(rst_root)
    dom = {n: place("fdw", n, p.kind) for n, p in fdw.projects.items()}
    rdom = {n: place("rst", n, p.kind) for n, p in rst.projects.items()}
    count = uses(repo)
    rdeps = collections.defaultdict(set)
    for n, p in fdw.projects.items():
        for r in p.refs:
            if r in fdw.projects:
                rdeps[r].add(n)
    made = []
    for d in sorted({dom[n] for n, p in fdw.projects.items() if p.kind == "src"}):
        if d not in SUMMARY:
            raise SystemExit(f"no summary for domain {d}: add one to SUMMARY")
        src = sorted((p for n, p in fdw.projects.items() if p.kind == "src" and dom[n] == d), key=lambda p: p.name)
        tests = sorted((p for n, p in fdw.projects.items() if p.kind == "tests" and dom[n] == d), key=lambda p: p.name)
        rsrc = sorted((p for n, p in rst.projects.items() if p.kind == "src" and rdom[n] == d), key=lambda p: p.name)
        rtests = sorted((p for n, p in rst.projects.items() if p.kind == "tests" and rdom[n] == d), key=lambda p: p.name)
        names = {p.name for p in src}
        up, down = collections.Counter(), collections.Counter()
        for p in src:
            for x in p.refs:
                if x in fdw.projects and dom[x] != d and fdw.projects[x].kind == "src":
                    up[dom[x]] += 1
            for x in rdeps[p.name]:
                if dom[x] != d and fdw.projects[x].kind == "src":
                    down[dom[x]] += 1
        folder = os.path.join(repo, "public", "src", *where(d).split("/"))
        if not os.path.isdir(folder):
            raise SystemExit(f"domain folder {folder} does not exist")
        w = where(d)
        b = [f"# The {d} domain", "", SUMMARY[d], ""]
        b += [f"This folder, `public/src/{w}/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/{w}/`"
              + (f", and its reference-servicetypes implementations under `reference-servicetypes/public/src/{w}/`." if rsrc else "."), ""]
        b += ["## Projects", "", "fractaldataworks:", ""]
        b += [f"- `{p.name}`" + ("" if p.packable else " (not a package)") + (f": {desc}" if (desc := description(repo, p)) else "") for p in src]
        if rsrc:
            b += ["", "reference-servicetypes:", ""]
            b += [f"- `{p.name}`" + (f": {desc}" if (desc := description(rst_root, p)) else "") for p in rsrc]
        sm = seams(repo, src, count)
        b += ["", "## Seams", ""]
        if sm:
            b += ["The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:", ""]
            b += [f"- `{name}` ({proj})" for name, proj in sm[:20]]
            if len(sm) > 20:
                b += [f"- and {len(sm) - 20} more in the `.Abstractions` projects above"]
        else:
            b += ["This domain has no `.Abstractions` project; its projects above are its surface."]
        b += ["", "## Rules that matter here", ""] + [f"- {r}" for r in EXTRA.get(d, []) + RULES]
        b += ["", "## Tests", ""]
        if tests or rtests:
            b += ["Run a test project from the repository root:", ""]
            b += [f"- `dotnet test {p.path}/{p.name}.csproj -c Release`" for p in tests]
            b += [f"- `dotnet test {p.path}/{p.name}.csproj -c Release` (in reference-servicetypes)" for p in rtests]
        else:
            b += ["No test project sits in this domain; the cross-domain tests in `public/tests/_cross/` cover it."]
        b += ["", "## Dependencies", ""]
        b += ["Depends on: " + (", ".join(f"`{x}` ({up[x]})" for x in sorted(up, key=lambda x: (-up[x], x))) or "no other domain") + ".",
              "Depended on by: " + (", ".join(f"`{x}` ({down[x]})" for x in sorted(down, key=lambda x: (-down[x], x))) or "no other domain") + ".",
              "(The number is how many references cross between the two domains' src projects.)", ""]
        if d in FOUNDATION:
            out = sorted({(p.name, x) for p in src for x in p.refs
                          if x in fdw.projects and dom[x] not in FOUNDATION and fdw.projects[x].kind == "src"})
            if out:
                b += ["A foundation project that references a domain outside the foundation is a layering defect or a misfiled project:", ""]
                b += [f"- `{k}` -> `{x}` ({dom[x]})" for k, x in out] + [""]
        b += ["What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.", ""]
        docs_for = [x for x in ANATOMY.get(d, []) if args.anatomy and os.path.exists(os.path.join(args.anatomy, "content", x + ".json"))]
        if docs_for:
            b += ["## Further reading", "", "fdw-anatomy (the cited reference): " + ", ".join(f"`{x}`" for x in docs_for) + ".", ""]
        made.append((d, len(src), len(tests), len(rsrc)))
        with open(os.path.join(folder, "CLAUDE.md"), "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(b).rstrip("\n") + "\n")
    gitignore_block(os.path.join(repo, "public", ".gitignore"), [where(m[0]) for m in made])
    for m in made:
        print(*m, sep="\t")
    print(f"{len(made)} domain guides written")


if __name__ == "__main__":
    main()

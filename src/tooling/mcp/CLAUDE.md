# The mcp domain

MCP: the MCP bus and hosting, and WebMCP (abstractions, hosting and its MCP surface).

This folder, `public/src/tooling/mcp/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/tooling/mcp/`.

## Projects

fractaldataworks:

- `Fdw.Mcp.Bus`
- `Fdw.Mcp.Bus.Abstractions`
- `Fdw.Mcp.Hosting`
- `Fdw.WebMcp.Abstractions`: The WebMCP tool marker attribute and the collection endpoint options declare themselves into. Deliberately free of ASP.NET and of Fdw.WebMcp.Hosting so an endpoint package can mark a tool without taking a dependency on the server that serves it.
- `Fdw.WebMcp.Hosting`: Server-side WebMCP tool hosting — the tool registry, its discovery attribute, the generated client script and the API-key gate. Split out of Fdw.Hosting, which is not where a feature subsystem belongs.
- `Fdw.WebMcp.Mcp`: Serves an FDW host's WebMCP tool catalogue to headless agents over MCP. The tools, their schemas and the requests they make are the same plan the browser script renders, so the two transports cannot describe a tool differently.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IMcpEventBus` (Fdw.Mcp.Bus.Abstractions)
- `IViewIntent` (Fdw.Mcp.Bus.Abstractions)
- `IMcpToolSource` (Fdw.Mcp.Bus.Abstractions)
- `ViewIntentBase` (Fdw.Mcp.Bus.Abstractions)
- `IMcpToolSourceKind` (Fdw.Mcp.Bus.Abstractions)
- `McpToolSourceKindBase` (Fdw.Mcp.Bus.Abstractions)

## Rules that matter here

- No fallbacks: a `??` never falls back to a value that could be a real one, and a missing value fails with a MessageLogging error and a failed result. Allowed: a null logger, `?? string.Empty`, `?? []`, `??=` on a field or local, a chain of failure results or messages, a `[PlaceholderText]` constant, a `[PlatformDefault]` member, and a member Mike approved with `[ApprovedFallback]`. Never a default message. FDW055 enforces it.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## How a WebMCP tool describes its request (FDW-6143)

- `WebMcpToolPlan.Create(tool, maxDepth, logger)` describes the endpoint's request type through `WebMcpRequestDescriber`: scalars as before, a list as `array` with `items`, a class as `object` with `properties` and `required`. A tool with only scalars produces the same schema it always did (the golden script test pins it). A member no shape describes (a dictionary, bytes, a framework type) is skipped and logged. Lists and objects are described only for a call that carries a JSON body.
- The depth is the server setting `WebMcpToolSchemaMaxDepth` (seeded in databases-seed, read by `WebMcpHostSettingsReader`, no default in code). A request that nests deeper (`HOSTING-61015`) or reaches itself (`HOSTING-61016`) is refused when its plan is built, and the registry does not serve that tool.
- The registry builds every plan once (`IWebMcpToolRegistry.Plans`); the script generator and the MCP catalog only render them.
- `WebMcpArgumentValidator` holds an MCP call's arguments to the same description before `WebMcpToolInvoker` sends anything (`HOSTING-20002`, naming each member by path). The browser script describes the request but does not check a call: the endpoint's own validator refuses there.
- Adding a member type means changing `WebMcpRequestDescriber` and the validator together, and adding the case to both transports' tests.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/tooling/mcp/Fdw.Mcp.Bus.Tests/Fdw.Mcp.Bus.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/mcp/Fdw.Mcp.Hosting.SampleTools/Fdw.Mcp.Hosting.SampleTools.csproj -c Release`
- `dotnet test public/tests/tooling/mcp/Fdw.Mcp.Hosting.Tests/Fdw.Mcp.Hosting.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/mcp/Fdw.WebMcp.Hosting.Tests/Fdw.WebMcp.Hosting.Tests.csproj -c Release`
- `dotnet test public/tests/tooling/mcp/Fdw.WebMcp.Mcp.Tests/Fdw.WebMcp.Mcp.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (11), `collections` (6), `results` (5), `hosts` (2), `abstractions` (1), `authentication` (1), `secretmanagers` (1).
Depended on by: `devsession` (2), `endpoints` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `webmcp`.

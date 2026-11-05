# The devsession domain

Development sessions: the DevSession service, its abstractions and MCP server.

This folder, `public/src/tooling/devsession/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/tooling/devsession/`.

## Projects

fractaldataworks:

- `Fdw.DevSession`
- `Fdw.DevSession.Abstractions`
- `Fdw.DevSession.McpServer`: Stdio MCP server exposing development sessions: isolated copies (branch or worktree), session lifecycle, and strand fencing for concurrent agents working in one session.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IDevSession` (Fdw.DevSession.Abstractions)
- `IWorktreeEngine` (Fdw.DevSession.Abstractions)
- `SessionStateBase` (Fdw.DevSession.Abstractions)
- `IDevSessionManager` (Fdw.DevSession.Abstractions)
- `StrandStateBase` (Fdw.DevSession.Abstractions)
- `IWorkspaceCoordinator` (Fdw.DevSession.Abstractions)
- `IIsolationLevel` (Fdw.DevSession.Abstractions)
- `ISessionState` (Fdw.DevSession.Abstractions)
- `IsolationLevelBase` (Fdw.DevSession.Abstractions)
- `IStrandState` (Fdw.DevSession.Abstractions)
- `StrandHandlerBase` (Fdw.DevSession.Abstractions)
- `IStrandHandler` (Fdw.DevSession.Abstractions)

## Rules that matter here

- No fallbacks: a `??` never falls back to a value that could be a real one, and a missing value fails with a MessageLogging error and a failed result. Allowed: a null logger, `?? string.Empty`, `?? []`, `??=` on a field or local, a chain of failure results or messages, a `[PlaceholderText]` constant, a `[PlatformDefault]` member, and a member Mike approved with `[ApprovedFallback]`. Never a default message. FDW055 enforces it.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/tooling/devsession/Fdw.DevSession.Tests/Fdw.DevSession.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (4), `messagelogging` (3), `mcp` (2), `results` (2), `abstractions` (1), `registration` (1).
Depended on by: no other domain.
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

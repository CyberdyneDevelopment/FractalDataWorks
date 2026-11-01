# The shells domain

Shells: the terminal UI management, the VS Code shell and terminal abstractions.

This folder, `public/src/tooling/shells/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/tooling/shells/`.

## Projects

fractaldataworks:

- `Fdw.Services.Terminal.Abstractions`: Abstractions for persistent terminal sessions and management.
- `Fdw.TUI.Management`: Console-based management application for Fdw. Provides configuration management, monitoring, and log viewing via terminal UI.
- `Fdw.VsCodeShell`: Ready-made VS Code extension shell: ship a .NET host that contributes commands and webviews; the framework supplies the JavaScript bootstrap and packaging glue. Build a VS Code extension without writing TypeScript.
- `Fdw.VsCodeShell.Abstractions`: Manifest, command, webview, and editor-event contracts shared by VS Code extensions built on the FDW VsCodeShell.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IVsCodeCommandType` (Fdw.VsCodeShell.Abstractions)
- `VsCodeCommandTypeBase` (Fdw.VsCodeShell.Abstractions)
- `IVsCodeCommandHandler` (Fdw.VsCodeShell.Abstractions)
- `IVsCodeManifest` (Fdw.VsCodeShell.Abstractions)
- `IVsCodeCommandDescriptor` (Fdw.VsCodeShell.Abstractions)
- `IVsCodeWebviewDescriptor` (Fdw.VsCodeShell.Abstractions)
- `ITerminalSession` (Fdw.Services.Terminal.Abstractions)
- `IVsCodeCommandFactory` (Fdw.VsCodeShell.Abstractions)
- `ITerminalNotifier` (Fdw.Services.Terminal.Abstractions)
- `ITerminalService` (Fdw.Services.Terminal.Abstractions)

## Rules that matter here

- No fallbacks: a missing value fails with a MessageLogging error and a failed result. The only `??` allowed is `logger ?? NullLogger<T>.Instance` in a constructor.
- Log through MessageLogging (source-generated messages), never raw `ILogger` calls.
- Return `IGenericResult` with a ResultCode; a failure never carries a plain string.
- All data access goes through `IDataGateway.Execute()` with command objects. No `SqlConnection`, `SqlCommand` or connection string outside the connection projects.
- The connection type is invisible above the connection layer: no `is`/`as`/`typeof` on a connection type, no switch on connection type names.
- Services register only through the ServiceTypeCollection three-phase lifecycle; no `AddXxx` extension methods and no ad-hoc `services.Add*` in applications.
- Every async method takes `CancellationToken cancellationToken = default` and passes it on. Compare strings with an explicit `StringComparison`.
- One type per file. Before adding a type, search the repo for one with the same name.

## Tests

Run a test project from the repository root:

- `dotnet test public/tests/tooling/shells/Fdw.VsCodeShell.Tests/Fdw.VsCodeShell.Tests.csproj -c Release`

## Dependencies

Depends on: `collections` (5), `messagelogging` (5), `results` (4), `abstractions` (3), `registration` (3), `ui` (3), `configuration` (2), `pipelines` (2), `services` (2), `authentication` (1), `authorization` (1), `connections` (1), `data` (1), `endpoints` (1), `multitenancy` (1), `notifications` (1), `quality` (1), `scheduling` (1), `secretmanagers` (1), `users` (1).
Depended on by: no other domain.
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

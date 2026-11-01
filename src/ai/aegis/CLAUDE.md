# The aegis domain

Aegis: its abstractions, configuration and MCP server.

This folder, `public/src/ai/aegis/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/ai/aegis/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/ai/aegis/`.

## Projects

fractaldataworks:

- `Fdw.Aegis`: Aegis Gateway: deterministic approval policy evaluation and secret-below-the-boundary injection.
- `Fdw.Aegis.Abstractions`: Contracts for the Aegis Gateway deterministic approval and secret-broker: ApprovalRequest/Verdict, VerdictDispositions, IApprovalPolicyEvaluator, and AegisResultCodes.
- `Fdw.Aegis.Configuration`: ManagedConfiguration POCOs for the Aegis Gateway approval-policy domain (the AegisCommand domain's PreApproved/AdHoc implementations and their children, plus the ApprovalPolicyTypes collection). Mirrors Fdw.Data.Configuration/Fdw.Services.Connections so Fdw.Services.Data can reference it without a package cycle.
- `Fdw.Aegis.McpServer`: The Aegis Gateway's stdio MCP tool surface and host wiring: deterministic approval-policy evaluation and secret-below-the-boundary injection. The entry point that hosts it lives in reference-servicetypes.

reference-servicetypes:

- `FdwReferenceAegis.McpServer`: Standalone stdio MCP server exposing the Aegis Gateway's deterministic approval-policy evaluation and secret-below-the-boundary injection. Its configuration (declared connections, AegisCommand rows, secret managers) is read through a declared FileSystem connection.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `AegisResultCodeBase` (Fdw.Aegis.Abstractions)
- `IApprovalPolicyConfiguration` (Fdw.Aegis.Abstractions)
- `VerdictDispositionBase` (Fdw.Aegis.Abstractions)
- `IApprovalPolicyEvaluator` (Fdw.Aegis.Abstractions)
- `IVerdictDisposition` (Fdw.Aegis.Abstractions)

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

- `dotnet test public/tests/ai/aegis/Fdw.Aegis.Abstractions.Tests/Fdw.Aegis.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/ai/aegis/Fdw.Aegis.McpServer.Tests/Fdw.Aegis.McpServer.Tests.csproj -c Release`
- `dotnet test public/tests/ai/aegis/FdwReferenceAegis.McpServer.Tests/FdwReferenceAegis.McpServer.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `collections` (5), `configuration` (3), `data` (3), `messagelogging` (3), `secretmanagers` (3), `abstractions` (2), `results` (2), `connections` (1), `services` (1).
Depended on by: `data` (2).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-aegis`.

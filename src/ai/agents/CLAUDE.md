# The agents domain

Agents and the agent UI (AUI): agent services, their clients and the intelligence abstractions.

This folder, `public/src/ai/agents/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/ai/agents/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/ai/agents/`.

## Projects

fractaldataworks:

- `Fdw.Agents.Clients`: API client, DTOs, and registration types for agent action review-queue operations.
- `Fdw.Aui`: Implementation of Agent User Interfaces (AUI) services and middleware.
- `Fdw.Aui.Abstractions`: Abstractions for Agent User Interfaces (AUI) following Google's A2UI and WebMCP standards.
- `Fdw.Intelligence`: Implementation of semantic memory and agent intelligence services.
- `Fdw.Intelligence.Abstractions`: Abstractions for semantic memory and agent intelligence services.
- `Fdw.Services.Agents`
- `Fdw.Services.Agents.Abstractions`

reference-servicetypes:

- `FdwReferenceAgentActions.Endpoints`: The AgentActions domain's HTTP endpoints.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IAgentActionService` (Fdw.Services.Agents.Abstractions)
- `AgentActionStatusTypeBase` (Fdw.Services.Agents.Abstractions)
- `IAuiProvider` (Fdw.Aui.Abstractions)
- `IAgentActionStatusType` (Fdw.Services.Agents.Abstractions)
- `IVectorMemoryStore` (Fdw.Intelligence.Abstractions)
- `IAuiAction` (Fdw.Aui.Abstractions)

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

- `dotnet test public/tests/ai/agents/Fdw.Aui.Tests/Fdw.Aui.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (7), `results` (7), `data` (5), `abstractions` (4), `collections` (4), `authentication` (2), `commands` (2), `endpoints` (2), `services` (1).
Depended on by: `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-agent`.

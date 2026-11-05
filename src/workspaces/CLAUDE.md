# The workspaces domain

Workspaces: the workspace service and endpoints, and the TDS protocol and endpoints that serve them over SQL Server's wire protocol.

This folder, `public/src/workspaces/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/workspaces/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/workspaces/`.

## Projects

fractaldataworks:

- `Fdw.Protocols.Tds`
- `Fdw.Services.Workspaces`
- `Fdw.Services.Workspaces.Abstractions`
- `Fdw.Services.Workspaces.Endpoints`
- `Fdw.Services.Workspaces.Tds`
- `Fdw.Tds.Endpoints`

reference-servicetypes:

- `FdwReferenceWorkspaces.Endpoints`: The Workspaces domain HTTP endpoints — collaborative data projects, their members, resources and relationships.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IWorkspaceMemberRole` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceMemberRoleBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceMemberStateBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceMembershipRequestStatusBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceResourceKindBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceStatusBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceVisibilityBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceJoinPolicyBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceRelationshipCardinalityBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceResourceRelationshipBase` (Fdw.Services.Workspaces.Abstractions)
- `WorkspaceSubjectTypeBase` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceVisibility` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceJoinPolicy` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceMemberState` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceMembershipRequestStatus` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceRelationshipCardinality` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceResourceKind` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceResourceRelationship` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceStatus` (Fdw.Services.Workspaces.Abstractions)
- `IWorkspaceSubjectType` (Fdw.Services.Workspaces.Abstractions)

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

- `dotnet test public/tests/workspaces/Fdw.Protocols.Tds.Tests/Fdw.Protocols.Tds.Tests.csproj -c Release`
- `dotnet test public/tests/workspaces/Fdw.Tds.Endpoints.Tests/Fdw.Tds.Endpoints.Tests.csproj -c Release`
- `dotnet test public/tests/workspaces/FdwReferenceWorkspaces.Endpoints.Tests/FdwReferenceWorkspaces.Endpoints.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `collections` (9), `data` (9), `messagelogging` (9), `results` (6), `abstractions` (3), `authentication` (3), `datasets` (2), `endpoints` (2), `authorization` (1), `commands` (1), `configuration` (1), `multitenancy` (1), `services` (1), `users` (1).
Depended on by: `calculations` (2), `data` (2), `catalog` (1), `datasets` (1), `pipelines` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `catalog-dataverse`, `access-dataverses`.

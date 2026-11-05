# The documents domain

Documents: the document abstractions, for file storage and for the document record layer.

This folder, `public/src/documents/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/documents/`.

## Projects

fractaldataworks:

- `Fdw.Documents.Abstractions`: What a connection that stores whole files needs: the document path and scope, the document command family, the capabilities a connection declares, and the result codes and logging of anticipated failures. Abstractions only: a consumer supplies the connection implementations.
- `Fdw.Documents.Records`: FDW's one document record store (FDW-3673): `ConnectionDocumentRecordStore` carries out every record seam as a document record command on the DocumentRecord connection an organization was assigned, refusing a call for another organization or tenant and running `DocumentLinkRule` before a link is sent. `IDocumentRecordStores.For` (in `Fdw.Services.Connections`, beside `DocumentStores`) finds that connection through the organization's `DocumentRecords` assignment and its tenant's grant. Storage is the connection's; FDW names no table.
- `Fdw.Documents.Records.Abstractions`: The document record layer (FDW-3395, FDW-3673): the document, stored media, record tree, attribute values, state history, requirements, reminders and the per-organization revision; the seams that read and write them; the document record command family (`DocumentRecordCommands`) and capabilities (`DocumentRecordCapabilities`, `AtomicRevisionWrites`) a DocumentRecord connection carries out and declares; the public id rule (`PublicIds.Check`: a UUIDv7 the application sets, never generated); the definitions held at the platform, a tenant or an organization with FDW's rule for which is in force (`ScopedDefinitions`, FDW-3398); and document links with FDW's check of a link before it is stored (`DocumentLinkRule`, FDW-3397). A record backing is a connection (`IDocumentRecordConnection`, in `Fdw.Services.Connections.Abstractions`), never a class above the connection layer.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IDocumentCommand` (Fdw.Documents.Abstractions)
- `DocumentCommandBase` (Fdw.Documents.Abstractions)
- `DocumentCapabilityBase` (Fdw.Documents.Abstractions)
- `DocumentResultCodeBase` (Fdw.Documents.Abstractions)
- `IDocumentCapability` (Fdw.Documents.Abstractions)
- `IDocumentRecords`, `IDocumentMedia`, `IRecordTree`, `IAttributeValues`, `IDocumentStateHistory`, `IRecordRequirements`, `IReminderLog`, `IOrganizationRevisions`, `IDocumentDefinitions<T>`, `IDocumentLinks`, `ILinkTargets` (Fdw.Documents.Records.Abstractions): implemented by FDW's store over the organization's DocumentRecord connection; a backing implements `IDocumentRecordConnection` and the record commands instead

## Rules that matter here

- A document record's public id is a UUIDv7 the application sets; `PublicIds.Check` refuses an empty or other-version id and never mints one. This is on purpose the opposite of configuration rows, which mint an id when theirs is empty.

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

- `dotnet test public/tests/documents/Fdw.Documents.Abstractions.Tests/Fdw.Documents.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/documents/Fdw.Documents.Records.Abstractions.Tests/Fdw.Documents.Records.Abstractions.Tests.csproj -c Release`
- `dotnet test public/tests/documents/Fdw.Documents.Records.Tests/Fdw.Documents.Records.Tests.csproj -c Release`

## Dependencies

Depends on: `messagelogging` (3), `collections` (2), `results` (2), `abstractions` (1).
Depended on by: `connections` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

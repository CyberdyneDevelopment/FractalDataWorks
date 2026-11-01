# The security domain

Security primitives: encryption, hashing and claims abstractions.

This folder, `public/src/foundation/security/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/foundation/security/`.

## Projects

fractaldataworks:

- `Fdw.Claims.Abstractions`
- `Fdw.Security.Encryption`
- `Fdw.Security.Hashing`

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `ClaimDefinitionBase` (Fdw.Claims.Abstractions)
- `IClaimDefinition` (Fdw.Claims.Abstractions)

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

No test project sits in this domain; the cross-domain tests in `public/tests/_cross/` cover it.

## Dependencies

Depends on: `collections` (4), `abstractions` (2), `messagelogging` (2), `secretmanagers` (2), `results` (1).
Depended on by: `authentication` (4), `multitenancy` (2), `users` (2), `authorization` (1), `catalog` (1), `configuration` (1), `connections` (1), `credentials` (1), `data` (1), `endpoints` (1), `pipelines` (1), `quality` (1), `resiliency` (1), `scheduling` (1), `schema` (1), `ui` (1).
(The number is how many references cross between the two domains' src projects.)

A foundation project that references a domain outside the foundation is a layering defect or a misfiled project:

- `Fdw.Security.Encryption` -> `Fdw.Services.SecretManagers` (secretmanagers)
- `Fdw.Security.Encryption` -> `Fdw.Services.SecretManagers.Abstractions` (secretmanagers)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `security`.

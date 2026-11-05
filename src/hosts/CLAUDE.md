# The hosts domain

Hosts: host services and abstractions, hosting, the CLI host and SignalR.

This folder, `public/src/hosts/`, holds the domain's fractaldataworks projects; its tests are under `public/tests/hosts/`, and its reference-servicetypes implementations under `reference-servicetypes/public/src/hosts/`.

## Projects

fractaldataworks:

- `Fdw.Hosting.Abstractions`
- `Fdw.Hosting.Cli`: Hosting extensions for Fdw CLI / admin console hosts. Wires Spectre rendering, env-var secret manager, audit context accessor, and schema discovery dispatch into the DI container so any FDW-built CLI (fdw-cli, per-app admin tools) can bootstrap with a single extension method.
- `Fdw.Services.Hosts`
- `Fdw.Services.Hosts.Abstractions`
- `Fdw.SignalR`

reference-servicetypes:

- `FdwReferenceHost`
- `FdwReferenceHosts.Cors`: The CORS host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.Cors.Registration`: The CORS host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.EmptyBody`: The CORS host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.EmptyBody.Registration`: The CORS host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.ForwardedHeaders`: The CORS host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.ForwardedHeaders.Registration`: The CORS host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.OutboundIdentity`: The outbound-identity host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.OutboundIdentity.Registration`: The outbound-identity host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.ResponseBuffering`: The CORS host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.ResponseBuffering.Registration`: The CORS host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.ResponseStreaming`: The response-streaming host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.ResponseStreaming.Registration`: The response-streaming host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.SecurityHeaders`: The CORS host option: its configuration record and the command that reads it. Settings live here, not on a shared host interface.
- `FdwReferenceHosts.SecurityHeaders.Registration`: The CORS host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.
- `FdwReferenceHosts.SqlServerEndpoint`: The SQL Server endpoint host option: its configuration record, the command that reads it, and the source that turns the row and its secrets into the listener's options.
- `FdwReferenceHosts.SqlServerEndpoint.Registration`: The SQL Server endpoint host-option registration: the [Implementation] only. Referencing it gives a host a TDS listener presenting its workspaces as a SQL Server database.
- `FdwReferenceHosts.WebMcp`: The WebMCP host option: the client key and header a host injects into the generated webmcp.js, read from the server configuration tier rather than bound in Program.cs.
- `FdwReferenceHosts.WebMcp.Registration`: The WebMCP host-option registration: the [Implementation] only. References the implementation, so referencing THIS package brings the implementation with it and registers the option.

## Seams

The public interfaces and abstract bases the domain's `.Abstractions` projects declare, most used across the repo first:

- `IHostImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IWebMcpImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IWebMcpConfigurationProvider` (Fdw.Services.Hosts.Abstractions)
- `IHostService` (Fdw.Services.Hosts.Abstractions)
- `IHostFactory` (Fdw.Services.Hosts.Abstractions)
- `IHostType` (Fdw.Services.Hosts.Abstractions)
- `IHostImplementationConfigurationProvider` (Fdw.Services.Hosts.Abstractions)
- `IApiReferenceImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IAuthChallengeImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `ICorsImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IEmptyBodyImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IForwardedHeadersImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IHostConfigurationProvider` (Fdw.Services.Hosts.Abstractions)
- `IHostPipelinePosition` (Fdw.Services.Hosts.Abstractions)
- `IOutboundIdentityImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IResponseBufferingImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IResponseStreamingImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `ISecurityHeadersImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `ISqlServerEndpointImplementationConfiguration` (Fdw.Services.Hosts.Abstractions)
- `IApiReferenceConfigurationProvider` (Fdw.Services.Hosts.Abstractions)
- and 21 more in the `.Abstractions` projects above

## Rules that matter here

- An endpoint class is mapped by a host only when an endpoints option names it.
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

- `dotnet test public/tests/hosts/Fdw.SignalR.Tests/Fdw.SignalR.Tests.csproj -c Release`
- `dotnet test public/tests/hosts/FdwReferenceHost.Tests/FdwReferenceHost.Tests.csproj -c Release` (in reference-servicetypes)
- `dotnet test public/tests/hosts/FdwReferenceHosts.ForwardedHeaders.Registration.Tests/FdwReferenceHosts.ForwardedHeaders.Registration.Tests.csproj -c Release` (in reference-servicetypes)

## Dependencies

Depends on: `data` (7), `messagelogging` (7), `collections` (5), `configuration` (3), `results` (3), `services` (3), `commands` (2), `abstractions` (1), `multitenancy` (1), `pipelines` (1), `secretmanagers` (1), `ui` (1), `validation` (1).
Depended on by: `data` (4), `mcp` (2), `pipelines` (2), `authentication` (1), `authorization` (1), `calculations` (1), `catalog` (1), `connections` (1), `endpoints` (1), `multitenancy` (1), `notifications` (1), `quality` (1), `scheduling` (1), `schema` (1), `ui` (1), `users` (1).
(The number is how many references cross between the two domains' src projects.)

What a change here builds and ships: `python3 public/tools/versioning/changeset.py plan --repo . --base-ref <ref>`.

## Further reading

fdw-anatomy (the cited reference): `host-endpoint`, `middleware`.

namespace Fdw.Services.Connections.Clients;

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Results;
using System;
using Fdw.Services.Connections.Clients.Models;
using Fdw.Web.Clients.Abstractions;

/// <summary>
/// API client for connection management endpoints.
/// </summary>
public class ConnectionApiClient : ApiClientBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionApiClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger.</param>
    public ConnectionApiClient(HttpClient httpClient, ILogger<ConnectionApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>
    /// Gets all configured connections.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of connections.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConnectionPayload>>> GetConnections(CancellationToken ct = default)
        => GetList<ConnectionPayload>("connections", ct);

    /// <summary>
    /// Gets a list of available connection types.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the list of connection types.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConnectionTypePayload>>> GetConnectionTypes(CancellationToken ct = default)
        => GetList<ConnectionTypePayload>("connections/types", ct);

    /// <summary>Gets the connections declared for a particular connection type.</summary>
    /// <param name="connectionType">The connection type to list connections for.</param>
    /// <param name="ct">A token to cancel the request.</param>
    /// <returns>A result containing the connections of that type.</returns>
    public virtual Task<IGenericResult<IReadOnlyList<ConnectionByTypePayload>>> GetConnectionsByType(string connectionType, CancellationToken ct = default)
        => GetList<ConnectionByTypePayload>($"connections/by-type/{Uri.EscapeDataString(connectionType)}", ct);

    /// <summary>
    /// Gets a specific connection by name.
    /// </summary>
    /// <param name="name">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the connection details.</returns>
    public virtual Task<IGenericResult<ConnectionDetailResponse>> GetConnection(string name, CancellationToken ct = default)
        => Get<ConnectionDetailResponse>($"connections/{name}", ct);

    /// <summary>
    /// Creates a new connection.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the created connection details.</returns>
    /// <remarks>
    /// Why: each connection type has its own typed-body create endpoint (the MsSql endpoint at
    /// <c>connections</c> only knows Server/Port/Database, so posting an HTTP connection there
    /// silently drops BaseUrl/Protocol/SecurityType). Dispatch on ServiceType so each type POSTs
    /// its own route with the right typed body.
    /// </remarks>
    public virtual Task<IGenericResult<ConnectionDetailResponse>> CreateConnection(CreateConnectionClientRequest request, CancellationToken ct = default)
        => Post<CreateConnectionClientRequest, ConnectionDetailResponse>(ResolveCreateRoute(request.ServiceType), request, ct);

    /// <summary>
    /// Resolves the per-type create route for the given service type. Unknown / database types
    /// fall through to the MsSql-style <c>connections</c> endpoint (Server/Port/Database body);
    /// non-database types (Http, PostgreSql, FileSystem, RoslynWorkspace) POST their own route.
    /// </summary>
    private static string ResolveCreateRoute(string? serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return "connections";
        }

        if (serviceType.Contains("Http", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/http";
        }

        if (serviceType.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/postgresql";
        }

        if (serviceType.Contains("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/filesystem";
        }

        if (serviceType.Contains("Roslyn", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/roslynworkspace";
        }

        return "connections";
    }

    /// <summary>
    /// Updates an existing connection.
    /// </summary>
    /// <param name="name">The connection name.</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the updated connection details.</returns>
    /// <remarks>
    /// Why: each connection type has its own typed-body update endpoint. Dispatch on ServiceType so
    /// each type PUTs its own route with the correct typed body — the same pattern as CreateConnection.
    /// </remarks>
    public virtual Task<IGenericResult<ConnectionDetailResponse>> UpdateConnection(string name, UpdateConnectionClientRequest request, CancellationToken ct = default)
        => Patch<UpdateConnectionClientRequest, ConnectionDetailResponse>($"{ResolveUpdateRoute(request.ServiceType)}/{name}", request, ct);

    /// <summary>
    /// Resolves the per-type update route for the given service type. Unknown / database types
    /// fall through to the MsSql-style <c>connections</c> endpoint; non-database types post their own route.
    /// </summary>
    private static string ResolveUpdateRoute(string? serviceType)
    {
        if (string.IsNullOrWhiteSpace(serviceType))
        {
            return "connections";
        }

        if (serviceType.Contains("Http", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/http";
        }

        if (serviceType.Contains("PostgreSql", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/postgresql";
        }

        if (serviceType.Contains("FileSystem", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/filesystem";
        }

        if (serviceType.Contains("Roslyn", StringComparison.OrdinalIgnoreCase))
        {
            return "connections/roslynworkspace";
        }

        return "connections";
    }

    /// <summary>
    /// Deletes a connection.
    /// </summary>
    /// <param name="name">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result indicating whether the deletion succeeded.</returns>
    public virtual Task<IGenericResult> DeleteConnection(string name, CancellationToken ct = default)
        => Delete($"connections/{name}", ct);

    /// <summary>
    /// Tests a connection by name.
    /// </summary>
    /// <param name="name">The connection name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the test result.</returns>
    public virtual Task<IGenericResult<TestConnectionClientResponse>> TestConnection(string name, CancellationToken ct = default)
        => PostWithResponse<TestConnectionClientResponse>($"connections/{name}/test", ct);

    /// <summary>
    /// Tests a connection configuration in-memory without persisting to the database.
    /// Avoids IOptionsMonitor polling delays during wizard flows.
    /// </summary>
    /// <param name="request">The connection configuration to test.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the test result.</returns>
    public virtual Task<IGenericResult<TestConnectionClientResponse>> TestConnectionConfig(CreateConnectionClientRequest request, CancellationToken ct = default)
        => Post<CreateConnectionClientRequest, TestConnectionClientResponse>("connections/test-config", request, ct);

    /// <summary>
    /// Generates DDL from a connection's discovered schema.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="request">The generate DDL request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the generated DDL.</returns>
    public virtual Task<IGenericResult<GenerateDdlResponse>> GenerateDdl(string connectionName, GenerateDdlRequestPayload request, CancellationToken ct = default)
        => Post<GenerateDdlRequestPayload, GenerateDdlResponse>($"connections/{connectionName}/generate-ddl", request, ct);

    /// <summary>
    /// Executes DDL (create table) on a connection.
    /// </summary>
    /// <param name="connectionName">The connection name.</param>
    /// <param name="request">The execute DDL request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the execution result.</returns>
    public virtual Task<IGenericResult<ExecuteDdlConnectionResponse>> ExecuteDdl(string connectionName, ExecuteDdlConnectionRequest request, CancellationToken ct = default)
        => Post<ExecuteDdlConnectionRequest, ExecuteDdlConnectionResponse>($"connections/{connectionName}/execute-ddl", request, ct);

    /// <summary>
    /// Gets the capability metadata declared by the specified connection type.
    /// </summary>
    /// <param name="connectionTypeName">The connection type name (e.g. "MsSql", "PostgreSql").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A result containing the capabilities DTO, with empty lists for capabilities not declared
    /// by the connection type.
    /// </returns>
    public virtual Task<IGenericResult<ConnectionTypeCapabilitiesPayload>> GetCapabilities(
        string connectionTypeName, CancellationToken cancellationToken = default)
        => Get<ConnectionTypeCapabilitiesPayload>(
            $"connection-types/{Uri.EscapeDataString(connectionTypeName)}/capabilities", cancellationToken);
}

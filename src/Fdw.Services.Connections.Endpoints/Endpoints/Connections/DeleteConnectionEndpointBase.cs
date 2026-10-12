using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints.Logging;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for deleting a connection configuration by name, then evicting the live connection
/// instance from the connection provider.
/// </summary>
/// <remarks>
/// Why this is not generic over the typed body: the provider's soft delete cascades the whole aggregate on
/// its own — it reads the composed connection, retires the typed body's children and property bags, then the
/// typed body through its registered provider, then the header row. The endpoint has nothing type-specific
/// left to do, so every connection type deletes through this one class. The previous shape held a typed
/// provider purely to delete the body itself, which meant one delete endpoint per connection type and a
/// silent "typed body missing is non-fatal" branch that let a half-deleted connection report success.
/// </remarks>
public abstract class DeleteConnectionEndpointBase : CrudDeleteEndpointBase<ConnectionNameRequest>
{
    private readonly ConnectionConfigurationProvider _connectionProvider;
    private readonly IConnectionProvider _connectionLookupProvider;
    private readonly ILogger<DeleteConnectionEndpointBase> _logger;

    /// <inheritdoc />
    protected DeleteConnectionEndpointBase(
        ConnectionConfigurationProvider connectionProvider,
        IConnectionProvider connectionLookupProvider,
        ILogger<DeleteConnectionEndpointBase>? logger = null)
        : base(logger ?? NullLogger<DeleteConnectionEndpointBase>.Instance)
    {
        _connectionProvider = connectionProvider;
        _connectionLookupProvider = connectionLookupProvider;
        _logger = logger ?? NullLogger<DeleteConnectionEndpointBase>.Instance;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Returns the connection name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ConnectionNameRequest request) => request.Name;

    /// <summary>Checks whether the connection exists before attempting deletion.</summary>
    protected override async Task<IGenericResult<bool>> CheckExistsForDelete(ConnectionNameRequest request, CancellationToken ct)
    {
        var existingResult = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        return GenericResult<bool>.Success(existingResult.IsSuccess && existingResult.Value != null);
    }

    /// <summary>Soft-deletes the connection aggregate, then deletes the secrets it owned.</summary>
    protected override async Task<IGenericResult> Delete(ConnectionNameRequest request, CancellationToken ct)
    {
        var existing = await _connectionProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (existing.IsFailure)
            return existing;
        if (existing.Value is null)
            return GenericResult.Failure(ConnectionEndpointLog.ConnectionNotFound(_logger, request.Name));

        // The connection owns the secrets its credentials name (FDW-1202): they are read before the delete and removed after it.
        return await ConnectionCredentialStore.DeleteConnection(
            ConnectionTypes.ByName(existing.Value.Implementation),
            existing.Value,
            token => _connectionProvider.Delete(request.Name, token),
            HttpContext.RequestServices,
            _logger,
            ct).ConfigureAwait(false);
    }
}

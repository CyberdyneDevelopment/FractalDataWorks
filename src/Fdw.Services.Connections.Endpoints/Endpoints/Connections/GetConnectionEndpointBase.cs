using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using Fdw.Web.RestEndpoints.Crud;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Base endpoint for retrieving a specific connection configuration by name. Type-agnostic: it reads
/// the parent header (which <see cref="ConnectionConfigurationProvider"/> populates with the
/// polymorphic typed body in <see cref="ConnectionConfiguration.Configuration"/>) and hands that body
/// to <see cref="MapToDetail"/>. The concrete endpoint maps the typed body to the DTO by dispatching
/// on <see cref="ConnectionConfiguration.ServiceOptionType"/> — so one GET-by-name endpoint renders
/// every connection type (MsSql, Http, PostgreSql, FileSystem, RoslynWorkspace) rather than being
/// locked to a single typed provider.
/// </summary>
public abstract class GetConnectionEndpointBase : CrudGetEndpointBase<ConnectionNameRequest, ConnectionDetailDto>
{
    // Why: the parent provider reads conn.Connection via the configuration gateway AND populates
    // header.Configuration with the typed body (PopulateTypedBody dispatches on ServiceOptionType).
    // There is no second typed-provider read here — the polymorphic body comes back on the header,
    // so the GET path renders any connection type without being closed to one TConfig.
    private readonly ConnectionConfigurationProvider _configProvider;

    /// <inheritdoc />
    protected GetConnectionEndpointBase(ConnectionConfigurationProvider configProvider)
    {
        _configProvider = configProvider;
    }

    /// <summary>Gets the resource name used for route and policy generation.</summary>
    protected override string ResourceName => "connections";

    /// <summary>Returns the connection name as the resource identifier.</summary>
    protected override string GetResourceIdentifier(ConnectionNameRequest request) => request.Name;

    /// <summary>
    /// Finds a connection by name (or Guid id) and maps it — together with its polymorphic typed
    /// body (<see cref="ConnectionConfiguration.Configuration"/>) — to a detail DTO.
    /// </summary>
    protected override async Task<IGenericResult<ConnectionDetailDto?>> FindByIdentifier(ConnectionNameRequest request, CancellationToken ct)
    {
        // Accept either Guid (connection Id) or string name as identifier.
        var parentResult = Guid.TryParse(request.Name, out var id)
            ? await _configProvider.Get(id, ct).ConfigureAwait(false)
            : await _configProvider.Get(request.Name, ct).ConfigureAwait(false);
        if (!parentResult.IsSuccess) return parentResult.ToNewResult<ConnectionDetailDto?>();

        var parent = parentResult.Value;
        if (parent is null) return GenericResult<ConnectionDetailDto?>.Success(null);

        // Why: parent.Configuration is the typed body already loaded by PopulateTypedBody for this
        // connection's ServiceOptionType — no redundant per-type read. May be null when the header
        // has no typed body yet; MapToDetail renders header-only in that case.
        return GenericResult<ConnectionDetailDto?>.Success(MapToDetail(parent, parent.Configuration));
    }

    /// <summary>
    /// Maps the parent connection and its polymorphic typed body to a detail DTO. Implementations
    /// dispatch the type-specific projection on <see cref="ConnectionConfiguration.ServiceOptionType"/>.
    /// The body may be null if the typed row does not exist yet (header-only render).
    /// </summary>
    protected abstract ConnectionDetailDto MapToDetail(ConnectionConfiguration connection, IConnectionConfiguration? body);
}

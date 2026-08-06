using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Returns the schema information for a connection. If no schema has been discovered yet,
/// discovers it on-demand and persists the results before returning.
/// </summary>
// Why: Schema information is lazy-loaded — the first GET triggers discovery if the DataStore
// doesn't exist yet. Subsequent calls return the cached metadata. This eliminates the need
// for a separate startup discovery service.
public abstract class GetConnectionSchemaEndpointBase : Endpoint<ConnectionNameRequest, SchemaInformationDto>
{
    private readonly ISchemaInformationService _schemaService;

    /// <summary>
    /// Initializes a new instance of <see cref="GetConnectionSchemaEndpointBase"/>.
    /// </summary>
    protected GetConnectionSchemaEndpointBase(ISchemaInformationService schemaService)
    {
        _schemaService = schemaService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/connections/{Name}/schema");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get connection schema";
            s.Description = "Returns the discovered schema (DataStore, paths, containers, fields) for a connection. Discovers on first access if not yet cached.";
        });
        ConfigureEndpoint();
    }

    /// <summary>
    /// Override in concrete endpoints for additional configuration (Tags, etc.).
    /// </summary>
    protected virtual void ConfigureEndpoint()
    {
    }

    /// <inheritdoc />
    public override async Task HandleAsync(ConnectionNameRequest req, CancellationToken ct)
    {
        var result = await _schemaService.GetSchema(req.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value == null)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(SchemaInformationDto.FromSchema(result.Value), ct).ConfigureAwait(false);
    }
}

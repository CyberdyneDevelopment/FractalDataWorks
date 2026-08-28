using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Microsoft.AspNetCore.Http;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Forces re-discovery of schema for a connection, persists the results, and returns the updated metadata.
/// </summary>
public abstract class RefreshConnectionSchemaEndpointBase : Endpoint<ConnectionNameRequest, SchemaInformationDto>
{
    private readonly ISchemaInformationService _schemaService;

    /// <summary>
    /// Initializes a new instance of <see cref="RefreshConnectionSchemaEndpointBase"/>.
    /// </summary>
    protected RefreshConnectionSchemaEndpointBase(ISchemaInformationService schemaService)
    {
        _schemaService = schemaService;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/connections/{Name}/schema/refresh");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("connections:write");
#endif
        Summary(s =>
        {
            s.Summary = "Refresh connection schema";
            s.Description = "Forces re-discovery of schema for a connection and returns the updated metadata.";
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
        var result = await _schemaService.RefreshSchema(req.Name, ct).ConfigureAwait(false);

        if (!result.IsSuccess || result.Value == null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await HttpContext.Response.WriteAsJsonAsync(
                new { Error = "Schema discovery failed", Details = result.CurrentMessage ?? string.Empty }, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(SchemaInformationDto.FromSchema(result.Value), ct).ConfigureAwait(false);
    }
}

using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Fdw.Results;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Forces re-discovery of schema for a connection, persists the results, and returns the updated metadata.
/// </summary>
public abstract class RefreshConnectionSchemaEndpointBase : Endpoint<ConnectionSchemaRequest, SchemaInformationDto>
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
    public override async Task HandleAsync(ConnectionSchemaRequest req, CancellationToken ct)
    {
        // The discovery's choices come from the request (FDW-3823); one left out is refused, naming it, before anything is read.
        var options = DataStoreDiscoveryChoices.FromRequest(
            req.ExcludedSchemas, req.IncludeOnlySchemas, req.DiscoverViews, req.DiscoverIndexes, req.DiscoverForeignKeys, req.DiscoverDescriptions,
            req.Name, Logger);
        if (options.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(options, HttpContext)).ConfigureAwait(false);
            return;
        }

        var result = await _schemaService.RefreshSchema(req.Name, options.Value!, ct).ConfigureAwait(false);

        if (result.IsFailure)
        {
            // Answered as every endpoint answers a failed result: the status is its category's and the reason the client text its message declares, so a refresh the
            // cross-tenant change setting forbids is a 403 naming the setting and the log's own line (tenant ids, the cause) does not reach the caller (FDW-4459).
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (result.Value == null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(ConnectionsAnswerLog.RefreshConnectionSchemaSchemaDiscoveryFailed(Logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(SchemaInformationDto.FromSchema(result.Value), ct).ConfigureAwait(false);
    }
}

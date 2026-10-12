using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Web.RestEndpoints.Logging;

using Fdw.Data.DataSets;
using Microsoft.AspNetCore.Http;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to save field mappings for a source (replace all).
/// </summary>
/// <remarks>
/// A source owns its mappings as a child of the data set aggregate, so the replacement is assigned to
/// the source and the data set is saved: the cascade supplies the row key tying each mapping to its
/// source and retires the mappings left out. Each mapping keeps the Id its source gives its field
/// (<see cref="DataSetSourceConfiguration.MappingIdFor"/>), so re-saving a source does not detach the
/// transformations attached to its mappings (FDW-877).
/// </remarks>
public abstract class SaveSourceMappingsEndpointBase : Endpoint<SaveSourceMappingsRequest, List<FieldMappingResponsePayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<SaveSourceMappingsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveSourceMappingsEndpointBase"/> class.
    /// </summary>
    protected SaveSourceMappingsEndpointBase(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<SaveSourceMappingsEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<SaveSourceMappingsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Patch("/datasets/{Name}/sources/{SourceName}/mappings");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Save field mappings for a source";
            s.Description = "Replaces all field mappings for a specific source. Existing mappings are soft-deleted.";
        });
    }

    /// <summary>
    /// Soft-deletes existing mappings for the source and inserts new mappings from the request.
    /// </summary>
    public override async Task HandleAsync(SaveSourceMappingsRequest req, CancellationToken ct)
    {
        EndpointLog.UpdatingResource(_logger, "field mappings", req.Name);

        // Resolve the DataSet and its SourceIds via the provider (AssembleHierarchy runs on Get(name)).
        var dsResult = await _dataSetProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (dsResult.IsFailure || dsResult.Value is null)
        {
            EndpointLog.ResourceNotFound(_logger, "DataSet", req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var source = dsResult.Value.Sources?
            .FirstOrDefault(s => string.Equals(s.SourceName, req.SourceName, StringComparison.OrdinalIgnoreCase));

        if (source is null)
        {
            EndpointLog.ResourceNotFound(_logger, "DataSet source", req.SourceName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var validation = DataSetSourceConfiguration.ValidateMappingFields(
            req.Mappings.Select(m => m.LogicalFieldName), req.Name, source.SourceName, _logger);
        if (validation.IsFailure)
        {
            await SendError(validation, ct).ConfigureAwait(false);
            return;
        }

        // ToList runs before the assignment, so MappingIdFor still reads the mappings being replaced.
        source.Mappings = req.Mappings
            .Select((m, i) => new DataSetFieldMappingConfiguration
            {
                Id = source.MappingIdFor(m.LogicalFieldName),
                // The field it fills is what identifies it within the source.
                Name = m.LogicalFieldName,
                LogicalFieldName = m.LogicalFieldName,
                SourceKind = source.SourceKind,
                PhysicalFieldName = m.PhysicalFieldName,
                TransformExpression = m.TransformExpression,
                Ordinal = i,
            })
            .ToList();

        var saveResult = await _dataSetProvider.Save(dsResult.Value, "DataSet", dsResult.Value.Implementation, dsResult.Value.Name, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            await SendError(saveResult, ct).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(source.Mappings.Select(m => new FieldMappingResponsePayload
        {
            Id = m.Id,
            DataSetSourceId = source.Id,
            SourceName = source.SourceName,
            LogicalFieldName = m.LogicalFieldName,
            PhysicalFieldName = m.PhysicalFieldName,
            TransformExpression = m.TransformExpression,
        }).ToList(), ct).ConfigureAwait(false);
    }

    /// <summary>Answers a failed validation or save with the status its result codes map to.</summary>
    /// <remarks>
    /// Through <see cref="ResultHttpStatusMapper"/> rather than a status picked here, so a caller's
    /// mistake (DATASETS-20011/20012) answers 400 and a failed save answers what its code says.
    /// </remarks>
    /// <param name="result">The failed result.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    private async Task SendError(IGenericResult result, CancellationToken ct)
    {
        var (statusCode, errorResponse) = ResultHttpStatusMapper.Map(result, HttpContext);
        await Send.ResultAsync(TypedResults.Json(errorResponse, statusCode: statusCode)).ConfigureAwait(false);
    }
}

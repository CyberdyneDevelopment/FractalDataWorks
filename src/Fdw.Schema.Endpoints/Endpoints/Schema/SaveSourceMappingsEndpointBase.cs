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
using Fdw.Web.RestEndpoints.Logging;

using Fdw.Data.DataSets;
using Microsoft.AspNetCore.Http;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to save field mappings for a source (replace all).
/// </summary>
/// <remarks>
/// Why: DataSetConfigurationProvider.Get(name) provides the authoritative DataSet lookup with
/// Sources already composed into the aggregate (including SourceName for matching).
/// IDataGateway is retained only for the FieldMapping child record operations (insert/soft-delete),
/// which are a documented exception because DataSetFieldMapping does not implement IGenericConfiguration.
/// </remarks>
public abstract class SaveSourceMappingsEndpointBase : Endpoint<SaveSourceMappingsRequest, List<FieldMappingResponsePayload>>
{
    // Why: IDataGateway retained for FieldMapping child record operations only.
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<SaveSourceMappingsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SaveSourceMappingsEndpointBase"/> class.
    /// </summary>
    protected SaveSourceMappingsEndpointBase(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider,
        ILogger<SaveSourceMappingsEndpointBase> logger)
    {
        _dataGateway = dataGateway;
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

        // Why: Sources are part of the composed aggregate returned by DataSetConfigurationProvider.Get.
        var source = dsResult.Value.Sources?
            .FirstOrDefault(s => string.Equals(s.SourceName, req.SourceName, StringComparison.OrdinalIgnoreCase));

        if (source is null)
        {
            EndpointLog.ResourceNotFound(_logger, "DataSet source", req.SourceName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // Why the whole set is assigned and the data set saved, rather than the rows being written
        // here: a mapping is a child of its source, and the provider's save is what cascades an
        // aggregate's children — including the row key that ties a child to its parent. Writing the
        // rows directly had no way to supply that key, so every insert was refused.
        //
        // Assigning the collection is what makes this a replacement: mappings left out are retired
        // by the same cascade that inserts the new ones, so there is no separate delete step.
        source.Mappings = req.Mappings
            .Select((m, i) => new DataSetFieldMappingConfiguration
            {
                // Why: default id means insert, and these are the mappings as the caller now states
                // them — an existing one keeps its identity through the cascade's own matching.
                Id = default,
                DataSetSourceId = source.Id,
                // The field it fills is what identifies it within the source.
                Name = m.LogicalFieldName,
                LogicalFieldName = m.LogicalFieldName,
                PhysicalFieldName = m.PhysicalFieldName,
                TransformExpression = m.TransformExpression,
                Ordinal = i,
            })
            .ToList();

        var saveResult = await _dataSetProvider.Save(dsResult.Value, ct).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            await SendSaveFailure(saveResult, ct).ConfigureAwait(false);
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

    /// <summary>Answers a failed save with the reason rather than an empty success.</summary>
    /// <param name="result">The failed result the gateway returned.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    private Task SendSaveFailure(IGenericResult result, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = 500;
        HttpContext.Response.ContentType = "application/json";
        return HttpContext.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = 500,
            Title = "Field mappings were not saved",
            Detail = result.CurrentMessage,
            Instance = HttpContext.Request.Path.HasValue ? HttpContext.Request.Path.Value : null,
        }, ct);
    }

    /// <summary>Soft-deletes all active mappings for the specified source by setting IsDeleted and clearing IsCurrent.</summary>
    protected virtual async Task<IGenericResult> SoftDeleteExistingMappings(Guid sourceId, CancellationToken ct)
    {
        var existingCommand = new QueryCommand<FieldMappingDbRecord>
        {
            Filter = new FilterExpression
            {
                Root = new FilterGroup
                {
                    Operator = LogicalOperator.And,
                    Nodes =
                    [
                        new FilterCondition
                        {
                            PropertyName = "DataSetSourceId",
                            Operator = FilterOperators.ByName("Equal"),
                            Value = sourceId
                        },
                        new FilterCondition
                        {
                            PropertyName = "IsDeleted",
                            Operator = FilterOperators.ByName("Equal"),
                            Value = false
                        }
                    ]
                }
            }
        };

        // Why: DataStoreName and PathName come from the provider to avoid hardcoding "ConfigurationDb"/"data".
        var fieldMappingTarget = new DataStoreTarget(_dataSetProvider.DataStoreName, _dataSetProvider.PathName, "DataSetFieldMapping");
        var existingResult = await _dataGateway.Execute<IEnumerable<FieldMappingDbRecord>>(existingCommand, fieldMappingTarget, ct).ConfigureAwait(false);
        if (existingResult.IsFailure)
        {
            return existingResult;
        }

        var existingMappings = existingResult.Value?.ToList() ?? [];

        foreach (var existing in existingMappings)
        {
            existing.IsDeleted = true;
            existing.IsCurrent = false;

            var updateCommand = new UpdateCommand<FieldMappingDbRecord>(existing)
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition
                    {
                        PropertyName = "Id",
                        Operator = FilterOperators.ByName("Equal"),
                        Value = existing.Id
                    }
                }
            };

            // Why the result is read: discarding it left a mapping current that the save believed
            // it had retired, so the insert that followed collided with a row nobody thought existed.
            var retire = await _dataGateway.Execute<int>(updateCommand, fieldMappingTarget, ct).ConfigureAwait(false);
            if (retire.IsFailure)
            {
                return retire;
            }
        }

        return GenericResult.Success();
    }

    /// <summary>Inserts new field mapping records and returns the created response DTOs.</summary>
    protected virtual async Task<IGenericResult<IReadOnlyList<FieldMappingResponsePayload>>> InsertNewMappings(
        Guid sourceId,
        string sourceName,
        IList<FieldMappingInputPayload> mappings,
        CancellationToken ct)
    {
        var created = new List<FieldMappingResponsePayload>();

        foreach (var mapping in mappings)
        {
            var newMapping = new FieldMappingDbRecord
            {
                // Why: Id = default (Guid.Empty) signals INSERT with UUIDv7 from the gateway.
                // No Guid.NewGuid() — the DB/gateway mints the ID.
                Id = default,
                DataSetSourceId = sourceId,
                LogicalFieldName = mapping.LogicalFieldName,
                PhysicalFieldName = mapping.PhysicalFieldName,
                TransformExpression = mapping.TransformExpression,
                IsCurrent = true,
                IsDeleted = false
            };

            var insertCommand = new InsertCommand<FieldMappingDbRecord>(newMapping);

            var fieldMappingTarget = new DataStoreTarget(_dataSetProvider.DataStoreName, _dataSetProvider.PathName, "DataSetFieldMapping");
            var insertResult = await _dataGateway.Execute<int>(insertCommand, fieldMappingTarget, ct).ConfigureAwait(false);

            // Why a failed row ends the save: skipping it returned the mappings that happened to
            // land, and when none did the caller got 200 and an empty list — a save that saved
            // nothing, reported as success. The gateway already says what went wrong; this passes
            // it back rather than dropping it.
            if (insertResult.IsFailure)
            {
                return insertResult.ToNewResult<IReadOnlyList<FieldMappingResponsePayload>>();
            }

            created.Add(new FieldMappingResponsePayload
            {
                Id = newMapping.Id,
                DataSetSourceId = sourceId,
                SourceName = sourceName,
                LogicalFieldName = newMapping.LogicalFieldName,
                PhysicalFieldName = newMapping.PhysicalFieldName,
                TransformExpression = newMapping.TransformExpression
            });
        }

        return GenericResult<IReadOnlyList<FieldMappingResponsePayload>>.Success(created);
    }
}

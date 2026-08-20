using Microsoft.AspNetCore.Http;
using Fdw.Results;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.Abstractions;
// DataSetRecord and DataSetSourcePayload now in this namespace
// ApiEndpointLog now in this namespace
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;
using Fdw.Schema.Clients.Models;
using Fdw.Web.RestEndpoints.Logging;

using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to get all field mappings for a DataSet (across all sources).
/// </summary>
public abstract class GetDataSetMappingsEndpoint : Endpoint<GetMappingsRequest, List<FieldMappingResponsePayload>>
{
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<GetDataSetMappingsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetMappingsEndpoint"/> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for database operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetDataSetMappingsEndpoint(IDataGateway dataGateway, ILogger<GetDataSetMappingsEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _logger = logger;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/datasets/{Name}/mappings");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get field mappings for a DataSet";
            s.Description = "Returns all field mappings across all sources for the specified DataSet.";
        });
    }

    /// <summary>
    /// Retrieves all field mappings across all sources for the specified data set.
    /// </summary>
    public override async Task HandleAsync(GetMappingsRequest req, CancellationToken ct)
    {
        EndpointLog.GettingResource(_logger, "field mappings", req.Name);

        // Why the read is checked before its value: a failed read and a name that does not exist
        // both arrived here as null, so a broken query answered 404 "this data set does not exist".
        var dataSetResult = await FindDataSet(req.Name, ct).ConfigureAwait(false);
        if (!dataSetResult.IsSuccess)
        {
            await SendReadFailure("data set", dataSetResult.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        if (dataSetResult.Value is not { } dataSet)
        {
            EndpointLog.ResourceNotFound(_logger, "DataSet", req.Name);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var sourcesResult = await GetSources(dataSet.Id, ct).ConfigureAwait(false);
        if (!sourcesResult.IsSuccess)
        {
            await SendReadFailure("data set sources", sourcesResult.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        var sources = sourcesResult.Value ?? [];
        if (sources.Count == 0)
        {
            await Send.OkAsync(new List<FieldMappingResponsePayload>(), ct).ConfigureAwait(false);
            return;
        }

        var allMappings = new List<FieldMappingResponsePayload>();
        foreach (var source in sources)
        {
            var mappingsResult = await GetMappingsForSource(source.Id, ct).ConfigureAwait(false);
            if (!mappingsResult.IsSuccess)
            {
                await SendReadFailure("field mappings", mappingsResult.CurrentMessage, ct).ConfigureAwait(false);
                return;
            }

            allMappings.AddRange((mappingsResult.Value ?? []).Select(m => MapToResponse(m, source.SourceName)));
        }

        await Send.OkAsync(allMappings, ct).ConfigureAwait(false);
    }

    /// <summary>Finds a data set record by name.</summary>
    protected virtual async Task<IGenericResult<DataSetRecord?>> FindDataSet(string name, CancellationToken ct)
    {
        var command = new QueryCommand<DataSetRecord>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "Name",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = name
                }
            }
        };

        var result = await _dataGateway.Execute<IEnumerable<DataSetRecord>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSet"), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<DataSetRecord?>();
        }

        return GenericResult<DataSetRecord?>.Success(result.Value?.FirstOrDefault());
    }

    /// <summary>Gets all source records for the specified data set.</summary>
    protected virtual async Task<IGenericResult<IList<DataSetSourceConfiguration>>> GetSources(Guid dataSetId, CancellationToken ct)
    {
        var command = new QueryCommand<DataSetSourceConfiguration>
        {
            Filter = new FilterExpression
            {
                Root = new FilterCondition
                {
                    PropertyName = "DataSetId",
                    Operator = FilterOperators.ByName("Equal"),
                    Value = dataSetId
                }
            }
        };

        var result = await _dataGateway.Execute<IEnumerable<DataSetSourceConfiguration>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSetSource"), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IList<DataSetSourceConfiguration>>();
        }

        return GenericResult<IList<DataSetSourceConfiguration>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>Gets all active (non-deleted) field mapping records for the specified source.</summary>
    protected virtual async Task<IGenericResult<IList<FieldMappingDbRecord>>> GetMappingsForSource(Guid sourceId, CancellationToken ct)
    {
        var command = new QueryCommand<FieldMappingDbRecord>
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

        var result = await _dataGateway.Execute<IEnumerable<FieldMappingDbRecord>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSetFieldMapping"), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return result.ToNewResult<IList<FieldMappingDbRecord>>();
        }

        return GenericResult<IList<FieldMappingDbRecord>>.Success(result.Value?.ToList() ?? []);
    }

    /// <summary>Maps a field mapping database record to a response DTO.</summary>
    protected virtual FieldMappingResponsePayload MapToResponse(FieldMappingDbRecord record, string sourceName)
    {
        return new FieldMappingResponsePayload
        {
            Id = record.Id,
            DataSetSourceId = record.DataSetSourceId,
            SourceName = sourceName,
            LogicalFieldName = record.LogicalFieldName,
            PhysicalFieldName = record.PhysicalFieldName,
            TransformExpression = record.TransformExpression
        };
    }

    // Why a 500 rather than an empty list: the read did not happen, so nothing is known about what
    // it would have returned. Answering [] claims the data set has no mappings, which is a different
    // and possibly false statement.
    private Task SendReadFailure(string what, string? reason, CancellationToken ct)
    {
        HttpContext.Response.StatusCode = 500;
        HttpContext.Response.ContentType = "application/json";
        return HttpContext.Response.WriteAsJsonAsync(new
        {
            errorCode = "ReadFailed",
            messages = new[] { $"Reading {what} failed: {reason ?? "no reason given"}" }
        }, ct);
    }

}

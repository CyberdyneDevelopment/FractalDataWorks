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
using Fdw.Services.Data;
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
public abstract class GetDataSetMappingsEndpointBase : Endpoint<GetMappingsRequest, List<FieldMappingResponsePayload>>
{
    private readonly IDataGatewayProvider _dataGateways;

    // Why resolved here rather than injected: the gateway is scoped and this is not, so holding one
    // would be a captive dependency. The provider is asked when a call is actually being made.
    private IDataGateway Gateway => _dataGateways.ByName("Main");
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetDataSetMappingsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetDataSetMappingsEndpointBase"/> class.
    /// </summary>
    /// <param name="dataGateways">The data gateway for database operations.</param>
    /// <param name="dataSetProvider">Reads the data set with its sources and fields composed.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetDataSetMappingsEndpointBase(IDataGatewayProvider dataGateways, DataSetConfigurationProvider dataSetProvider, ILogger<GetDataSetMappingsEndpointBase> logger)
    {
        _dataGateways = dataGateways;
        _dataSetProvider = dataSetProvider;
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

        // Get(name) composes Sources and Fields onto the implementation record, so the data set and its
        // children are one read.
        var dataSetResult = await _dataSetProvider.Get(req.Name, ct).ConfigureAwait(false);
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

        var sources = dataSet.Sources;
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

        var result = await Gateway.Execute<IEnumerable<FieldMappingDbRecord>>(
            command, new DataStoreTarget("PlatformConfiguration", "data", "DataSetFieldMapping"), ct).ConfigureAwait(false);
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

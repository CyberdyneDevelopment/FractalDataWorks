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

using Fdw.Data.DataSets;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to get field mappings for a specific source.
/// </summary>
public abstract class GetSourceMappingsEndpointBase : Endpoint<GetSourceMappingsRequest, List<FieldMappingResponsePayload>>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetSourceMappingsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSourceMappingsEndpointBase"/> class.
    /// </summary>
    /// <param name="dataSetProvider">Reads the data set with its sources and fields composed.</param>
    /// <param name="logger">The logger instance.</param>
    protected GetSourceMappingsEndpointBase(DataSetConfigurationProvider dataSetProvider, ILogger<GetSourceMappingsEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<GetSourceMappingsEndpointBase>.Instance;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Get("/datasets/{Name}/sources/{SourceName}/mappings");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:read");
#endif
        Summary(s =>
        {
            s.Summary = "Get field mappings for a specific source";
            s.Description = "Returns all field mappings for a specific source within a DataSet.";
        });
    }

    /// <summary>
    /// Retrieves all field mappings for a specific source within a data set.
    /// </summary>
    public override async Task HandleAsync(GetSourceMappingsRequest req, CancellationToken ct)
    {
        EndpointLog.GettingResource(_logger, "source mappings", req.Name);

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

        if (dataSet.Sources.FirstOrDefault(s => string.Equals(s.SourceName, req.SourceName, StringComparison.OrdinalIgnoreCase)) is not { } source)
        {
            EndpointLog.ResourceNotFound(_logger, "DataSet source", req.SourceName);
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        // The source's mappings are children of the data set aggregate, composed by the same read.
        var response = source.Mappings.Select(m => MapToResponse(m, source)).ToList();
        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Maps one of a source's field mappings to a response DTO.</summary>
    protected virtual FieldMappingResponsePayload MapToResponse(DataSetFieldMappingConfiguration mapping, DataSetSourceConfiguration source)
    {
        return new FieldMappingResponsePayload
        {
            Id = mapping.Id,
            DataSetSourceId = source.Id,
            SourceName = source.SourceName,
            LogicalFieldName = mapping.LogicalFieldName,
            PhysicalFieldName = mapping.PhysicalFieldName,
            TransformExpression = mapping.TransformExpression
        };
    }

    private async Task SendReadFailure(string what, string? reason, CancellationToken ct)
    {
        await Send.ResultAsync(TypedResults.Json(new
        {
            errorCode = "ReadFailed",
            messages = new[] { $"Reading {what} failed: {reason ?? "no reason given"}" }
        }, statusCode: 500)).ConfigureAwait(false);
    }

}

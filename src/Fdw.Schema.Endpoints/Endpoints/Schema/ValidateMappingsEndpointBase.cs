using Microsoft.AspNetCore.Http;
using Fdw.Services.Data.Clients.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Data.Abstractions;
// DataSetRecord and DataSetSourcePayload now in this namespace
// ApiEndpointLog now in this namespace
using Microsoft.Extensions.Logging;
using Fdw.Operations.Endpoints;
using Fdw.Schema.Clients.Models;

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to validate field mappings without saving.
/// </summary>
public abstract class ValidateMappingsEndpointBase : Endpoint<ValidateMappingsRequest, MappingValidationResponse>
{
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<ValidateMappingsEndpointBase> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateMappingsEndpointBase"/> class.
    /// </summary>
    /// <param name="dataSetProvider">Reads the data set with its sources and fields composed.</param>
    /// <param name="logger">The logger instance.</param>
    protected ValidateMappingsEndpointBase(DataSetConfigurationProvider dataSetProvider, ILogger<ValidateMappingsEndpointBase> logger)
    {
        _dataSetProvider = dataSetProvider;
        _logger = logger;
    }

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/datasets/{Name}/mappings/validate");
#if DEVELOP
        AllowAnonymous();
#else
        Policies("datasets:write");
#endif
        Summary(s =>
        {
            s.Summary = "Validate field mappings";
            s.Description = "Validates field mappings against the DataSet schema without saving.";
        });
    }

    /// <summary>
    /// Validates the provided field mappings against the data set schema and returns validation results.
    /// </summary>
    public override async Task HandleAsync(ValidateMappingsRequest req, CancellationToken ct)
    {
        var errors = new List<MappingValidationError>();
        var warnings = new List<MappingValidationWarning>();

        // Get(name) composes Fields onto the implementation record. A failed read is reported as one:
        // validating against an empty field list would pass or fail mappings for a reason that has
        // nothing to do with them.
        var dataSetResult = await _dataSetProvider.Get(req.Name, ct).ConfigureAwait(false);
        if (!dataSetResult.IsSuccess)
        {
            await SendReadFailure("data set", dataSetResult.CurrentMessage, ct).ConfigureAwait(false);
            return;
        }

        if (dataSetResult.Value is not { } dataSet)
        {
            errors.Add(new MappingValidationError
            {
                Code = "DATASET_NOT_FOUND",
                Message = $"DataSet '{req.Name}' not found"
            });

            await Send.OkAsync(new MappingValidationResponse
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            }, ct).ConfigureAwait(false);
            return;
        }

        ValidateMappingEntries(req.Mappings, dataSet.Fields, errors, warnings);

        var response = new MappingValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates mapping entries against the data set fields, checking for empty names, unknown fields, duplicates, and unmapped required fields.
    /// </summary>
    protected virtual void ValidateMappingEntries(
        IList<FieldMappingInputPayload> mappings,
        IList<DataSetFieldConfiguration> fields,
        IList<MappingValidationError> errors,
        IList<MappingValidationWarning> warnings)
    {
        var fieldNames = fields.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seenLogicalFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mapping in mappings)
        {
            if (string.IsNullOrWhiteSpace(mapping.LogicalFieldName))
            {
                errors.Add(new MappingValidationError
                {
                    Code = "EMPTY_LOGICAL_FIELD",
                    Message = "Logical field name cannot be empty"
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(mapping.PhysicalFieldName))
            {
                errors.Add(new MappingValidationError
                {
                    Code = "EMPTY_PHYSICAL_FIELD",
                    Message = $"Physical field name cannot be empty for logical field '{mapping.LogicalFieldName}'",
                    PropertyPath = mapping.LogicalFieldName
                });
                continue;
            }

            if (fields.Count > 0 && !fieldNames.Contains(mapping.LogicalFieldName))
            {
                warnings.Add(new MappingValidationWarning
                {
                    Code = "UNKNOWN_LOGICAL_FIELD",
                    Message = $"Logical field '{mapping.LogicalFieldName}' is not defined in the DataSet schema"
                });
            }

            if (!seenLogicalFields.Add(mapping.LogicalFieldName))
            {
                errors.Add(new MappingValidationError
                {
                    Code = "DUPLICATE_MAPPING",
                    Message = $"Duplicate mapping for logical field '{mapping.LogicalFieldName}'",
                    PropertyPath = mapping.LogicalFieldName
                });
            }
        }

        var requiredFields = fields.Where(f => f.IsRequired).Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mappedFields = mappings.Select(m => m.LogicalFieldName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unmappedRequired = requiredFields.Except(mappedFields, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var unmapped in unmappedRequired)
        {
            warnings.Add(new MappingValidationWarning
            {
                Code = "UNMAPPED_REQUIRED_FIELD",
                Message = $"Required field '{unmapped}' has no mapping"
            });
        }
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

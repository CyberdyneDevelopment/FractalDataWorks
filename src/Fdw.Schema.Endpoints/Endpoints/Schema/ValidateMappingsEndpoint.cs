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

namespace Fdw.Schema.Endpoints;

/// <summary>
/// Endpoint to validate field mappings without saving.
/// </summary>
public abstract class ValidateMappingsEndpoint : Endpoint<ValidateMappingsRequest, MappingValidationResponse>
{
    private readonly IDataGateway _dataGateway;
    private readonly ILogger<ValidateMappingsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateMappingsEndpoint"/> class.
    /// </summary>
    /// <param name="dataGateway">The data gateway for database operations.</param>
    /// <param name="logger">The logger instance.</param>
    protected ValidateMappingsEndpoint(IDataGateway dataGateway, ILogger<ValidateMappingsEndpoint> logger)
    {
        _dataGateway = dataGateway;
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

        var dataSet = await FindDataSet(req.Name, ct).ConfigureAwait(false);
        if (dataSet == null)
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

        var fields = await GetDataSetFields(dataSet.Id, ct).ConfigureAwait(false);

        ValidateMappingEntries(req.Mappings, fields, errors, warnings);

        var response = new MappingValidationResponse
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };

        await Send.OkAsync(response, ct).ConfigureAwait(false);
    }

    /// <summary>Finds a data set record by name.</summary>
    protected virtual async Task<DataSetRecord?> FindDataSet(string name, CancellationToken ct)
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
            return null;
        }

        return result.Value?.FirstOrDefault();
    }

    /// <summary>Gets all field records for the specified data set.</summary>
    protected virtual async Task<IList<DataSetFieldPayload>> GetDataSetFields(Guid dataSetId, CancellationToken ct)
    {
        var command = new QueryCommand<DataSetFieldPayload>
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

        var result = await _dataGateway.Execute<IEnumerable<DataSetFieldPayload>>(
            command, new DataStoreTarget("ConfigurationDb", "data", "DataSetField"), ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return [];
        }

        return result.Value?.ToList() ?? [];
    }

    /// <summary>
    /// Validates mapping entries against the data set fields, checking for empty names, unknown fields, duplicates, and unmapped required fields.
    /// </summary>
    protected virtual void ValidateMappingEntries(
        IList<FieldMappingInputPayload> mappings,
        IList<DataSetFieldPayload> fields,
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
}

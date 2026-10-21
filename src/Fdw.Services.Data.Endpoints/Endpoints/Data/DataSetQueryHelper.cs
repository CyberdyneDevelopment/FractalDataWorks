using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Calculations.Aggregations;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.DataSets.Results;
using Fdw.Results;
using Microsoft.Extensions.Logging;
using Fdw.Services.Data.Clients.Models;
// Alias, not a namespace import: this is the only Fdw.Data.DataSets type this file needs, and that
// namespace also holds the pipeline JoinConfiguration, which has nothing to do with dataset joins.
using DataSetFieldMappingConfiguration = Fdw.Data.DataSets.DataSetFieldMappingConfiguration;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Internal helper for building common QueryCommand filters and DTO mapping functions
/// used by DataSet endpoint base classes.
/// </summary>
internal static class DataSetQueryHelper
{
    private static readonly IFilterOperator EqualOperator = FilterOperators.ByName("Equal")
        ?? throw new InvalidOperationException("FilterOperators.Equal not found");

    /// <summary>Builds filter for active rows (IsCurrent=true AND IsDeleted=false).</summary>
    internal static FilterExpression ActiveFilterFor(string propertyName, object value) => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsDeleted), Operator = EqualOperator, Value = false },
                new FilterCondition { PropertyName = propertyName, Operator = EqualOperator, Value = value }
            ]
        }
    };

    internal static FilterExpression ActiveFilter() => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsDeleted), Operator = EqualOperator, Value = false }
            ]
        }
    };

    /// <summary>Builds filter for a specific dataset by name + active.</summary>
    internal static FilterExpression ByNameFilter(string name) => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.Name), Operator = EqualOperator, Value = name },
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(IDataSetImplementationConfiguration.IsDeleted), Operator = EqualOperator, Value = false }
            ]
        }
    };

    /// <summary>Builds filter for child records by parent FK + active.</summary>
    internal static FilterExpression ByParentIdFilter(string fkPropertyName, Guid domainConfigurationId) => new()
    {
        Root = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes =
            [
                new FilterCondition { PropertyName = fkPropertyName, Operator = EqualOperator, Value = domainConfigurationId },
                new FilterCondition { PropertyName = "IsCurrent", Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = "IsDeleted", Operator = EqualOperator, Value = false }
            ]
        }
    };

    internal static DataSetDetailResponse MapToDetail(IDataSetImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        Version = config.Version,
        Implementation = config.Implementation,
        FederationStrategy = config.FederationStrategyOrNone(),
        SourceDataSetName = config.SourceDataSetName,
        RecordTypeName = config.RecordTypeName,
        Fields = config.Fields
            .OrderBy(f => f.Ordinal)
            .Select(f => MapToFieldDto(config.Id, f))
            .ToList(),
        Sources = config.Sources
            .OrderBy(s => s.Priority)
            .Select(MapToSourceDto)
            .ToList(),
        Aggregates = config.AggregatesOrNone()
            .OrderBy(a => a.Ordinal)
            .Select(MapToAggregateDto)
            .ToList(),
        SurrogateKeyFields = config.KeyFields
            .Where(k => string.Equals(k.KeyType, "Surrogate", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.Ordinal)
            .Select(k => k.KeyName)
            .ToList(),
        NaturalKeyFields = config.KeyFields
            .Where(k => string.Equals(k.KeyType, "Natural", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k.Ordinal)
            .Select(k => k.KeyName)
            .ToList(),
        Filters = config.Filters
            .Select(f => new DataSetFilterConditionPayload
            {
                FieldName = f.FieldName,
                Operator = f.Operator,
                Value = f.Value,
                DataType = f.DataType
            })
            .ToList(),
        Joins = config.JoinsOrNone()
            .OrderBy(j => j.Ordinal)
            .Select(j => new DataSetJoinPayload
            {
                LeftSource = j.LeftSourceName,
                LeftField = j.LeftFieldName,
                RightSource = j.RightSourceName,
                RightField = j.RightFieldName,
                JoinType = j.JoinType
            })
            .ToList(),
        Caching = config.Caching is null
            ? null
            : new DataSetCachingPayload
            {
                Enabled = config.Caching.Enabled,
                DurationMinutes = config.Caching.DurationMinutes,
                KeyPattern = config.Caching.KeyPattern
            },
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
        CreatedBy = config.CreateBy,
        ModifiedBy = config.ModifyBy,
        CreatedOnBehalfOf = config.CreateOnBehalfOf,
        ModifiedOnBehalfOf = config.ModifyOnBehalfOf
    };

    internal static DataSetSummaryResponse MapToSummary(IDataSetImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        Version = config.Version,
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
        CreatedBy = config.CreateBy,
        ModifiedBy = config.ModifyBy,
        CreatedOnBehalfOf = config.CreateOnBehalfOf,
        ModifiedOnBehalfOf = config.ModifyOnBehalfOf
    };

    // The field row carries no data set id: it hangs from its data set by RowId. The caller names the
    // data set it read the field from, and the data set's Id is the domain Id its implementation carries.
    internal static DataSetFieldPayload MapToFieldDto(Guid dataSetId, DataSetFieldConfiguration field) => new()
    {
        Id = field.Id,
        DataSetId = dataSetId,
        Name = field.Name,
        DataType = field.TypeName,
        TypeName = field.TypeName,
        IsNullable = field.IsNullable,
        IsRequired = field.IsRequired,
        IsKey = field.IsKey,
        IsIndexed = field.IsIndexed,
        IsFilterable = field.IsFilterable,
        FilterOperators = [.. DataSetFieldFilters.Operators(field)],
        MaxLength = field.MaxLength,
        DefaultValue = field.DefaultValue,
        Ordinal = field.Ordinal,
        Description = field.Description,
        ClassificationId = field.ClassificationId,
        DatumTypeId = field.DatumTypeId,
        Role = field.Role,
        IsJoinKey = field.IsJoinKey,
        CalculationName = field.CalculationName,
        IsCalculated = field.IsCalculated
    };

    internal static DataSetSourcePayload MapToSourceDto(DataSetSourceConfiguration source) => new()
    {
        Id = source.Id,
        SourceName = source.SourceName,
        DataStoreName = source.DataStoreName,
        ConnectionName = source.ConnectionName,
        ConnectionType = source.ConnectionType,
        PathValue = source.PathValue,
        ContainerName = source.ContainerName,
        ContainerId = source.ContainerId,
        Priority = source.Priority,
        SourceKind = source.SourceKind,
        SourceDataSetId = source.SourceDataSetId,
        SourceDataSetName = source.SourceDataSetName,
        IsPrimary = source.IsPrimary,
        IsActive = source.IsCurrent && !source.IsDeleted,
        // Why the Id goes out: attaching a transformation addresses the mapping by it
        // (/field-mappings/{id}/transformations), so a client that cannot read it cannot attach one.
        FieldMappings = source.Mappings
            .Where(m => m.IsCurrent && !m.IsDeleted)
            .OrderBy(m => m.Ordinal)
            .Select(m => new DataSetFieldMappingPayload
            {
                Id = m.Id,
                DataSetFieldName = m.LogicalFieldName,
                SourceName = source.SourceName,
                SourceFieldName = m.PhysicalFieldName,
                Ordinal = m.Ordinal
            })
            .ToList()
    };

    internal static DataSetAggregateDto MapToAggregateDto(DataSetAggregateConfiguration aggregate) => new()
    {
        Id = aggregate.Id,
        AggregateColumnName = aggregate.AggregateColumnName,
        GroupByFieldNames = aggregate.GroupByFieldNames,
        AggregateFunctionName = aggregate.AggregateFunctionName,
        InputFieldName = aggregate.InputFieldName,
        DisplayName = aggregate.DisplayName,
        Description = aggregate.Description,
        Ordinal = aggregate.Ordinal
    };

    // ========================================================================
    // Write-side mapping (request DTO -> child configuration), shared by
    // CreateDataSetEndpointBase and UpdateDataSetEndpointBase so both operations
    // compose the same cascade child collections the same way (pattern symmetry).
    // ========================================================================

    /// <summary>Checks each field's datum type and data type, when the request carries fields.</summary>
    /// <param name="fields">The fields the request carries, or <see langword="null"/> when it carries none.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or the first refusal: DatumTypeUnknown, then DataTypeUnknown.</returns>
    /// <remarks>One check for create and update, so both refuse the same field the same way.</remarks>
    internal static IGenericResult ValidateFieldTypes(IList<CreateDataSetFieldRequest>? fields, ILogger logger)
    {
        if (fields is null) return GenericResult.Success();

        var datumTyped = DatumTypeValidator.Validate(fields.Select(f => f.DatumTypeId), logger);
        return datumTyped.IsFailure ? datumTyped : ValidateFieldDataTypes(fields, logger);
    }

    /// <summary>Checks the data type each field states against the <c>DataTypes</c> options (FDW-1159).</summary>
    /// <param name="fields">The fields the request carries.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or DataTypeUnknown naming the first refused name and the options.</returns>
    /// <remarks>
    /// A field's data type is the option's name: <c>Int32</c>, never <c>int</c> or <c>System.Int32</c>. A field
    /// that states none is a field nobody has typed yet, which a sketched data set is allowed to hold, so only a
    /// stated name is checked.
    /// </remarks>
    internal static IGenericResult ValidateFieldDataTypes(IEnumerable<CreateDataSetFieldRequest> fields, ILogger logger) =>
        DataTypeValidator.Validate(fields.Select(f => f.DataType).Where(dataType => !string.IsNullOrEmpty(dataType)), logger);

    /// <summary>Maps composed field requests onto the cascade child collection.</summary>
    internal static List<DataSetFieldConfiguration> MapFields(IList<CreateDataSetFieldRequest> fields) =>
        fields.Select(f => new DataSetFieldConfiguration
        {
            Name = f.Name,
            Description = f.Description,
            ClassificationId = f.ClassificationId,
            DatumTypeId = f.DatumTypeId,
            TypeName = f.DataType,
            Role = f.Role,
            IsKey = f.IsKey,
            IsRequired = f.IsRequired,
            IsIndexed = f.IsIndexed,
            IsFilterable = f.IsFilterable ?? throw new InvalidOperationException($"field '{f.Name}' did not state isFilterable; the create validator refuses that before this runs"),
            MaxLength = f.MaxLength,
            DefaultValue = f.DefaultValue,
            IsJoinKey = f.IsJoinKey,
            CalculationName = f.CalculationName,
            Ordinal = f.Ordinal
        }).ToList();

    /// <summary>Maps composed source requests onto the cascade child collection.</summary>
    /// <remarks>
    /// Why: ContainerId/SourceKind/SourceDataSetId/IsPrimary are mapped here — dropping them (as the
    /// prior implementation did) silently discarded provenance for dataset-of-dataset sources and the
    /// Compound primary-source marker. SourceDataSetName is intentionally NOT taken from the request:
    /// denormalizing it from the resolved source dataset is a separate mechanism (not yet built); no
    /// fallback value is fabricated here.
    /// </remarks>
    /// <param name="sources">The composed source requests.</param>
    /// <param name="existing">
    /// The sources already persisted under this dataset, so a source that is being edited keeps the
    /// identity it already has. Empty on create.
    /// </param>
    internal static List<DataSetSourceConfiguration> MapSources(
        IList<CreateDataSetSourceRequest> sources,
        IEnumerable<DataSetSourceConfiguration> existing)
    {
        var prior = existing
            .Where(e => !string.IsNullOrEmpty(e.SourceName))
            .GroupBy(e => e.SourceName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        return sources.Select(s => new DataSetSourceConfiguration
        {
            // A source the dataset already has keeps its Id; a genuinely new one is left unset and the
            // save cascade mints it, which is the one place a configuration id is created.
            Id = prior.TryGetValue(s.SourceName, out var priorSource) ? priorSource.Id : default,
            Name = s.SourceName,
            SourceName = s.SourceName,
            DataStoreName = s.DataStoreName,
            ConnectionName = s.ConnectionName ?? string.Empty,
            ConnectionType = s.ConnectionType ?? string.Empty,
            PathValue = s.PathValue,
            ContainerName = s.ContainerName,
            ContainerId = s.ContainerId,
            SupportsPredicatePushdown = s.SupportsPredicatePushdown,
            Priority = s.Priority,
            MapperTypeName = s.MapperTypeName,
            SourceKind = s.SourceKind,
            SourceDataSetId = s.SourceDataSetId,
            IsPrimary = s.IsPrimary,
            HttpEndpoint = s.HttpEndpoint,
            HttpMethod = s.HttpMethod,
            FilePath = s.FilePath,
            FileFormat = s.FileFormat,
            Mappings = MapFieldMappings(s, prior.TryGetValue(s.SourceName, out var stored) ? stored : null)
        }).ToList();
    }

    /// <summary>Maps one source request's field mappings onto the source's stored mapping list.</summary>
    /// <remarks>
    /// A null list on the request leaves the stored mappings as they are, and a source with nothing
    /// stored has none to leave. A list replaces them, each keeping the Id the stored source gives its
    /// field (<see cref="DataSetSourceConfiguration.MappingIdFor"/>), so the transformations attached to
    /// it stay attached. The mapping's kind is its source's kind: the request's mapping shape does not
    /// carry one. Callers must run <see cref="ValidateFieldMappings"/> first — this method never validates.
    /// </remarks>
    private static List<DataSetFieldMappingConfiguration> MapFieldMappings(
        CreateDataSetSourceRequest source, DataSetSourceConfiguration? stored)
    {
        if (source.FieldMappings is null)
            return stored is null ? [] : stored.Mappings;

        return source.FieldMappings.Select(m => new DataSetFieldMappingConfiguration
        {
            // A source that is new has no mapping identities yet; each is left for the cascade to mint.
            Id = stored is null ? default : stored.MappingIdFor(m.DataSetFieldName),
            Name = m.DataSetFieldName,
            LogicalFieldName = m.DataSetFieldName,
            SourceKind = source.SourceKind,
            // An empty source field on the wire is the absence of one; the column spells that NULL.
            PhysicalFieldName = string.IsNullOrEmpty(m.SourceFieldName) ? null : m.SourceFieldName,
            Ordinal = m.Ordinal
        }).ToList();
    }

    /// <summary>Maps join requests onto the cascade child collection saved to <c>data.DataSetJoin</c>.</summary>
    /// <remarks>A join's position in the request is its <c>Ordinal</c>: joins run in the order they were authored.</remarks>
    internal static List<DataSetJoinConfiguration> MapJoins(IList<DataSetJoinPayload> joins) =>
        joins.Select((j, i) => new DataSetJoinConfiguration
        {
            LeftSourceName = j.LeftSource,
            LeftFieldName = j.LeftField,
            RightSourceName = j.RightSource,
            RightFieldName = j.RightField,
            JoinType = j.JoinType,
            Ordinal = i
        }).ToList();

    /// <summary>Maps the caching request onto the (currently non-persisted) caching configuration.</summary>
    internal static CachingConfiguration? MapCaching(DataSetCachingPayload? caching) =>
        caching is null
            ? null
            : new CachingConfiguration
            {
                Enabled = caching.Enabled,
                DurationMinutes = caching.DurationMinutes,
                KeyPattern = caching.KeyPattern ?? new CachingConfiguration().KeyPattern
            };

    /// <summary>Maps composed aggregate requests onto the cascade child collection.</summary>
    /// <remarks>Callers must run <see cref="ValidateAggregates"/> first — this method never validates.</remarks>
    internal static List<DataSetAggregateConfiguration> MapAggregates(IList<CreateDataSetAggregateRequest> aggregates) =>
        aggregates.Select(a => new DataSetAggregateConfiguration
        {
            AggregateColumnName = a.AggregateColumnName,
            GroupByFieldNames = a.GroupByFieldNames,
            AggregateFunctionName = a.AggregateFunctionName,
            InputFieldName = a.InputFieldName,
            DisplayName = a.DisplayName,
            Description = a.Description,
            Ordinal = a.Ordinal
        }).ToList();

    // ========================================================================
    // Fail-loud validation shared by create + update (NO FALLBACKS: a missing or
    // unregistered strategy/function fails the request rather than persisting a
    // dataset that would fail loudly later, at execution time, instead).
    // ========================================================================

    /// <summary>
    /// Validates that a Federated dataset carries a registered <c>FederationStrategies</c> member and
    /// that a non-Federated dataset does not carry one.
    /// </summary>
    internal static IGenericResult<bool> ValidateFederationStrategy(
        string? implementation, string? federationStrategy, string dataSetName, ILogger logger)
    {
        var isFederated = string.Equals(implementation, "Federated", StringComparison.OrdinalIgnoreCase);

        if (isFederated)
        {
            if (string.IsNullOrWhiteSpace(federationStrategy))
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.FederationStrategyRequired, logger,
                    ResultDetails.Create("name", dataSetName));
            }

            if (ReferenceEquals(FederationStrategies.ByName(federationStrategy), FederationStrategies.NotFound))
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.FederationStrategyInvalid, logger,
                    ResultDetails.Create("name", dataSetName, "federationStrategy", federationStrategy));
            }
        }
        else if (!string.IsNullOrWhiteSpace(federationStrategy))
        {
            return GenericResult<bool>.Failure(
                DataSetsResultCodes.FederationStrategyNotAllowed, logger,
                ResultDetails.Create("name", dataSetName, "implementation", implementation ?? string.Empty));
        }

        return GenericResult<bool>.Success(true);
    }

    /// <summary>
    /// Validates that a data set carries only what its kind has: joins on a Compound or Federated one, aggregates
    /// on a Simple one (FDW-1031).
    /// </summary>
    internal static IGenericResult<bool> ValidateKindParts(
        string implementation, bool hasJoins, bool hasAggregates, string dataSetName, ILogger logger)
    {
        if (hasJoins && string.Equals(implementation, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
        {
            return GenericResult<bool>.Failure(
                DataSetsResultCodes.JoinsNotAllowed, logger,
                ResultDetails.Create("name", dataSetName, "implementation", implementation));
        }

        if (hasAggregates && !string.Equals(implementation, SimpleDataSetConfiguration.ImplementationName, StringComparison.Ordinal))
        {
            return GenericResult<bool>.Failure(
                DataSetsResultCodes.AggregatesNotAllowed, logger,
                ResultDetails.Create("name", dataSetName, "implementation", implementation));
        }

        return GenericResult<bool>.Success(true);
    }

    /// <summary>
    /// Validates each aggregate measure request: column/input field names required, aggregate function
    /// resolves against <c>AggregationFunctions</c>, and groupByFieldNames splits into non-empty elements.
    /// </summary>
    internal static IGenericResult<bool> ValidateAggregates(
        IList<CreateDataSetAggregateRequest> aggregates, string dataSetName, ILogger logger)
    {
        foreach (var aggregate in aggregates)
        {
            if (string.IsNullOrWhiteSpace(aggregate.AggregateColumnName) || string.IsNullOrWhiteSpace(aggregate.InputFieldName))
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.AggregateColumnNameRequired, logger,
                    ResultDetails.Create("name", dataSetName));
            }

            if (ReferenceEquals(AggregationFunctions.ByName(aggregate.AggregateFunctionName), AggregationFunctions.NotFound))
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.AggregateFunctionInvalid, logger,
                    ResultDetails.Create("name", dataSetName, "aggregateFunctionName", aggregate.AggregateFunctionName));
            }

            if (string.IsNullOrWhiteSpace(aggregate.GroupByFieldNames)
                || aggregate.GroupByFieldNames.Split(',').Any(g => string.IsNullOrWhiteSpace(g)))
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.AggregateGroupByEmpty, logger,
                    ResultDetails.Create("name", dataSetName, "aggregateColumnName", aggregate.AggregateColumnName));
            }
        }

        return GenericResult<bool>.Success(true);
    }

    /// <summary>
    /// Validates each source's replacement field mappings by the one rule every replace path uses
    /// (<see cref="DataSetSourceConfiguration.ValidateMappingFields"/>). A source whose request carries
    /// no mappings keeps its stored ones and has nothing to validate.
    /// </summary>
    internal static IGenericResult<bool> ValidateFieldMappings(
        IList<CreateDataSetSourceRequest> sources, string dataSetName, ILogger logger)
    {
        foreach (var source in sources)
        {
            if (source.FieldMappings is null) continue;

            var validation = DataSetSourceConfiguration.ValidateMappingFields(
                source.FieldMappings.Select(m => m.DataSetFieldName), dataSetName, source.SourceName, logger);
            if (validation.IsFailure) return validation;
        }

        return GenericResult<bool>.Success(true);
    }
}

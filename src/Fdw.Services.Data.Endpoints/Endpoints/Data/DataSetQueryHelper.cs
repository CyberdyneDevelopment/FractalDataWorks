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
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsDeleted), Operator = EqualOperator, Value = false },
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
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsDeleted), Operator = EqualOperator, Value = false }
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
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.Name), Operator = EqualOperator, Value = name },
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsCurrent), Operator = EqualOperator, Value = true },
                new FilterCondition { PropertyName = nameof(DataSetConfiguration.IsDeleted), Operator = EqualOperator, Value = false }
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

    internal static DataSetDetailResponse MapToDetail(DataSetConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        Category = config.Category,
        Version = config.Version,
        ServiceOptionType = config.ServiceOptionType,
        FederationStrategy = config.FederationStrategy,
        TransformExpression = config.TransformExpression,
        SourceDataSetName = config.SourceDataSetName,
        RecordTypeName = config.RecordTypeName,
        Fields = config.Fields
            .OrderBy(f => f.Ordinal)
            .Select(MapToFieldDto)
            .ToList(),
        Sources = config.Sources
            .OrderBy(s => s.Priority)
            .Select(MapToSourceDto)
            .ToList(),
        Aggregates = config.Aggregates
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
        Joins = config.Joins
            .Select(j => new DataSetJoinPayload
            {
                LeftSource = j.LeftSource,
                LeftField = j.LeftField,
                RightSource = j.RightSource,
                RightField = j.RightField,
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

    internal static DataSetSummaryResponse MapToSummary(DataSetConfiguration config, int sourceCount) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        Category = config.Category,
        Version = config.Version,
        FieldCount = config.Fields.Count,
        SourceCount = sourceCount,
        CreatedAt = config.CreateDate,
        ModifiedAt = config.ModifyDate,
        CreatedBy = config.CreateBy,
        ModifiedBy = config.ModifyBy,
        CreatedOnBehalfOf = config.CreateOnBehalfOf,
        ModifiedOnBehalfOf = config.ModifyOnBehalfOf
    };

    internal static DataSetFieldPayload MapToFieldDto(DataFieldConfiguration field) => new()
    {
        Name = field.Name,
        DataType = field.TypeName,
        IsNullable = !field.IsRequired,
        IsKey = field.IsKey,
        Ordinal = field.Ordinal,
        Description = field.Description,
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
        IsActive = source.IsCurrent && !source.IsDeleted
    };

    internal static DataSetAggregateDto MapToAggregateDto(DataSetAggregateDefinition aggregate) => new()
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

    /// <summary>Maps composed field requests onto the cascade child collection.</summary>
    internal static List<DataFieldConfiguration> MapFields(IList<CreateDataSetFieldRequest> fields) =>
        fields.Select(f => new DataFieldConfiguration
        {
            Name = f.Name,
            Description = f.Description,
            TypeName = f.DataType,
            Role = f.Role,
            IsKey = f.IsKey,
            IsRequired = f.IsRequired,
            IsIndexed = f.IsIndexed,
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
        var priorIds = existing
            .Where(e => !string.IsNullOrEmpty(e.SourceName))
            .GroupBy(e => e.SourceName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.Ordinal);

        return sources.Select(s => new DataSetSourceConfiguration
        {
            // A source the dataset already has keeps its Id; a genuinely new one is left unset and the
            // save cascade mints it, which is the one place a configuration id is created.
            Id = priorIds.TryGetValue(s.SourceName, out var priorId) ? priorId : default,
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
            FileFormat = s.FileFormat
        }).ToList();
    }

    /// <summary>Maps join requests onto the (currently non-persisted) join configuration list.</summary>
    internal static List<JoinConfiguration> MapJoins(IList<DataSetJoinPayload> joins) =>
        joins.Select(j => new JoinConfiguration
        {
            LeftSource = j.LeftSource,
            LeftField = j.LeftField,
            RightSource = j.RightSource,
            RightField = j.RightField,
            JoinType = j.JoinType
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
    internal static List<DataSetAggregateDefinition> MapAggregates(IList<CreateDataSetAggregateRequest> aggregates) =>
        aggregates.Select(a => new DataSetAggregateDefinition
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
        string? serviceOptionType, string? federationStrategy, string dataSetName, ILogger logger)
    {
        var isFederated = string.Equals(serviceOptionType, "Federated", StringComparison.OrdinalIgnoreCase);

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
                ResultDetails.Create("name", dataSetName, "serviceOptionType", serviceOptionType ?? string.Empty));
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
}

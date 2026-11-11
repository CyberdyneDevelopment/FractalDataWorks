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

    /// <summary>Maps what a name resolved to: the data set's detail, under the name and id of the variation when the name is one (FDW-2390).</summary>
    /// <param name="resolved">What the name names.</param>
    /// <returns>The detail, marked with its kind and the name of what it varies directly.</returns>
    internal static DataSetDetailResponse MapToDetail(ResolvedDataSet resolved)
    {
        var detail = MapToDetail(resolved.DataSet);
        detail.Id = resolved.Id;
        detail.Name = resolved.Name;
        detail.Kind = resolved.Kind;
        detail.ParentName = resolved.ParentName;
        if (resolved.Variation is { } view)
        {
            detail.DisplayName = view.DisplayName;
            detail.Description = view.Description;
            detail.ClassificationId = view.ClassificationId;
            // The detail is the base data set's, under the view's name: the owner it shows is the view's.
            detail.OwnerUserId = view.OwnerUserId;
            Describe(detail, view);
        }

        return detail;
    }

    // What only a variation has: the kind, what it varies, who owns it and its stored filter. A data set of another kind has none of it.
    private static void Describe(DataSetDetailResponse detail, IDataSetVariationImplementationConfiguration variation)
    {
        detail.VariationKind = variation.VariationKind;
        detail.BaseDataSetId = variation.BaseDataSetId;
        detail.ParentVariationId = variation.ParentVariationId;
        detail.OwnerWorkspaceId = variation.OwnerWorkspaceId;
        detail.VariationFilters = variation.StoredFilters;
    }

    private static void Describe(DataSetSummaryResponse summary, IDataSetVariationImplementationConfiguration variation)
    {
        summary.VariationKind = variation.VariationKind;
        summary.BaseDataSetId = variation.BaseDataSetId;
        summary.ParentVariationId = variation.ParentVariationId;
        summary.OwnerWorkspaceId = variation.OwnerWorkspaceId;
        summary.VariationFilters = variation.StoredFilters;
    }

    internal static DataSetDetailResponse MapToDetail(IDataSetImplementationConfiguration config)
    {
        var detail = DetailOf(config);
        if (config is IDataSetVariationImplementationConfiguration variation)
            Describe(detail, variation);

        return detail;
    }

    private static DataSetDetailResponse DetailOf(IDataSetImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        OwnerUserId = config.OwnerUserId,
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
            .Where(f => f.IsCurrent && !f.IsDeleted)
            .OrderBy(f => f.Ordinal)
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

    internal static DataSetSummaryResponse MapToSummary(IDataSetImplementationConfiguration config)
    {
        var summary = SummaryOf(config);
        if (config is IDataSetVariationImplementationConfiguration variation)
            Describe(summary, variation);

        return summary;
    }

    private static DataSetSummaryResponse SummaryOf(IDataSetImplementationConfiguration config) => new()
    {
        Id = config.Id,
        Implementation = config.Implementation,
        Name = config.Name,
        DisplayName = config.DisplayName,
        Abbreviation = config.Abbreviation,
        Description = config.Description,
        ClassificationId = config.ClassificationId,
        OwnerUserId = config.OwnerUserId,
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
    /// <param name="fields">The composed field requests.</param>
    /// <param name="existing">The fields already stored under this data set, so one the request does not say the nullability of keeps its own. Empty on create.</param>
    /// <param name="values">Refuses a new field that does not state its nullability (FDW-3931).</param>
    /// <returns>The fields, or <c>ValueRequired</c> naming a new field that does not state its nullability.</returns>
    internal static IGenericResult<List<DataSetFieldConfiguration>> MapFields(
        IList<CreateDataSetFieldRequest> fields, IEnumerable<DataSetFieldConfiguration> existing, DataSetRequestValues values)
    {
        var stored = existing
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .GroupBy(e => e.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var mapped = new List<DataSetFieldConfiguration>(fields.Count);
        foreach (var field in fields)
        {
            var nullable = NullabilityOf(field, stored, values);
            if (nullable.IsFailure) return nullable.ToNewResult<List<DataSetFieldConfiguration>>();
            mapped.Add(MapField(field, nullable.Value));
        }

        return GenericResult<List<DataSetFieldConfiguration>>.Success(mapped);
    }

    // A field the request does not say the nullability of keeps the one it has; a new one must say it (FDW-3931).
    private static IGenericResult<bool> NullabilityOf(
        CreateDataSetFieldRequest field, Dictionary<string, DataSetFieldConfiguration> stored, DataSetRequestValues values) =>
        field.IsNullable is null && stored.TryGetValue(field.Name, out var kept)
            ? GenericResult<bool>.Success(kept.IsNullable)
            : values.Required(field.IsNullable, "field", field.Name, "isNullable");

    private static DataSetFieldConfiguration MapField(CreateDataSetFieldRequest f, bool isNullable) =>
        new()
        {
            Name = f.Name,
            IsNullable = isNullable,
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
        };

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
    /// identity it already has, and the kind, priority and pushdown the request does not state. Empty on create.
    /// </param>
    /// <param name="values">Refuses a new source that does not state its kind, priority or pushdown (FDW-3931).</param>
    /// <returns>The sources, or <c>ValueRequired</c> naming a new source's missing value.</returns>
    internal static IGenericResult<List<DataSetSourceConfiguration>> MapSources(
        IList<CreateDataSetSourceRequest> sources,
        IEnumerable<DataSetSourceConfiguration> existing,
        DataSetRequestValues values)
    {
        var prior = existing
            .Where(e => !string.IsNullOrEmpty(e.SourceName))
            .GroupBy(e => e.SourceName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var mapped = new List<DataSetSourceConfiguration>(sources.Count);
        foreach (var source in sources)
        {
            var built = MapSource(source, prior.TryGetValue(source.SourceName, out var stored) ? stored : null, values);
            if (built.IsFailure) return built.ToNewResult<List<DataSetSourceConfiguration>>();
            mapped.Add(built.Value!);
        }

        return GenericResult<List<DataSetSourceConfiguration>>.Success(mapped);
    }

    // A source the data set already has keeps the kind, priority and pushdown the request does not state; a new one must state them (FDW-3931).
    private static IGenericResult<DataSetSourceConfiguration> MapSource(
        CreateDataSetSourceRequest s, DataSetSourceConfiguration? stored, DataSetRequestValues values)
    {
        var kind = s.SourceKind is null && stored is not null ? GenericResult<string>.Success(stored.SourceKind) : values.RequiredText(s.SourceKind, "source", s.SourceName, "sourceKind");
        if (kind.IsFailure) return kind.ToNewResult<DataSetSourceConfiguration>();

        var priority = s.Priority is null && stored is not null ? GenericResult<int>.Success(stored.Priority) : values.Required(s.Priority, "source", s.SourceName, "priority");
        if (priority.IsFailure) return priority.ToNewResult<DataSetSourceConfiguration>();

        var pushdown = s.SupportsPredicatePushdown is null && stored is not null
            ? GenericResult<bool>.Success(stored.SupportsPredicatePushdown)
            : values.Required(s.SupportsPredicatePushdown, "source", s.SourceName, "supportsPredicatePushdown");
        if (pushdown.IsFailure) return pushdown.ToNewResult<DataSetSourceConfiguration>();

        return GenericResult<DataSetSourceConfiguration>.Success(new DataSetSourceConfiguration
        {
            // A source the dataset already has keeps its Id; a genuinely new one is left unset and the
            // save cascade mints it, which is the one place a configuration id is created.
            Id = stored is null ? default : stored.Id,
            Name = s.SourceName,
            SourceName = s.SourceName,
            DataStoreName = s.DataStoreName,
            ConnectionName = s.ConnectionName ?? string.Empty,
            ConnectionType = s.ConnectionType ?? string.Empty,
            PathValue = s.PathValue,
            ContainerName = s.ContainerName,
            ContainerId = s.ContainerId,
            SupportsPredicatePushdown = pushdown.Value,
            Priority = priority.Value,
            MapperTypeName = s.MapperTypeName,
            SourceKind = kind.Value!,
            SourceDataSetId = s.SourceDataSetId,
            IsPrimary = s.IsPrimary,
            HttpEndpoint = s.HttpEndpoint,
            HttpMethod = s.HttpMethod,
            FilePath = s.FilePath,
            FileFormat = s.FileFormat,
            Mappings = MapFieldMappings(s, kind.Value!, stored)
        });
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
        CreateDataSetSourceRequest source, string sourceKind, DataSetSourceConfiguration? stored)
    {
        if (source.FieldMappings is null)
            return stored is null ? [] : stored.Mappings;

        return source.FieldMappings.Select(m => new DataSetFieldMappingConfiguration
        {
            // A source that is new has no mapping identities yet; each is left for the cascade to mint.
            Id = stored is null ? default : stored.MappingIdFor(m.DataSetFieldName),
            Name = m.DataSetFieldName,
            LogicalFieldName = m.DataSetFieldName,
            SourceKind = sourceKind,
            // An empty source field on the wire is the absence of one; the column spells that NULL.
            PhysicalFieldName = string.IsNullOrEmpty(m.SourceFieldName) ? null : m.SourceFieldName,
            Ordinal = m.Ordinal
        }).ToList();
    }

    /// <summary>Maps join requests onto the cascade child collection saved to <c>data.DataSetJoin</c>.</summary>
    /// <remarks>A join's position in the request is its <c>Ordinal</c>: joins run in the order they were authored.</remarks>
    /// <summary>Maps a request's filter conditions onto the cascade child collection saved to <c>data.DataSetFilter</c> (FDW-4997).</summary>
    /// <remarks>Mapped as stated, in the order the request lists them; <see cref="ValidateSaved"/> checks them before anything is written.</remarks>
    internal static List<DataSetFilterConditionConfiguration> MapFilters(IEnumerable<DataSetFilterConditionPayload> filters) =>
        [.. filters.Select((f, i) => new DataSetFilterConditionConfiguration
        {
            FieldName = f.FieldName,
            Operator = f.Operator,
            Value = f.Value,
            DataType = f.DataType,
            Ordinal = i
        })];

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

    /// <summary>Checks that each filter states its operator and data type: a choice its caller makes, never assumed (FDW-3931).</summary>
    /// <param name="filters">The filters the request carries.</param>
    /// <param name="name">The data set's name, for the message.</param>
    /// <param name="logger">The logger the refusal is reported through.</param>
    /// <returns>Success, or <c>ValueRequired</c> naming the first filter that leaves one out.</returns>
    internal static IGenericResult<bool> ValidateFilters(IEnumerable<DataSetFilterConditionPayload> filters, string name, ILogger logger)
    {
        foreach (var filter in filters)
        {
            var missing = string.IsNullOrWhiteSpace(filter.Operator) ? "operator" : string.IsNullOrWhiteSpace(filter.DataType) ? "dataType" : null;
            if (missing is not null)
            {
                return GenericResult<bool>.Failure(
                    DataSetsResultCodes.ValueRequired, logger,
                    ResultDetails.Create("name", name, "part", "filter", "partName", filter.FieldName).With("field", missing));
            }
        }

        return GenericResult<bool>.Success(true);
    }

    /// <summary>
    /// Maps the caching request onto the (currently non-persisted, FDW-3943) caching configuration. A value the block leaves out is the one
    /// the data set kind declares, or a host's override of it (FDW-3956).
    /// </summary>
    /// <param name="caching">The caching block the request sends; null for none.</param>
    /// <param name="implementation">The data set's kind, which declares the caching defaults.</param>
    /// <param name="values">Resolves a value the block leaves out.</param>
    /// <returns>The caching configuration, null when the request sends none, or the refusal naming a value the kind declares none of.</returns>
    internal static IGenericResult<CachingConfiguration?> MapCaching(
        DataSetCachingPayload? caching, string implementation, DataSetRequestValues values)
    {
        if (caching is null) return GenericResult<CachingConfiguration?>.Success(null);

        var enabled = values.Declared(caching.Enabled, implementation, DataSetDeclaredDefaults.CachingEnabled, "caching.enabled");
        if (enabled.IsFailure) return enabled.ToNewResult<CachingConfiguration?>();

        var duration = values.Declared(caching.DurationMinutes, implementation, DataSetDeclaredDefaults.CachingDurationMinutes, "caching.durationMinutes");
        if (duration.IsFailure) return duration.ToNewResult<CachingConfiguration?>();

        var keyPattern = values.DeclaredText(caching.KeyPattern, implementation, DataSetDeclaredDefaults.CachingKeyPattern, "caching.keyPattern");
        if (keyPattern.IsFailure) return keyPattern.ToNewResult<CachingConfiguration?>();

        return GenericResult<CachingConfiguration?>.Success(new CachingConfiguration
        {
            Enabled = enabled.Value,
            DurationMinutes = duration.Value,
            KeyPattern = keyPattern.Value
        });
    }

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
    /// The rules a data set's own parts keep before it is saved, run over the whole configuration as it will be stored: its filter conditions
    /// can be kept (<see cref="DataSetFilters"/>, FDW-4997), every join names two of its sources (<see cref="JoinedDataSetRules"/>), and a
    /// Compound one's mappings name every column it reads (<see cref="CompoundMappingRules"/>, FDW-5022). Each join states a type that
    /// joins (FDW-5087, FDW-5099): a join with no type, an unknown one or None is refused, never stored as Inner. The same rules run again
    /// when the data set is read.
    /// </summary>
    /// <param name="config">The data set as it will be saved: built from a create request, or a stored one with an update merged over it.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or the first refusal.</returns>
    internal static IGenericResult ValidateSaved(IDataSetImplementationConfiguration config, ILogger logger)
    {
        // A filter condition the data set cannot keep is refused, never stored to be dropped at read (FDW-4997).
        var filters = DataSetFilters.Expression(config, logger);
        if (filters.IsFailure)
            return filters;

        if (config is ICompoundDataSetConfiguration compound)
            return CompoundMappingRules.Validate(compound, logger);

        return config is IJoinedDataSetConfiguration joined ? JoinedDataSetRules.Validate(joined, logger) : GenericResult.Success();
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

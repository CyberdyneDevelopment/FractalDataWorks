using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Execution;

/// <summary>
/// Analyzes filter expressions and decomposes them by source for predicate pushdown optimization.
/// </summary>
/// <remarks>
/// <para>
/// Predicate pushdown is a critical optimization that pushes filter conditions down to
/// their respective data sources instead of fetching all data and filtering in memory.
/// </para>
/// <para>
/// Example:
/// Filter: State = "Texas" AND OrderDate > 2025-01-01
/// With dataset sources:
/// - State field belongs to SQL_Primary
/// - OrderDate field belongs to REST_Orders
///
/// Result:
/// - SQL_Primary filter: StateCode = 'Texas' (logical → physical mapping)
/// - REST_Orders filter: order_date gt 2025-01-01 (logical → physical mapping)
/// </para>
/// <para>
/// Performance Impact:
/// - Without pushdown: Fetch 600K rows, filter in memory (2600ms)
/// - With pushdown: Fetch 2.5K pre-filtered rows (160ms) - 16x faster!
/// </para>
/// </remarks>
public sealed class PredicatePushdownAnalyzer
{
    private readonly ILogger<PredicatePushdownAnalyzer> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredicatePushdownAnalyzer"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public PredicatePushdownAnalyzer(ILogger<PredicatePushdownAnalyzer> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Decomposes a filter by source: the parts a source can apply in its store and still give the same rows (FDW-5049).
    /// </summary>
    /// <param name="filter">The filter expression to decompose, in the data set's logical names.</param>
    /// <param name="dataset">The dataset configuration; its joins say which sources an outer join makes optional.</param>
    /// <param name="sources">The pre-resolved source configurations for this dataset.</param>
    /// <param name="fieldMappingsBySource">Pre-resolved field mappings keyed by source name.</param>
    /// <returns>Each source's share in its physical names (no entry for a source given none), or failure information.</returns>
    /// <remarks>
    /// <para>
    /// Pushdown is only an optimisation: the caller applies its whole filter to the rows its sources join, so a part that is not pushed is
    /// still applied, never dropped. A top-level AND conjunct (the whole filter when it is not an AND) is pushed to a source when every
    /// field it names is mapped by that source and no other, the source states that its store applies filters
    /// (<see cref="DataSetSourceConfiguration.SupportsPredicatePushdown"/>), and no outer join makes that source optional: a row an outer
    /// join keeps with that source empty would be dropped by the source's own filter.
    /// </para>
    /// <para>
    /// Before FDW-5049 a condition on a field no source mapped was dropped, one on a field several sources mapped went to the first only,
    /// and a source that does not apply filters got none, while the caller applied nothing to the joined rows.
    /// </para>
    /// </remarks>
    public IGenericResult<Dictionary<string, IFilterExpression>> DecomposeBySource(
        IFilterExpression? filter,
        IDataSetImplementationConfiguration dataset,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource)
    {
        var validationResult = ValidateDecomposeInputs(filter, dataset, sources);
        if (validationResult != null)
            return validationResult;

        PredicatePushdownLog.DecomposingFilter(_logger, dataset.Name, sources.Count);

        try
        {
            var optional = OptionalSources(dataset);
            var pushed = new Dictionary<string, List<IFilterNode>>(StringComparer.OrdinalIgnoreCase);
            foreach (var conjunct in Conjuncts(filter!.Root!))
            {
                var fields = string.Join(", ", FieldNames(conjunct));
                if (PushdownOwner(conjunct, sources, fieldMappingsBySource, optional) is not { } owner)
                {
                    PredicatePushdownLog.ConditionKeptForJoinedRows(_logger, dataset.Name, fields);
                    continue;
                }

                fieldMappingsBySource.TryGetValue(owner.SourceName, out var mappings);
                if (!pushed.TryGetValue(owner.SourceName, out var nodes))
                    pushed[owner.SourceName] = nodes = [];
                nodes.Add(TranslateNode(conjunct, mappings));
                PredicatePushdownLog.ConditionRouted(_logger, fields, owner.SourceName);
            }

            var sourceFilters = pushed.ToDictionary(
                share => share.Key,
                share => (IFilterExpression)new FilterExpression
                {
                    Root = share.Value.Count == 1 ? share.Value[0] : new FilterGroup { Operator = LogicalOperator.And, Nodes = share.Value },
                },
                StringComparer.OrdinalIgnoreCase);
            PredicatePushdownLog.DecompositionComplete(_logger, sourceFilters.Count);
            return GenericResult<Dictionary<string, IFilterExpression>>.Success(sourceFilters);
        }
        catch (Exception ex)
        {
            PredicatePushdownLog.DecompositionFailed(_logger, ex.Message);
            return GenericResult<Dictionary<string, IFilterExpression>>.Failure(
                DataServiceResultCodes.ByName("FilterDecompositionFailed"),
                ResultDetails.Create("Error", ex.Message));
        }
    }

    // The parts of a filter that stand alone: each node of a top-level AND, or the whole filter.
    private static IReadOnlyList<IFilterNode> Conjuncts(IFilterNode root) =>
        root is FilterGroup group && ReferenceEquals(group.Operator, LogicalOperator.And) ? group.Nodes : [root];

    // Every field a filter node names, once each.
    private static IEnumerable<string> FieldNames(IFilterNode node) => node switch
    {
        IFilterCondition condition => [condition.PropertyName],
        FilterGroup group => group.Nodes.SelectMany(FieldNames).Distinct(StringComparer.Ordinal),
        _ => [],
    };

    // The one source a conjunct can be pushed to, or null when it must be applied to the joined rows: every field it names is mapped by
    // that source and no other, the source applies filters in its store, and no outer join makes it optional.
    private static DataSetSourceConfiguration? PushdownOwner(
        IFilterNode conjunct,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource,
        HashSet<string> optional)
    {
        DataSetSourceConfiguration? owner = null;
        foreach (var field in FieldNames(conjunct))
        {
            var owners = sources
                .Where(source => fieldMappingsBySource.TryGetValue(source.SourceName, out var mappings) && mappings.ContainsKey(field))
                .ToList();
            if (owners.Count != 1 || (owner is not null && !ReferenceEquals(owner, owners[0])))
                return null;

            owner = owners[0];
        }

        return owner is { SupportsPredicatePushdown: true } && !optional.Contains(owner.SourceName) ? owner : null;
    }

    // The sources an outer join may leave without a row: the right source of a join that keeps every left row, and every source joined
    // before a join that keeps every right row. Which side a join keeps is its join type's own statement.
    private static HashSet<string> OptionalSources(IDataSetImplementationConfiguration dataset)
    {
        var optional = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (dataset is not IJoinedDataSetConfiguration joinedSet)
            return optional;

        var joined = new List<string>();
        foreach (var join in joinedSet.Joins.OrderBy(j => j.Ordinal))
        {
            if (!joined.Contains(join.LeftSourceName, StringComparer.OrdinalIgnoreCase))
                joined.Add(join.LeftSourceName);

            var kind = StatedJoinType.Of(join.JoinType);
            if (kind.IncludesAllRight)
                optional.UnionWith(joined);
            if (kind.IncludesAllLeft)
                optional.Add(join.RightSourceName);
            joined.Add(join.RightSourceName);
        }

        return optional;
    }

    /// <summary>
    /// Validates inputs for DecomposeBySource. Returns null if valid, or a failure result.
    /// </summary>
    private IGenericResult<Dictionary<string, IFilterExpression>>? ValidateDecomposeInputs(
        IFilterExpression? filter,
        IDataSetImplementationConfiguration dataset,
        IReadOnlyList<DataSetSourceConfiguration> sources)
    {
        if (filter == null || filter.Root == null)
        {
            PredicatePushdownLog.NoFilterProvided(_logger);
            return GenericResult<Dictionary<string, IFilterExpression>>.Success([]);
        }

        if (dataset == null)
        {
            return GenericResult<Dictionary<string, IFilterExpression>>.Failure(
                DataServiceResultCodes.ByName("DataSetConfigurationNull"));
        }

        if (sources == null)
        {
            return GenericResult<Dictionary<string, IFilterExpression>>.Failure(
                DataServiceResultCodes.ByName("SourcesListNull"));
        }

        return null;
    }

    /// <summary>
    /// Translates logical field names to physical field names using source mappings.
    /// </summary>
    /// <param name="logicalFilter">Filter using logical field names.</param>
    /// <param name="fieldMappings">Dictionary mapping logical field names to physical field names.</param>
    /// <returns>A result containing the filter with physical field names, or failure information.</returns>
    /// <remarks>
    /// Example:
    /// Logical: State = "Texas"
    /// Mapping: { "State": "StateCode" }
    /// Result: StateCode = "Texas"
    /// </remarks>
    public IGenericResult<IFilterExpression> TranslateToPhysical(
        IFilterExpression logicalFilter,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        if (logicalFilter == null || logicalFilter.Root == null)
        {
            return GenericResult<IFilterExpression>.Success(new FilterExpression { Root = null });
        }

        try
        {
            var translatedRoot = TranslateNode(logicalFilter.Root, fieldMappings);
            return GenericResult<IFilterExpression>.Success(
                new FilterExpression { Root = translatedRoot });
        }
        catch (Exception ex)
        {
            PredicatePushdownLog.TranslationFailed(_logger, ex.Message);
            return GenericResult<IFilterExpression>.Failure(
                DataServiceResultCodes.ByName("FilterTranslationFailed"),
                ResultDetails.Create("Error", ex.Message));
        }
    }

    /// <summary>
    /// Translates an ordering's logical field names to physical ones, the same way
    /// <see cref="TranslateToPhysical"/> translates a filter.
    /// </summary>
    /// <param name="logicalOrdering">Ordering using logical field names, or null.</param>
    /// <param name="fieldMappings">Dictionary mapping logical field names to physical field names.</param>
    /// <returns>The ordering with physical names, or null when none was given.</returns>
    /// <remarks>
    /// A name with no mapping passes through unchanged, exactly as an unmapped filter field does, so
    /// a filter and a sort on the same field always reach the store under the same column name.
    /// </remarks>
    public static IOrderingExpression? TranslateOrderingToPhysical(
        IOrderingExpression? logicalOrdering,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        if (logicalOrdering is null || fieldMappings is null)
            return logicalOrdering;

        return new OrderingExpression
        {
            OrderedFields = [.. logicalOrdering.OrderedFields.Select(field => new OrderedField
            {
                PropertyName = fieldMappings.TryGetValue(field.PropertyName, out var physical) ? physical : field.PropertyName,
                Direction = field.Direction,
            })],
        };
    }

    /// <summary>
    /// Translates a filter node (condition or group) to use physical field names.
    /// </summary>
    private IFilterNode TranslateNode(
        IFilterNode node,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        return node switch
        {
            IFilterCondition condition => (IFilterNode)TranslateCondition(condition, fieldMappings),
            FilterGroup group => TranslateGroup(group, fieldMappings),
            _ => node
        };
    }

    /// <summary>
    /// Translates a filter condition to use physical field names.
    /// </summary>
    private static IFilterCondition TranslateCondition(
        IFilterCondition condition,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        if (fieldMappings == null)
        {
            return condition;
        }

        var physicalFieldName = fieldMappings.TryGetValue(condition.PropertyName, out var physical)
            ? physical
            : condition.PropertyName;

        return new FilterCondition
        {
            PropertyName = physicalFieldName,
            Operator = condition.Operator,
            Value = condition.Value
        };
    }

    /// <summary>
    /// Translates a filter group recursively to use physical field names.
    /// </summary>
    private FilterGroup TranslateGroup(
        FilterGroup group,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        var translatedNodes = group.Nodes
            .Select(node => TranslateNode(node, fieldMappings))
            .ToList();

        return new FilterGroup
        {
            Operator = group.Operator,
            Nodes = translatedNodes
        };
    }
}

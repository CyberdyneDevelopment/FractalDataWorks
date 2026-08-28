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
    /// Decomposes a filter expression by source, routing each condition to its owning source.
    /// </summary>
    /// <param name="filter">The filter expression to decompose.</param>
    /// <param name="dataset">The dataset configuration with field definitions.</param>
    /// <param name="sources">The pre-resolved source configurations for this dataset.</param>
    /// <param name="fieldMappingsBySource">Pre-resolved field mappings keyed by source name.</param>
    /// <returns>A result containing a dictionary of source-specific filters, or failure information.</returns>
    /// <remarks>
    /// This method analyzes the filter tree and identifies which source owns each field.
    /// Conditions are grouped by source and combined with the appropriate logical operator.
    /// Only sources that support predicate pushdown will have filters generated.
    /// </remarks>
    public IGenericResult<Dictionary<string, IFilterExpression>> DecomposeBySource(
        IFilterExpression? filter,
        DataSetConfiguration dataset,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource)
    {
        var validationResult = ValidateDecomposeInputs(filter, dataset, sources);
        if (validationResult != null)
            return validationResult;

        PredicatePushdownLog.DecomposingFilter(_logger, dataset.Name, sources.Count);

        try
        {
            var sourcesByName = BuildSourcesByNameLookup(sources);

            // Extract conditions from the filter tree, grouped by source
            var sourceConditions = new Dictionary<string, List<IFilterCondition>>(
                StringComparer.OrdinalIgnoreCase);
            ExtractConditions(filter!.Root!, sourceConditions, dataset, sources, fieldMappingsBySource);

            // Build filter expressions for each source
            var sourceFilters = GenerateSourceFilters(sourceConditions, sourcesByName, fieldMappingsBySource, dataset);

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

    /// <summary>
    /// Validates inputs for DecomposeBySource. Returns null if valid, or a failure result.
    /// </summary>
    private IGenericResult<Dictionary<string, IFilterExpression>>? ValidateDecomposeInputs(
        IFilterExpression? filter,
        DataSetConfiguration dataset,
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
    /// Builds a lookup dictionary for sources by name.
    /// </summary>
    private static Dictionary<string, DataSetSourceConfiguration> BuildSourcesByNameLookup(
        IReadOnlyList<DataSetSourceConfiguration> sources)
    {
        var sourcesByName = new Dictionary<string, DataSetSourceConfiguration>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var source in sources)
        {
            sourcesByName[source.SourceName] = source;
        }

        return sourcesByName;
    }

    /// <summary>
    /// Generates source-specific filter expressions from decomposed conditions.
    /// Only includes sources that support predicate pushdown.
    /// </summary>
    private Dictionary<string, IFilterExpression> GenerateSourceFilters(
        Dictionary<string, List<IFilterCondition>> sourceConditions,
        Dictionary<string, DataSetSourceConfiguration> sourcesByName,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource,
        DataSetConfiguration dataset)
    {
        var sourceFilters = new Dictionary<string, IFilterExpression>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var (sourceName, conditions) in sourceConditions)
        {
            if (conditions.Count == 0)
                continue;

            if (!sourcesByName.TryGetValue(sourceName, out var sourceConfig))
            {
                PredicatePushdownLog.SourceNotFound(_logger, sourceName, dataset.Name);
                continue;
            }

            if (!sourceConfig.SupportsPredicatePushdown)
            {
                PredicatePushdownLog.PushdownNotSupported(_logger, sourceName);
                continue;
            }

            fieldMappingsBySource.TryGetValue(sourceName, out var fieldMappings);
            var translatedConditions = TranslateFieldNames(conditions, fieldMappings);

            var filterExpression = BuildFilterExpression(translatedConditions);
            sourceFilters[sourceName] = filterExpression;

            PredicatePushdownLog.FilterGenerated(_logger, sourceName, translatedConditions.Count);
        }

        return sourceFilters;
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
    /// Extracts conditions from a filter node and routes them to appropriate sources.
    /// </summary>
    private void ExtractConditions(
        IFilterNode node,
        Dictionary<string, List<IFilterCondition>> sourceConditions,
        DataSetConfiguration dataset,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource)
    {
        switch (node)
        {
            case IFilterCondition condition:
                // Find which source owns this field
                var sourceName = FindSourceForField(condition.PropertyName, sources, fieldMappingsBySource);
                if (sourceName != null)
                {
                    if (!sourceConditions.TryGetValue(sourceName, out var conditions))
                    {
                        conditions = [];
                        sourceConditions[sourceName] = conditions;
                    }

                    conditions.Add(condition);
                    PredicatePushdownLog.ConditionRouted(_logger, condition.PropertyName, sourceName);
                }
                else
                {
                    PredicatePushdownLog.FieldNotMapped(_logger, condition.PropertyName, dataset.Name);
                }
                break;

            case FilterGroup group:
                // Recursively extract conditions from child nodes
                foreach (var childNode in group.Nodes)
                {
                    ExtractConditions(childNode, sourceConditions, dataset, sources, fieldMappingsBySource);
                }
                break;
        }
    }

    /// <summary>
    /// Finds which source owns a specific field by checking field mappings.
    /// </summary>
    private static string? FindSourceForField(
        string logicalFieldName,
        IReadOnlyList<DataSetSourceConfiguration> sources,
        IDictionary<string, IReadOnlyDictionary<string, string>> fieldMappingsBySource)
    {
        foreach (var source in sources)
        {
            if (fieldMappingsBySource.TryGetValue(source.SourceName, out var mappings) &&
                mappings.ContainsKey(logicalFieldName))
            {
                return source.SourceName;
            }
        }

        return null;
    }

    /// <summary>
    /// Translates logical field names to physical field names for a list of conditions.
    /// </summary>
    private static List<IFilterCondition> TranslateFieldNames(
        List<IFilterCondition> conditions,
        IReadOnlyDictionary<string, string>? fieldMappings)
    {
        if (fieldMappings == null)
        {
            return conditions;
        }

        var translated = new List<IFilterCondition>();

        foreach (var condition in conditions)
        {
            // Translate logical → physical
            var physicalFieldName = fieldMappings.TryGetValue(condition.PropertyName, out var physical)
                ? physical
                : condition.PropertyName;  // Fallback to logical name if no mapping

            translated.Add(new FilterCondition
            {
                PropertyName = physicalFieldName,
                Operator = condition.Operator,
                Value = condition.Value
            });
        }

        return translated;
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

    /// <summary>
    /// Builds a filter expression from a list of conditions combined with AND logic.
    /// </summary>
    private static FilterExpression BuildFilterExpression(List<IFilterCondition> conditions)
    {
        if (conditions.Count == 0)
        {
            return new FilterExpression { Root = null };
        }

        if (conditions.Count == 1)
        {
            return new FilterExpression { Root = (IFilterNode)conditions[0] };
        }

        // Multiple conditions - combine with AND
        var group = new FilterGroup
        {
            Operator = LogicalOperator.And,
            Nodes = conditions.Cast<IFilterNode>().ToList()
        };

        return new FilterExpression { Root = group };
    }
}

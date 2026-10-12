using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Data.OData.Logging;
using Fdw.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Data.OData;

/// <summary>
/// Base class for REST/OData data command translators.
/// Returns HttpRequestMessage objects with proper OData query conventions.
/// </summary>
/// <remarks>
/// <para>
/// This base provides common REST/OData translation utilities for all REST command translators.
/// Each translator (Query, Insert, Update, Delete) inherits from this base and implements
/// domain-specific translation logic for RESTful HTTP operations.
/// </para>
/// <para>
/// REST translators convert DataCommands to HttpRequestMessage with OData query conventions:
/// <list type="bullet">
/// <item>Query → GET with $filter, $orderby, $top, $skip</item>
/// <item>Insert → POST with JSON body</item>
/// <item>Update → PUT/PATCH with JSON body</item>
/// <item>Delete → DELETE</item>
/// </list>
/// </para>
/// </remarks>
public abstract class ODataCommandTranslatorBase : DataCommandTranslatorBase<HttpRequestMessage>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ODataCommandTranslatorBase"/> class.
    /// ID is auto-calculated from name using FNV-1a hash.
    /// </summary>
    /// <param name="name">Name of the translator (must match TypeOption attribute).</param>
    protected ODataCommandTranslatorBase(string name)
        : base(name, "Rest")
    {
    }

    /// <summary>
    /// Builds OData $filter query string from hierarchical filter expression.
    /// </summary>
    /// <param name="filter">The filter expression.</param>
    /// <returns>OData filter string (e.g., "(Name eq 'Acme' or Name eq 'Corp') and IsActive eq true").</returns>
    protected static string BuildODataFilter(IFilterExpression filter)
    {
        if (filter?.Root == null)
        {
            return string.Empty;
        }

        return BuildODataNode(filter.Root);
    }

    /// <summary>
    /// Recursively builds OData filter for a filter node (condition or group).
    /// </summary>
    private static string BuildODataNode(IFilterNode node)
    {
        switch (node)
        {
            case FilterCondition condition:
                return BuildODataCondition(condition);
            case FilterGroup group:
                return BuildODataGroup(group);
            default:
                ODataCommandTranslatorBaseLog.UnknownFilterNodeType(
                    NullLogger<ODataCommandTranslatorBase>.Instance, node.GetType().Name);
                throw new InvalidOperationException($"Unknown filter node type: {node.GetType().Name}");
        }
    }

    /// <summary>
    /// Builds OData filter for a single condition.
    /// </summary>
    private static string BuildODataCondition(FilterCondition condition)
    {
        var odataCondition = $"{condition.PropertyName} {condition.Operator.ODataOperator}";

        if (condition.Operator.RequiresValue)
        {
            var formattedValue = condition.Operator.FormatODataValue(condition.Value);
            odataCondition += $" {formattedValue}";
        }

        return odataCondition;
    }

    /// <summary>
    /// Builds OData filter for a group of conditions with proper parentheses for precedence.
    /// </summary>
    private static string BuildODataGroup(FilterGroup group)
    {
        var clauses = new List<string>();

        foreach (var childNode in group.Nodes)
        {
            var clause = BuildODataNode(childNode);
            if (!string.IsNullOrEmpty(clause))
            {
                clauses.Add(clause);
            }
        }

        if (clauses.Count == 0)
        {
            return string.Empty;
        }

        if (clauses.Count == 1)
        {
            return clauses[0]; // Single condition doesn't need parentheses
        }

        var logicalOp = group.Operator == LogicalOperator.Or ? " or " : " and ";
        var joined = string.Join(logicalOp, clauses);

        return $"({joined})"; // Always wrap groups in parentheses for precedence
    }

    /// <summary>
    /// Builds an OData $orderby query string from ordering expression.
    /// </summary>
    /// <param name="ordering">The ordering expression.</param>
    /// <returns>The OData $orderby query string (without "$orderby=" prefix).</returns>
    protected static string BuildODataOrderBy(IOrderingExpression ordering)
    {
        var clauses = new List<string>();

        foreach (var field in ordering.OrderedFields)
        {
            var direction = field.Direction.IsAscending ? " asc" : " desc";
            clauses.Add($"{field.PropertyName}{direction}");
        }

        return string.Join(", ", clauses);
    }

    /// <summary>
    /// Builds OData $top and $skip query parameters from paging expression.
    /// </summary>
    /// <param name="paging">The paging expression.</param>
    /// <param name="queryParams">Dictionary to add paging parameters to.</param>
    protected static void AddODataPaging(IPagingExpression paging, IDictionary<string, string> queryParams)
    {
        if (paging.Skip > 0)
        {
            queryParams["$skip"] = paging.Skip.ToString(CultureInfo.InvariantCulture);
        }

        if (paging.Take.HasValue && paging.Take.Value > 0)
        {
            queryParams["$top"] = paging.Take.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Builds an OData $select query string from projection expression.
    /// </summary>
    /// <param name="projection">The projection expression.</param>
    /// <returns>The OData $select query string (without "$select=" prefix).</returns>
    protected static string BuildODataSelect(IProjectionExpression projection)
    {
        return string.Join(",", projection.PropertyNames ?? []);
    }
}

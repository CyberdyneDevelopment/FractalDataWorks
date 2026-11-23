using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Data.Abstractions.Mappers.PocoMappers;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>
/// What a domain query route's declared fields allow (FDW-6273): which declarations are valid against the record's mapper, which filters and sorts a filled
/// field refuses, and how a row of the asked fields is read. <see cref="QueryDomainEndpointBase{TDomain}"/> runs each; a conformance test can run them over
/// every route without a request.
/// </summary>
public static class QueryableFieldDeclarations
{
    /// <summary>Checks a route's declaration against its record's mapper: a column field must be one the mapper reaches; a filled field reaches none.</summary>
    /// <param name="resource">The resource the route queries.</param>
    /// <param name="record">The name of the record a page holds.</param>
    /// <param name="declared">The fields the route declares.</param>
    /// <param name="mapper">The record's mapper.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, or <c>RESTENDPOINTS-60004</c> naming the first field that does not fit; the declaration is the resource's defect, never narrowed.</returns>
    public static IGenericResult Check(string resource, string record, IReadOnlyList<QueryableField> declared, IPocoMapper mapper, ILogger logger)
    {
        if (declared.Count == 0)
            return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(logger, resource, "it declares none"));

        var columns = mapper.GetPropertyNames();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in declared)
        {
            if (!field.IsFilled && !columns.Contains(field.Column, StringComparer.Ordinal))
                return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(
                    logger, resource, $"'{field.Name}' names column '{field.Column}', which {record} does not have"));

            if (!seen.Add(field.Name))
                return GenericResult.Failure(EndpointLogger.QueryableFieldsInvalid(logger, resource, $"'{field.Name}' is declared more than once"));
        }

        return GenericResult.Success();
    }

    /// <summary>Refuses a query whose filter or sort names a filled field: the list's table holds no column of it, and a view cannot join it.</summary>
    /// <param name="resource">The resource the route queries.</param>
    /// <param name="request">The caller's query.</param>
    /// <param name="declared">The fields the route declares.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>Success, <c>RESTENDPOINTS-20002</c> for a filter on a filled field, or <c>RESTENDPOINTS-20003</c> for a sort on one.</returns>
    public static IGenericResult Refuse(string resource, QueryRequest request, IReadOnlyList<QueryableField> declared, ILogger logger)
    {
        if (FilledNamedBy(request.Filter, declared) is { } filtered)
            return GenericResult.Failure(EndpointLogger.QueryFilterOnFilledField(logger, resource, filtered.Name));

        foreach (var sort in request.Sort ?? [])
        {
            if (sort is not null && Filled(sort.Field, declared) is { } sorted)
                return GenericResult.Failure(EndpointLogger.QuerySortOnFilledField(logger, resource, sorted.Name));
        }

        return GenericResult.Success();
    }

    /// <summary>Reads one row of the asked fields: a column through the record's mapper, a filled field through its getter.</summary>
    /// <param name="record">A record of the page, already filled by the provider's read observer.</param>
    /// <param name="mapper">The record's mapper.</param>
    /// <param name="selected">The fields asked for.</param>
    /// <returns>The row, keyed by each field's request name.</returns>
    public static IReadOnlyDictionary<string, object?> Row(object record, IPocoMapper mapper, IReadOnlyList<QueryableField> selected)
    {
        var values = mapper.MapToParameters(record);
        var row = new Dictionary<string, object?>(selected.Count, StringComparer.Ordinal);
        foreach (var field in selected)
            row[field.Name] = field.Read is { } read ? read(record) : values[field.Column];

        return row;
    }

    // The first filled field a filter tree names, at any depth.
    private static QueryableField? FilledNamedBy(QueryFilterNode? node, IReadOnlyList<QueryableField> declared)
    {
        if (node is null)
            return null;

        if (Filled(node.Field, declared) is { } named)
            return named;

        foreach (var child in node.Nodes ?? [])
        {
            if (FilledNamedBy(child, declared) is { } found)
                return found;
        }

        return null;
    }

    // The filled field a request names, matched ignoring case as every field name is; null when the name is not a filled field's.
    private static QueryableField? Filled(string? name, IReadOnlyList<QueryableField> declared)
        => name is null ? null : declared.FirstOrDefault(field => field.IsFilled && string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase));
}

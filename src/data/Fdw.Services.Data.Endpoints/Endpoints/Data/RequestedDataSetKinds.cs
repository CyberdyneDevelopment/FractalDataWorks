using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Configuration;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>The kinds of data set a list or a lookup is asked for with <c>?kind=</c> (FDW-3638, FDW-3873): one reading, so the two routes narrow alike.</summary>
internal static class RequestedDataSetKinds
{
    /// <summary>Reads <c>?kind=A,B</c> (or several <c>kind=</c> parameters) as the kinds asked for: none when it is absent, else each once, and each a kind of data set.</summary>
    /// <param name="request">The request.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>The kind names as the request spelled them, or the refusal that names a kind <see cref="DataSetTypes"/> does not hold.</returns>
    public static IGenericResult<IReadOnlyList<string>> Of(HttpRequest request, ILogger logger)
    {
        var asked = request.Query["kind"]
            .SelectMany(value => (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var kind in asked)
        {
            if (ReferenceEquals(DataSetTypes.ByName(kind), DataSetTypes.NotFound))
                return GenericResult<IReadOnlyList<string>>.Failure(DataSetEndpointLog.LookupKindUnknown(logger, kind));
        }

        return GenericResult<IReadOnlyList<string>>.Success(asked);
    }

    /// <summary>The names of the implementations the kinds stand for.</summary>
    /// <param name="kinds">The kinds asked for, as <see cref="Of"/> read them.</param>
    public static IReadOnlyList<string> ImplementationNames(IReadOnlyList<string> kinds)
        => [.. kinds.Select(name => DataSetTypes.ByName(name).Name)];

    /// <summary>The caller's query, kept to the rows of the kinds asked for: the kind of a row is the implementation its domain row names.</summary>
    /// <param name="query">The caller's query.</param>
    /// <param name="kinds">The kinds asked for.</param>
    public static DomainQuery Narrow(DomainQuery query, IReadOnlyList<string> kinds)
    {
        var kind = new FilterCondition
        {
            PropertyName = nameof(DomainConfiguration.Implementation),
            Operator = FilterOperators.ByName("In"),
            Value = ImplementationNames(kinds).Select(name => (object?)name).ToList(),
        };

        var root = query.Filter?.Root is { } caller
            ? (IFilterNode)new FilterGroup { Operator = LogicalOperator.And, Nodes = [caller, kind] }
            : kind;
        return new DomainQuery(new FilterExpression { Root = root }, query.Ordering, query.Skip, query.Take, query.IncludeTotal);
    }
}

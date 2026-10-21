using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Schema;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Turns a <see cref="DataQueryRequest"/> body into the <see cref="QueryCommand{T}"/> the
/// data gateway dispatches, checked against the data set's own fields.
/// </summary>
/// <remarks>
/// The translation is <see cref="QueryTranslator"/>'s, shared with every other queryable route. What a
/// data set adds is which fields exist (its own, current ones), which are calculated after the rows
/// are read, and the columns a result is described by. A refusal is <c>DataQueryInvalid</c>, naming the
/// data set.
/// </remarks>
internal sealed class DataQueryTranslator
{
    private readonly QueryTranslator _translator;
    private readonly QueryTranslator _planner;
    private readonly IReadOnlyList<DataSetFieldConfiguration> _fields;
    private readonly DataQueryGrouping _grouping;

    private DataQueryTranslator(
        QueryTranslator translator, QueryTranslator planner, IReadOnlyList<DataSetFieldConfiguration> fields, DataQueryGrouping grouping)
    {
        _translator = translator;
        _planner = planner;
        _fields = fields;
        _grouping = grouping;
    }

    /// <summary>Creates a translator over the data set's current fields.</summary>
    /// <param name="fields">The data set's current fields.</param>
    /// <param name="dataSetName">The data set queried, for refusals.</param>
    /// <param name="logger">The logger refusals are written to.</param>
    /// <returns>The translator, or a refusal when the data set declares no fields or two fields' names differ only by case.</returns>
    public static IGenericResult<DataQueryTranslator> Create(
        IEnumerable<DataSetFieldConfiguration> fields, string dataSetName, ILogger logger)
    {
        var declared = fields.ToList();
        var refusals = new DataQueryRefusals(dataSetName, logger);

        // Why refused here: a data set reads only the fields it declares, so with none a query has no column to select and used to
        // answer rows of empty arrays (FDW-2114). This is where every query over a data set is built, so every route inherits it.
        if (declared.Count == 0)
            return refusals.Invalid<DataQueryTranslator>("it declares no fields, so there are no columns to read; declare its fields first");

        var queryable = declared.Select(f => Queryable(f, dataSetName)).ToList();
        var translator = QueryTranslator.Create(queryable, "this data set", refusals);
        if (translator.IsFailure) return translator.ToNewResult<DataQueryTranslator>();

        // Why two over the same fields: a plain read plans its filter and sort and may name a calculated field (FDW-1045),
        // which the data route applies after the read; a grouped query is run whole by the store and cannot, so it keeps
        // the translator that refuses a calculated field.
        var planner = QueryTranslator.Create(queryable, "this data set", refusals, allowCalculated: true);
        return planner.IsFailure
            ? planner.ToNewResult<DataQueryTranslator>()
            : GenericResult<DataQueryTranslator>.Success(
                new DataQueryTranslator(translator.Value!, planner.Value!, declared, new DataQueryGrouping(declared, translator.Value!, refusals)));
    }

    // The field as a query sees it: what it takes is DataSetFieldFilters's answer, the same one the field read returns, and an
    // empty answer says why so a refusal tells the caller what to do about it (FDW-1102).
    private static QueryableField Queryable(DataSetFieldConfiguration field, string dataSetName)
    {
        var operators = DataSetFieldFilters.Operators(field);
        return new QueryableField(
            field.Name, field.Name, DataSetFieldTypes.Resolve(field.TypeName), field.IsCalculated, field.IsFilterable,
            operators, operators.Count == 0 ? WhyNoOperators(field, dataSetName) : null);
    }

    private static string WhyNoOperators(DataSetFieldConfiguration field, string dataSetName)
    {
        if (DataSetFieldTypes.Resolve(field.TypeName) is null)
            return $"'{field.Name}' takes no filter operator: its data type '{field.TypeName}' is not a known data type";

        return field.DatumTypeId == DatumTypes.NotYetTyped.Id
            ? $"'{field.Name}' is not yet typed, so it takes no filter operator until it is; type it with PATCH /datasets/{dataSetName}/fields/datum-type"
            : $"'{field.Name}' takes no filter operator: its datum type {DatumTypes.ById(field.DatumTypeId).Name} allows none";
    }

    /// <summary>Gets the result columns: every field, in ordinal order.</summary>
    /// <returns>The columns.</returns>
    public IReadOnlyList<DataQueryColumnDto> Columns()
        => _fields
            .OrderBy(f => f.Ordinal)
            .Select(f => new DataQueryColumnDto
            {
                Name = f.Name,
                DataType = f.TypeName,
                IsKey = f.IsKey,
                Role = f.Role,
                IsFilterable = f.IsFilterable,
                FilterOperators = [.. DataSetFieldFilters.Operators(f)],
            })
            .ToList();

    /// <summary>Refuses a caller's filter that names a field the data set's administrator has not made filterable.</summary>
    /// <param name="filter">The caller's own filter, before any route-added filter is merged into it.</param>
    /// <returns>Success, or <c>DataQueryInvalid</c> naming the field.</returns>
    public IGenericResult RequireFilterable(QueryFilterNode? filter) => _translator.RequireFilterable(filter);

    /// <summary>Builds the query command, reading one row past the page so a following page can be reported.</summary>
    /// <param name="request">The request body.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <returns>The command, or <c>DataQueryInvalid</c> naming the first thing that did not resolve.</returns>
    public IGenericResult<QueryCommand<Dictionary<string, object?>>> Translate(DataQueryRequest request, int take)
        => _translator.Translate(request, take);

    // A plain read: the store's part of the query and the part applied to the rows after they are read.
    public IGenericResult<QueryPlan> Plan(DataQueryRequest request, int take, int rowCap)
        => _planner.Plan(request, take, rowCap);

    /// <summary>Builds the grouped query command, reading one group past the page so a following page can be reported.</summary>
    /// <param name="request">The request body, carrying <c>GroupBy</c> or <c>Aggregates</c>.</param>
    /// <param name="take">The page size, already resolved against the pagination settings.</param>
    /// <returns>The command and the columns its rows carry, or <c>DataQueryInvalid</c> naming the first thing that did not resolve.</returns>
    internal IGenericResult<GroupedQuery> TranslateGrouped(DataQueryRequest request, int take)
        => _grouping.Translate(request, take);
}

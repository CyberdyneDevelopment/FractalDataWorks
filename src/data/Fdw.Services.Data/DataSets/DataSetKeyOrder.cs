using System;
using System.Collections.Generic;
using System.Linq;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Data;

/// <summary>
/// The order a data set is read a page at a time when the caller states none: the order of its key fields (FDW-1322, FDW-3454).
/// </summary>
/// <remarks>
/// Why the data set states it: a page is a slice of an ordered sequence, and an order nobody declared is an order nobody can
/// reproduce, so the store is never left to choose one. A data set declares its key fields on its own fields (<c>IsKey</c>), and
/// the same declaration orders a stream (<c>DataSetRecordSource</c>), so a page and a stream of one data set follow one order.
/// A data set that declares none is refused, naming it; the order is not taken from the source container.
/// </remarks>
internal static class DataSetKeyOrder
{
    /// <summary>
    /// Gets the ordering a read of <paramref name="dataSet"/> runs under: the one the command states, else, for a page, the data
    /// set's key fields in their declared order. A page whose stated order leaves out key fields ends on them (FDW-5024).
    /// </summary>
    /// <param name="dataSet">The data set being read.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The ordering, or null when the command asks for no page and states none; a refusal when it asks for a page, states no
    /// ordering and the data set declares no key field.
    /// </returns>
    /// <remarks>
    /// Why the tie-break: a caller's order on a column that repeats leaves the rows that share a value in no stated order, so the store
    /// may hand one to two pages and another to none. The key fields identify a row, so ending on them gives every row one place. A data
    /// set that declares no key field keeps the caller's order as it is; it is not refused for stating one.
    /// </remarks>
    public static IGenericResult<IOrderingExpression?> For(IDataSetImplementationConfiguration dataSet, IDataCommand command, ILogger logger)
    {
        var requested = command as IQueryCommand;
        if (requested?.Paging is null)
            return GenericResult<IOrderingExpression?>.Success(requested?.Ordering);

        var keys = KeyOrder(dataSet);
        if (requested.Ordering is { OrderedFields.Count: > 0 } stated)
            return GenericResult<IOrderingExpression?>.Success(EndingOnKeys(stated, keys));

        return keys.Count == 0
            ? GenericResult<IOrderingExpression?>.Failure(
                DataServiceResultCodes.ByName("DataSetNotPageable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", "it is read a page at a time in the order of its key fields and declares none; mark the fields that identify a row as keys"))
            : GenericResult<IOrderingExpression?>.Success(new OrderingExpression { OrderedFields = keys });
    }

    // The stated order, then the key fields it leaves out; the stated order itself when it names them all.
    private static IOrderingExpression EndingOnKeys(IOrderingExpression stated, List<IOrderedField> keys)
    {
        var tieBreak = keys
            .Where(key => !stated.OrderedFields.Any(field => string.Equals(field.PropertyName, key.PropertyName, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return tieBreak.Count == 0 ? stated : new OrderingExpression { OrderedFields = [.. stated.OrderedFields, .. tieBreak] };
    }

    // The data set's key fields in their declared order, ascending.
    private static List<IOrderedField> KeyOrder(IDataSetImplementationConfiguration dataSet) =>
        [.. dataSet.Fields
            .Where(field => field.IsCurrent && !field.IsDeleted && field.IsKey && !field.IsCalculated)
            .OrderBy(field => field.Ordinal)
            .Select(field => (IOrderedField)new OrderedField { PropertyName = field.Name, Direction = SortDirections.Ascending })];
}

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
    /// set's key fields in their declared order.
    /// </summary>
    /// <param name="dataSet">The data set being read.</param>
    /// <param name="command">The command being run.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <returns>
    /// The ordering, or null when the command asks for no page and states none; a refusal when it asks for a page, states no
    /// ordering and the data set declares no key field.
    /// </returns>
    public static IGenericResult<IOrderingExpression?> For(IDataSetImplementationConfiguration dataSet, IDataCommand command, ILogger logger)
    {
        var requested = command as IQueryCommand;
        if (requested?.Paging is null || requested.Ordering is { OrderedFields.Count: > 0 })
            return GenericResult<IOrderingExpression?>.Success(requested?.Ordering);

        var keys = dataSet.Fields
            .Where(field => field.IsCurrent && !field.IsDeleted && field.IsKey && !field.IsCalculated)
            .OrderBy(field => field.Ordinal)
            .Select(field => (IOrderedField)new OrderedField { PropertyName = field.Name, Direction = SortDirections.Ascending })
            .ToList();

        return keys.Count == 0
            ? GenericResult<IOrderingExpression?>.Failure(
                DataServiceResultCodes.ByName("DataSetNotPageable"), logger,
                ResultDetails.Create("dataSet", dataSet.Name,
                    "reason", "it is read a page at a time in the order of its key fields and declares none; mark the fields that identify a row as keys"))
            : GenericResult<IOrderingExpression?>.Success(new OrderingExpression { OrderedFields = keys });
    }
}

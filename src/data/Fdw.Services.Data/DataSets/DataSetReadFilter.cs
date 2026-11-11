using Fdw.Commands.Data.Abstractions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Execution;

namespace Fdw.Services.Data;

/// <summary>
/// The filter a read of a data set runs with (FDW-4997): the data set's own conditions ANDed with the caller's filter. The one place every
/// data set kind takes its read filter from.
/// </summary>
/// <remarks>
/// Every route that reads a data set (/data and /datasets/{name}/query, preview, export, StableRead's keyset pages, unpaged reads and
/// streams, flows, TDS) reaches a kind through the gateway's data set path, and each kind asks this for its filter before translating it
/// into its source's names, so a data set's own filter goes to the store with every read. Both filters are in the data set's logical field
/// names, so each kind's own translation (Simple's mappings, Compound's owner-qualified names, Federated's per-source decomposition) applies
/// to the whole of it.
/// </remarks>
public static class DataSetReadFilter
{
    /// <summary>Gets the filter a read runs with.</summary>
    /// <param name="context">The data set being read.</param>
    /// <param name="command">The read; a command that is not a query carries no filter of its own.</param>
    /// <returns>
    /// The caller's filter when the data set states no condition, the data set's when the caller states none, both ANDed when both do, null
    /// when neither does; or the refusal of a stored condition that breaks a rule (<see cref="DataSetFilters"/>).
    /// </returns>
    public static IGenericResult<IFilterExpression?> For(DataSetExecutionContext context, IDataCommand command)
    {
        var own = DataSetFilters.Expression(context.Config, context.Logger);
        if (own.IsFailure)
            return own;

        var caller = (command as IQueryCommand)?.Filter is { Root: not null } stated ? stated : null;
        if (own.Value?.Root is not { } ownRoot)
            return GenericResult<IFilterExpression?>.Success(caller);

        return caller?.Root is { } callerRoot
            ? GenericResult<IFilterExpression?>.Success(new FilterExpression { Root = new FilterGroup { Operator = LogicalOperator.And, Nodes = [ownRoot, callerRoot] } })
            : own;
    }
}

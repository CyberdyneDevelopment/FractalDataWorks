using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Data;

/// <summary>
/// Reads every stored version of one configuration row: current, retired, and deleted.
/// </summary>
/// <remarks>
/// Configuration is versioned on write. A save retires the current row (IsCurrent = 0) and inserts
/// a new one under the same logical Id; a delete leaves a row marked IsDeleted. Every other read
/// filters to the current row. This one deliberately does not, so a history screen can show how a
/// row changed. It is a plain read of rows already written, not a new tracking mechanism.
/// </remarks>
public static class ConfigurationVersions
{
    /// <summary>All stored rows whose logical Id is <paramref name="id"/>, in the order they were written.</summary>
    /// <typeparam name="T">The mapped configuration type of the container.</typeparam>
    /// <param name="gateway">The gateway the container is read through.</param>
    /// <param name="target">The container holding the versioned rows.</param>
    /// <param name="id">The row's logical Id, shared by all of its versions.</param>
    /// <param name="writtenAt">The column each version was written at, used to order them oldest first.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task<IGenericResult<IReadOnlyList<T>>> Read<T>(
        IDataGateway gateway,
        DataStoreTarget target,
        Guid id,
        Func<T, DateTimeOffset> writtenAt,
        CancellationToken cancellationToken = default)
    {
        var rows = await gateway.Execute<IEnumerable<T>>(
            new QueryCommand<T>
            {
                Filter = new FilterExpression
                {
                    Root = new FilterCondition { PropertyName = "Id", Operator = FilterOperators.ByName("Equal"), Value = id }
                }
            },
            target,
            cancellationToken).ConfigureAwait(false);
        if (rows.IsFailure)
            return rows.ToNewResult<IReadOnlyList<T>>();

        return GenericResult<IReadOnlyList<T>>.Success([.. rows.Value!.OrderBy(writtenAt)]);
    }
}

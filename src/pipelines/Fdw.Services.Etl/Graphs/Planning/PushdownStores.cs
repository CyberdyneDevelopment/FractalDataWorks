using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>The key a read's data store is compared by (FDW-6970): the data store record its container is described by, never its name.</summary>
public static class PushdownStores
{
    /// <summary>Judges the store of a read of one container.</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="read">The read's component name, for the trace.</param>
    /// <param name="target">The container it reads.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The store's key; empty, with the reason in the trace, when the container is not described.</returns>
    public static async Task<string> Key(PushdownScope scope, string read, DataStoreTarget target, CancellationToken cancellationToken = default)
    {
        var described = await scope.Describe(target, cancellationToken).ConfigureAwait(false);
        if (described.IsFailure)
        {
            PushdownLog.FederatedReadStoreUnknown(scope.Logger, scope.Graph.Name, read, described.FailureReason().Message);
            return string.Empty;
        }

        return described.Value is { } container ? $"data store {container.DataStoreId:D}" : string.Empty;
    }
}

using Fdw.Results;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The refusals the map and lineage endpoints share: a request that cannot be answered, and a node that is not there.</summary>
internal static class MapRequestRefusals
{
    /// <summary>Refuses a request with the reason a caller can act on.</summary>
    public static IGenericResult<T> Invalid<T>(ILogger logger, string reason)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName("MapRequestInvalid"), logger, ResultDetails.Create("reason", reason));

    /// <summary>Refuses a request that names a node the caller cannot see, exactly as one that does not exist.</summary>
    public static IGenericResult<T> NodeNotFound<T>(ILogger logger, string nodeId)
        => GenericResult<T>.Failure(
            WorkspacesResultCodes.ByName("MapNodeNotFound"), logger, ResultDetails.Create("node", nodeId));
}

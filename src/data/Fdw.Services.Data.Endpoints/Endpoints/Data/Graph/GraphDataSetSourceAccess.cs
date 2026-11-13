using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// Decides whether a caller may read every source a Graph data set reads (FDW-5295, plan answer 2 of FDW-5286): the graph, its sources and its intended sources
/// are answered only to a caller who may.
/// </summary>
/// <remarks>
/// What the graph reads is <see cref="GraphDataSetReads"/>, the one reading lineage and status take of it. Each kind of thing it reads is asked the read permission of the
/// route that reads that kind (<c>datasets:read</c> for a data set, <c>datastores:read</c> for a data store or a container), through the framework's own authorization,
/// as the map asks its edit permissions. A kind of read this table does not know is not allowed, and a caller who cannot be named is not allowed: it never reads
/// as allowed because nothing said otherwise.
/// </remarks>
internal sealed class GraphDataSetSourceAccess
{
    private const string Read = "read";

    // The resource whose read permission each kind of read is asked, by the name of the read's kind: one row each, so no kind is a branch of its own.
    private static readonly Dictionary<string, string> ResourceByReadKind = new(StringComparer.Ordinal)
    {
        [GraphDataSetReadKinds.DataSet] = "datasets",
        [GraphDataSetReadKinds.DataStore] = "datastores",
        [GraphDataSetReadKinds.Container] = "datastores",
    };

    private readonly IFrameworkAuthorizationService _authorization;

    /// <summary>Initializes a new instance of the <see cref="GraphDataSetSourceAccess"/> class.</summary>
    /// <param name="authorization">The framework's authorization, which answers a permission asked of the caller.</param>
    internal GraphDataSetSourceAccess(IFrameworkAuthorizationService authorization)
    {
        _authorization = authorization;
    }

    /// <summary>Asks whether the caller may read everything the graph reads.</summary>
    /// <param name="graph">The Graph data set, read whole (its components and intents).</param>
    /// <param name="caller">The caller.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True when the caller holds the read permission of every kind of thing the graph reads (a graph that reads nothing is true); false when one is missing; or the failure when the authorization could not answer.</returns>
    internal async Task<IGenericResult<bool>> MayReadEvery(IGraphDataSetConfiguration graph, IAuthenticationContext caller, CancellationToken ct)
    {
        var resources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var read in GraphDataSetReads.Of(graph))
        {
            if (!ResourceByReadKind.TryGetValue(read.Kind, out var resource))
            {
                return GenericResult<bool>.Success(false);
            }

            resources.Add(resource);
        }

        foreach (var resource in resources)
        {
            var answer = await _authorization.Authorize(caller, resource, Read, ct).ConfigureAwait(false);
            if (answer.IsFailure || !answer.Value)
            {
                return answer;
            }
        }

        return GenericResult<bool>.Success(true);
    }
}

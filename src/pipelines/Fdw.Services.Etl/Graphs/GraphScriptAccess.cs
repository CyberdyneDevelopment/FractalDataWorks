using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Etl.Logging;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Etl.Graphs;

/// <summary>
/// The one rule that says who may save a graph that holds script text (FDW-3676): a caller who may write scripts.
/// </summary>
/// <remarks>
/// Script text in a pipeline is a script write by another route, so the rule asks for the permission the stored scripts ask for. It does not look at who the caller
/// is: the answer comes in, so every save route asks the same permission and the rule is the same for each. The answer is asked for only when the graph holds script
/// text, so a graph without any costs no lookup.
/// </remarks>
public static class GraphScriptAccess
{
    /// <summary>The permission to write a script: the policy the stored script routes apply to a write.</summary>
    public const string WritePolicy = "scripts:write";

    /// <summary>Judges whether a caller may save a graph.</summary>
    /// <param name="graph">The graph to save.</param>
    /// <param name="mayWriteScripts">Says whether the caller holds <see cref="WritePolicy"/>; asked only when the graph holds script text.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">Cancels the question.</param>
    /// <returns>A success when the graph holds no script text or the caller may write scripts; otherwise a Forbidden failure naming the pipeline and the components.</returns>
    public static async Task<IGenericResult> Check(
        GraphPipelineConfiguration graph, Func<CancellationToken, Task<bool>> mayWriteScripts, ILogger logger, CancellationToken cancellationToken = default)
    {
        var holding = GraphScripts.ComponentsHolding(graph);
        if (holding.Count == 0 || await mayWriteScripts(cancellationToken).ConfigureAwait(false))
        {
            return GenericResult.Success();
        }

        var message = EtlLog.GraphScriptsNotPermitted(logger, graph.Name, string.Join(", ", holding.Select(name => $"'{name}'")), WritePolicy);
        return GenericResult.Failure(EtlResultCodes.ByName("GraphScriptsNotPermitted"), ResultDetails.Create("Reason", message.Message));
    }
}

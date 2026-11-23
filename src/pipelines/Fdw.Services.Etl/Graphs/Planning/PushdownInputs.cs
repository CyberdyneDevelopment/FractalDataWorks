using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Etl.Abstractions.Flow;

namespace Fdw.Services.Etl.Graphs.Planning;

/// <summary>
/// Resolves one input of a join or lookup to what feeds it once pushed (FDW-6969): a container source, or, on a left input, a join pushed with it;
/// then the input's own mappings. An input fed by anything else, or by a component whose rows also go elsewhere, keeps its consumer in memory.
/// </summary>
public static class PushdownInputs
{
    /// <summary>Resolves an input.</summary>
    /// <param name="scope">The graph.</param>
    /// <param name="consumer">The join or lookup.</param>
    /// <param name="input">The input's name.</param>
    /// <param name="takesJoins">Whether a join may feed it: the left input of a join and the main input of a lookup; never the right or reference.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The side the input reads, or why its consumer runs in memory.</returns>
    public static async Task<PushdownAttempt> Side(
        PushdownScope scope, PipelineComponentConfiguration consumer, string input, bool takesJoins, CancellationToken cancellationToken = default)
    {
        var lines = scope.Into(consumer.Name, input);
        if (lines.Count != 1)
            return PushdownAttempt.Stays($"its input '{input}' takes {lines.Count} lines, not one");

        if (scope.Component(lines[0].FromComponent) is not { } feeder)
            return PushdownAttempt.Stays($"its input '{input}' takes a line from '{lines[0].FromComponent}', which the graph does not hold");

        if (scope.Consumers(feeder.Name) != 1)
            return PushdownAttempt.Stays($"'{feeder.Name}', which feeds its input '{input}', also sends its rows elsewhere");

        var part = PushdownParts.ByName(feeder.ComponentType);
        if (ReferenceEquals(part, PushdownParts.NotFound) || (part.Combines && !takesJoins))
            return PushdownAttempt.Stays($"its input '{input}' takes the rows of '{feeder.Name}', a {feeder.ComponentType}, which is not pushed into a store there");

        var fed = await part.Resolve(scope, feeder, false, cancellationToken).ConfigureAwait(false);
        if (fed.Side is not { } side)
            return PushdownAttempt.Stays($"'{feeder.Name}', which feeds its input '{input}', runs in memory: {fed.Reason}");

        var (fields, reason) = PushdownMappings.Apply(consumer, input, ComponentTypeBase.MappingsSetting, side);
        return reason.Length > 0 ? PushdownAttempt.Stays(reason) : PushdownAttempt.Pushed(side with { Fields = fields });
    }
}

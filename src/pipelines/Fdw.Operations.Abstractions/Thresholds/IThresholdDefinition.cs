using System.Collections.Generic;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>A threshold of any type, as the evaluator holds it (FDW-3936): the non-generic view of <see cref="Threshold{T}"/>.</summary>
public interface IThresholdDefinition
{
    /// <summary>Gets the threshold name.</summary>
    string Name { get; }

    /// <summary>Gets the threshold type.</summary>
    IThreshold Type { get; }

    /// <summary>Gets whether the threshold is judged at all.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets a value indicating whether a rule fires its consequence only when its criteria start to hold, and again only after they have stopped holding. When false it fires on every report that matches.</summary>
    bool FiresOnEntry { get; }

    /// <summary>Gets the parameters the threshold states, by name: one value, or the values in order for a list parameter. Empty for a type that takes none.</summary>
    IReadOnlyDictionary<string, object?> Parameters { get; }

    /// <summary>Gets the rules in the order they are judged.</summary>
    IReadOnlyList<ThresholdRule> Rules { get; }
}

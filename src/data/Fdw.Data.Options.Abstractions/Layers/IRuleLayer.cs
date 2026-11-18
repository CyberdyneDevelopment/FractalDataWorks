using System.Collections.Generic;

namespace Fdw.Data.Options;

/// <summary>
/// One level of a data configuration rule's path (FDW-6340): <c>DataConfigurationRules:level:level:Kind</c>. The rule layer names no
/// consumer: who builds the type collection that is a child of the root decides what the levels below it mean.
/// </summary>
public interface IRuleLayer
{
    /// <summary>Gets the name a path states at this level.</summary>
    string Name { get; }

    /// <summary>Gets the name a console shows for this level; every layer is a type option, which states one (FDW-6342).</summary>
    string DisplayName { get; }

    /// <summary>Gets what this level is for; empty when the layer states none (FDW-6342).</summary>
    string Description { get; }

    /// <summary>Gets the layers below this one; empty for a kind.</summary>
    IReadOnlyList<IRuleLayer> Layers { get; }
}

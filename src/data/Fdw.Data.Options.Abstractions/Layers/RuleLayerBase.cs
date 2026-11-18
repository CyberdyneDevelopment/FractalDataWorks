using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>
/// The base of every child of the DataConfigurationRules root (FDW-6340). Whoever builds a child type collection registers one of
/// these to open it; the rule layer itself names none.
/// </summary>
public abstract class RuleLayerBase : TypeOptionBase<RuleLayerBase>, IRuleLayerOption
{
    /// <summary>Initializes a new instance of the <see cref="RuleLayerBase"/> class.</summary>
    /// <param name="name">The name a path states for this layer.</param>
    protected RuleLayerBase(string name)
        : base(name)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="RuleLayerBase"/> class with the display name and description a console shows (FDW-6342).</summary>
    /// <param name="name">The layer's name.</param>
    /// <param name="displayName">The name a console shows for the layer.</param>
    /// <param name="description">What the layer is for, in a sentence a person choosing it reads as a hint.</param>
    protected RuleLayerBase(string name, string displayName, string description)
        : base(name, displayName, description, null)
    {
    }

    /// <inheritdoc />
    public virtual IReadOnlyList<IRuleLayer> Layers => [];
}

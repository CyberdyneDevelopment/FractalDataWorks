using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Operations.Abstractions.Thresholds;

/// <summary>Base class for the options of <see cref="ThresholdTypes"/> (FDW-3936), using the CRTP pattern.</summary>
public abstract class ThresholdTypeBase : TypeOptionBase<ThresholdTypeBase>, IThreshold
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdTypeBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What the threshold judges, as the console shows it.</param>
    protected ThresholdTypeBase(string name, string description)
        : base(name, name, description, "ThresholdTypes")
    {
    }

    /// <inheritdoc/>
    public abstract string SubjectKind { get; }

    /// <inheritdoc/>
    public abstract IReadOnlyList<ThresholdField> Fields { get; }

    /// <inheritdoc/>
    public virtual IReadOnlyList<ThresholdParameterDeclaration> Parameters => [];
}

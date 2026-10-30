using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.OptionTypes;

/// <summary>
/// Base class for the options of <see cref="TargetKinds"/>, using the CRTP pattern.
/// </summary>
public abstract class TargetKindBase : TypeOptionBase<TargetKindBase>, ITargetKind
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TargetKindBase"/> class.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="description">What a source of this kind reads, as the console shows it beside the option.</param>
    protected TargetKindBase(string name, string description) : base(name, name, description, "TargetKinds")
    {
    }

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> Needs => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> Refuses => [];

    /// <inheritdoc/>
    public virtual bool ForSources => true;

    /// <inheritdoc/>
    public virtual bool ForDestinations => true;

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> WritesNeed => [];

    /// <inheritdoc/>
    public virtual IReadOnlyList<string> WritesRefuse => [];
}

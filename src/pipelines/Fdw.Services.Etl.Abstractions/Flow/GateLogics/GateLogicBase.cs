using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.Flow;

/// <summary>The base of the options of <see cref="GateLogics"/>.</summary>
public abstract class GateLogicBase : TypeOptionBase<GateLogicBase>, IGateLogic
{
    /// <summary>Initializes a new instance of the <see cref="GateLogicBase"/> class.</summary>
    /// <param name="name">The logic's name.</param>
    /// <param name="description">What the logic answers.</param>
    /// <param name="minimumInputs">The fewest input lines.</param>
    /// <param name="maximumInputs">The most input lines; <see langword="null"/> for any number.</param>
    protected GateLogicBase(string name, string description, int minimumInputs, int? maximumInputs)
        : base(name, name, description, nameof(GateLogics))
    {
        MinimumInputs = minimumInputs;
        MaximumInputs = maximumInputs;
    }

    /// <inheritdoc />
    public int MinimumInputs { get; }

    /// <inheritdoc />
    public int? MaximumInputs { get; }

    /// <inheritdoc />
    public abstract bool Judge(IReadOnlyList<bool> inputs);
}

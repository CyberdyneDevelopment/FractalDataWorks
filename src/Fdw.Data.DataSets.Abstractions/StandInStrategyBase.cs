using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Base for the ways a sketched field's query can be answered without real data.</summary>
/// <remarks>
/// No id is passed — <see cref="TypeOptionBase{TBase}"/> derives one from the option's fully
/// qualified type name. Nothing persists that id; data.DataSetFieldStandIn carries no strategy-type
/// column at all -- which of the seven strategy tables has a row for a given stand-in IS the active
/// strategy, and this option's NAME is what names that table.
/// </remarks>
public abstract class StandInStrategyBase : TypeOptionBase<StandInStrategyBase>, IStandInStrategy
{
    /// <summary>Initializes a new instance of the <see cref="StandInStrategyBase"/> class.</summary>
    /// <remarks>Used only by the source generator's Empty sentinel — a real option always supplies its parameters.</remarks>
    protected StandInStrategyBase()
        : this(string.Empty, [])
    {
    }

    /// <summary>Initializes a new instance of the <see cref="StandInStrategyBase"/> class.</summary>
    /// <param name="name">The strategy name, which also names its own configuration table.</param>
    /// <param name="parameters">The parameters this strategy takes.</param>
    /// <param name="limitations">What's simplified about this strategy's value generation, or null.</param>
    protected StandInStrategyBase(string name, IReadOnlyList<StandInParameterSchema> parameters, string? limitations = null)
        : base(name)
    {
        Parameters = parameters;
        Limitations = limitations;
    }

    /// <summary>Gets the parameters this strategy takes, for a caller building a form from it.</summary>
    public IReadOnlyList<StandInParameterSchema> Parameters { get; }

    /// <summary>Gets what's simplified about this strategy's value generation, or null when there isn't one.</summary>
    public string? Limitations { get; }
}

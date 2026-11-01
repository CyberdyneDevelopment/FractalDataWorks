using System.Collections.Generic;
using Fdw.Collections;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>A way to answer a sketched field's query without real data.</summary>
public interface IStandInStrategy : ITypeOption<int, StandInStrategyBase>
{
    /// <summary>Gets the parameters this strategy takes, for a caller building a form from it.</summary>
    IReadOnlyList<StandInParameterSchema> Parameters { get; }

    /// <summary>Gets what's simplified about this strategy's value generation, or null when there isn't one.</summary>
    string? Limitations { get; }
}

using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>A state a field out of a Graph data set can be in, with its own test (FDW-5294).</summary>
public interface IGraphFieldState : ITypeOption<int, GraphFieldStateBase>
{
    /// <summary>Gets the order the states are tried in: the lowest precedence that applies is the field's state.</summary>
    int Precedence { get; }

    /// <summary>Gets whether this state applies to the field.</summary>
    /// <param name="field">The field out.</param>
    /// <param name="dataSetHasIntendedSource">Whether the field's data set states at least one intended source.</param>
    /// <returns>True when the field is in this state.</returns>
    bool Applies(GraphDataSetFieldConfiguration field, bool dataSetHasIntendedSource);
}

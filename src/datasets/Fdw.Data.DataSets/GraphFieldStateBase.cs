using Fdw.Collections;
using Fdw.Data.DataSets.Abstractions;

namespace Fdw.Data.DataSets;

/// <summary>Base for the states of a field out of a Graph data set.</summary>
/// <remarks>A state is worked out, never stored: it cannot disagree with the field, the intended sources and the notes it is worked out from.</remarks>
public abstract class GraphFieldStateBase : TypeOptionBase<GraphFieldStateBase>, IGraphFieldState
{
    /// <summary>Initializes a new instance of the <see cref="GraphFieldStateBase"/> class.</summary>
    /// <remarks>Used only by the source generator's Empty sentinel; a real option always supplies its parameters.</remarks>
    protected GraphFieldStateBase()
        : this(string.Empty, int.MaxValue)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="GraphFieldStateBase"/> class.</summary>
    /// <param name="name">The state name.</param>
    /// <param name="precedence">The order the states are tried in; the lowest that applies wins.</param>
    protected GraphFieldStateBase(string name, int precedence)
        : base(name)
    {
        Precedence = precedence;
    }

    /// <inheritdoc />
    public int Precedence { get; }

    /// <inheritdoc />
    public abstract bool Applies(GraphDataSetFieldConfiguration field, bool dataSetHasIntendedSource);
}

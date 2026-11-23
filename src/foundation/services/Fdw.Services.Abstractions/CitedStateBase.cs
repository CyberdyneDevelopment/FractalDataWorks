using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>Base of the <see cref="CitedStates"/> options.</summary>
public abstract class CitedStateBase : TypeOptionBase<CitedStateBase>, ICitedState
{
    /// <summary>Initializes a new instance of the <see cref="CitedStateBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The option's name.</param>
    protected CitedStateBase(int id, string name)
        : base(id, name)
    {
    }
}

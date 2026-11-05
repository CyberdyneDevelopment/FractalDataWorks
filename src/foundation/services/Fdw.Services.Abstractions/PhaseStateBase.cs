using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>Base of the <see cref="PhaseStates"/> options: each names a state and says what the lifecycle does with a phase in it.</summary>
public abstract class PhaseStateBase : TypeOptionBase<PhaseStateBase>, IPhaseState
{
    /// <summary>Initializes a new instance of the <see cref="PhaseStateBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The state's name.</param>
    /// <param name="hasRun">Whether a phase in this state has run.</param>
    /// <param name="isDeferred">Whether a phase in this state is claimed by a host.</param>
    protected PhaseStateBase(int id, string name, bool hasRun, bool isDeferred)
        : base(id, name)
    {
        HasRun = hasRun;
        IsDeferred = isDeferred;
    }

    /// <inheritdoc />
    public bool HasRun { get; }

    /// <inheritdoc />
    public bool IsDeferred { get; }
}

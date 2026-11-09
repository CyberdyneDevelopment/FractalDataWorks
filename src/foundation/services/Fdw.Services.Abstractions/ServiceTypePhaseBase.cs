using Fdw.Collections;

namespace Fdw.Services.Abstractions;

/// <summary>Base of the <see cref="ServiceTypePhases"/> options.</summary>
public abstract class ServiceTypePhaseBase : TypeOptionBase<ServiceTypePhaseBase>, IServiceTypePhase
{
    /// <summary>Initializes a new instance of the <see cref="ServiceTypePhaseBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The phase's name.</param>
    protected ServiceTypePhaseBase(int id, string name)
        : base(id, name)
    {
    }
}

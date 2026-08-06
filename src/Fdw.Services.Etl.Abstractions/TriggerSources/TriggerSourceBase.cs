using Fdw.Collections;

namespace Fdw.Services.Etl.Abstractions.TriggerSources;

/// <summary>
/// Base type for <see cref="ITriggerSource"/> members.
/// </summary>
public abstract class TriggerSourceBase : TypeOptionBase<int, TriggerSourceBase>, ITriggerSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TriggerSourceBase"/> class.
    /// </summary>
    /// <param name="id">The stable identifier for this source.</param>
    /// <param name="name">The name carried on the execution request.</param>
    protected TriggerSourceBase(int id, string name) : base(id, name)
    {
    }
}

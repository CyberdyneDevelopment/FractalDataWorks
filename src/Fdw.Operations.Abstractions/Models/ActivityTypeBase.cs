using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>Base class for activity types in the timeline.</summary>
public abstract class ActivityTypeBase : TypeOptionBase<int, ActivityTypeBase>, IActivityType
{
    /// <summary>Initializes a new instance of <see cref="ActivityTypeBase"/>.</summary>
    protected ActivityTypeBase(int id, string name) : base(id, name) { }
}

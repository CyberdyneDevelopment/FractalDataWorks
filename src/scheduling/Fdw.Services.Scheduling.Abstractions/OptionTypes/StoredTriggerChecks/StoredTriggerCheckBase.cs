using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>Base class for the checks a stored trigger's own validation makes of its configuration, by trigger type.</summary>
public abstract class StoredTriggerCheckBase : TypeOptionBase<StoredTriggerCheckBase>, IStoredTriggerCheck
{
    /// <summary>Initializes a new instance of the <see cref="StoredTriggerCheckBase"/> class.</summary>
    /// <param name="name">The option name, the trigger type's name.</param>
    protected StoredTriggerCheckBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IGenericResult Check(IReadOnlyDictionary<string, object> configuration);
}

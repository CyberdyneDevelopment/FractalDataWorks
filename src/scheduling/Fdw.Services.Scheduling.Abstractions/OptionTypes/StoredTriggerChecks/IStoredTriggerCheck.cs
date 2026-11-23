using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;

namespace Fdw.Services.Scheduling.Abstractions.OptionTypes;

/// <summary>The check a stored trigger's own validation makes of its configuration, by trigger type.</summary>
public interface IStoredTriggerCheck : ITypeOption<int, StoredTriggerCheckBase>
{
    /// <summary>Checks the configuration of a trigger of this type.</summary>
    /// <param name="configuration">The trigger's configuration.</param>
    /// <returns>Success, or the failure that names what the configuration lacks.</returns>
    IGenericResult Check(IReadOnlyDictionary<string, object> configuration);
}

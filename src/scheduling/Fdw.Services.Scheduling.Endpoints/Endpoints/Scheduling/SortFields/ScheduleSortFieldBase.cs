using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Services.Scheduling.Abstractions.Configuration;

namespace Fdw.Services.Scheduling.Endpoints;

/// <summary>Base class for the columns a schedule list can be sorted by.</summary>
public abstract class ScheduleSortFieldBase : TypeOptionBase<ScheduleSortFieldBase>, IScheduleSortField
{
    /// <summary>Initializes a new instance of the <see cref="ScheduleSortFieldBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="key">The lower-case word a request names the column by.</param>
    protected ScheduleSortFieldBase(string name, string key)
        : base(name)
    {
        Key = key;
    }

    /// <inheritdoc />
    [TypeLookup("ByKey")]
    public string Key { get; }

    /// <inheritdoc />
    public abstract IEnumerable<IScheduleImplementationConfiguration> Order(IEnumerable<IScheduleImplementationConfiguration> schedules, bool descending);
}

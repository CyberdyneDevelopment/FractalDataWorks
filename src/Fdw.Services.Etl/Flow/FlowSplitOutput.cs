using System;
using System.Collections.Generic;

namespace Fdw.Services.Etl.Flow;

/// <summary>
/// One filtered output node of a split: its name and the filter a row must meet to be sent through it (FDW-1386).
/// </summary>
public sealed class FlowSplitOutput
{
    /// <summary>Initializes a new instance of the <see cref="FlowSplitOutput"/> class.</summary>
    /// <param name="name">The output node's name.</param>
    /// <param name="filter">The filter; true sends the row through this output.</param>
    public FlowSplitOutput(string name, Func<IDictionary<string, object?>, bool> filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
        Filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    /// <summary>Gets the output node's name.</summary>
    public string Name { get; }

    /// <summary>Gets the filter.</summary>
    public Func<IDictionary<string, object?>, bool> Filter { get; }
}

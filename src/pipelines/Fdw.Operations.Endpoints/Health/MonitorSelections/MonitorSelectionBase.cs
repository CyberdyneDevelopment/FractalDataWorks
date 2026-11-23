using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>Base class for the ways the HealthMonitor domain can answer which monitor a host reports to.</summary>
public abstract class MonitorSelectionBase : TypeOptionBase<MonitorSelectionBase>, IMonitorSelection
{
    /// <summary>Initializes a new instance of the <see cref="MonitorSelectionBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    protected MonitorSelectionBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Matches(int count);

    /// <inheritdoc />
    public abstract IGenericResult<string> Answer(IReadOnlyList<string> names, ILogger logger);
}

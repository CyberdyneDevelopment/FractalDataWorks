using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>A way the HealthMonitor domain can answer which monitor a host reports to: it names none, one or several.</summary>
public interface IMonitorSelection : ITypeOption<int, MonitorSelectionBase>
{
    /// <summary>Tells whether this is the answer for a domain that names this many monitors.</summary>
    /// <param name="count">How many monitors the domain names.</param>
    /// <returns><c>true</c> when it is.</returns>
    bool Matches(int count);

    /// <summary>Answers which monitor the host reports to.</summary>
    /// <param name="names">The names of the monitors the domain names.</param>
    /// <param name="logger">The logger the coded failure is written through.</param>
    /// <returns>The monitor's name, or the coded failure that says the domain names none or several.</returns>
    IGenericResult<string> Answer(IReadOnlyList<string> names, ILogger logger);
}

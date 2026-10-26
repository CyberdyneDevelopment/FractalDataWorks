using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// The answer to a question about several alerts (FDW-2420).
/// </summary>
public sealed class AlertList
{
    /// <summary>Gets or sets what each alert found allows the caller, in the order asked.</summary>
    public IReadOnlyList<AlertSummary> Items { get; set; } = [];

    /// <summary>Gets or sets the alerts asked about that do not exist or that the caller may not see.</summary>
    public IReadOnlyList<AlertKey> Missing { get; set; } = [];
}

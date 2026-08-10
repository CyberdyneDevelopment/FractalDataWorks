using System;
using Fdw.Web.Endpoints.Contracts;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// Summary DTO for a connection, used in list views.
/// </summary>
public class ConnectionSummaryDto : ResourceSummary
{
    /// <summary>Gets or sets the unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the connection type name.</summary>
    public required string ConnectionType { get; set; }

    /// <summary>Gets or sets the timestamp of the last connection test, or null if never tested.</summary>
    public DateTimeOffset? LastTestedAt { get; set; }

    // Why: tri-state on the wire, not a collapsed bool — a connection that has never been tested
    // (null) is neither Healthy nor Unhealthy; collapsing it to false previously rendered every
    // never-tested connection as a permanent "Unhealthy" badge (see FDW-559 investigation).
    /// <summary>Gets or sets whether the last connection test succeeded, false if it failed, or null if never tested.</summary>
    public bool? LastTestSuccess { get; set; }
}

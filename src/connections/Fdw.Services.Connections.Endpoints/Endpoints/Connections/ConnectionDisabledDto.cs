using System;
using Fdw.Services.Connections.Abstractions;

namespace Fdw.Services.Connections.Endpoints;

/// <summary>
/// The state of a connection that is disabled (FDW-4290): since when, why and by whom. A connection that is not disabled carries none.
/// </summary>
/// <remarks>
/// The enable that reversed an earlier disable is history in <c>conn.ConnectionDisabled</c> and is not shown: a connection enabled again reads as not disabled.
/// </remarks>
public sealed class ConnectionDisabledDto
{
    /// <summary>Gets or sets when the connection was disabled.</summary>
    public DateTimeOffset DisabledAt { get; set; }

    /// <summary>Gets or sets why, in the words a person reads: the threshold that fired, or what an administrator wrote.</summary>
    public required string Reason { get; set; }

    /// <summary>Gets or sets who or what disabled it: the threshold firing, or the person.</summary>
    public required string DisabledBy { get; set; }

    /// <summary>Maps the stored state of a disabled connection.</summary>
    /// <param name="record">The state.</param>
    /// <returns>The state as the endpoints show it.</returns>
    public static ConnectionDisabledDto Of(ConnectionDisabledRecord record)
        => new() { DisabledAt = record.DisabledAt, Reason = record.Reason, DisabledBy = record.DisabledBy };
}

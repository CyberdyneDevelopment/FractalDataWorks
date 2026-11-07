using System;

namespace Fdw.Services.Connections.Abstractions;

/// <summary>
/// A connection that is disabled (FDW-3951): which one, when it was disabled, why, and by whom.
/// </summary>
/// <param name="ConnectionId">The Id of the connection's domain row.</param>
/// <param name="DisabledAt">When the connection was disabled.</param>
/// <param name="Reason">Why, in the words a person reads: the threshold that fired, or what an administrator wrote.</param>
/// <param name="DisabledBy">Who or what did it: the threshold firing, or the person.</param>
public sealed record ConnectionDisabledRecord(Guid ConnectionId, DateTimeOffset DisabledAt, string Reason, string DisabledBy);

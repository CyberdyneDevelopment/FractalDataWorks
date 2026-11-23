using System;

namespace Fdw.Services.Etl.Execution.Claims;

/// <summary>A hold on a key that stands (FDW-6939): who placed it, when, and why.</summary>
/// <param name="HeldBy">The durable id of the person who placed the hold.</param>
/// <param name="HeldAt">When the hold was placed, on the database's clock.</param>
/// <param name="Reason">Why the key is held, as the person stated it.</param>
public sealed record PipelineKeyHold(Guid HeldBy, DateTimeOffset HeldAt, string Reason);

using System.Collections.Generic;

namespace Fdw.Services.Etl;

/// <summary>
/// A record a pipeline stage rejected (FDW-1142): the record, the name of the stage that rejected it and why. The
/// pipeline's reject steps write it after the load.
/// </summary>
/// <param name="Stage">The name of the stage that rejected the record.</param>
/// <param name="Reason">Why the stage rejected it.</param>
/// <param name="Record">The rejected record, as the stage received it.</param>
public sealed record RejectedRecord(string Stage, string Reason, IDictionary<string, object?> Record);

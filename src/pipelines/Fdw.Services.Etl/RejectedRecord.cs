using System.Collections.Generic;

namespace Fdw.Services.Etl;

/// <summary>
/// A record a pipeline component rejected (FDW-1142): the record, the name of the component that rejected it and why. The
/// pipeline's reject steps write it after the load.
/// </summary>
/// <param name="Component">The name of the component that rejected the record.</param>
/// <param name="Reason">Why the component rejected it.</param>
/// <param name="Record">The rejected record, as the component received it.</param>
public sealed record RejectedRecord(string Component, string Reason, IDictionary<string, object?> Record);

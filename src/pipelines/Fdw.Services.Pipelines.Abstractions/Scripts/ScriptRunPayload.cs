using System;
using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>What a run of a stored script is given: the values of its parameters and, for a query, the most rows to read.</summary>
public class ScriptRunPayload
{
    /// <summary>Gets or sets the value of each parameter by name, as text; the declared type reads it.</summary>
    public IDictionary<string, string?> Values { get; set; } = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets or sets the most rows a query may read. A query run states it; a command run leaves it out.</summary>
    public int? MaxRows { get; set; }
}

using System.Collections.Generic;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A value an Execute pipeline, Execute SQL or Set variable component gives a named parameter (FDW-1403, FDW-2041).</summary>
public class GraphComponentParameterPayload
{
    /// <summary>Gets or sets the parameter's name, as the started pipeline declares it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text; it may name a variable in double braces.</summary>
    public string Value { get; set; } = string.Empty;
}

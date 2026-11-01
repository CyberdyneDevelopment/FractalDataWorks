using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One thing wrong with a script draft, with the part of the draft it concerns.</summary>
public class ScriptProblemPayload
{
    /// <summary>Gets or sets the path to the part of the draft, such as <c>parameters</c> then the parameter's name.</summary>
    public IList<string> Path { get; set; } = [];

    /// <summary>Gets or sets what is wrong.</summary>
    public string Reason { get; set; } = string.Empty;
}

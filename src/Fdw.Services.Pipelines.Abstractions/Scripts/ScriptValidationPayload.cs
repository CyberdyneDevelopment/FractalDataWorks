using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The answer to checking a script draft: every problem saving it would be refused for.</summary>
public class ScriptValidationPayload
{
    /// <summary>Gets or sets whether saving the draft would be accepted.</summary>
    public bool IsValid { get; set; }

    /// <summary>Gets or sets the problems; empty when the draft is valid.</summary>
    public IList<ScriptProblemPayload> Problems { get; set; } = [];
}

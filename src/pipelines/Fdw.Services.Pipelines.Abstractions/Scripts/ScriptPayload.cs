using System.Collections.Generic;

namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A stored script as the API writes and reads it.</summary>
public class ScriptPayload
{
    /// <summary>Gets or sets the script's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the script is for.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the name of the data store it runs on.</summary>
    public string Connection { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind: Query selects rows, NonQuery changes data.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the text, with its values as named parameters such as <c>@Year</c>.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameters the text takes.</summary>
    public IList<ScriptParameterPayload> Parameters { get; set; } = [];
}

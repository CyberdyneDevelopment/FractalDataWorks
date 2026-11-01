using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Request to run a stored script: its name from the route and the run's values from the body.</summary>
public class ScriptRunRequest : ScriptRunPayload
{
    /// <summary>Gets or sets the script's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}

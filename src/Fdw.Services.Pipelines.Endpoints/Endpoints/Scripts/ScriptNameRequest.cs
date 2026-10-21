namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>Request naming a stored script: the route of the endpoints that act on one.</summary>
public class ScriptNameRequest
{
    /// <summary>Gets or sets the script's name, from the route.</summary>
    public string Name { get; set; } = string.Empty;
}

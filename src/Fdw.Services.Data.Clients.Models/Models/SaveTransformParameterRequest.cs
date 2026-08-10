namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A single parameter value in a save transform request.
/// </summary>
public sealed class SaveTransformParameterRequest
{
    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter value.</summary>
    public string Value { get; set; } = string.Empty;
}

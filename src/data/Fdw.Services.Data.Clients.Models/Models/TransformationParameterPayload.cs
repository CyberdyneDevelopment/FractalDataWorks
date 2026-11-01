namespace Fdw.Services.Data.Clients.Models;

/// <summary>One parameter value of a field transformation, named as its operation declares it.</summary>
public sealed class TransformationParameterPayload
{
    /// <summary>Gets or sets the parameter name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the parameter value.</summary>
    public string Value { get; set; } = string.Empty;
}

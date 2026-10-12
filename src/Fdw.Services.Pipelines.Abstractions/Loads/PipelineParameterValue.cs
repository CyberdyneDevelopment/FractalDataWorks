namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A run parameter's value, as a trigger or a schedule supplies it; parsed by the declared type.</summary>
public sealed class PipelineParameterValue
{
    /// <summary>Gets or sets the declared parameter's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value as text.</summary>
    public string Value { get; set; } = string.Empty;
}

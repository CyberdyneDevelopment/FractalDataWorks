namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One single value a Compute stage hands to an input of the Built transformation it applies.</summary>
public sealed class PipelineComputeScalar
{
    /// <summary>Gets or sets the name of the input the transformation declares.</summary>
    public string Input { get; set; } = string.Empty;

    /// <summary>Gets or sets the value, written as text.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the value's type: Number, Flag or Text.</summary>
    public string ValueType { get; set; } = string.Empty;
}

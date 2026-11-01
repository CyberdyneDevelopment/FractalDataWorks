namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>One input of the Built transformation a Compute stage applies: which field of the record feeds it.</summary>
public sealed class PipelineComputeInput
{
    /// <summary>Gets or sets the name of the input the Built transformation declares.</summary>
    public string InputName { get; set; } = string.Empty;

    /// <summary>Gets or sets the field of the record that feeds the input.</summary>
    public string SourceField { get; set; } = string.Empty;
}

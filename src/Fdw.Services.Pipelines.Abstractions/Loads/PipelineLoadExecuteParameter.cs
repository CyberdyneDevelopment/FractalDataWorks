namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>A parameter an Execute step passes to its container, and the value source that supplies it.</summary>
public sealed class PipelineLoadExecuteParameter
{
    /// <summary>Gets or sets the parameter's name, a field of the container (with or without a leading @).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value source: Constant or CurrentTimestamp.</summary>
    public PipelineLoadValueSource? Source { get; set; }
}

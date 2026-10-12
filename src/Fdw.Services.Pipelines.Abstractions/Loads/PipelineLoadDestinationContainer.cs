namespace Fdw.Services.Pipelines.Clients.Abstractions;

/// <summary>The executable container an Execute step runs: a container the named data store already knows (contract v3.1).</summary>
public sealed class PipelineLoadDestinationContainer
{
    /// <summary>Gets or sets the data store, one of the destination data set's sources' stores.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the data store path the container sits under.</summary>
    public string PathName { get; set; } = string.Empty;

    /// <summary>Gets or sets the container's name.</summary>
    public string ContainerName { get; set; } = string.Empty;
}

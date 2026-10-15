namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline configuration that can write its rows into a data store container rather than a data set or a connection (FDW-1269).
/// </summary>
/// <remarks>
/// The destination is addressed exactly as a <c>DataStoreTarget</c> is: the store, the path within it and the container.
/// All three are set together or none is, and never together with a destination data set. It mirrors
/// <see cref="IDataStoreSourcedPipelineConfiguration"/> for the other end of the pipeline.
/// </remarks>
public interface IDataStoreDestinedPipelineConfiguration
{
    /// <summary>Gets the data store the pipeline writes to, or <see langword="null"/> when its destination is not a data store container.</summary>
    string? DestinationDataStoreName { get; }

    /// <summary>Gets the path within the destination data store, or <see langword="null"/> when its destination is not a data store container.</summary>
    string? DestinationDataStorePath { get; }

    /// <summary>Gets the container within that path, or <see langword="null"/> when its destination is not a data store container.</summary>
    string? DestinationContainerName { get; }
}

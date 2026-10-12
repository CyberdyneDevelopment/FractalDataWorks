namespace Fdw.Services.Etl;

/// <summary>
/// A pipeline configuration that can read its rows from a data store container rather than a data set (FDW-1103).
/// </summary>
/// <remarks>
/// The source is addressed exactly as a <c>DataStoreTarget</c> is: the store, the path within it and the container.
/// All three are set together or none is, and never together with a source data set. A pipeline engine that reads
/// only data sets does not implement this, so a reader asks for the capability instead of naming an engine.
/// </remarks>
public interface IDataStoreSourcedPipelineConfiguration
{
    /// <summary>Gets the data store the pipeline reads from, or <see langword="null"/> when its source is not a data store container.</summary>
    string? SourceDataStoreName { get; }

    /// <summary>Gets the path within the source data store, or <see langword="null"/> when its source is not a data store container.</summary>
    string? SourceDataStorePath { get; }

    /// <summary>Gets the container within that path, or <see langword="null"/> when its source is not a data store container.</summary>
    string? SourceContainerName { get; }
}

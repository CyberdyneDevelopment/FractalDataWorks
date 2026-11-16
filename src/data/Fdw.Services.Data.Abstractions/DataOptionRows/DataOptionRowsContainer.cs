namespace Fdw.Services.Data.Abstractions.DataOptionRows;

/// <summary>The data store container whose option rows a request is about (FDW-5664).</summary>
public sealed class DataOptionRowsContainer
{
    /// <summary>Gets the data store name.</summary>
    public required string DataStore { get; init; }

    /// <summary>Gets the path the container is under; its discovered fields are what a row's column is checked against.</summary>
    public required string Path { get; init; }

    /// <summary>Gets the container name.</summary>
    public required string Container { get; init; }
}

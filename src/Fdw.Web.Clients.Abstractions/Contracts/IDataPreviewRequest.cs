namespace Fdw.Web.Clients.Abstractions.Contracts;

/// <summary>
/// Abstraction for data preview requests used across Schema and Data domains.
/// </summary>
public interface IDataPreviewRequest
{
    /// <summary>Gets the optional DataSet name.</summary>
    string? DataSetName { get; }
    /// <summary>Gets the optional DataStore name.</summary>
    string? DataStoreName { get; }
    /// <summary>Gets the optional path name within the DataStore.</summary>
    string? PathName { get; }
    /// <summary>Gets the optional container name within the path.</summary>
    string? ContainerName { get; }
    /// <summary>Gets the maximum number of rows to return.</summary>
    int MaxRows { get; }
}

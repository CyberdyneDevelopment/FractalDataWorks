using System;

namespace Fdw.Web.Analytics.Clients.Models;

/// <summary>
/// Represents a data profile for a DataSet, containing row count and profiling metadata.
/// </summary>
public sealed class DataProfilePayload
{
    /// <summary>
    /// Gets or sets the unique identifier of the data profile.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the DataSet that was profiled.
    /// </summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of rows in the DataSet.
    /// </summary>
    public int RowCount { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the profile was generated.
    /// </summary>
    public DateTimeOffset ProfiledAt { get; set; }
}

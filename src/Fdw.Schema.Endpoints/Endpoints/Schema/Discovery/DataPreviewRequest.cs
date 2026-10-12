using System.Collections.Generic;

namespace Fdw.Schema.Endpoints.Discovery;

/// <summary>
/// Request for data preview.
/// </summary>
public class DataPreviewRequest
{
    /// <summary>
    /// Gets or sets the DataSet name (optional — use this OR DataStore+Path+Container).
    /// </summary>
    public string? DataSetName { get; set; }

    /// <summary>
    /// Gets or sets the DataStore name (if not using DataSet).
    /// </summary>
    public string? DataStoreName { get; set; }

    /// <summary>
    /// Gets or sets the path name within the DataStore (if not using DataSet).
    /// </summary>
    public string? PathName { get; set; }

    /// <summary>
    /// Gets or sets the container name within the path (if not using DataSet).
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Gets or sets the maximum rows to return. Default is 100.
    /// </summary>
    public int MaxRows { get; set; } = 100;

    /// <summary>
    /// Gets or sets the columns to include (null = all).
    /// </summary>
    public IList<string>? Columns { get; set; }
}

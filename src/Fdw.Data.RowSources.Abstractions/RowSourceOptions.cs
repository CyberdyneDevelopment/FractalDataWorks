namespace Fdw.Data.RowSources.Abstractions;

/// <summary>
/// Base options for row source configuration.
/// </summary>
public class RowSourceOptions
{
    /// <summary>
    /// Gets or sets the buffer size for streaming operations.
    /// Default is 16KB.
    /// </summary>
    public int BufferSize { get; set; } = 16 * 1024;

    /// <summary>
    /// Gets or sets the maximum row count to process.
    /// 0 or negative means unlimited.
    /// </summary>
    public long MaxRows { get; set; }

    /// <summary>
    /// Gets or sets whether to continue processing after row-level errors.
    /// Default is true (continue on error).
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of row errors before aborting.
    /// 0 means unlimited errors allowed.
    /// </summary>
    public int MaxRowErrors { get; set; }
}

using System;
using System.Collections.Generic;

namespace Fdw.Data.Transformers.Abstractions;

/// <summary>
/// Provides context and metadata for transformation operations.
/// </summary>
public sealed class TransformContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransformContext"/> class.
    /// </summary>
    public TransformContext()
    {
        Metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TransformContext"/> class with metadata.
    /// </summary>
    /// <param name="metadata">The metadata dictionary.</param>
    public TransformContext(IDictionary<string, object> metadata)
    {
        Metadata = new Dictionary<string, object>(metadata, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the source name for tracking.
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the connection type (SQL, REST, File, etc.).
    /// </summary>
    public string ConnectionType { get; set; } = string.Empty;

    /// <summary>
    /// Gets the metadata dictionary for storing contextual information.
    /// </summary>
    public IDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets or sets a value indicating whether to throw on transformation errors.
    /// If false, failed records are skipped and logged.
    /// </summary>
    public bool ThrowOnError { get; set; } = true;
}

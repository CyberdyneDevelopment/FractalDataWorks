using System.Collections.Generic;

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Editor-state payload for a DataSet source during in-place workbench composition.
/// </summary>
public sealed class DataSetSourceEditorPayload
{
    /// <summary>Gets or sets the source alias name.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the DataStore name this source draws from.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema/path within the DataStore.</summary>
    /// <remarks>
    /// Named PathValue and not Path: a member called Path shadows <see cref="System.IO.Path"/> inside
    /// the declaring type, so <c>Path.Combine(...)</c> there resolves to this value and fails to
    /// compile in a way that reads as nonsense.
    /// </remarks>
    public string PathValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the container (table) name within the path.</summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is the primary source.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Gets or sets the field mappings from this source to DataSet fields.</summary>
    public IReadOnlyList<DataSetFieldMappingPayload> FieldMappings { get; set; } = [];

    /// <summary>
    /// Gets or sets whether this source can be removed.
    /// The primary source can only be removed when it is the sole source.
    /// </summary>
    public bool CanRemove { get; set; }
}

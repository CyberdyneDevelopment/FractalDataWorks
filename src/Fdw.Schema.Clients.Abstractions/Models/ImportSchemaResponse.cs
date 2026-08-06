using System;
using System.Collections.Generic;

namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Response from a schema import operation via the API client.
/// </summary>
public sealed class ImportSchemaResponse
{
    /// <summary>Gets or sets whether the import was successful.</summary>
    public bool Success { get; set; }

    /// <summary>Gets or sets the DataStore name that was created or updated.</summary>
    public string DataStoreName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of schemas imported.</summary>
    public int SchemasImported { get; set; }

    /// <summary>Gets or sets the number of tables imported.</summary>
    public int TablesImported { get; set; }

    /// <summary>Gets or sets the number of views imported.</summary>
    public int ViewsImported { get; set; }

    /// <summary>Gets or sets the total number of columns imported.</summary>
    public int ColumnsImported { get; set; }

    /// <summary>Gets or sets the import timestamp.</summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets any warnings during import.</summary>
    public IList<string> Warnings { get; set; } = [];

    /// <summary>Gets or sets the error message if import failed.</summary>
    public string? ErrorMessage { get; set; }
}

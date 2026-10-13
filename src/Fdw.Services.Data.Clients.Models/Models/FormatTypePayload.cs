namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// Describes a file or serialization format from the FormatTypes TypeCollection (FDW-1315): the name a container states as its
/// <c>format</c>, the family that reads and writes it, and what it defines for itself.
/// </summary>
public sealed class FormatTypePayload
{
    /// <summary>Gets or sets the format name, the value a container states as its format (for example <c>Csv</c>).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the family: the name of the record source and writer that read and write this format. Csv, Tsv and
    /// Delimited share the family Delimited. A form shows the options of a family (a header row for Delimited and Xlsx).
    /// </summary>
    public string Family { get; set; } = string.Empty;

    /// <summary>Gets or sets the media type of the format.</summary>
    public string MediaType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the format is binary.</summary>
    public bool IsBinary { get; set; }

    /// <summary>Gets or sets whether the format can be streamed.</summary>
    public bool SupportsStreaming { get; set; }

    /// <summary>
    /// Gets or sets the extension a file of this format has, with its dot; empty when a container of this format is not a
    /// file (Tabular).
    /// </summary>
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the field separator this format defines (Csv a comma, Tsv a tab), or null when it defines none: the
    /// container of the plain Delimited format states its own.
    /// </summary>
    public string? Separator { get; set; }
}

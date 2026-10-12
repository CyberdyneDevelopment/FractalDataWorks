namespace Fdw.Schema.Clients.Models;

/// <summary>
/// Response containing generated DDL.
/// </summary>
public sealed class DdlResponse
{
    /// <summary>Gets or sets the generated DDL script.</summary>
    public string Ddl { get; set; } = string.Empty;
}

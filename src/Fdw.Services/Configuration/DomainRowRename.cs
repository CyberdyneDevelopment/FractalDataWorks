namespace Fdw.Services.Configuration;

/// <summary>
/// The one column a rename of a domain row writes. Narrower than the domain record on purpose: the rename touches the name and nothing
/// else on the row (FDW-3082).
/// </summary>
internal sealed class DomainRowRename
{
    /// <summary>Gets or sets the member's new name.</summary>
    public string Name { get; set; } = string.Empty;
}

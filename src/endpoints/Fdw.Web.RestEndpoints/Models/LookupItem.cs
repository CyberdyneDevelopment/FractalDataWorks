namespace Fdw.Web.RestEndpoints.Models;

/// <summary>
/// One member found by a lookup: what a picker needs to offer it and to refer to it.
/// </summary>
/// <remarks>
/// The members of the domain row, and no more: a lookup is answered from the domain table alone, so nothing that
/// hangs from the member is read to find it.
/// </remarks>
public sealed class LookupItem
{
    /// <summary>Gets or sets the member's identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the member's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the implementation the member is (its kind), as its domain row names it.</summary>
    public string? Implementation { get; set; }
}

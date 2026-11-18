namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A changeable member of a data option rule or of one of its values (FDW-6339): its payload name, its display name, which it belongs
/// to, and whether a change may clear it. The history's changes and a change body's <c>clear</c> name members by this name, so a
/// caller reads the names from here and holds no list of its own.
/// </summary>
public sealed class DataOptionMemberPayload
{
    /// <summary>Gets or sets the member's payload name, such as <c>description</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the member's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the member belongs to: <c>rule</c> or <c>value</c>.</summary>
    public string Of { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a change may clear the member (name it in <c>clear</c>).</summary>
    public bool Clearable { get; set; }
}

namespace Fdw.Services.Data.Clients.Models;

/// <summary>
/// A kind of data option value (FDW-6339): its name, its display name, and which member of a value holds its content, so a
/// caller shows the right input for each kind without naming any kind itself.
/// </summary>
public sealed class DataOptionValueKindPayload
{
    /// <summary>Gets or sets the kind's name (a DataOptionValueKinds name), as a value's <c>kind</c> states it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind's display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the member of a value that holds this kind's content: <c>value</c> or <c>parameter</c>.</summary>
    public string Member { get; set; } = string.Empty;
}

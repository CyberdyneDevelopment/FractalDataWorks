namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Information about a member.
/// </summary>
public sealed class MemberInfo
{
    /// <summary>
    /// Gets or sets the member name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the member kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the accessibility.
    /// </summary>
    public string Accessibility { get; set; } = string.Empty;
}
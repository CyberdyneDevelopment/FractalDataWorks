using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Formatting.Results;

/// <summary>
/// Data for sorted members.
/// </summary>
public sealed class SortedMembersData
{
    /// <summary>
    /// Gets or sets the type name.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of members.
    /// </summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// Gets or sets the list of sorted members.
    /// </summary>
    public IReadOnlyList<FormattedMemberInfo> SortedMembers { get; set; } = System.Array.Empty<FormattedMemberInfo>();
}
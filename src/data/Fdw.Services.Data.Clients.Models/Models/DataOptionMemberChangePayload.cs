
namespace Fdw.Services.Data.Clients.Models;

/// <summary>One member of a data option rule a change changed (FDW-6339).</summary>
public sealed class DataOptionMemberChangePayload
{
    /// <summary>Gets or sets the member's name.</summary>
    public string Member { get; set; } = string.Empty;

    /// <summary>Gets or sets the member's display name, as the limits' <c>members</c> declares it.</summary>
    public string MemberDisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the value before the change.</summary>
    public string? Old { get; set; }

    /// <summary>Gets or sets the value after the change.</summary>
    public string? New { get; set; }
}

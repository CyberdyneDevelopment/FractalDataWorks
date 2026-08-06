using System;

namespace Fdw.Services.Notifications.Endpoints;

/// <summary>
/// DTO for a notification condition on a rule.
/// </summary>
public sealed class NotificationConditionDto
{
    /// <summary>Gets or sets the condition unique identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the condition name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the condition type.</summary>
    public required string ConditionType { get; set; }

    /// <summary>Gets or sets the threshold value.</summary>
    public int? Threshold { get; set; }

    /// <summary>Gets or sets the field name.</summary>
    public string? Field { get; set; }

    /// <summary>Gets or sets the comparison operator.</summary>
    public string? Operator { get; set; }

    /// <summary>Gets or sets the comparison value.</summary>
    public string? Value { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets whether the condition is negated.</summary>
    public bool IsNegated { get; set; }
}

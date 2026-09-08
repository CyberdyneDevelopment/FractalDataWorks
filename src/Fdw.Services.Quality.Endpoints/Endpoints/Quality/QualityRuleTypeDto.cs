namespace Fdw.Services.Quality.Endpoints;

/// <summary>One rule type a caller may name.</summary>
public class QualityRuleTypeDto
{
    /// <summary>Gets or sets the exact value to send as <c>ruleType</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets what the rule asserts.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets whether this type needs a field to apply to.</summary>
    /// <remarks>
    /// False for the whole-data-set types, so a client can hide the field picker rather than
    /// offering something the rule cannot use.
    /// </remarks>
    public bool RequiresField { get; set; }

    /// <summary>Gets or sets whether this type applies to more than one field.</summary>
    public bool SupportsMultipleFields { get; set; }

    /// <summary>Gets or sets whether this type needs bounds, a pattern or an expression.</summary>
    public bool RequiresParameters { get; set; }
}

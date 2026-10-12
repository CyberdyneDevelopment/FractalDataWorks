using System;

namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Quality check result.
/// </summary>
public sealed class QualityCheckResultPayload
{
    /// <summary>Gets or sets the unique rule identifier.</summary>
    public Guid RuleId { get; set; }

    /// <summary>Gets or sets the rule name.</summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the rule passed.</summary>
    public bool Passed { get; set; }

    /// <summary>Gets or sets the result message.</summary>
    public string? Message { get; set; }
}

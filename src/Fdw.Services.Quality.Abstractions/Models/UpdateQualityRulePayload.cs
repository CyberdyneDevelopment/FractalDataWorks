namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Update quality rule request.
/// </summary>
public sealed class UpdateQualityRulePayload
{
    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule expression.</summary>
    public string Expression { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the rule is enabled.</summary>
    public bool IsEnabled { get; set; }
}

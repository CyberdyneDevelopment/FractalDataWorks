namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Create quality rule request.
/// </summary>
public sealed class CreateQualityRulePayload
{
    /// <summary>Gets or sets the rule name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the DataSet this rule applies to.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of rule.</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule expression.</summary>
    public string Expression { get; set; } = string.Empty;
}

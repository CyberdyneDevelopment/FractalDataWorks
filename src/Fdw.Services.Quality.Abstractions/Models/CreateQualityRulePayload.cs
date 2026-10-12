namespace Fdw.Services.Quality.Clients.Models;

/// <summary>
/// Create quality rule request.
/// </summary>
public sealed class CreateQualityRulePayload
{
    // No Name: quality.QualityRule has no Name column and the server contract takes none (FDW-734).

    /// <summary>Gets or sets the rule description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the DataSet this rule applies to.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the type of rule.</summary>
    public string RuleType { get; set; } = string.Empty;

    /// <summary>Gets or sets the rule expression.</summary>
    public string Expression { get; set; } = string.Empty;
}

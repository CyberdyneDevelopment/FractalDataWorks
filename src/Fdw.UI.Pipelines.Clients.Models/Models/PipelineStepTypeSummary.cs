namespace Fdw.UI.Pipelines.Clients.Models;

/// <summary>
/// Summary of a pipeline step type for UI pickers and dropdowns.
/// </summary>
public sealed class PipelineStepTypeSummary
{
    /// <summary>Gets or sets the unique step type name (e.g., "Extract", "Transform").</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this step type requires source configuration.</summary>
    public bool RequiresSourceConfig { get; set; }

    /// <summary>Gets or sets whether this step type requires transform configuration.</summary>
    public bool RequiresTransformConfig { get; set; }

    /// <summary>Gets or sets whether this step type requires target configuration.</summary>
    public bool RequiresTargetConfig { get; set; }

    /// <summary>Gets or sets whether this step type requires validation configuration.</summary>
    public bool RequiresValidationConfig { get; set; }

    /// <summary>Gets or sets whether this step type requires notification configuration.</summary>
    public bool RequiresNotificationConfig { get; set; }

    /// <summary>Gets or sets whether this step type requires a branch condition.</summary>
    public bool RequiresBranchCondition { get; set; }
}

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// One action a kind of alert offers the caller's company (FDW-2430).
/// </summary>
public class AlertKindActionResponse
{
    /// <summary>Gets or sets the action's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the wording a person sees.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the action does.</summary>
    public string Description { get; set; } = string.Empty;
}

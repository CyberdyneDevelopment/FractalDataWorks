namespace Fdw.Services.Messaging;

/// <summary>
/// One action a kind of alert offers the people of a company (FDW-2430).
/// </summary>
public sealed class AlertKindAction
{
    /// <summary>Gets or sets the action's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the wording a person sees.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the action does.</summary>
    public string Description { get; set; } = string.Empty;
}

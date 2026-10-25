namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// One length of time an action offers (FDW-2420).
/// </summary>
public class AlertChoiceResponse
{
    /// <summary>Gets or sets the name to send as the choice.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the wording a person sees.</summary>
    public string DisplayName { get; set; } = string.Empty;
}

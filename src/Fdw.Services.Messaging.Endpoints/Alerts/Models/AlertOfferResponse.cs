using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// An action an alert allows the caller (FDW-2420).
/// </summary>
public class AlertOfferResponse
{
    /// <summary>Gets or sets the action's name, which is sent to take it.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the wording a person sees.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets what the action does.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the action is taken for everyone the alert is shown to.</summary>
    public bool ForEveryone { get; set; }

    /// <summary>Gets or sets a value indicating whether the action is in force now: taken, and for a timed one not yet over.</summary>
    public bool Done { get; set; }

    /// <summary>Gets or sets a value indicating whether a choice must be sent with the action.</summary>
    public bool NeedsChoice { get; set; }

    /// <summary>Gets or sets the lengths of time on offer; empty when the action takes none.</summary>
    public IReadOnlyList<AlertChoiceResponse> Choices { get; set; } = [];

    /// <summary>
    /// Makes the response for an offer.
    /// </summary>
    /// <param name="offer">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertOfferResponse Of(AlertActionOffer offer) => new()
    {
        Name = offer.Name,
        DisplayName = offer.DisplayName,
        Description = offer.Description,
        ForEveryone = offer.ForEveryone,
        Done = offer.Done,
        NeedsChoice = offer.NeedsChoice,
        Choices = [.. offer.Choices.Select(choice => new AlertChoiceResponse { Name = choice.Name, DisplayName = choice.DisplayName })]
    };
}

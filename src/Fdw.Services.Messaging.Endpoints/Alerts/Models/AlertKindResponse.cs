using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// A kind of alert with the actions it offers by default and the actions it offers the caller's company (FDW-2430).
/// </summary>
public class AlertKindResponse
{
    /// <summary>Gets or sets the name of the kind.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the names of the actions the kind offers when a company has chosen none.</summary>
    public IReadOnlyList<string> DefaultActions { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the caller's company has chosen its own list for the kind.</summary>
    public bool Configured { get; set; }

    /// <summary>Gets or sets the actions the kind offers the caller's company, in order.</summary>
    public IReadOnlyList<AlertKindActionResponse> Actions { get; set; } = [];

    /// <summary>Gets or sets every action the kind can be given: its own first, in its order, then the other declared actions; a company's list is chosen among these.</summary>
    public IReadOnlyList<AlertKindActionResponse> Available { get; set; } = [];

    /// <summary>
    /// Makes the response for a kind.
    /// </summary>
    /// <param name="summary">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertKindResponse Of(AlertKindSummary summary) => new()
    {
        Kind = summary.Kind,
        DefaultActions = summary.DefaultActions,
        Configured = summary.Configured,
        Actions = [.. summary.Actions.Select(Described)],
        Available = [.. summary.Available.Select(Described)]
    };

    private static AlertKindActionResponse Described(AlertKindAction action)
        => new() { Name = action.Name, DisplayName = action.DisplayName, Description = action.Description };
}

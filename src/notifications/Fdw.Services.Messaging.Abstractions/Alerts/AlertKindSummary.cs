using System.Collections.Generic;

namespace Fdw.Services.Messaging;

/// <summary>
/// A kind of alert, with the actions it offers by default and the actions it offers the caller's company (FDW-2430).
/// </summary>
public sealed class AlertKindSummary
{
    /// <summary>Gets or sets the name of the kind.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the names of the actions the kind offers when a company has chosen none.</summary>
    public IReadOnlyList<string> DefaultActions { get; set; } = [];

    /// <summary>Gets or sets a value indicating whether the caller's company has chosen its own list for the kind.</summary>
    public bool Configured { get; set; }

    /// <summary>Gets or sets the actions the kind offers the caller's company, in order.</summary>
    public IReadOnlyList<AlertKindAction> Actions { get; set; } = [];

    /// <summary>
    /// Gets or sets every action the kind can be given: the kind's own actions first, in its order, then the other declared actions in the order of
    /// <see cref="AlertActions"/>. An administrator choosing a company's list picks among these.
    /// </summary>
    public IReadOnlyList<AlertKindAction> Available { get; set; } = [];
}

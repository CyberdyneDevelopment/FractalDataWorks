using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// Every kind of alert with what it offers the caller's company (FDW-2430).
/// </summary>
public class AlertKindListResponse
{
    /// <summary>Gets or sets the kinds, by name.</summary>
    public IReadOnlyList<AlertKindResponse> Kinds { get; set; } = [];

    /// <summary>
    /// Makes the response for a list of kinds.
    /// </summary>
    /// <param name="kinds">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertKindListResponse Of(IReadOnlyList<AlertKindSummary> kinds) => new() { Kinds = [.. kinds.Select(AlertKindResponse.Of)] };
}

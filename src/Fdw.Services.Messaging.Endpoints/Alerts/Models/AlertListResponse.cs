using System.Collections.Generic;
using System.Linq;

namespace Fdw.Services.Messaging.Endpoints.Models;

/// <summary>
/// The answer to a question about several alerts (FDW-2420).
/// </summary>
public class AlertListResponse
{
    /// <summary>Gets or sets what each alert found allows the caller, in the order asked.</summary>
    public IReadOnlyList<AlertResponse> Items { get; set; } = [];

    /// <summary>Gets or sets the alerts asked about that do not exist or that the caller may not see, each written <c>kind:id</c>.</summary>
    public IReadOnlyList<string> Missing { get; set; } = [];

    /// <summary>
    /// Makes the response for a list.
    /// </summary>
    /// <param name="list">What the service answered.</param>
    /// <returns>The response.</returns>
    public static AlertListResponse Of(AlertList list) => new()
    {
        Items = [.. list.Items.Select(AlertResponse.Of)],
        Missing = [.. list.Missing.Select(key => $"{key.Kind}:{key.Id:D}")]
    };
}

using System.Text.Json;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Turns the JSON text a home page is held as into the element a response carries (FDW-2325).
/// </summary>
internal static class HomePageJson
{
    internal static JsonElement? Of(string? body)
    {
        if (body is null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }

    internal static HomePageResponse Of(HomePageSet set) => new(Of(set.Published), Of(set.Draft));
}

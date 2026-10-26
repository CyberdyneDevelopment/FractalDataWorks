using System.Text.Json;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The two home pages a scope holds (FDW-2325). Both are always present in the response; each is null when the scope holds none.
/// </summary>
/// <param name="Published">The published page, or null.</param>
/// <param name="Draft">The draft, or null.</param>
public sealed record HomePageResponse(JsonElement? Published, JsonElement? Draft);

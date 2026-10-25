namespace Fdw.Services.Settings;

/// <summary>
/// The two pages a scope holds: the published page and the draft, each the page's JSON or null when the scope holds none (FDW-2325).
/// </summary>
/// <param name="Published">The published page, or null.</param>
/// <param name="Draft">The draft, or null.</param>
public sealed record HomePageSet(string? Published, string? Draft);

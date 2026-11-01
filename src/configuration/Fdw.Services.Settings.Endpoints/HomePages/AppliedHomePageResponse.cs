using System.Text.Json;

namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// The home page that applies to the caller and where it comes from (FDW-2325).
/// </summary>
/// <param name="Source">Where the page comes from.</param>
/// <param name="Page">The published page; null when no scope holds one.</param>
public sealed record AppliedHomePageResponse(HomePageSourceResponse Source, JsonElement? Page);

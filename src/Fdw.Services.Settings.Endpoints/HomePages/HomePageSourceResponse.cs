namespace Fdw.Services.Settings.Endpoints;

/// <summary>
/// Where the home page that applies to a person comes from (FDW-2325).
/// </summary>
/// <param name="Kind">One of <see cref="HomePageKinds"/>: <c>Person</c>, <c>Organization</c>, <c>Tenant</c> or <c>None</c>.</param>
/// <param name="Name">The id of that person, organization or tenant as text; null for none.</param>
public sealed record HomePageSourceResponse(string Kind, string? Name);

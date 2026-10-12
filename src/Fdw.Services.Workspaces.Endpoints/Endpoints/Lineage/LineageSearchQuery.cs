namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>A search request that passed validation.</summary>
/// <param name="Text">The text to find in names.</param>
/// <param name="Kinds">The kinds to search.</param>
/// <param name="Take">How many matches one page holds.</param>
/// <param name="Skip">How many matches precede the page.</param>
/// <param name="StartsWith">Whether a name must start with the text, where it otherwise need only contain it.</param>
internal sealed record LineageSearchQuery(string Text, LineageKindSelection Kinds, int Take, int Skip = 0, bool StartsWith = false);

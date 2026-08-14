namespace Fdw.TUI.Management.Services;

/// <summary>
/// What a spec refresh and regeneration produced.
/// </summary>
/// <param name="Paths">How many paths the fetched document declares.</param>
/// <param name="Operations">How many operations across those paths.</param>
/// <param name="Requests">How many requests the regenerated collection holds.</param>
public sealed record NewmanRefresh(int Paths, int Operations, int Requests);

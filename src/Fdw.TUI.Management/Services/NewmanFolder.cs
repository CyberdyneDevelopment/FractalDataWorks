namespace Fdw.TUI.Management.Services;

/// <summary>
/// A folder in the generated collection — one API domain, and how many requests cover it.
/// </summary>
/// <param name="Name">The folder name, as it appears in the collection.</param>
/// <param name="RequestCount">How many requests the folder holds.</param>
public sealed record NewmanFolder(string Name, int RequestCount);

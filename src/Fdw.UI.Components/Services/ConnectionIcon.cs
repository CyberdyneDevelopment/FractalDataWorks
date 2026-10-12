namespace Fdw.UI.Components.Services;

/// <summary>
/// Represents icon metadata for a connection type.
/// </summary>
/// <param name="DisplayName">Human-readable connection type name.</param>
/// <param name="IconCategory">Semantic icon category (database, cloud, file, etc.).</param>
/// <param name="IconKey">Specific icon identifier for the connection type.</param>
public sealed record ConnectionIcon(string DisplayName, string IconCategory, string IconKey);

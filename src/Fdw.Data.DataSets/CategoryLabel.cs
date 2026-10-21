namespace Fdw.Data.DataSets.Abstractions;

/// <summary>How the category a thing sits under is answered: its stable name and the path of display names that leads to it.</summary>
/// <param name="Name">The category's stable name; null when the thing sits under none.</param>
/// <param name="Path">The display names from the top of the tree down, joined by " / "; null when the thing sits under none.</param>
public sealed record CategoryLabel(string? Name, string? Path);

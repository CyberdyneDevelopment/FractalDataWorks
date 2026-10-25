namespace Fdw.Services.Configuration;

/// <summary>A row a search found and how close it is to the text.</summary>
/// <param name="Row">The row.</param>
/// <param name="Rank">How close it is, by <see cref="Fdw.Services.Data.Abstractions.SearchRank"/>: 0 is closest.</param>
/// <param name="Key">The row's key, which settles the order of rows with the same name.</param>
public sealed record RankedRow(ConfigurationRow Row, int Rank, long Key);

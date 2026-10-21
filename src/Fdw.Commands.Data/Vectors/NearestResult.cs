namespace Fdw.Commands.Data;

/// <summary>
/// One row a nearest search answered with, and its distance from the vector searched for.
/// </summary>
/// <typeparam name="T">The type the row is read as.</typeparam>
public sealed class NearestResult<T>
{
    /// <summary>
    /// Gets the row.
    /// </summary>
    public required T Record { get; init; }

    /// <summary>
    /// Gets the row's distance from the vector searched for, by the search's metric: the smaller, the nearer.
    /// </summary>
    public required double Distance { get; init; }
}

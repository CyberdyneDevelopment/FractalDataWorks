using Fdw.Conventions;

namespace Fdw.Services.Data.ColumnValues;

/// <summary>
/// The page sizes of a column values search (FDW-5666), as the Main data gateway option declares them: the take a request
/// that states none gets, and the most a request may ask for.
/// </summary>
/// <remarks>
/// Declared on the option, which a host may change at registration; nothing in the service states a number. A request above
/// <see cref="MaximumTake"/> is refused, never cut.
/// </remarks>
public sealed class ColumnValuesPaging
{
    /// <summary>Initializes a new instance of the <see cref="ColumnValuesPaging"/> class.</summary>
    /// <param name="defaultTake">The take a request that states none gets.</param>
    /// <param name="maximumTake">The most values one page may hold.</param>
    public ColumnValuesPaging(int defaultTake, int maximumTake)
    {
        DefaultTake = defaultTake;
        MaximumTake = maximumTake;
    }

    /// <summary>Gets the take a request that states none gets.</summary>
    [PlatformDefault]
    public int DefaultTake { get; }

    /// <summary>Gets the most values one page may hold.</summary>
    public int MaximumTake { get; }

    /// <summary>Gets whether the settings make a page: both at least one, and the default no more than the maximum.</summary>
    public bool IsUsable => DefaultTake >= 1 && MaximumTake >= 1 && DefaultTake <= MaximumTake;
}

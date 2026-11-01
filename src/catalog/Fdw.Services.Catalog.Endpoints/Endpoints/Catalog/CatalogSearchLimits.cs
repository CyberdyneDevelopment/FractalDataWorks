namespace Fdw.Services.Catalog.Endpoints;

/// <summary>The bounds of a catalog search page, stated here and refused beyond (FDW-1681).</summary>
public static class CatalogSearchLimits
{
    /// <summary>How many entries come back when the caller names no count.</summary>
    public const int DefaultTake = 25;

    /// <summary>The most entries one call may ask for.</summary>
    public const int MaxTake = 100;

    /// <summary>The most matches a search reads from any one source: skip plus take may not pass it.</summary>
    /// <remarks>
    /// A page is merged from the closest matches each source returns, so a source reads skip plus take of them. This is the ceiling of the
    /// In operator that places the parents of a page's fields, which carries each key as a parameter of the statement.
    /// </remarks>
    public const int MaxWindow = 1000;
}

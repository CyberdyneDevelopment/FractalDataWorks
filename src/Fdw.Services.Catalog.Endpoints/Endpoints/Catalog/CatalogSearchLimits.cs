namespace Fdw.Services.Catalog.Endpoints;

/// <summary>The bounds of a catalog search page, stated here and refused beyond (FDW-1681).</summary>
public static class CatalogSearchLimits
{
    /// <summary>How many entries come back when the caller names no count.</summary>
    public const int DefaultTake = 25;

    /// <summary>The most entries one call may ask for.</summary>
    public const int MaxTake = 100;
}

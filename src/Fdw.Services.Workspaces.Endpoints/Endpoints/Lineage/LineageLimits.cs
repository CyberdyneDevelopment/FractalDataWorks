namespace Fdw.Services.Workspaces.Endpoints;

/// <summary>The bounds the lineage endpoints state in their responses and refuse beyond.</summary>
internal static class LineageLimits
{
    /// <summary>The most nodes one neighbourhood answers with; past it the answer says it was truncated.</summary>
    public const int MaxNodes = 500;

    /// <summary>The deepest a neighbourhood may walk.</summary>
    public const int MaxDepth = 5;

    /// <summary>The deepest a path search may go.</summary>
    public const int MaxPathDepth = 8;

    /// <summary>How many search hits come back when the caller names no count.</summary>
    public const int DefaultSearchTake = 20;

    /// <summary>The most search hits one call may ask for.</summary>
    public const int MaxSearchTake = 100;
}

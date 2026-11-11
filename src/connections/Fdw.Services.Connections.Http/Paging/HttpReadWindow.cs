namespace Fdw.Services.Connections.Http.Paging;

/// <summary>The rows of a set one read returns (FDW-4938): every row, or the window a query asks for with its paging.</summary>
/// <param name="Skip">The rows at the start of the set the read skips.</param>
/// <param name="Take">The most rows the read returns; null for every row after the skipped ones.</param>
public sealed record HttpReadWindow(int Skip, int? Take)
{
    /// <summary>Gets the window of a read of the complete set.</summary>
    public static HttpReadWindow All { get; } = new(0, null);
}

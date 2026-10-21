namespace Fdw.Services.Messaging;

/// <summary>
/// A list of UI comments after the service has checked the query it was asked for: the conditions the store narrows by, and the two the store cannot
/// (FDW-2143).
/// </summary>
/// <param name="Filter">What the store narrows by.</param>
/// <param name="Tag">The tag every comment must carry, or <see langword="null"/>.</param>
/// <param name="ByUrgency">Whether the most urgent comes first.</param>
/// <param name="Skip">The number of comments to skip.</param>
/// <param name="Take">The number of comments to take.</param>
internal readonly record struct UiFeedbackListRequest(UiFeedbackFilter Filter, string? Tag, bool ByUrgency, int Skip, int Take)
{
    /// <summary>Gets a value indicating whether the list needs more than the store's own order and paging: a tag or an urgency order.</summary>
    public bool NeedsWindow => Tag is not null || ByUrgency;
}

namespace Fdw.Services.Messaging;

/// <summary>
/// What a list of UI comments is narrowed and ordered by (FDW-2143). Which companies' comments are reached is not stated here: it follows from who asks.
/// </summary>
/// <remarks>
/// The store cannot filter by tag or order by an urgency's rank, so when <see cref="Tag"/> or <see cref="SortByUrgency"/> is stated the list looks at the 1000
/// newest comments that match the other conditions, and pages over those.
/// </remarks>
public sealed class UiFeedbackQuery
{
    /// <summary>Gets or sets the state to list, Open or Resolved; none lists both.</summary>
    public string? State { get; set; }

    /// <summary>Gets or sets the route pattern to list the comments of; none lists every screen.</summary>
    public string? Route { get; set; }

    /// <summary>Gets or sets the urgency to list, an option of UiFeedbackUrgencies; none lists every urgency.</summary>
    public string? Urgency { get; set; }

    /// <summary>Gets or sets the tag to list, an option of UiFeedbackTags; none lists every tag.</summary>
    public string? Tag { get; set; }

    /// <summary>Gets or sets whether the most urgent comes first, by the rank the urgency states now; otherwise the newest comes first.</summary>
    public bool SortByUrgency { get; set; }

    /// <summary>Gets or sets the number of comments to skip.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the number of comments to take; from 1 to <see cref="UiFeedbackLimits.MaxPageSize"/>. Defaults to 50.</summary>
    public int Take { get; set; } = 50;
}

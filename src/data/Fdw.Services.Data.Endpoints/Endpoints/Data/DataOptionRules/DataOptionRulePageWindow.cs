namespace Fdw.Services.Data.Endpoints;

/// <summary>The window of a list the request asks for (FDW-6489): the rows to skip, the most to take, and the page number the response states.</summary>
/// <param name="Skip">How many rows precede the page.</param>
/// <param name="Take">How many rows the page holds at most, from the pagination settings.</param>
/// <param name="Page">The page number, counted from one.</param>
public sealed record DataOptionRulePageWindow(int Skip, int Take, int Page);

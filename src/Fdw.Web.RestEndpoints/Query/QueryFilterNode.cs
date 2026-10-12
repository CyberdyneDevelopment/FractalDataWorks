using System.Collections.Generic;
using System.Text.Json;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>One node of a query's filter tree: either a group or a condition, never both.</summary>
/// <remarks>
/// A group sets <see cref="Logic"/> and <see cref="Nodes"/>. A condition sets <see cref="Field"/>,
/// <see cref="Operator"/> and, for every operator except <c>IsNull</c>/<c>IsNotNull</c>,
/// <see cref="Value"/>; or, on a date field, <see cref="Field"/> with <see cref="StartDate"/> and/or <see cref="EndDate"/>.
/// A node that sets both halves, or neither, is refused rather than read as
/// whichever half happens to be present.
/// </remarks>
public sealed class QueryFilterNode
{
    /// <summary>Gets or sets how a group combines its nodes: <c>And</c> or <c>Or</c>.</summary>
    public string? Logic { get; set; }

    /// <summary>Gets or sets a group's child nodes.</summary>
    public IReadOnlyList<QueryFilterNode?>? Nodes { get; set; }

    /// <summary>Gets or sets a condition's field — a field name the resource declares.</summary>
    public string? Field { get; set; }

    /// <summary>
    /// Gets or sets a condition's operator: <c>Equal</c>, <c>NotEqual</c>, <c>GreaterThan</c>,
    /// <c>GreaterThanOrEqual</c>, <c>LessThan</c>, <c>LessThanOrEqual</c>, <c>Contains</c>,
    /// <c>StartsWith</c>, <c>EndsWith</c>, <c>In</c>, <c>NotIn</c>, <c>Between</c>, <c>IsNull</c> or <c>IsNotNull</c>.
    /// </summary>
    /// <remarks>
    /// <c>In</c> takes a list of values; <c>NotIn</c> takes a list of strings or whole numbers. <c>Between</c> is for number
    /// fields and takes <c>{"start": .., "stop": ..}</c>, both ends inclusive. A date field has no operator for a range: its
    /// condition sets <see cref="StartDate"/> and <see cref="EndDate"/> instead.
    /// </remarks>
    public string? Operator { get; set; }

    /// <summary>
    /// Gets or sets a date range's first day or moment, on a condition over a <c>DateTime</c> or <c>DateTimeOffset</c> field,
    /// in place of <see cref="Operator"/> and <see cref="Value"/>.
    /// </summary>
    public JsonElement? StartDate { get; set; }

    /// <summary>
    /// Gets or sets a date range's last day or moment, on the same kind of condition as <see cref="StartDate"/>. Either
    /// bound may stand alone. An end date written with no time keeps the whole of that day.
    /// </summary>
    public JsonElement? EndDate { get; set; }

    /// <summary>
    /// Gets or sets a condition's value: a string, number or boolean; an array of them for <c>In</c> and <c>NotIn</c>;
    /// an object with the range's two bounds for <c>Between</c>.
    /// </summary>
    public JsonElement? Value { get; set; }
}

namespace Fdw.Services.Data.Clients.Models;

/// <summary>One result a Built transformation produces: an exact decimal number, or a date-time.</summary>
public sealed class TransformationOutputPayload
{
    /// <summary>Gets or sets the result name; a kept result becomes a column of that name on each record a Compute stage handles.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value saying whether the result is written out; false for a working number only later results use.</summary>
    public bool IsKept { get; set; }

    /// <summary>
    /// Gets or sets the kind of value the result is: <c>Number</c> or <c>DateTime</c>. Null when the formula cannot be typed, for
    /// example a stored definition the calculation engine refuses.
    /// </summary>
    public string? ValueType { get; set; }

    /// <summary>
    /// Gets or sets the data type name a data set field of that kind states, <c>Decimal</c> or <c>DateTimeOffset</c>, so a client can
    /// match a result to a field. Null when <see cref="ValueType"/> is.
    /// </summary>
    public string? DataType { get; set; }
}

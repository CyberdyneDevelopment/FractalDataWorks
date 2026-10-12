namespace Fdw.Services.Data.Clients.Models;

/// <summary>One result a Built transformation produces. Every result is an exact decimal number.</summary>
public sealed class TransformationOutputPayload
{
    /// <summary>Gets or sets the result name; a kept result becomes a column of that name on each record a Compute stage handles.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value saying whether the result is written out; false for a working number only later results use.</summary>
    public bool IsKept { get; set; }
}

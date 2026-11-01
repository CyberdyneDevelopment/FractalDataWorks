namespace Fdw.Services.Data.Clients.Models;

/// <summary>One column a table input of a Built transformation declares: its name and the kind of value it holds.</summary>
public sealed class TableColumnPayload
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the value type: Number, Flag or Text.</summary>
    public string ValueType { get; set; } = string.Empty;
}

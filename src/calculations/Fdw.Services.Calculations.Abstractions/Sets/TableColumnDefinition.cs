namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>One column a table input declares: its name and the kind of value it holds.</summary>
/// <param name="Name">The column name.</param>
/// <param name="ValueType">Number, Flag or Text.</param>
public sealed record TableColumnDefinition(string Name, string ValueType);

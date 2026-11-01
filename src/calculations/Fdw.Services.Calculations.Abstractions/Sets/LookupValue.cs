using Fdw.Results;

namespace Fdw.Services.Calculations.Abstractions.Sets;

/// <summary>A value found in another table: the row whose key column equals one of the current row's values. No matching row is a refusal, never an empty or zero value.</summary>
public sealed class LookupValue : DerivedValueDefinition
{
    private readonly string _keyColumn;
    private readonly string _matchField;

    /// <summary>Initializes a new instance of the <see cref="LookupValue"/> class.</summary>
    /// <param name="name">The name formulas read the value under.</param>
    /// <param name="table">The table that is searched.</param>
    /// <param name="keyColumn">The column of that table that holds the key.</param>
    /// <param name="matchField">The name of the current row's value that is looked up.</param>
    /// <param name="valueColumn">The column of the found row whose value is taken.</param>
    public LookupValue(string name, string table, string keyColumn, string matchField, string valueColumn)
        : base(name, table)
    {
        _keyColumn = keyColumn;
        _matchField = matchField;
        ValueColumn = valueColumn;
    }

    /// <inheritdoc />
    public override string Kind => "Lookup";

    /// <inheritdoc />
    public override string Column => ValueColumn;

    /// <inheritdoc />
    public override string? KeyColumn => _keyColumn;

    /// <inheritdoc />
    public override string? MatchField => _matchField;

    /// <summary>Gets the column of the found row whose value is taken.</summary>
    public string ValueColumn { get; }

    /// <inheritdoc />
    public override IGenericResult<object?> Compute(string output, DriverRow driver, SetRunScope scope)
    {
        if (!driver.Values.TryGetValue(_matchField, out var key))
        {
            return SetFailure.Of<object?>("SetColumnMissing", output, $"the row has no value '{_matchField}' to look up in '{Table}'");
        }

        var index = scope.Index(output, Table, _keyColumn);
        if (index.IsFailure)
        {
            return index.ToNewResult<object?>();
        }

        if (!index.Value!.TryGetValue(SetRunScope.KeyText(key), out var found))
        {
            return SetFailure.Of<object?>("SetLookupKeyNotFound", output, $"'{Table}' has no row with {_keyColumn} '{SetRunScope.KeyText(key)}'");
        }

        return found.TryGetValue(ValueColumn, out var value)
            ? GenericResult<object?>.Success(value)
            : SetFailure.Of<object?>("SetColumnMissing", output, $"'{Table}' has a row without column '{ValueColumn}'");
    }
}

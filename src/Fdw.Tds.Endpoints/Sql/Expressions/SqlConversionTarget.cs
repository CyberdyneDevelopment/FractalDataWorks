using System;
using System.Globalization;
using Fdw.Protocols.Tds.Types;

namespace Fdw.Tds.Endpoints.Sql.Expressions;

/// <summary>
/// A SQL type a value is converted to — by CAST, CONVERT, a variable's declaration — with the rule that
/// converts to it.
/// </summary>
public sealed class SqlConversionTarget
{
    private readonly Func<object, SqlConversionTarget, object?> _convert;

    /// <summary>Initializes a new instance of the <see cref="SqlConversionTarget"/> class.</summary>
    /// <param name="sqlName">The type's T-SQL name.</param>
    /// <param name="dataType">The SQL type a value of it is written as.</param>
    /// <param name="length">Characters or bytes for a sized type, <see cref="TdsColumnType.Max"/>, or zero.</param>
    /// <param name="precision">The precision of a decimal.</param>
    /// <param name="scale">The scale of a decimal, or fractional-second digits.</param>
    /// <param name="convert">Converts a value that is not NULL; answers null when it does not convert.</param>
    public SqlConversionTarget(string sqlName, ISqlDataType dataType, int length, byte precision, byte scale, Func<object, SqlConversionTarget, object?> convert)
    {
        SqlName = sqlName ?? throw new ArgumentNullException(nameof(sqlName));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        Length = length;
        Precision = precision;
        Scale = scale;
        _convert = convert ?? throw new ArgumentNullException(nameof(convert));
    }

    /// <summary>Gets the type's T-SQL name.</summary>
    public string SqlName { get; }

    /// <summary>Gets the SQL type a value of it is written as.</summary>
    public ISqlDataType DataType { get; }

    /// <summary>Gets the length of a sized type.</summary>
    public int Length { get; }

    /// <summary>Gets the precision of a decimal.</summary>
    public byte Precision { get; }

    /// <summary>Gets the scale of a decimal, or fractional-second digits.</summary>
    public byte Scale { get; }

    /// <summary>Gets the column type a value of this target is written as.</summary>
    /// <returns>The column type.</returns>
    public TdsColumnType ColumnType() => new(DataType, Length, Precision, Scale);

    /// <summary>Gets the type as T-SQL declares it.</summary>
    /// <returns>The declaration, such as nvarchar(50).</returns>
    public override string ToString() => Length switch
    {
        TdsColumnType.Max => $"{SqlName}(max)",
        > 0 => string.Create(CultureInfo.InvariantCulture, $"{SqlName}({Length})"),
        _ => DataType.HasPrecision
            ? string.Create(CultureInfo.InvariantCulture, $"{SqlName}({Precision},{Scale})")
            : SqlName,
    };

    /// <summary>Converts a value to this type.</summary>
    /// <param name="value">The value; null for NULL, which converts to NULL.</param>
    /// <param name="converted">The converted value.</param>
    /// <returns>True when the value converts.</returns>
    public bool TryConvert(object? value, out object? converted)
    {
        if (value is null)
        {
            converted = null;
            return true;
        }

        converted = _convert(value, this);
        return converted is not null;
    }
}

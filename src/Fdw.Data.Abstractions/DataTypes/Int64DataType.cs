using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 64-bit signed integer values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>bigint</c>, PostgreSQL <c>int8</c>/<c>bigint</c>,
/// C# <see cref="long"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Int64")]
public sealed class Int64DataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int64DataType"/> class.</summary>
    public Int64DataType()
        : base(name: "Int64")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(long);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.WholeNumber(text, long.MinValue, long.MaxValue);
}

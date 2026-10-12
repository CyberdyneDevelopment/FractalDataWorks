using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 32-bit signed integer values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>int</c>, PostgreSQL <c>int4</c>/<c>integer</c>,
/// C# <see cref="int"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Int32")]
public sealed class Int32DataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int32DataType"/> class.</summary>
    public Int32DataType()
        : base(name: "Int32")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(int);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.WholeNumber(text, int.MinValue, int.MaxValue) is { } held ? (int)held : null;
}

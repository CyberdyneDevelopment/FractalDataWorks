using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 32-bit IEEE 754 floating-point values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>real</c>, PostgreSQL <c>float4</c>/<c>real</c>,
/// C# <see cref="float"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Float")]
public sealed class FloatDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatDataType"/> class.</summary>
    public FloatDataType()
        : base(name: "Float")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(float);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Double(text) is { } held && Math.Abs(held) <= float.MaxValue ? (float)held : null;
}

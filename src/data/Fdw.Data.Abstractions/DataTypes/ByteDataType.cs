using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 8-bit unsigned integer values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>tinyint</c>, C# <see cref="byte"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Byte")]
public sealed class ByteDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ByteDataType"/> class.</summary>
    public ByteDataType()
        : base(name: "Byte")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(byte);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.WholeNumber(text, byte.MinValue, byte.MaxValue) is { } held ? (byte)held : null;
}

using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for globally unique identifier (UUID) values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>uniqueidentifier</c>, PostgreSQL <c>uuid</c>,
/// C# <see cref="System.Guid"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Guid")]
public sealed class GuidDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="GuidDataType"/> class.</summary>
    public GuidDataType()
        : base(name: "Guid")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(Guid);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Identifier(text);
}

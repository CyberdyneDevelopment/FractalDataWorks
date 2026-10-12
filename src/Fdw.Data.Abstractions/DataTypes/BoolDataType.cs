using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for boolean (true/false) values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>bit</c>, PostgreSQL <c>boolean</c>,
/// C# <see cref="bool"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Bool")]
public sealed class BoolDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BoolDataType"/> class.</summary>
    public BoolDataType()
        : base(name: "Bool")
    {
    }

    /// <inheritdoc />
    public override Type ClrType => typeof(bool);

    /// <inheritdoc />
    public override object? Parse(string text) => DataValueReader.Boolean(text);

    /// <inheritdoc />
    public override string Format(object value) => (bool)value ? "true" : "false";
}

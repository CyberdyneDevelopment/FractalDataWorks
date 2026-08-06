using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for variable-length Unicode text values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>nvarchar</c>/<c>varchar</c>, PostgreSQL <c>text</c>/<c>varchar</c>,
/// C# <see cref="string"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "String")]
public sealed class StringDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="StringDataType"/> class.</summary>
    public StringDataType()
        : base(id: 5, name: "String")
    {
    }
}

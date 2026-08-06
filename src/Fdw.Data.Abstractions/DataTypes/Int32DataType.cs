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
        : base(id: 2, name: "Int32")
    {
    }
}

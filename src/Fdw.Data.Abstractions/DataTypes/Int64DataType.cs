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
        : base(id: 1, name: "Int64")
    {
    }
}

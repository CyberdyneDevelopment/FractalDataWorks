using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 16-bit signed integer values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>smallint</c>, PostgreSQL <c>int2</c>/<c>smallint</c>,
/// C# <see cref="short"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Int16")]
public sealed class Int16DataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="Int16DataType"/> class.</summary>
    public Int16DataType()
        : base(id: 3, name: "Int16")
    {
    }
}

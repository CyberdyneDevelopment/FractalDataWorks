using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for 64-bit IEEE 754 floating-point values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>float</c>, PostgreSQL <c>float8</c>/<c>double precision</c>,
/// C# <see cref="double"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Double")]
public sealed class DoubleDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DoubleDataType"/> class.</summary>
    public DoubleDataType()
        : base(id: 8, name: "Double")
    {
    }
}

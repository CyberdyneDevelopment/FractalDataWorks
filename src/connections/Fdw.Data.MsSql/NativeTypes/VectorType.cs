using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server 2025 <c>vector(n)</c>: a fixed-dimension vector of single-precision numbers (FDW-1354).
/// </summary>
/// <remarks>
/// Written as its JSON text (<c>[0.6,0.8,0]</c>), which SQL Server converts, so a plain insert or update stores one;
/// searched by distance only through the vectorized commands. Its length is its dimension, at most 1998.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "vector")]
public sealed class VectorType : DataTypeOptionBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VectorType"/> class.
    /// </summary>
    public VectorType()
        : base(
            name: "vector",
            description: "A fixed-dimension vector of single-precision numbers (SQL Server 2025).",
            abstractType: DataTypes.Vector,
            maxLength: 1998, requiresLength: true)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for variable-length binary (byte array) values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>varbinary</c>/<c>binary</c>/<c>image</c>,
/// PostgreSQL <c>bytea</c>, C# <c>byte[]</c>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Binary")]
public sealed class BinaryDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BinaryDataType"/> class.</summary>
    public BinaryDataType()
        : base(id: 14, name: "Binary")
    {
    }
}

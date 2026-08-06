using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>binary</c> — normalizes to <see cref="DataTypes.Binary"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "binary")]
public sealed class BinaryType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="BinaryType"/> class.</summary>
    public BinaryType()
        : base(
            id: 21,
            name: "binary",
            description: "Fixed-length binary data.",
            abstractType: DataTypes.Binary,
            isBinary: true, maxLength: 8000, requiresLength: true, defaultLength: 1)
    {
    }
}

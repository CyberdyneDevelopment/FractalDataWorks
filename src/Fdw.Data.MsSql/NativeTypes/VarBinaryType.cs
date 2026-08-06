using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>varbinary</c> — normalizes to <see cref="DataTypes.Binary"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "varbinary")]
public sealed class VarBinaryType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="VarBinaryType"/> class.</summary>
    public VarBinaryType()
        : base(
            id: 20,
            name: "varbinary",
            description: "Variable-length binary data.",
            abstractType: DataTypes.Binary,
            isBinary: true, isVariableLength: true, maxLength: 8000, requiresLength: true, defaultLength: 1, supportsStreaming: true)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>uniqueidentifier</c> — normalizes to <see cref="DataTypes.Guid"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "uniqueidentifier")]
public sealed class UniqueIdentifierType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="UniqueIdentifierType"/> class.</summary>
    public UniqueIdentifierType()
        : base(
            id: 22,
            name: "uniqueidentifier",
            description: "Globally unique identifier.",
            abstractType: DataTypes.Guid)
    {
    }
}

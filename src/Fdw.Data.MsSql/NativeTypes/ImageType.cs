using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>image</c> — normalizes to <see cref="DataTypes.Binary"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "image")]
public sealed class ImageType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageType"/> class.</summary>
    public ImageType()
        : base(
            id: 24,
            name: "image",
            description: "Variable-length binary data. Superseded by varbinary(max).",
            abstractType: DataTypes.Binary,
            isBinary: true, supportsStreaming: true, isDeprecated: true)
    {
    }
}

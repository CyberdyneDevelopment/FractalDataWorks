using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.MsSql;

/// <summary>
/// SQL Server native type <c>xml</c> — normalizes to <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MsSqlNativeTypes), "xml")]
public sealed class XmlType : DataTypeOptionBase
{
    /// <summary>Initializes a new instance of the <see cref="XmlType"/> class.</summary>
    public XmlType()
        : base(
            id: 23,
            name: "xml",
            description: "XML document or fragment.",
            abstractType: DataTypes.String,
            supportsStreaming: true)
    {
    }
}

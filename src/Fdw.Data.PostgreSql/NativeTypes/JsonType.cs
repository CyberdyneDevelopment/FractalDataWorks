using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>json</c> — maps to abstract type <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "json")]
public sealed class JsonType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonType"/> class.</summary>
    public JsonType()
        : base(id: 17, name: "json")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.String;
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Data.PostgreSql;

/// <summary>
/// PostgreSQL native type <c>jsonb</c> — maps to abstract type <see cref="DataTypes.String"/>.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(PostgreSqlNativeTypes), "jsonb")]
public sealed class JsonbType : PostgreSqlNativeTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="JsonbType"/> class.</summary>
    public JsonbType()
        : base(id: 18, name: "jsonb")
    {
    }

    /// <inheritdoc/>
    public override IDataType AbstractType => DataTypes.String;
}

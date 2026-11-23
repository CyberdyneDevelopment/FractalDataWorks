using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Float type.</summary>
[TypeOption(typeof(SqlTypeWords), "Float")]
[ExcludeFromCodeCoverage]
public sealed class FloatSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatSqlTypeWord"/> class.</summary>
    public FloatSqlTypeWord()
        : base("Float", SqlDbType.Float, ["double"])
    {
    }
}

using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL DateTime2 type.</summary>
[TypeOption(typeof(SqlTypeWords), "DateTime2")]
[ExcludeFromCodeCoverage]
public sealed class DateTime2SqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTime2SqlTypeWord"/> class.</summary>
    public DateTime2SqlTypeWord()
        : base("DateTime2", SqlDbType.DateTime2, ["datetime"])
    {
    }
}

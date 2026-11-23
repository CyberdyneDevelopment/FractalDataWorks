using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Real type.</summary>
[TypeOption(typeof(SqlTypeWords), "Real")]
[ExcludeFromCodeCoverage]
public sealed class RealSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="RealSqlTypeWord"/> class.</summary>
    public RealSqlTypeWord()
        : base("Real", SqlDbType.Real, ["float", "single"])
    {
    }
}

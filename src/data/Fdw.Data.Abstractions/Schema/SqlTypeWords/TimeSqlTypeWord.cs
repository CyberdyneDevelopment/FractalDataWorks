using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL Time type.</summary>
[TypeOption(typeof(SqlTypeWords), "Time")]
[ExcludeFromCodeCoverage]
public sealed class TimeSqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeSqlTypeWord"/> class.</summary>
    public TimeSqlTypeWord()
        : base("Time", SqlDbType.Time, ["time"])
    {
    }
}

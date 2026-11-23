using System.Data;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>The SQL VarBinary type.</summary>
[TypeOption(typeof(SqlTypeWords), "VarBinary")]
[ExcludeFromCodeCoverage]
public sealed class VarBinarySqlTypeWord : SqlTypeWordBase
{
    /// <summary>Initializes a new instance of the <see cref="VarBinarySqlTypeWord"/> class.</summary>
    public VarBinarySqlTypeWord()
        : base("VarBinary", SqlDbType.VarBinary, ["binary", "varbinary"])
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions;

/// <summary>
/// Abstract type for fixed-precision decimal numeric values.
/// </summary>
/// <remarks>
/// Maps to: SQL Server <c>decimal</c>/<c>numeric</c>/<c>money</c>,
/// PostgreSQL <c>numeric</c>, C# <see cref="decimal"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataTypes), "Decimal")]
public sealed class DecimalDataType : DataTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalDataType"/> class.</summary>
    public DecimalDataType()
        : base(id: 6, name: "Decimal")
    {
    }
}

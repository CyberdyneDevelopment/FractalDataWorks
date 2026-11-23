using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A decimal.</summary>
[TypeOption(typeof(ReaderValueKinds), "Decimal")]
[ExcludeFromCodeCoverage]
public sealed class DecimalReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalReaderValueKind"/> class.</summary>
    public DecimalReaderValueKind()
        : base("Decimal")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is decimal;

    /// <inheritdoc />
    public override bool ToBoolean(object value) => (decimal)value != 0;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => (decimal)value;
}

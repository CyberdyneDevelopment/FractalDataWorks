using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A 64-bit whole number.</summary>
[TypeOption(typeof(ReaderValueKinds), "Whole")]
[ExcludeFromCodeCoverage]
public sealed class WholeReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="WholeReaderValueKind"/> class.</summary>
    public WholeReaderValueKind()
        : base("Whole")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is long;

    /// <inheritdoc />
    public override bool ToBoolean(object value) => (long)value != 0;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => (long)value;
}

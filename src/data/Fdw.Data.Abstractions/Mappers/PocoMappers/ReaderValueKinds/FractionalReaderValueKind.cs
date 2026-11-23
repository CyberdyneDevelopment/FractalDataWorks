using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Abstractions.Mappers.PocoMappers;

/// <summary>A double.</summary>
[TypeOption(typeof(ReaderValueKinds), "Fractional")]
[ExcludeFromCodeCoverage]
public sealed class FractionalReaderValueKind : ReaderValueKindBase
{
    /// <summary>Initializes a new instance of the <see cref="FractionalReaderValueKind"/> class.</summary>
    public FractionalReaderValueKind()
        : base("Fractional")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is double;

    /// <inheritdoc />
    public override bool ToBoolean(object value) => (double)value != 0;

    /// <inheritdoc />
    public override decimal ToDecimal(object value) => (decimal)(double)value;
}

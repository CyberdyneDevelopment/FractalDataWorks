using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 64-bit whole number.</summary>
[TypeOption(typeof(WholeNumberInputKinds), "Int64")]
[ExcludeFromCodeCoverage]
public sealed class Int64WholeNumberInputKind : WholeNumberInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int64WholeNumberInputKind"/> class.</summary>
    public Int64WholeNumberInputKind()
        : base("Int64")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is long;

    /// <inheritdoc />
    public override long Read(object value) => (long)value;
}

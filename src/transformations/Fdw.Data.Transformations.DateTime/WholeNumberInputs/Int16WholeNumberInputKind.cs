using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 16-bit whole number.</summary>
[TypeOption(typeof(WholeNumberInputKinds), "Int16")]
[ExcludeFromCodeCoverage]
public sealed class Int16WholeNumberInputKind : WholeNumberInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int16WholeNumberInputKind"/> class.</summary>
    public Int16WholeNumberInputKind()
        : base("Int16")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is short;

    /// <inheritdoc />
    public override long Read(object value) => (short)value;
}

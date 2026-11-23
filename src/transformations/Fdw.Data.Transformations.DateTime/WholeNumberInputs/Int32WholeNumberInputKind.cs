using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Transformations;

/// <summary>A 32-bit whole number.</summary>
[TypeOption(typeof(WholeNumberInputKinds), "Int32")]
[ExcludeFromCodeCoverage]
public sealed class Int32WholeNumberInputKind : WholeNumberInputKindBase
{
    /// <summary>Initializes a new instance of the <see cref="Int32WholeNumberInputKind"/> class.</summary>
    public Int32WholeNumberInputKind()
        : base("Int32")
    {
    }

    /// <inheritdoc />
    public override bool Holds(object value) => value is int;

    /// <inheritdoc />
    public override long Read(object value) => (int)value;
}

using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>The base of every option of <see cref="NumericInputKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class NumericInputKindBase : TypeOptionBase<NumericInputKindBase>, INumericInputKind
{
    /// <summary>Initializes a new instance of the <see cref="NumericInputKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected NumericInputKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract decimal ToDecimal(object value);
}

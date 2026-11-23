using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>The base of every option of <see cref="WholeNumberInputKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class WholeNumberInputKindBase : TypeOptionBase<WholeNumberInputKindBase>, IWholeNumberInputKind
{
    /// <summary>Initializes a new instance of the <see cref="WholeNumberInputKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected WholeNumberInputKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract long Read(object value);
}

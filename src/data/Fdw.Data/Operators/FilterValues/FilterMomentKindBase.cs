using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>The base of every option of <see cref="FilterMomentKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class FilterMomentKindBase : TypeOptionBase<FilterMomentKindBase>, IFilterMomentKind
{
    /// <summary>Initializes a new instance of the <see cref="FilterMomentKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected FilterMomentKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract DateTimeOffset ToMoment(object value);
}

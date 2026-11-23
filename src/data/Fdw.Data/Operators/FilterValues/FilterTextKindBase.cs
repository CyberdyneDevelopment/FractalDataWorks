using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>The base of every option of <see cref="FilterTextKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class FilterTextKindBase : TypeOptionBase<FilterTextKindBase>, IFilterTextKind
{
    /// <summary>Initializes a new instance of the <see cref="FilterTextKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected FilterTextKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract string Text(object value);
}

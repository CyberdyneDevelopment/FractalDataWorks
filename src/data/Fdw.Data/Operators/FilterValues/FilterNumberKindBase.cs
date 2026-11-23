using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data;

/// <summary>The base of every option of <see cref="FilterNumberKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class FilterNumberKindBase : TypeOptionBase<FilterNumberKindBase>, IFilterNumberKind
{
    /// <summary>Initializes a new instance of the <see cref="FilterNumberKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected FilterNumberKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract decimal ToDecimal(object value);
}

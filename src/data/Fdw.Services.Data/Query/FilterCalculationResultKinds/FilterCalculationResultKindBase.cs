using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Web.RestEndpoints.Query;

/// <summary>The base of every kind of calculation result a filter can compare.</summary>
/// <remarks>The members are abstract, not constructor parameters, so the NotFound sentinel is stubbed and never handed a null.</remarks>
[ExcludeFromCodeCoverage]
public abstract class FilterCalculationResultKindBase : TypeOptionBase<FilterCalculationResultKindBase>, IFilterCalculationResultKind
{
    /// <summary>Initializes a new instance of the <see cref="FilterCalculationResultKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected FilterCalculationResultKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    [TypeLookup("ByValueType")]
    public abstract Type ValueType { get; }

    /// <inheritdoc />
    public abstract string Word { get; }

    /// <inheritdoc />
    public abstract IReadOnlyList<Type> FieldTypes { get; }

    /// <inheritdoc />
    public abstract JsonElement Literal(object value);
}

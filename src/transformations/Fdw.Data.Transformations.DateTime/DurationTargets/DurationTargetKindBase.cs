using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Data.Transformations;

/// <summary>The base of every option of <see cref="DurationTargetKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class DurationTargetKindBase : TypeOptionBase<DurationTargetKindBase>, IDurationTargetKind
{
    /// <summary>Initializes a new instance of the <see cref="DurationTargetKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected DurationTargetKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract object Add(object value, TimeSpan duration);
}

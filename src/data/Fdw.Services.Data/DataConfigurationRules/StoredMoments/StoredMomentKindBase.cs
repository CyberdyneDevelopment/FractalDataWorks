using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>The base of every option of <see cref="StoredMomentKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class StoredMomentKindBase : TypeOptionBase<StoredMomentKindBase>, IStoredMomentKind
{
    /// <summary>Initializes a new instance of the <see cref="StoredMomentKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected StoredMomentKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract DateTimeOffset? Read(object value);
}

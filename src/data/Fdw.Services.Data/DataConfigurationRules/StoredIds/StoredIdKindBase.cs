using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Fdw.Collections;

namespace Fdw.Services.Data.DataConfigurationRules;

/// <summary>The base of every option of <see cref="StoredIdKinds"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class StoredIdKindBase : TypeOptionBase<StoredIdKindBase>, IStoredIdKind
{
    /// <summary>Initializes a new instance of the <see cref="StoredIdKindBase"/> class.</summary>
    /// <param name="name">The option's name.</param>
    protected StoredIdKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool Holds(object value);

    /// <inheritdoc />
    public abstract Guid? Read(object value);
}

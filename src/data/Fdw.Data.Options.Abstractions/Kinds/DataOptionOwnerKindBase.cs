using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>The base of the owner kinds of a data configuration rule (FDW-6454).</summary>
public abstract class DataOptionOwnerKindBase : TypeOptionBase<DataOptionOwnerKindBase>, IDataOptionOwnerKind
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionOwnerKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    protected DataOptionOwnerKindBase(string name)
        : base(name)
    {
    }
}

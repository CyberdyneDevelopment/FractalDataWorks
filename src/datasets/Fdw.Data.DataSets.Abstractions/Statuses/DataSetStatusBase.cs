using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>Base for the options of <see cref="DataSetStatuses"/> (FDW-4488).</summary>
public abstract class DataSetStatusBase : TypeOptionBase<DataSetStatusBase>, IDataSetStatus
{
    /// <summary>Initializes a new instance of the <see cref="DataSetStatusBase"/> class.</summary>
    /// <remarks>Used only by the source generator's NotFound sentinel: its rank is 0 and no rule applies, so it can never win.</remarks>
    protected DataSetStatusBase()
        : this(string.Empty, 0)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="DataSetStatusBase"/> class.</summary>
    /// <param name="name">The status name, which a list shows.</param>
    /// <param name="rank">How the status ranks among the others; higher wins. Leave gaps so a developer's option fits between two.</param>
    protected DataSetStatusBase(string name, int rank)
        : base(name)
    {
        Rank = rank;
    }

    /// <inheritdoc/>
    [TypeLookup("ByRank")]
    public int Rank { get; }

    /// <inheritdoc/>
    public virtual bool IsStatedByKind => false;

    /// <inheritdoc/>
    public abstract bool Applies(DataSetStatusInput input);
}

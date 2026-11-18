using Fdw.Collections;

namespace Fdw.Data.Options;

/// <summary>The base of the value kinds of a data configuration rule (FDW-6454).</summary>
public abstract class DataOptionValueKindBase : TypeOptionBase<DataOptionValueKindBase>, IDataOptionValueKind
{
    /// <summary>Initializes a new instance of the <see cref="DataOptionValueKindBase"/> class.</summary>
    /// <param name="name">The kind's name.</param>
    /// <param name="member">The member of a value that holds the kind's content.</param>
    protected DataOptionValueKindBase(string name, string member)
        : base(name)
    {
        Member = member;
    }

    /// <inheritdoc />
    public string Member { get; }
}

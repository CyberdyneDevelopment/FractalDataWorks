using Fdw.Collections;

namespace Fdw.Data.RowSources.Delimited.Xlsx;

/// <summary>Base class for the kinds of value a numeric Excel cell holds.</summary>
public abstract class XlsxValueKindBase : TypeOptionBase<XlsxValueKindBase>, IXlsxValueKind
{
    /// <summary>Initializes a new instance of the <see cref="XlsxValueKindBase"/> class.</summary>
    /// <param name="name">The name of this kind.</param>
    protected XlsxValueKindBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract object Read(string text, bool date1904);
}

using Fdw.Collections;

namespace Fdw.Services.Connections.Abstractions.DocumentRecords;

/// <summary>Base class for the options of <see cref="DocumentRecordConnectionTypes"/> (FDW-3673), using the CRTP pattern.</summary>
public abstract class DocumentRecordConnectionTypeBase : TypeOptionBase<DocumentRecordConnectionTypeBase>, IDocumentRecordConnectionType
{
    /// <summary>Initializes a new instance of the <see cref="DocumentRecordConnectionTypeBase"/> class.</summary>
    /// <param name="name">The kind of connection this extension is for, named as its connection type is (for example <c>MsSql</c>).</param>
    protected DocumentRecordConnectionTypeBase(string name) : base(name)
    {
    }

    /// <inheritdoc />
    public abstract IDocumentRecordConnection? Adapt(IGenericConnection connection);
}

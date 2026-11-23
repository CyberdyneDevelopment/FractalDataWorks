using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Tds.Endpoints.Sql.Rows;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Fdw.Tds.Endpoints.Sql.Queries;

/// <summary>The base of every handler of a kind of FROM source, through <see cref="TableReferenceHandlerBase{TReference}"/>.</summary>
[ExcludeFromCodeCoverage]
public abstract class TableReferenceHandlerBase : TypeOptionBase<TableReferenceHandlerBase>, ITableReferenceHandler
{
    /// <summary>Initializes a new instance of the <see cref="TableReferenceHandlerBase"/> class.</summary>
    /// <param name="name">The option's name: the name of the parsed source type it reads.</param>
    protected TableReferenceHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract IGenericResult<SqlRowSet> Read(TableReference reference, SqlScope outer);
}

/// <summary>The base of a handler of one parsed source type.</summary>
/// <typeparam name="TReference">The parsed source type the handler reads.</typeparam>
[ExcludeFromCodeCoverage]
public abstract class TableReferenceHandlerBase<TReference> : TableReferenceHandlerBase
    where TReference : TableReference
{
    /// <summary>Initializes a new instance of the <see cref="TableReferenceHandlerBase{TReference}"/> class.</summary>
    /// <param name="name">The option's name: the name of <typeparamref name="TReference"/>.</param>
    protected TableReferenceHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public sealed override IGenericResult<SqlRowSet> Read(TableReference reference, SqlScope outer)
        => Handle((TReference)reference, outer);

    /// <summary>Reads a source of this handler's type.</summary>
    /// <param name="reference">The source.</param>
    /// <param name="outer">The scope it runs in.</param>
    /// <returns>The rows, or the refusal.</returns>
    protected abstract IGenericResult<SqlRowSet> Handle(TReference reference, SqlScope outer);
}

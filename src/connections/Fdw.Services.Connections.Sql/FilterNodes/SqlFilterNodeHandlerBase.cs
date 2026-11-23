using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql.FilterNodes;

/// <summary>The type the SQL filter node handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class SqlFilterNodeHandlerBase : TypeOptionBase<SqlFilterNodeHandlerBase>, ISqlFilterNodeHandler
{
    /// <summary>Initializes the handler with the name of the filter node kind it writes.</summary>
    /// <param name="name">The filter node kind's name.</param>
    protected SqlFilterNodeHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract string Build(IFilterNode node, SqlBuildContext context);
}

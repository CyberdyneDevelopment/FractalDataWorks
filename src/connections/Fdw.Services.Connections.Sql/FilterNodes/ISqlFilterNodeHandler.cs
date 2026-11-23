using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Services.Connections.Sql.FilterNodes;

/// <summary>Writes the WHERE text of one kind of filter node; the collection is keyed by the node kind's name (<c>FilterNodeKinds</c>).</summary>
public interface ISqlFilterNodeHandler : ITypeOption<int, ISqlFilterNodeHandler>
{
    /// <summary>Writes the node, adding its values as parameters.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <param name="context">The state of the clause being built.</param>
    /// <returns>The SQL text; empty when the node writes nothing.</returns>
    string Build(IFilterNode node, SqlBuildContext context);
}

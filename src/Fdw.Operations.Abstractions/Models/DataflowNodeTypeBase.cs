using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>Base class for dataflow node types.</summary>
public abstract class DataflowNodeTypeBase : TypeOptionBase<int, DataflowNodeTypeBase>, IDataflowNodeType
{
    /// <summary>Initializes a new instance of <see cref="DataflowNodeTypeBase"/>.</summary>
    protected DataflowNodeTypeBase(int id, string name) : base(id, name) { }
}

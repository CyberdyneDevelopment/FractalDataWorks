using Fdw.Collections;

namespace Fdw.Operations.Clients.Models;

/// <summary>Base class for dataflow node types.</summary>
public abstract class DataflowNodeTypeBase : TypeOptionBase<DataflowNodeTypeBase>, IDataflowNodeType
{
    /// <summary>Initializes a new instance of <see cref="DataflowNodeTypeBase"/>.</summary>
    protected DataflowNodeTypeBase(string name) : base(name) { }
}

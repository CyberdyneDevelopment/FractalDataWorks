using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols.FilterNodes;

/// <summary>Describes, for the UnsupportedFilterOperator refusal, a filter node of one kind a REST protocol cannot express; the collection is keyed by the node kind's name (<c>FilterNodeKinds</c>).</summary>
public interface IRestFilterNodeHandler : ITypeOption<int, IRestFilterNodeHandler>
{
    /// <summary>Describes the node the protocol cannot express.</summary>
    /// <param name="node">The node, of the kind this handler is named for.</param>
    /// <param name="protocolName">The protocol, for the refusal.</param>
    /// <returns>The details of the refusal: the protocol, the operator or shape, and the field.</returns>
    ResultDetails Describe(IFilterNode node, string protocolName);
}

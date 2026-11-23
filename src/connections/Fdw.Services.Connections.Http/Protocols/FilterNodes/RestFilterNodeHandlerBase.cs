using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Protocols.FilterNodes;

/// <summary>The type the REST filter node handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class RestFilterNodeHandlerBase : TypeOptionBase<RestFilterNodeHandlerBase>, IRestFilterNodeHandler
{
    /// <summary>Initializes the handler with the name of the filter node kind it describes.</summary>
    /// <param name="name">The filter node kind's name.</param>
    protected RestFilterNodeHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract ResultDetails Describe(IFilterNode node, string protocolName);
}

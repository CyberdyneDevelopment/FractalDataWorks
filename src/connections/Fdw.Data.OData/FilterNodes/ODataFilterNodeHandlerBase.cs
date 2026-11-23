using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Data.OData.FilterNodes;

/// <summary>The type the OData filter node handler collection is built on.</summary>
[ExcludeFromCodeCoverage]
public abstract class ODataFilterNodeHandlerBase : TypeOptionBase<ODataFilterNodeHandlerBase>, IODataFilterNodeHandler
{
    /// <summary>Initializes the handler with the name of the filter node kind it writes.</summary>
    /// <param name="name">The filter node kind's name.</param>
    protected ODataFilterNodeHandlerBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc/>
    public abstract string Build(IFilterNode node);
}

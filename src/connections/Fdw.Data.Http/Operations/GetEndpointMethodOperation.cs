using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>GET reads.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointMethodOperations), "Get")]
public sealed class GetEndpointMethodOperation : EndpointMethodOperationBase
{
    /// <summary>Initializes a new instance of the <see cref="GetEndpointMethodOperation"/> class.</summary>
    public GetEndpointMethodOperation()
        : base("Get", "GET", "Query")
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>PUT replaces, which is an update.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointMethodOperations), "Put")]
public sealed class PutEndpointMethodOperation : EndpointMethodOperationBase
{
    /// <summary>Initializes a new instance of the <see cref="PutEndpointMethodOperation"/> class.</summary>
    public PutEndpointMethodOperation()
        : base("Put", "PUT", "Update")
    {
    }
}

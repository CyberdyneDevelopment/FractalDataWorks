using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>DELETE removes.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointMethodOperations), "Delete")]
public sealed class DeleteEndpointMethodOperation : EndpointMethodOperationBase
{
    /// <summary>Initializes a new instance of the <see cref="DeleteEndpointMethodOperation"/> class.</summary>
    public DeleteEndpointMethodOperation()
        : base("Delete", "DELETE", "Delete")
    {
    }
}

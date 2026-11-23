using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>PATCH changes, which is an update.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointMethodOperations), "Patch")]
public sealed class PatchEndpointMethodOperation : EndpointMethodOperationBase
{
    /// <summary>Initializes a new instance of the <see cref="PatchEndpointMethodOperation"/> class.</summary>
    public PatchEndpointMethodOperation()
        : base("Patch", "PATCH", "Update")
    {
    }
}

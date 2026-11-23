using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>POST creates.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointMethodOperations), "Post")]
public sealed class PostEndpointMethodOperation : EndpointMethodOperationBase
{
    /// <summary>Initializes a new instance of the <see cref="PostEndpointMethodOperation"/> class.</summary>
    public PostEndpointMethodOperation()
        : base("Post", "POST", "Insert")
    {
    }
}

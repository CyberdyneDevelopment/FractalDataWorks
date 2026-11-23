using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.Http.Operations;

/// <summary>The data operation an HTTP method stands for when an endpoint container is called with it.</summary>
public interface IEndpointMethodOperation : ITypeOption<int, IEndpointMethodOperation>
{
    /// <summary>Gets the HTTP method, in upper case (<c>GET</c>).</summary>
    [TypeLookup("ByMethod")]
    string Method { get; }

    /// <summary>Gets the name of the data operation the method stands for (<c>Query</c>, <c>Insert</c>, <c>Update</c> or <c>Delete</c>).</summary>
    string Operation { get; }
}

namespace Fdw.Web.Http.Abstractions.EndPoints;

/// <summary>
/// Endpoint metadata declaring that the endpoint writes its response progressively and must not be
/// buffered.
/// </summary>
/// <remarks>
/// Middleware that holds a response to set an explicit Content-Length turns a streaming endpoint back
/// into a materialising one — silently, because the output is byte-for-byte identical and only the
/// timing and the memory ceiling change. Such middleware reads this metadata off the resolved endpoint
/// and passes the response straight through.
/// <para>
/// Why metadata rather than a path prefix: a route like <c>/datasets/{name}/export</c> cannot be
/// expressed as a prefix without also excluding every other route under <c>/datasets/</c>. The
/// endpoint declaring its own behaviour is exact, and it cannot drift from the route.
/// </para>
/// </remarks>
public interface IStreamingResponse
{
}

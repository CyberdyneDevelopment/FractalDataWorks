using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.WebMcp.Hosting;

/// <summary>
/// Where a host's own address comes from (FDW-5721): the address its MCP bridge sends each tool call to.
/// A host's WebMcp row names one in <c>SelfAddressSource</c>; there is no default source, and a host that
/// serves MCP and names none is refused.
/// </summary>
/// <remarks>
/// <c>ListeningAddress</c> reads the address the running process listens on, so two instances of one
/// release on two ports (A/B slots) each call themselves and the release carries no port.
/// <c>FixedAddress</c> reads the row's <c>SelfBaseAddress</c>, for a host behind a proxy. There is no source
/// that reads the incoming request's Host header: the caller controls it. The source generator emits
/// <c>ByName</c>, <c>All</c> and <c>NotFound</c>.
/// </remarks>
[TypeCollection(typeof(WebMcpSelfAddressSourceBase), typeof(WebMcpSelfAddressSourceBase), typeof(WebMcpSelfAddressSources))]
public abstract partial class WebMcpSelfAddressSources : TypeCollectionBase<WebMcpSelfAddressSourceBase, WebMcpSelfAddressSourceBase>
{
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Results.Abstractions;

namespace Fdw.Web.Clients.Abstractions.Results;

/// <summary>
/// TypeCollection for HTTP API client result codes -- the band a non-success response falls into,
/// so the caller's own HTTP layer (a proxy endpoint, for instance) can answer with the SAME status
/// the downstream service actually returned instead of a single hardcoded one.
/// </summary>
[TypeCollection(typeof(ClientResultCodeBase), typeof(IResultCode), typeof(ClientResultCodes))]
[ExcludeFromCodeCoverage]
public abstract partial class ClientResultCodes : TypeCollectionBase<ClientResultCodeBase, IResultCode>
{
}

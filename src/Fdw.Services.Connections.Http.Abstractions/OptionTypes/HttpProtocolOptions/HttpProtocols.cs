using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

/// <summary>
/// Collection of HTTP protocols for enhanced enum functionality.
/// Source generator creates static HttpProtocols class automatically.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(HttpProtocolBase), typeof(IHttpProtocol), typeof(HttpProtocols))]
public abstract partial class HttpProtocols : TypeCollectionBase<HttpProtocolBase, IHttpProtocol>
{
    // DO NOT IMPLEMENT BY HAND!
    // Source generator automatically creates static HttpProtocols class with:
    // - HttpProtocols.Rest (returns IHttpProtocol)
    // - HttpProtocols.Soap (returns IHttpProtocol)
    // - HttpProtocols.GraphQL (returns IHttpProtocol)
    // - HttpProtocols.All (collection of IHttpProtocol)
    // - HttpProtocols.ById(int id) (returns IHttpProtocol)
    // - HttpProtocols.ByName(string name) (returns IHttpProtocol)
}
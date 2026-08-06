using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// Standard REST protocol implementation using common conventions.
/// </summary>
/// <remarks>
/// <para>
/// This protocol uses standard REST conventions:
/// <list type="bullet">
/// <item><description>Offset/limit pagination: offset=20&amp;limit=10</description></item>
/// <item><description>Sort with prefix: sort=name,-created_at (- for descending)</description></item>
/// <item><description>Simple filter: field=value</description></item>
/// <item><description>JSON request/response bodies</description></item>
/// </list>
/// </para>
/// <para>
/// For OData-style APIs, use <see cref="ODataProtocol"/>.
/// For JSON:API specification, use <see cref="JsonApiProtocol"/>.
/// For custom APIs, extend <see cref="RestProtocolBase"/>.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "Rest")]
public sealed class RestProtocol : RestProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RestProtocol"/> class.
    /// </summary>
    public RestProtocol()
        : base(1, "Rest", "Standard REST API protocol with offset/limit pagination")
    {
    }
}
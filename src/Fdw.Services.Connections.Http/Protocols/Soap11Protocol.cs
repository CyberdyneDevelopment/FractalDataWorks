using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// SOAP 1.1 protocol implementation.
/// </summary>
/// <remarks>
/// <para>
/// SOAP 1.1 characteristics:
/// <list type="bullet">
/// <item><description>Namespace: http://schemas.xmlsoap.org/soap/envelope/</description></item>
/// <item><description>Content-Type: text/xml; charset=utf-8</description></item>
/// <item><description>mustUnderstand: "1" (string)</description></item>
/// <item><description>Fault structure: faultcode, faultstring, faultactor, detail</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "Soap11")]
public class Soap11Protocol : SoapProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Soap11Protocol"/> class.
    /// </summary>
    public Soap11Protocol()
        : base(2, "Soap11", "SOAP 1.1 protocol", SoapVersion.Soap11.ContentType)
    {
    }

    /// <inheritdoc/>
    public override SoapVersion Version => SoapVersion.Soap11;
}

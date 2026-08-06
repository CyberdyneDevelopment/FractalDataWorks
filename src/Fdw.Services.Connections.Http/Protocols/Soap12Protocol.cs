using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http.Protocols;

/// <summary>
/// SOAP 1.2 protocol implementation.
/// </summary>
/// <remarks>
/// <para>
/// SOAP 1.2 characteristics:
/// <list type="bullet">
/// <item><description>Namespace: http://www.w3.org/2003/05/soap-envelope</description></item>
/// <item><description>Content-Type: application/soap+xml; charset=utf-8</description></item>
/// <item><description>mustUnderstand: "true" (boolean)</description></item>
/// <item><description>Fault structure: Code/Value, Reason/Text, Node, Role, Detail</description></item>
/// </list>
/// </para>
/// <para>
/// This class can be extended to create service-specific SOAP protocols.
/// For example, ErcotProtocol would extend Soap12Protocol and override
/// <see cref="SoapProtocolBase.BuildSoapBody"/> to build ERCOT's RequestMessage format.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(HttpProtocols), "Soap12")]
public class Soap12Protocol : SoapProtocolBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Soap12Protocol"/> class.
    /// </summary>
    public Soap12Protocol()
        : base(3, "Soap12", "SOAP 1.2 protocol", SoapVersion.Soap12.ContentType)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Soap12Protocol"/> class for derived classes.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The protocol name.</param>
    /// <param name="description">The protocol description.</param>
    protected Soap12Protocol(int id, string name, string description)
        : base(id, name, description, SoapVersion.Soap12.ContentType)
    {
    }

    /// <inheritdoc/>
    public override SoapVersion Version => SoapVersion.Soap12;
}

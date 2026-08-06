using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// Interface for SOAP security processors that apply security to SOAP envelopes.
/// </summary>
public interface ISoapSecurityProcessor : ITypeOption<int, SoapSecurityProcessorBase>
{
    /// <summary>
    /// Gets the description of this security processor.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Processes the SOAP envelope and applies security (headers, signing, etc.).
    /// </summary>
    /// <param name="envelope">The SOAP envelope document.</param>
    /// <param name="soapVersion">The SOAP version being used.</param>
    /// <param name="context">The protocol context with configuration and resolved secrets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secured SOAP envelope.</returns>
    Task<IGenericResult<XDocument>> Process(
        XDocument envelope,
        SoapVersion soapVersion,
        HttpProtocolContext context,
        CancellationToken cancellationToken);
}

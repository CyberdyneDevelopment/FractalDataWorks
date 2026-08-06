using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// No-op security processor that passes the envelope through unchanged.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(SoapSecurityProcessors), "None")]
public sealed class NoneSoapSecurityProcessor : SoapSecurityProcessorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoneSoapSecurityProcessor"/> class.
    /// </summary>
    public NoneSoapSecurityProcessor()
        : base(1, "None", "No security applied")
    {
    }

    /// <inheritdoc/>
    public override Task<IGenericResult<XDocument>> Process(
        XDocument envelope,
        SoapVersion soapVersion,
        HttpProtocolContext context,
        CancellationToken cancellationToken)
    {
        // Pass through unchanged
        return Task.FromResult(GenericResult<XDocument>.Success(envelope));
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Protocols;

namespace Fdw.Services.Connections.Http.Security;

/// <summary>
/// Base class for SOAP security processor implementations.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public abstract class SoapSecurityProcessorBase : TypeOptionBase<int, SoapSecurityProcessorBase>, ISoapSecurityProcessor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SoapSecurityProcessorBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="name">The processor name.</param>
    /// <param name="description">The processor description.</param>
    protected SoapSecurityProcessorBase(int id, string name, string description)
        : base(id, name)
    {
        Description = description;
    }

    /// <inheritdoc/>
    public new string Description { get; }

    /// <inheritdoc/>
    public abstract Task<IGenericResult<XDocument>> Process(
        XDocument envelope,
        SoapVersion soapVersion,
        HttpProtocolContext context,
        CancellationToken cancellationToken);
}

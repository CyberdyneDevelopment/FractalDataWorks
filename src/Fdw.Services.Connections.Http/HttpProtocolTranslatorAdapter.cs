using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// Adapter that wraps an IHttpProtocol to implement IDataCommandTranslator.
/// </summary>
/// <remarks>
/// <para>
/// This adapter bridges the protocol pattern with the translator pattern used by ConnectionBase.
/// It delegates translation to the protocol while providing the additional context (secrets, configuration)
/// that the protocol needs.
/// </para>
/// <para>
/// The adapter is created by HttpConnection and holds:
/// <list type="bullet">
/// <item><description>The protocol instance (Rest, Soap11, Soap12, etc.)</description></item>
/// <item><description>The protocol context (configuration + resolved secrets)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class HttpProtocolTranslatorAdapter : IDataCommandTranslator<HttpRequestMessage>
{
    private readonly IHttpProtocol _protocol;
    private readonly HttpProtocolContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpProtocolTranslatorAdapter"/> class.
    /// </summary>
    /// <param name="protocol">The HTTP protocol to delegate to.</param>
    /// <param name="context">The protocol context with configuration and resolved secrets.</param>
    public HttpProtocolTranslatorAdapter(IHttpProtocol protocol, HttpProtocolContext context)
    {
        _protocol = protocol;
        _context = context;
    }

    /// <inheritdoc/>
    public int Id => _protocol.Id;

    /// <inheritdoc/>
    object Fdw.Collections.ITypeOption.Id => _protocol.Id;

    /// <inheritdoc/>
    public string Name => _protocol.Name;

    /// <inheritdoc/>
    public string Category => "Http";

    /// <inheritdoc/>
    public string DomainName => "Http";

    /// <inheritdoc/>
    public Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
    {
        return _protocol.Translate(command, container, _context, cancellationToken);
    }
}

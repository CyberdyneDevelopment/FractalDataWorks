using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.Connections.Http;

/// <summary>
/// The translator an HTTP connection answers for a command type its protocol does not map. Its name is the "NotFound"
/// sentinel <c>ConnectionBase</c> reads as "no translator for this command", which lets a command that expands into
/// simpler ones (Upsert, BulkUpsert, BulkUpdate) run those, and fails any other command with <c>TranslatorNotFound</c>.
/// It never builds a request.
/// </summary>
public sealed class HttpTranslatorNotFound : IDataCommandTranslator<HttpRequestMessage>
{
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTranslatorNotFound"/> class.
    /// </summary>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public HttpTranslatorNotFound(ILogger? logger)
    {
        _logger = logger ?? NullLogger.Instance;
    }

    /// <inheritdoc/>
    public int Id { get; } = OptionId.DeriveInt(typeof(HttpTranslatorNotFound).FullName!);

    /// <inheritdoc/>
    public string Name => "NotFound";

    /// <inheritdoc/>
    public string Category => "Http";

    /// <inheritdoc/>
    public string DomainName => "Http";

    /// <inheritdoc/>
    public Task<IGenericResult<HttpRequestMessage>> Translate(
        IDataCommand command,
        IStorageContainer container,
        CancellationToken cancellationToken = default)
        => Task.FromResult(GenericResult<HttpRequestMessage>.Failure(ConnectionLogger.TranslatorNotFound(_logger, command.CommandType)));
}

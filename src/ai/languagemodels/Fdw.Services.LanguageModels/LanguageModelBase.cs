using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.Services.LanguageModels.Abstractions;
using Fdw.Services.LanguageModels.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels;

/// <summary>
/// The base of every language model provider. It asks <see cref="LanguageModelRequestRule"/> before any call and only then hands the request to the
/// provider's own <see cref="ReplyCore"/> or <see cref="StreamCore"/>, so a request the model cannot carry never reaches a provider.
/// </summary>
/// <remarks>
/// A provider subclass states how to make the call; it never states whether the call may be made. A refusal in a stream is a stream of one failed event.
/// </remarks>
public abstract class LanguageModelBase : ILanguageModel, IPlatformService
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="LanguageModelBase"/> class.</summary>
    /// <param name="configuration">The configuration row of the model.</param>
    /// <param name="logger">The logger for the model.</param>
    protected LanguageModelBase(ILanguageModelImplementationConfiguration configuration, ILogger? logger)
    {
        Configuration = configuration;
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>Gets the configuration row of the model.</summary>
    protected ILanguageModelImplementationConfiguration Configuration { get; }

    /// <summary>Gets the logger for the model.</summary>
    protected ILogger Logger => _logger;

    /// <inheritdoc />
    public string Name => Configuration.Name;

    /// <inheritdoc />
    public string ModelId => Configuration.ModelId ?? string.Empty;

    /// <inheritdoc />
    public IReadOnlyCollection<string> Capabilities => Configuration.CapabilityNames;

    /// <inheritdoc />
    string IPlatformService.Id => Configuration.Id.ToString();

    /// <inheritdoc />
    string IPlatformService.ServiceType => "LanguageModel";

    /// <inheritdoc />
    bool IPlatformService.IsAvailable => true;

    /// <inheritdoc />
    Task<IGenericResult<T>> IGenericService.Execute<T>(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(LanguageModelLog.GenericCommandRejected(_logger, Name)));

    /// <inheritdoc />
    Task<IGenericResult> IGenericService.Execute(IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(LanguageModelLog.GenericCommandRejected(_logger, Name)));

    /// <inheritdoc />
    public async Task<IGenericResult<LanguageModelReply>> Reply(LanguageModelRequest request, CancellationToken cancellationToken = default)
    {
        var rule = LanguageModelRequestRule.Check(request, Configuration, _logger);
        if (!rule.IsSuccess)
            return rule.ToNewResult<LanguageModelReply>();

        return await ReplyCore(request, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<LanguageModelStreamEvent> Stream(
        LanguageModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var rule = LanguageModelRequestRule.Check(request, Configuration, _logger);
        if (!rule.IsSuccess)
        {
            yield return new FailedEvent(rule);
            yield break;
        }

        await foreach (var streamEvent in StreamCore(request, cancellationToken).WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return streamEvent;
    }

    /// <summary>Makes the call for a request the model can carry and returns the whole reply. Written by the provider.</summary>
    /// <param name="request">The request, already checked.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The reply, or a failure with a coded message.</returns>
    protected abstract Task<IGenericResult<LanguageModelReply>> ReplyCore(LanguageModelRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Makes the call for a request the model can carry and streams the events. Written by the provider. The stream ends with exactly one completed
    /// or failed event; a failure is a failed event, never an exception.
    /// </summary>
    /// <param name="request">The request, already checked.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The events.</returns>
    protected abstract IAsyncEnumerable<LanguageModelStreamEvent> StreamCore(LanguageModelRequest request, CancellationToken cancellationToken);
}

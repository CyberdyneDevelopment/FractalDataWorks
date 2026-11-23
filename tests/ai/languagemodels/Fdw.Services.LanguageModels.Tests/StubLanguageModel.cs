using System.Runtime.CompilerServices;
using Fdw.Results;
using Fdw.Services.LanguageModels.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fdw.Services.LanguageModels.Tests;

/// <summary>
/// A provider written for the tests: it makes no call to anything, counts the calls that reach it and answers with what the test scripted.
/// A call the test did not script fails loudly.
/// </summary>
internal sealed class StubLanguageModel : LanguageModelBase
{
    private readonly Func<LanguageModelRequest, LanguageModelReply>? _reply;
    private readonly IReadOnlyList<LanguageModelStreamEvent>? _events;

    private StubLanguageModel(
        StubLanguageModelConfiguration configuration,
        Func<LanguageModelRequest, LanguageModelReply>? reply,
        IReadOnlyList<LanguageModelStreamEvent>? events)
        : base(configuration, NullLogger.Instance)
    {
        _reply = reply;
        _events = events;
    }

    public int Calls { get; private set; }

    public LanguageModelRequest? LastRequest { get; private set; }

    public static StubLanguageModel Replying(StubLanguageModelConfiguration configuration, Func<LanguageModelRequest, LanguageModelReply> reply)
        => new(configuration, reply, null);

    public static StubLanguageModel Streaming(StubLanguageModelConfiguration configuration, IReadOnlyList<LanguageModelStreamEvent> events)
        => new(configuration, null, events);

    protected override Task<IGenericResult<LanguageModelReply>> ReplyCore(LanguageModelRequest request, CancellationToken cancellationToken)
    {
        Calls++;
        LastRequest = request;
        var reply = _reply ?? throw new InvalidOperationException("The stub was not scripted to reply.");
        return Task.FromResult(GenericResult<LanguageModelReply>.Success(reply(request)));
    }

    protected override async IAsyncEnumerable<LanguageModelStreamEvent> StreamCore(
        LanguageModelRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Calls++;
        LastRequest = request;
        var events = _events ?? throw new InvalidOperationException("The stub was not scripted to stream.");
        foreach (var streamEvent in events)
        {
            await Task.Yield();
            yield return streamEvent;
        }
    }
}

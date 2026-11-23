using Fdw.Results;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The end of a stream that failed, with the failed result that carries the coded message saying why.</summary>
public sealed class FailedEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="FailedEvent"/> class.</summary>
    /// <param name="failure">The failed result whose coded message says why the stream failed.</param>
    public FailedEvent(IGenericResult failure)
    {
        Failure = failure;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("Failed");

    /// <summary>Gets the failed result whose coded message says why the stream failed.</summary>
    public IGenericResult Failure { get; }
}

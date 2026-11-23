using System;
using System.Collections.Generic;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The end of a stream that went well: why the model stopped, what the call used and what the reply cites.</summary>
public sealed class CompletedEvent : LanguageModelStreamEvent
{
    /// <summary>Initializes a new instance of the <see cref="CompletedEvent"/> class.</summary>
    /// <param name="finishReason">Why the model stopped.</param>
    /// <param name="usage">The tokens the call used.</param>
    /// <param name="sources">The sources the reply cites.</param>
    public CompletedEvent(IFinishReason finishReason, LanguageModelUsage usage, IReadOnlyList<LanguageModelSource> sources)
    {
        FinishReason = finishReason;
        Usage = usage;
        Sources = sources;
    }

    /// <inheritdoc />
    public override IStreamEventKind Kind => StreamEventKinds.ByName("Completed");

    /// <summary>Gets why the model stopped.</summary>
    public IFinishReason FinishReason { get; }

    /// <summary>Gets the tokens the call used.</summary>
    public LanguageModelUsage Usage { get; }

    /// <summary>Gets the sources the reply cites.</summary>
    public IReadOnlyList<LanguageModelSource> Sources { get; }
}

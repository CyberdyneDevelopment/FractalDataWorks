using System;
using System.Collections.Generic;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>What a model answers a request with.</summary>
public sealed class LanguageModelReply
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelReply"/> class.</summary>
    /// <param name="parts">The parts of the reply: text and the tool calls the model asks the host to run.</param>
    /// <param name="reasoningText">The reasoning text, or <see cref="string.Empty"/> when the model gave none.</param>
    /// <param name="hostedToolActivities">What the tools the provider ran itself did.</param>
    /// <param name="sources">The sources the reply cites.</param>
    /// <param name="finishReason">Why the model stopped.</param>
    /// <param name="usage">The tokens the call used.</param>
    public LanguageModelReply(
        IReadOnlyList<LanguageModelMessagePart> parts,
        string reasoningText,
        IReadOnlyList<HostedToolActivity> hostedToolActivities,
        IReadOnlyList<LanguageModelSource> sources,
        IFinishReason finishReason,
        LanguageModelUsage usage)
    {
        Parts = parts;
        ReasoningText = reasoningText;
        HostedToolActivities = hostedToolActivities;
        Sources = sources;
        FinishReason = finishReason;
        Usage = usage;
    }

    /// <summary>Gets the parts of the reply.</summary>
    public IReadOnlyList<LanguageModelMessagePart> Parts { get; }

    /// <summary>Gets the reasoning text.</summary>
    public string ReasoningText { get; }

    /// <summary>Gets what the tools the provider ran itself did.</summary>
    public IReadOnlyList<HostedToolActivity> HostedToolActivities { get; }

    /// <summary>Gets the sources the reply cites.</summary>
    public IReadOnlyList<LanguageModelSource> Sources { get; }

    /// <summary>Gets why the model stopped.</summary>
    public IFinishReason FinishReason { get; }

    /// <summary>Gets the tokens the call used.</summary>
    public LanguageModelUsage Usage { get; }
}

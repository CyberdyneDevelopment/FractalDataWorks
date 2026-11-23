using System;
using System.Collections.Generic;

namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>What a consumer asks a model: the conversation so far, the tools offered, the reasoning effort and the most it may write.</summary>
public sealed class LanguageModelRequest
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelRequest"/> class.</summary>
    /// <param name="messages">The conversation so far.</param>
    /// <param name="tools">The tools offered to the model; empty when none are.</param>
    /// <param name="reasoningEffort">How much reasoning the model does. Stated by the caller on every call; there is no default.</param>
    /// <param name="maxOutputTokens">The most tokens the model may write. Stated by the caller on every call; there is no default.</param>
    /// <param name="systemInstruction">What the model is told before the conversation, or <see langword="null"/> for nothing.</param>
    public LanguageModelRequest(
        IReadOnlyList<LanguageModelMessage> messages,
        IReadOnlyList<LanguageModelToolDefinition> tools,
        IReasoningEffort reasoningEffort,
        int maxOutputTokens,
        string? systemInstruction)
    {
        Messages = messages;
        Tools = tools;
        ReasoningEffort = reasoningEffort;
        MaxOutputTokens = maxOutputTokens;
        SystemInstruction = systemInstruction;
    }

    /// <summary>Gets the conversation so far.</summary>
    public IReadOnlyList<LanguageModelMessage> Messages { get; }

    /// <summary>Gets the tools offered to the model.</summary>
    public IReadOnlyList<LanguageModelToolDefinition> Tools { get; }

    /// <summary>Gets how much reasoning the model does.</summary>
    public IReasoningEffort ReasoningEffort { get; }

    /// <summary>Gets the most tokens the model may write.</summary>
    public int MaxOutputTokens { get; }

    /// <summary>Gets what the model is told before the conversation, or <see langword="null"/> for nothing.</summary>
    public string? SystemInstruction { get; }
}

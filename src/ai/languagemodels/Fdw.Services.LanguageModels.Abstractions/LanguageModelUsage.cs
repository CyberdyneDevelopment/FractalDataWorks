namespace Fdw.Services.LanguageModels.Abstractions;

/// <summary>The tokens one call used, so a consumer can budget a turn.</summary>
public sealed class LanguageModelUsage
{
    /// <summary>Initializes a new instance of the <see cref="LanguageModelUsage"/> class.</summary>
    /// <param name="inputTokens">The tokens the model read.</param>
    /// <param name="outputTokens">The tokens the model wrote.</param>
    public LanguageModelUsage(int inputTokens, int outputTokens)
    {
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
    }

    /// <summary>Gets the tokens the model read.</summary>
    public int InputTokens { get; }

    /// <summary>Gets the tokens the model wrote.</summary>
    public int OutputTokens { get; }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Roslyn.Commands.Abstractions.Results;

/// <summary>
/// Translator not found for command.
/// </summary>
[TypeOption(typeof(RoslynResultCodes), "TranslatorNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TranslatorNotFoundCode : RoslynResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslatorNotFoundCode"/> class.
    /// </summary>
    public TranslatorNotFoundCode()
        : base(60002, "TranslatorNotFound",
            ResultSeverities.ByName("Error"),
            "Translator not found: {Message}",
            isRetryable: false)
    {
    }
}

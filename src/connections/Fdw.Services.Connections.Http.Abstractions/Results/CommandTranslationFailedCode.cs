using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Connections.Http.Abstractions.Results;

/// <summary>
/// Failed to translate command to HTTP request.
/// </summary>
[TypeOption(typeof(HttpResultCodes), "CommandTranslationFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class CommandTranslationFailedCode : HttpResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandTranslationFailedCode"/> class.
    /// </summary>
    public CommandTranslationFailedCode()
        : base(90002, "CommandTranslationFailed",
            ResultSeverities.ByName("Error"),
            "Failed to translate command: {ErrorMessage}",
            isRetryable: false)
    {
    }
}
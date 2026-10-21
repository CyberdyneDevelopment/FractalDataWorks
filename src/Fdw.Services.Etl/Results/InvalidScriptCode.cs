using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A stored script in a save, check or run request was refused: a kind that is neither Query nor Command, a store or text left
/// out, a parameter named twice or of a type nobody knows, or a value a run could not read as its parameter's type.
/// </summary>
/// <remarks>
/// 20004: the script is the caller's input, so the answer is a 400 carrying the reason, never a 500.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidScript", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidScriptCode : EtlResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidScriptCode"/> class.
    /// </summary>
    public InvalidScriptCode()
        : base(20004, "InvalidScript",
            ResultSeverities.ByName("Error"),
            "The script is refused: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A webhook trigger in a save request was refused: a name that cannot be an address, a pipeline that does not exist, roles left out, a
/// mapping to a parameter the pipeline does not declare, or the same parameter mapped twice.
/// </summary>
/// <remarks>
/// 20992: the trigger is the caller's input, so the answer is a 400 carrying the reason, never a 500.
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidWebhook", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidWebhookCode : EtlResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="InvalidWebhookCode"/> class.</summary>
    public InvalidWebhookCode()
        : base(20992, "InvalidWebhook",
            ResultSeverities.ByName("Error"),
            "The webhook trigger is refused: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

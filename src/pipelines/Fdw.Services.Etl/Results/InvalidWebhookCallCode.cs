using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Etl.Results;

/// <summary>
/// A call to a webhook trigger was refused after its secret was accepted: a body that is not JSON, or a value that cannot be given to
/// the pipeline parameter it is mapped to.
/// </summary>
/// <remarks>
/// 20993: the body is the caller's input, so the answer is a 400 carrying the reason, never a 500. A wrong secret is not this code:
/// it is answered without any reason (FDW-1884).
/// </remarks>
[TypeOption(typeof(EtlResultCodes), "InvalidWebhookCall", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InvalidWebhookCallCode : EtlResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="InvalidWebhookCallCode"/> class.</summary>
    public InvalidWebhookCallCode()
        : base(20993, "InvalidWebhookCall",
            ResultSeverities.ByName("Error"),
            "The call is refused: {Reason}",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller why the call was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

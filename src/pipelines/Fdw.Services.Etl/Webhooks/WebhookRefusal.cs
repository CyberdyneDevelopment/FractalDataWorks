using Fdw.Results;
using Fdw.Services.Etl.Results;

namespace Fdw.Services.Etl.Webhooks;

/// <summary>Builds the answer to a webhook trigger request that is refused.</summary>
public static class WebhookRefusal
{
    /// <summary>A refusal that carries the reason.</summary>
    /// <typeparam name="TValue">The type the request answers.</typeparam>
    /// <param name="reason">Why the trigger is refused.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidWebhook"), ResultDetails.Create("Reason", reason));
}

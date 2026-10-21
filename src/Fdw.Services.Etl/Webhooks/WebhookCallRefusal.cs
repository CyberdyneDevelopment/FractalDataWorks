using Fdw.Results;
using Fdw.Services.Etl.Results;

namespace Fdw.Services.Etl.Webhooks;

/// <summary>Builds the answer to a call to a webhook trigger that is refused after its secret was accepted.</summary>
public static class WebhookCallRefusal
{
    /// <summary>A refusal that carries the reason.</summary>
    /// <typeparam name="TValue">The type the call answers.</typeparam>
    /// <param name="reason">Why the call is refused.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidWebhookCall"), ResultDetails.Create("Reason", reason));
}

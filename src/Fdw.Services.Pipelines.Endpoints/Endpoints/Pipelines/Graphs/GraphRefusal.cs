using Fdw.Results;
using Fdw.Services.Etl.Results;
using Fdw.Services.Pipelines.Clients.Abstractions;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The refusal of a graph in a save request (FDW-1396): a 400 that carries the reason the check gave, which names the component
/// and the setting.
/// </summary>
public static class GraphRefusal
{
    /// <summary>Refuses with a reason written for the caller.</summary>
    /// <typeparam name="TValue">The value the refused call would have returned.</typeparam>
    /// <param name="reason">Why the graph is refused.</param>
    /// <returns>The failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidPipelineGraph"), ResultDetails.Create("Reason", reason));

    /// <summary>Turns the failure of a check into the refusal the caller sees, keeping the check's own message as the reason.</summary>
    /// <typeparam name="TValue">The value the refused call would have returned.</typeparam>
    /// <param name="failure">The check's failure.</param>
    /// <returns>The refusal, or the failure itself when it states no message.</returns>
    public static IGenericResult<TValue> From<TValue>(IGenericResult failure) =>
        failure.CurrentMessage is { Length: > 0 } reason ? Of<TValue>(reason) : failure.ToNewResult<TValue>();
}

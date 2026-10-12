using Fdw.Results;
using Fdw.Services.Etl.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// Gives a refusal of a pipeline's load definition the result code that answers the caller with the reason (FDW-1147).
/// </summary>
/// <remarks>
/// Why one place: create and update refuse a load definition in several steps (mapping, structure, schema, the truncate
/// flag), each of which logs its own message. A failure built from a logged message answers the caller only with the generic
/// text of its category, so the same refusal that the validate endpoints list in full used to arrive as "The request was
/// invalid". Every one of those refusals is the caller's own input, so each passes through here and arrives as
/// <c>InvalidPipelineLoad</c>, whose message the response shows.
/// </remarks>
public static class PipelineLoadRefusal
{
    /// <summary>Refuses a load definition for a reason a producer has already logged.</summary>
    /// <param name="reason">The reason, as the producer worded it.</param>
    /// <returns>The coded failure.</returns>
    public static IGenericResult Of(string reason) =>
        GenericResult.Failure(EtlResultCodes.ByName("InvalidPipelineLoad"), ResultDetails.Create("Reason", reason));

    /// <summary>Refuses a load definition for a reason a producer has already logged.</summary>
    /// <typeparam name="TValue">The type of value the failed step would have produced.</typeparam>
    /// <param name="reason">The reason, as the producer worded it.</param>
    /// <returns>The coded failure.</returns>
    public static IGenericResult<TValue> Of<TValue>(string reason) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidPipelineLoad"), ResultDetails.Create("Reason", reason));

    /// <summary>Refuses a load definition for a reason nothing has logged yet, and logs it.</summary>
    /// <typeparam name="TValue">The type of value the failed step would have produced.</typeparam>
    /// <param name="reason">The reason.</param>
    /// <param name="logger">The logger the refusal is written to.</param>
    /// <returns>The coded failure.</returns>
    public static IGenericResult<TValue> Logged<TValue>(string reason, ILogger logger) =>
        GenericResult<TValue>.Failure(EtlResultCodes.ByName("InvalidPipelineLoad"), logger, ResultDetails.Create("Reason", reason));

    /// <summary>Refuses a load definition with the failure a producer returned, keeping its reason.</summary>
    /// <typeparam name="TValue">The type of value the failed step would have produced.</typeparam>
    /// <param name="failure">The producer's failure.</param>
    /// <returns>The coded failure when the producer's failure carries a reason; the producer's failure unchanged when it carries none.</returns>
    public static IGenericResult<TValue> From<TValue>(IGenericResult failure) =>
        failure.CurrentMessage is { Length: > 0 } reason ? Of<TValue>(reason) : failure.ToNewResult<TValue>();
}

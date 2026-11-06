using System;
using System.Collections.Generic;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Data.Abstractions;

namespace Fdw.Services.Etl.Execution;

/// <summary>
/// What kind of failure ended a pipeline run (FDW-3862): the failure's result category, its code, and whether the platform says that category is transient.
/// </summary>
/// <remarks>
/// <para>
/// Read from the code and never from the text: a failure message's number divided by 10000 is its result category, and each category states whether it is retryable (a dependency
/// that could not be reached or timed out is, a bad query or a constraint is not). The reader is the one the HTTP answer uses, <see cref="FailureMessageExtensions.FirstFailureMessage"/>,
/// so the run and the caller are told the same thing about the same failure.
/// </para>
/// <para>
/// A failure that carries no coded failure message, or a code in no known category, is unclassified: it is not transient and not known to be anything else, and a threshold counts it
/// on its own. Nothing is guessed from the message text.
/// </para>
/// </remarks>
/// <param name="Classified">Whether the failure carries a coded failure message in a known category.</param>
/// <param name="Category">The category's name; none when the failure is unclassified.</param>
/// <param name="Code">The failure message's code, in the "PREFIX-NNNNN" form; none when the failure is unclassified.</param>
/// <param name="Transient">Whether the category is one the platform retries; false when the failure is unclassified.</param>
/// <param name="Refused">Whether the failure's category does not count toward a failure threshold (<see cref="IResultCategory.CountsTowardFailureThresholds"/>): a refusal by the state of something the run uses, such as a connection that is disabled. It is not the pipeline's own fault, so a threshold neither counts it nor resets on it (FDW-4005).</param>
/// <param name="ConnectionId">The Id of the connection that raised the failure (FDW-4192, FDW-4279), read from the failure itself; none when no connection raised it.</param>
public sealed record RunFailureClassification(bool Classified, string? Category, string? Code, bool Transient, bool Refused, Guid? ConnectionId)
{
    /// <summary>The type of the event a failed run records, with the keys <c>classified</c>, <c>category</c>, <c>code</c>, <c>transient</c>, <c>refused</c> and <c>connectionId</c>.</summary>
    public const string Event = "run.failed";

    /// <summary>Classifies a failed result.</summary>
    /// <param name="failure">The failed result.</param>
    /// <returns>The classification. Its connection is the one the failure names as its raiser (<see cref="ConnectionFailures.ConnectionOf"/>); a failure that names none was not raised by a connection, so it attributes to none (FDW-4279).</returns>
    public static RunFailureClassification Of(IGenericResult failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        var connectionId = ConnectionFailures.ConnectionOf(failure);
        var category = FailureCategory.Of(failure);
        var code = CodeText(failure);
        return category is null || ReferenceEquals(category, ResultCategories.NotFound) || !category.IsFailure
            ? new RunFailureClassification(false, null, code, false, false, connectionId)
            : new RunFailureClassification(true, category.Name, code, category.IsRetryable, !category.CountsTowardFailureThresholds, connectionId);
    }

    // The code of the failure the category was read from, in the "PREFIX-NNNNN" form: the result's own code when it is a failure code, else the first failure message's.
    private static string? CodeText(IGenericResult failure) =>
        failure.Code is { Id: >= 20000 } code ? code.Code : failure.FirstFailureMessage()?.Code;

    /// <summary>Gets the event's data.</summary>
    /// <returns>The values the event records.</returns>
    public IReadOnlyDictionary<string, object?> ToEventData() => new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        ["classified"] = Classified,
        ["category"] = Category,
        ["code"] = Code,
        ["transient"] = Transient,
        ["refused"] = Refused,
        ["connectionId"] = ConnectionId?.ToString(),
    };

    /// <summary>Words the classification for the event's message.</summary>
    /// <returns>A sentence naming the category and whether it is transient, or saying the failure is unclassified.</returns>
    public string Describe() => (Classified, Refused) switch
    {
        (true, true) => $"The run was refused with a {Category} failure ({Code}); a refusal is not the pipeline's own fault and is not counted",
        (true, false) => $"The run failed with a {Category} failure ({Code}), which is {(Transient ? "transient" : "not transient")}",
        (false, true) => $"The run was refused ({Code}); a refusal is not the pipeline's own fault and is not counted",
        _ => "The run failed with a failure that carries no coded category",
    };
}

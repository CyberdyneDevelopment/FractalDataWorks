using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A note was marked answered and its kind cannot be answered.
/// </summary>
/// <remarks>Whether a note can be answered is stated by its GraphFieldNoteKinds option (CanBeAnswered): a reply cannot be. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetNoteCannotBeAnswered", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetNoteCannotBeAnsweredCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetNoteCannotBeAnsweredCode"/> class.</summary>
    public GraphDataSetNoteCannotBeAnsweredCode()
        : base(21433, "GraphDataSetNoteCannotBeAnswered",
            ResultSeverities.ByName("Error"),
            "Note '{note}' on field '{field}' of Graph data set '{dataSet}' is a {kind} and cannot be answered",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which note cannot be answered and that its kind is the reason.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A route or an update named a note that the field out does not have.
/// </summary>
/// <remarks>Notes are addressed by their stable id (FDW-5452); an id that matches none of the field's notes is refused, never made into a new note. HTTP 404.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetNoteNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetNoteNotFoundCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetNoteNotFoundCode"/> class.</summary>
    public GraphDataSetNoteNotFoundCode()
        : base(31431, "GraphDataSetNoteNotFound",
            ResultSeverities.ByName("Error"),
            "Note '{note}' is not a note on field '{field}' of Graph data set '{dataSet}'",
            isRetryable: false)
    {
    }

    /// <summary>Tells the caller which note the field does not have.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

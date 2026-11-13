using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// A field out is fed by a field its component's output does not have.
/// </summary>
/// <remarks>Reported by the check before save and by save (FDW-5522), path <c>fieldsOut/{field}</c>, when the fields of the output that carries the fields out are known (derived by the describer from the data sets and the components over them) and the field is not among them. HTTP 400.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFedFieldMissing", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFedFieldMissingCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFedFieldMissingCode"/> class.</summary>
    public GraphDataSetFedFieldMissingCode()
        : base(21423, "GraphDataSetFedFieldMissing",
            ResultSeverities.ByName("Error"),
            "Field out '{field}' of Graph data set '{dataSet}' is fed by field '{feedField}' of output '{output}' of component '{component}', which that output does not have",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

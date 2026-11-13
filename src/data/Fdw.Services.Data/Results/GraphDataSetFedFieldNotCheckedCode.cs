using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Services.Data.Results;

/// <summary>
/// The fields of the output that feeds a field out are not known, so the field it is fed by was not checked.
/// </summary>
/// <remarks>A NOTE, not a problem (FDW-5522): it is reported in the notChecked list of the check's answer, path <c>fieldsOut/{field}</c>, never blocks a save and is not read by save. The describer derives no fields for a container or SQL source, or for a kind it has no deriver for; when the describer FAILED the reason says so with its own message, so a broken describer cannot hide here.</remarks>
[TypeOption(typeof(DataServiceResultCodes), "GraphDataSetFedFieldNotChecked", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class GraphDataSetFedFieldNotCheckedCode : DataServiceResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="GraphDataSetFedFieldNotCheckedCode"/> class.</summary>
    public GraphDataSetFedFieldNotCheckedCode()
        : base(21424, "GraphDataSetFedFieldNotChecked",
            ResultSeverities.ByName("Information"),
            "The fields of output '{output}' of component '{component}' are not known ({reason}), so field '{feedField}' that feeds field out '{field}' of Graph data set '{dataSet}' was not checked",
            isRetryable: false)
    {
    }

    /// <summary>Shows the caller what is wrong with their own Graph data set: the message names their data set and what they stated.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

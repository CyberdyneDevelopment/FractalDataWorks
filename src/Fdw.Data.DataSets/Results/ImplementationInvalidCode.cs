using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Collections.Attributes;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Results.Abstractions;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The requested/merged Implementation is missing or not a registered DataSetTypes strategy
/// (Simple/Compound/Federated). Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
/// <remarks>
/// The message is the caller's own input refused, so it is shown to them (as <c>InvalidPipelineLoad</c> does): without it the
/// answer is the generic "The request was invalid" and the caller cannot tell which implementation is wrong. The valid
/// implementations are read from <see cref="DataSetTypes"/> when the details are built, so a kind registered outside FDW is listed.
/// </remarks>
[TypeOption(typeof(DataSetsResultCodes), "ImplementationInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class ImplementationInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ImplementationInvalidCode"/> class.
    /// </summary>
    public ImplementationInvalidCode()
        : base(20001, "ImplementationInvalid",
            ResultSeverities.ByName("Error"),
            "DataSet '{name}' create/update rejected: Implementation '{implementation}' is not a registered DataSetTypes strategy; valid implementations: {valid}",
            isRetryable: false)
    {
    }

    /// <summary>Builds the details a refusal carries: the data set, the implementation it stated and the valid ones.</summary>
    /// <param name="name">The data set's name.</param>
    /// <param name="implementation">The implementation the request states; null when it states none.</param>
    /// <returns>The details to log the refusal with.</returns>
    public static ResultDetails Describe(string name, string? implementation) =>
        ResultDetails.Create(
            "name", name,
            "implementation", implementation,
            "valid", string.Join(", ", DataSetTypes.All().Select(type => type.Name).Order(StringComparer.Ordinal)));

    /// <summary>Shows the caller why the request was refused: the message is written for them.</summary>
    /// <param name="details">The details formatted into the message.</param>
    /// <returns>The formatted message.</returns>
    public override string? FormatClientMessage(IResultDetails? details = null) => FormatMessage(details);
}

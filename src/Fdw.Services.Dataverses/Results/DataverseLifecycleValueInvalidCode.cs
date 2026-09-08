using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Dataverses.Results;

/// <summary>
/// A dataverse's Status, Visibility or JoinPolicy was missing or not a registered option.
/// </summary>
/// <remarks>
/// Caller-input validation — HTTP 400. Names the field and the offending value, because the
/// alternative is a database CHECK violation that names a constraint and leaves the caller to
/// work out which of three columns it meant.
/// </remarks>
[TypeOption(typeof(DataversesResultCodes), "DataverseLifecycleValueInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DataverseLifecycleValueInvalidCode : DataversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataverseLifecycleValueInvalidCode"/> class.</summary>
    public DataverseLifecycleValueInvalidCode()
        : base(20001, "DataverseLifecycleValueInvalid",
            ResultSeverities.ByName("Error"),
            "Dataverse '{name}' rejected: {field} '{value}' is not a registered option",
            isRetryable: false)
    {
    }
}

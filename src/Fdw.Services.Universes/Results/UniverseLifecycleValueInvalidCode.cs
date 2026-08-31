using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Universes.Results;

/// <summary>
/// A universe's Status, Visibility or JoinPolicy was missing or not a registered option.
/// </summary>
/// <remarks>
/// Caller-input validation — HTTP 400. Names the field and the offending value, because the
/// alternative is a database CHECK violation that names a constraint and leaves the caller to
/// work out which of three columns it meant.
/// </remarks>
[TypeOption(typeof(UniversesResultCodes), "UniverseLifecycleValueInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UniverseLifecycleValueInvalidCode : UniversesResultCodeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniverseLifecycleValueInvalidCode"/> class.</summary>
    public UniverseLifecycleValueInvalidCode()
        : base(20001, "UniverseLifecycleValueInvalid",
            ResultSeverities.ByName("Error"),
            "Universe '{name}' rejected: {field} '{value}' is not a registered option",
            isRetryable: false)
    {
    }
}

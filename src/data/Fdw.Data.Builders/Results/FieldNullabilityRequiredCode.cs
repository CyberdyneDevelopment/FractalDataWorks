using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// The field states neither required nor optional. Nothing assumes one for it (FDW-3931).
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "FieldNullabilityRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNullabilityRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNullabilityRequiredCode"/> class.
    /// </summary>
    public FieldNullabilityRequiredCode()
        : base(21029, "FieldNullabilityRequired",
            ResultSeverities.ByName("Error"),
            "Field nullability is required",
            isRetryable: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Field name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "FieldNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldNameRequiredCode"/> class.
    /// </summary>
    public FieldNameRequiredCode()
        : base(21010, "FieldNameRequired",
            ResultSeverities.ByName("Error"),
            "Field name is required",
            isRetryable: false)
    {
    }
}
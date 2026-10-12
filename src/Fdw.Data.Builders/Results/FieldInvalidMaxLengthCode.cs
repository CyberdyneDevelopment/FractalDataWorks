using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// MaxLength must be greater than zero.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "FieldInvalidMaxLength", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldInvalidMaxLengthCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldInvalidMaxLengthCode"/> class.
    /// </summary>
    public FieldInvalidMaxLengthCode()
        : base(21012, "FieldInvalidMaxLength",
            ResultSeverities.ByName("Error"),
            "MaxLength for field '{FieldName}' must be greater than zero",
            isRetryable: false)
    {
    }
}
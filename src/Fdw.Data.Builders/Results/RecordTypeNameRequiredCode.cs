using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.Builders.Results;

/// <summary>
/// Record type name is required.
/// </summary>
[TypeOption(typeof(BuilderResultCodes), "RecordTypeNameRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class RecordTypeNameRequiredCode : BuilderResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RecordTypeNameRequiredCode"/> class.
    /// </summary>
    public RecordTypeNameRequiredCode()
        : base(21006, "RecordTypeNameRequired",
            ResultSeverities.ByName("Error"),
            "Record type name is required",
            isRetryable: false)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// The FieldMappingId addressing a transform chain was absent or not a GUID.
/// Caller-input validation failure — HTTP 400 (Validation category).
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FieldMappingIdInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldMappingIdInvalidCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMappingIdInvalidCode"/> class.
    /// </summary>
    public FieldMappingIdInvalidCode()
        : base(20010, "FieldMappingIdInvalid",
            ResultSeverities.ByName("Error"),
            "Transform chain request rejected: field mapping id '{fieldMappingId}' is absent or not a GUID",
            isRetryable: false)
    {
    }
}

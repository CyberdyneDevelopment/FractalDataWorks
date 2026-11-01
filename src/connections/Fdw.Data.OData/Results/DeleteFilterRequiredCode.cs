using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// DeleteCommand requires Filter in metadata for safety.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "DeleteFilterRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteFilterRequiredCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFilterRequiredCode"/> class.
    /// </summary>
    public DeleteFilterRequiredCode()
        : base(21000, "DeleteFilterRequired",
            ResultSeverities.ByName("Error"),
            "DeleteCommand must have Filter in metadata - delete without resource ID not allowed for safety",
            isRetryable: false)
    {
    }
}
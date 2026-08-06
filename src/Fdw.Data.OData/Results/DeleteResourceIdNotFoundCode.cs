using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Cannot determine resource ID from Filter expression.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "DeleteResourceIdNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteResourceIdNotFoundCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteResourceIdNotFoundCode"/> class.
    /// </summary>
    public DeleteResourceIdNotFoundCode()
        : base(21003, "DeleteResourceIdNotFound",
            ResultSeverities.ByName("Error"),
            "Cannot determine resource ID from Filter - must include primary key condition",
            isRetryable: false)
    {
    }
}
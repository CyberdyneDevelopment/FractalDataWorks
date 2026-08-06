using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// DeleteCommand has invalid Filter expression.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "DeleteFilterInvalid", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class DeleteFilterInvalidCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteFilterInvalidCode"/> class.
    /// </summary>
    public DeleteFilterInvalidCode()
        : base(20001, "DeleteFilterInvalid",
            ResultSeverities.ByName("Error"),
            "DeleteCommand must have valid Filter with Root node",
            isRetryable: false)
    {
    }
}
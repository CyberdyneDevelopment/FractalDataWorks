using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// Cannot determine resource ID for update.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "UpdateResourceIdNotFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateResourceIdNotFoundCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateResourceIdNotFoundCode"/> class.
    /// </summary>
    public UpdateResourceIdNotFoundCode()
        : base(30000, "UpdateResourceIdNotFound",
            ResultSeverities.ByName("Error"),
            "Cannot determine resource ID for update - need Filter or primary key in data",
            isRetryable: false)
    {
    }
}
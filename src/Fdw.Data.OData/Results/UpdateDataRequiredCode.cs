using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// UpdateCommand requires Data in metadata.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "UpdateDataRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class UpdateDataRequiredCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDataRequiredCode"/> class.
    /// </summary>
    public UpdateDataRequiredCode()
        : base(21002, "UpdateDataRequired",
            ResultSeverities.ByName("Error"),
            "UpdateCommand must have Data in metadata",
            isRetryable: false)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.OData.Results;

/// <summary>
/// InsertCommand requires Data in metadata.
/// </summary>
[TypeOption(typeof(ODataResultCodes), "InsertDataRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class InsertDataRequiredCode : RestDataResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InsertDataRequiredCode"/> class.
    /// </summary>
    public InsertDataRequiredCode()
        : base(21001, "InsertDataRequired",
            ResultSeverities.ByName("Error"),
            "InsertCommand must have Data in metadata",
            isRetryable: false)
    {
    }
}
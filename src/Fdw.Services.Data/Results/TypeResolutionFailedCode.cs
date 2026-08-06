using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Failed to resolve the record type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "TypeResolutionFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeResolutionFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypeResolutionFailedCode"/> class.
    /// </summary>
    public TypeResolutionFailedCode()
        : base(91006, "TypeResolutionFailed", ResultSeverities.ByName("Error"),
            "Failed to resolve type '{TypeName}' for DataSet '{DataSetName}': {Error}",
            isRetryable: false)
    {
    }
}
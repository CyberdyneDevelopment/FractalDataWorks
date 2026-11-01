using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Failed to map dictionary to POCO type (mapping error).
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "PocoMappingFailed", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class PocoMappingFailedCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PocoMappingFailedCode"/> class.
    /// </summary>
    public PocoMappingFailedCode()
        : base(91004, "PocoMappingFailed", ResultSeverities.ByName("Error"),
            "Failed to map dictionary to POCO type '{TypeName}': {Reason}",
            isRetryable: false)
    {
    }
}
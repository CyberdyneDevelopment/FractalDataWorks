using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Source mapping configuration was null.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourceMappingRequired", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMappingRequiredCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMappingRequiredCode"/> class.
    /// </summary>
    public SourceMappingRequiredCode()
        : base(21015, "SourceMappingRequired", ResultSeverities.ByName("Error"),
            "Source mapping configuration cannot be null",
            isRetryable: false)
    {
    }
}
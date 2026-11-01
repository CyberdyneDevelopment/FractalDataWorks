using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Source has no field mappings defined.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourceNoFieldMappings", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceNoFieldMappingsCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceNoFieldMappingsCode"/> class.
    /// </summary>
    public SourceNoFieldMappingsCode()
        : base(21016, "SourceNoFieldMappings", ResultSeverities.ByName("Error"),
            "Source '{SourceName}' has no field mappings defined",
            isRetryable: false)
    {
    }
}
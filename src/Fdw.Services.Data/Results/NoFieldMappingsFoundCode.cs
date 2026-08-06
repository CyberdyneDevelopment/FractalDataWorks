using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// No field mappings found for source.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "NoFieldMappingsFound", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class NoFieldMappingsFoundCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NoFieldMappingsFoundCode"/> class.
    /// </summary>
    public NoFieldMappingsFoundCode()
        : base(31010, "NoFieldMappingsFound", ResultSeverities.ByName("Error"),
            "No field mappings found for source '{SourceName}' in DataSet '{DataSetName}'",
            isRetryable: false)
    {
    }
}
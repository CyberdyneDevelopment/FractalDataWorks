using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Services.Data.Results;

/// <summary>
/// Source mapping type is not compatible with DataStore type.
/// </summary>
[TypeOption(typeof(DataServiceResultCodes), "SourceMappingIncompatible", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceMappingIncompatibleCode : DataServiceResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceMappingIncompatibleCode"/> class.
    /// </summary>
    public SourceMappingIncompatibleCode()
        : base(41000, "SourceMappingIncompatible", ResultSeverities.ByName("Error"),
            "Source mapping type is not compatible with DataStore type '{StoreType}'",
            isRetryable: false)
    {
    }
}
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Field mapping resolution requires database configuration.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "FieldMappingResolutionRequiresDatabaseConfiguration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class FieldMappingResolutionRequiresDatabaseConfigurationCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldMappingResolutionRequiresDatabaseConfigurationCode"/> class.
    /// </summary>
    public FieldMappingResolutionRequiresDatabaseConfigurationCode()
        : base(61000, "FieldMappingResolutionRequiresDatabaseConfiguration",
            ResultSeverities.ByName("Error"),
            "Field mapping resolution requires database configuration. Call AddMsSqlConfiguration() to enable FK-based field mapping resolution.",
            isRetryable: false)
    {
    }
}

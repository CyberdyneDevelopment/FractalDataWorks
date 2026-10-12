using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;

namespace Fdw.Data.DataSets.Results;

/// <summary>
/// Source resolution requires database configuration.
/// </summary>
[TypeOption(typeof(DataSetsResultCodes), "SourceResolutionRequiresDatabaseConfiguration", RestrictToCurrentCompilation = true)]
[ExcludeFromCodeCoverage]
public sealed class SourceResolutionRequiresDatabaseConfigurationCode : DataSetsResultCodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceResolutionRequiresDatabaseConfigurationCode"/> class.
    /// </summary>
    public SourceResolutionRequiresDatabaseConfigurationCode()
        : base(60001, "SourceResolutionRequiresDatabaseConfiguration",
            ResultSeverities.ByName("Error"),
            "Source resolution requires database configuration. Call AddMsSqlConfiguration() to enable FK-based source resolution.",
            isRetryable: false)
    {
    }
}

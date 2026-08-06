using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions.Options;

/// <summary>
/// Warning severity type indicating quality issues that should be reviewed but don't block processing.
/// </summary>
[TypeOption(typeof(QualitySeverityTypes), "Warning")]
[ExcludeFromCodeCoverage]
public sealed class WarningSeverityType : QualitySeverityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WarningSeverityType"/> class.
    /// </summary>
    public WarningSeverityType()
        : base(
            id: 2,
            name: "Warning",
            priority: 2,
            blocksProcessing: false)
    {
    }
}

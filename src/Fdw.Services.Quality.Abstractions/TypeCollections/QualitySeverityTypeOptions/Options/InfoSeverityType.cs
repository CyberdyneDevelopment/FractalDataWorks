using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions.Options;

/// <summary>
/// Info severity type indicating informational quality observations that don't require action.
/// </summary>
[TypeOption(typeof(QualitySeverityTypes), "Info")]
[ExcludeFromCodeCoverage]
public sealed class InfoSeverityType : QualitySeverityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InfoSeverityType"/> class.
    /// </summary>
    public InfoSeverityType()
        : base(
            id: 3,
            name: "Info",
            priority: 3,
            blocksProcessing: false)
    {
    }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Quality.Abstractions.TypeCollections.QualitySeverityTypeOptions.Options;

/// <summary>
/// Error severity type indicating critical quality violations that block processing.
/// </summary>
[TypeOption(typeof(QualitySeverityTypes), "Error")]
[ExcludeFromCodeCoverage]
public sealed class ErrorSeverityType : QualitySeverityTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorSeverityType"/> class.
    /// </summary>
    public ErrorSeverityType()
        : base(
            id: 1,
            name: "Error",
            priority: 1,
            blocksProcessing: true)
    {
    }
}

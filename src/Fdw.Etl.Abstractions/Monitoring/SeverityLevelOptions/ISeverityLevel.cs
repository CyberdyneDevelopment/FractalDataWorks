using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Interface for severity levels used in telemetry traces.
/// Extends ITypeOption to enable TypeCollection discovery.
/// </summary>
public interface ISeverityLevel : ITypeOption<int, SeverityLevelBase>
{
    /// <summary>
    /// Gets the numeric level (higher = more severe).
    /// </summary>
    int Level { get; }

    /// <summary>
    /// Gets a value indicating whether this severity should be logged by default.
    /// </summary>
    bool LogByDefault { get; }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;

namespace Fdw.Etl.Abstractions.Monitoring.SeverityLevelOptions;

/// <summary>
/// Base class for severity levels used in telemetry traces.
/// </summary>
/// <ExcludeFromCoverageReason>TypeOption base class - no logic to test</ExcludeFromCoverageReason>
[ExcludeFromCodeCoverage]
public abstract class SeverityLevelBase : TypeOptionBase<int, SeverityLevelBase>, ISeverityLevel
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SeverityLevelBase"/> class.
    /// </summary>
    /// <param name="id">The unique identifier for this severity level.</param>
    /// <param name="name">The name of this severity level.</param>
    /// <param name="level">The numeric level.</param>
    /// <param name="logByDefault">Whether this severity should be logged by default.</param>
    protected SeverityLevelBase(int id, string name, int level, bool logByDefault)
        : base(id, name)
    {
        Level = level;
        LogByDefault = logByDefault;
    }

    /// <inheritdoc />
    public int Level { get; }

    /// <inheritdoc />
    public bool LogByDefault { get; }
}

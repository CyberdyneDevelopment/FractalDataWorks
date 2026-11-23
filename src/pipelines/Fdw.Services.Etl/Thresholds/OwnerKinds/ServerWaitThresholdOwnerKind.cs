using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>The server: it is not a pipeline or a schedule, so there is no owner to find.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(WaitThresholdOwnerKinds), "Server")]
public sealed class ServerWaitThresholdOwnerKind : WaitThresholdOwnerKindBase
{
    /// <summary>Initializes a new instance of the <see cref="ServerWaitThresholdOwnerKind"/> class.</summary>
    public ServerWaitThresholdOwnerKind()
        : base(WaitThresholdOwners.Server)
    {
    }

    /// <inheritdoc />
    public override bool OwnerExists(string owner, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules) => true;
}

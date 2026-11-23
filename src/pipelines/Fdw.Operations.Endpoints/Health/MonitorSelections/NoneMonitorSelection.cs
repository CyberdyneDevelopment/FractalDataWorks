using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>The domain names no monitor.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MonitorSelections), "None")]
public sealed class NoneMonitorSelection : MonitorSelectionBase
{
    /// <summary>Initializes a new instance of the <see cref="NoneMonitorSelection"/> class.</summary>
    public NoneMonitorSelection()
        : base("None")
    {
    }

    /// <inheritdoc />
    public override bool Matches(int count) => count == 0;

    /// <inheritdoc />
    public override IGenericResult<string> Answer(IReadOnlyList<string> names, ILogger logger) => GenericResult<string>.Failure(OperationsEndpointLog.HealthMonitorNoneSelected(logger));
}

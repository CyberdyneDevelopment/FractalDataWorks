using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>The domain names more than one monitor.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MonitorSelections), "Several")]
public sealed class SeveralMonitorSelection : MonitorSelectionBase
{
    /// <summary>Initializes a new instance of the <see cref="SeveralMonitorSelection"/> class.</summary>
    public SeveralMonitorSelection()
        : base("Several")
    {
    }

    /// <inheritdoc />
    public override bool Matches(int count) => count > 1;

    /// <inheritdoc />
    public override IGenericResult<string> Answer(IReadOnlyList<string> names, ILogger logger) => GenericResult<string>.Failure(OperationsEndpointLog.HealthMonitorSeveralSelected(logger, names.Count));
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Microsoft.Extensions.Logging;

namespace Fdw.Operations.Endpoints.Health;

/// <summary>The domain names exactly one monitor.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(MonitorSelections), "Exactly")]
public sealed class ExactlyMonitorSelection : MonitorSelectionBase
{
    /// <summary>Initializes a new instance of the <see cref="ExactlyMonitorSelection"/> class.</summary>
    public ExactlyMonitorSelection()
        : base("Exactly")
    {
    }

    /// <inheritdoc />
    public override bool Matches(int count) => count == 1;

    /// <inheritdoc />
    public override IGenericResult<string> Answer(IReadOnlyList<string> names, ILogger logger) => GenericResult<string>.Success(names[0]);
}

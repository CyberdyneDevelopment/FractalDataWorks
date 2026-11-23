using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Services.Etl.Abstractions.Thresholds;

namespace Fdw.Services.Etl.Thresholds;

/// <summary>Base class for the kinds of owner a wait threshold level can have.</summary>
public abstract class WaitThresholdOwnerKindBase : TypeOptionBase<WaitThresholdOwnerKindBase>, IWaitThresholdOwnerKind
{
    /// <summary>Initializes a new instance of the <see cref="WaitThresholdOwnerKindBase"/> class.</summary>
    /// <param name="name">The option name, the word a stored level names its owner's kind by.</param>
    protected WaitThresholdOwnerKindBase(string name)
        : base(name)
    {
    }

    /// <inheritdoc />
    public abstract bool OwnerExists(string owner, IReadOnlyDictionary<string, WaitThresholdLayer> pipelines, IReadOnlyDictionary<string, WaitThresholdLayer> schedules);
}

using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>Base class for the ways a stored filter group combines what is in it.</summary>
public abstract class ThresholdGroupOperatorBase : TypeOptionBase<ThresholdGroupOperatorBase>, IThresholdGroupOperator
{
    /// <summary>Initializes a new instance of the <see cref="ThresholdGroupOperatorBase"/> class.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="word">The word a stored group names its operator by.</param>
    protected ThresholdGroupOperatorBase(string name, string word)
        : base(name)
    {
        Word = word;
    }

    /// <inheritdoc />
    [TypeLookup("ByWord")]
    public string Word { get; }

    /// <inheritdoc />
    public abstract LogicalOperator Logical { get; }
}

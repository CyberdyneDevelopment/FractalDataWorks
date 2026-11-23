using Fdw.Collections;
using Fdw.Data.Abstractions;

namespace Fdw.Operations.Thresholds;

/// <summary>How a stored filter group combines what is in it.</summary>
public interface IThresholdGroupOperator : ITypeOption<int, ThresholdGroupOperatorBase>
{
    /// <summary>Gets the word a stored group names its operator by: <c>All</c> or <c>Any</c>.</summary>
    string Word { get; }

    /// <summary>Gets the filter model's logical operator this word means.</summary>
    LogicalOperator Logical { get; }
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>
/// The data set is not written by a pipeline and reads no data set a pipeline writes, so a status from runs does not apply to it (FDW-5296): a Graph data set that reads
/// stores and containers directly, or only intends to read, is read live and has nothing to be current or idle about.
/// </summary>
/// <remarks>
/// Stated by the data set's kind (<see cref="IDataSetStatus.IsStatedByKind"/>), never picked from writers: its rule applies to no input, so it cannot take a
/// data set a pipeline writes from <c>Unwritten</c>, and the resolver's answer for every existing kind is unchanged.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(DataSetStatuses), "NotApplicable", RestrictToCurrentCompilation = true)]
public sealed class NotApplicableDataSetStatus : DataSetStatusBase
{
    /// <summary>Initializes a new instance of the <see cref="NotApplicableDataSetStatus"/> class.</summary>
    public NotApplicableDataSetStatus()
        : base("NotApplicable", 950)
    {
    }

    /// <inheritdoc/>
    public override bool IsStatedByKind => true;

    /// <inheritdoc/>
    public override bool Applies(DataSetStatusInput input)
        => false;
}

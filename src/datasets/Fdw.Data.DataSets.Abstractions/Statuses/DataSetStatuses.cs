using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>The statuses a data set can have in a list, read off the latest runs of the pipelines that write it (FDW-4488).</summary>
/// <remarks>
/// Extensible by design (Mike: "this is a framework, give the developer the ability to control it"). Each option carries its own rule and a rank; the highest-ranked
/// option whose rule applies is the data set's status. A developer adds an option in their own assembly with <c>[TypeOption(typeof(DataSetStatuses), "Name")]</c>; to
/// change what a shipped status means, add an option with another name and a higher rank that claims the case first.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(DataSetStatusBase), typeof(IDataSetStatus), typeof(DataSetStatuses))]
public abstract partial class DataSetStatuses : TypeCollectionBase<DataSetStatusBase, IDataSetStatus>
{
}

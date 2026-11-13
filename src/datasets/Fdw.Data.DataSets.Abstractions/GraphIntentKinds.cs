using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Data.DataSets.Abstractions;

/// <summary>What a Graph data set's intended source names: a data store or another data set (FDW-5294).</summary>
/// <remarks>
/// Extensible by design: <c>data.GraphDataSetIntent.IntentKind</c> carries no CHECK over the names, and its row CHECKs bind only the
/// kinds they name. No code switches on a kind's name; each states what it names through <see cref="IGraphIntentKind"/>.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(GraphIntentKindBase), typeof(IGraphIntentKind), typeof(GraphIntentKinds))]
public abstract partial class GraphIntentKinds : TypeCollectionBase<GraphIntentKindBase, IGraphIntentKind>
{
}

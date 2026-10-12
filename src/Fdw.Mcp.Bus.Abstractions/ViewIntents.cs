using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Mcp.Bus.Abstractions;

namespace Fdw.Mcp.Bus;

/// <summary>TypeCollection of view intents. Wave 1 ships Silent, Update, Compare, Ghost.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ViewIntentBase), typeof(IViewIntent), typeof(ViewIntents))]
public abstract partial class ViewIntents : TypeCollectionBase<ViewIntentBase, IViewIntent>
{
}

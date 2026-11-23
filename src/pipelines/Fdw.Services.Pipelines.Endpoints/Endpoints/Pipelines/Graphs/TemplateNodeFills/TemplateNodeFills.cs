using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Pipelines.Endpoints;

/// <summary>
/// The kinds of JSON node whose children a template fills: an object (each property) and an array (each item).
/// </summary>
/// <remarks>A node is found by asking each option whether it <c>Matches</c>; a node no option matches (a value, null) has no children to fill.</remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(TemplateNodeFillBase), typeof(ITemplateNodeFill), typeof(TemplateNodeFills))]
public abstract partial class TemplateNodeFills : TypeCollectionBase<TemplateNodeFillBase, ITemplateNodeFill>
{
}

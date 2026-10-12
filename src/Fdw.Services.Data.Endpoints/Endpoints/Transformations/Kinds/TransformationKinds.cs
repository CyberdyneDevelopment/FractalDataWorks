using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Data.Endpoints;

/// <summary>
/// The kinds of transformation the <c>/transforms</c> resource authors, chosen by a request's <c>implementation</c>.
/// </summary>
/// <remarks>
/// Why a collection and not a switch in the endpoints: each kind owns what only it knows (which members a request may state,
/// how it is checked and built, how it is read back), so a new kind is one more option and the endpoints do not change.
/// </remarks>
[TypeCollection(typeof(TransformationKindBase), typeof(ITransformationKind), typeof(TransformationKinds))]
[ExcludeFromCodeCoverage]
public abstract partial class TransformationKinds : TypeCollectionBase<TransformationKindBase, ITransformationKind>
{
}

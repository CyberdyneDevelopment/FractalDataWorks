using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Services.Authentication.Abstractions.Context;

/// <summary>
/// The things an authentication step may require or contribute.
/// </summary>
/// <remarks>
/// Closed deliberately. It is the vocabulary the chain is made of, not a list of what anyone might
/// want to do — a new step contributing <c>Claims</c> needs no change here. The issued token and
/// the session are absent: the flow's product is not something a step may read or write. A
/// TypeCollection rather than an enum because <see cref="AuthenticationContext"/> asks each element
/// itself whether it is present, not the other way around.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ContextElementBase), typeof(IContextElement), typeof(ContextElements))]
public abstract partial class ContextElements : TypeCollectionBase<ContextElementBase, IContextElement>
{
}

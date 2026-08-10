using Fdw.Collections;

namespace Fdw.Web.RestEndpoints.EndpointOptions;

/// <summary>
/// Base for a collection of declared endpoints, one collection per resource.
/// </summary>
/// <typeparam name="TBase">The option base every member of this collection derives from.</typeparam>
/// <remarks>
/// One collection per resource rather than per package: the endpoints over a resource are a CRUD
/// set, and the bases already say so through <c>ResourceName</c>. That granularity is what makes a
/// whole resource switchable in one move — skipping <c>ServerSettingEndpoints</c> takes its four
/// endpoints with it and leaves tenant and role settings alone.
///
/// A resource collection can in turn declare itself a member of its domain's collection, giving
/// three levels a host can switch at: one endpoint, one resource, or the whole domain. A child
/// declares the parent with <c>TypeOption</c> plus <c>TypeOptionName</c>; the parent declares
/// nothing and exposes each child as a <see cref="System.Type"/>.
///
/// Members register through a module initializer, not a static constructor — a call such as
/// <c>ServerSettingEndpoints.ByName(name)</c> binds to an inherited static, and C# does not run the
/// derived type's static constructor for that. It is also what lets a host add its own
/// <c>[TypeOption]</c> to a collection it does not own: replacing a packaged endpoint is a
/// <c>SkipRegistration</c> on the original plus a member declared in the host's own assembly.
/// </remarks>
public abstract class EndpointTypeCollectionBase<TBase> : TypeCollectionBase<TBase, IEndpointTypeOption>
    where TBase : EndpointTypeOptionBase, IEndpointTypeOption
{
}

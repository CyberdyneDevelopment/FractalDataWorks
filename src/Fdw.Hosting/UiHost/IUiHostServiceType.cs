using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Fdw.Hosting.UiHost;

/// <summary>
/// What a skin may set on its Blazor surface, resolved from the collection by name.
/// </summary>
/// <remarks>
/// The counterpart of <c>IApiHostServiceType</c>, and it exists for the same reason: an
/// abstract property can only be supplied by deriving, so a skin naming its own root component had
/// to publish a service-type package to hold that one line. Set from Program.cs instead, and the
/// package becomes something a shared bundle can carry.
///
/// Everything a skin does <em>not</em> set stays with the base — the pipeline order in particular,
/// because forwarded headers must precede anything reading the scheme, authentication precedes
/// authorization, antiforgery follows both, and the component router comes last. A skin contributes
/// through Pipeline and Mapping rather than restating that sequence.
/// </remarks>
public interface IUiHostServiceType
{
    /// <summary>Sets the root component the router mounts.</summary>
    /// <param name="component">The skin's root component, normally its <c>App</c>.</param>
    /// <returns>This, for chaining.</returns>
    IUiHostServiceType Root(Type component);

    /// <summary>Sets the path the exception handler redirects to outside development.</summary>
    /// <param name="path">The error path.</param>
    /// <returns>This, for chaining.</returns>
    IUiHostServiceType Error(string path);

    /// <summary>Sets the body that adds middleware between the framework pipeline and the router.</summary>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IUiHostServiceType Pipeline(Action<IApplicationBuilder> method);

    /// <summary>Sets the body that maps routes this skin serves beyond its components.</summary>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IUiHostServiceType Mapping(Action<IEndpointRouteBuilder> method);
}

using System;
using System.Collections.Generic;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using NSwag.Generation.AspNetCore;

namespace Fdw.Hosting.ApiHost;

/// <summary>
/// What a host may set on its API surface, resolved from the collection by name.
/// </summary>
/// <remarks>
/// The collection stores its members as a non-generic base, so a caller reaching one back out has
/// nothing typed to call. This interface is that surface — everything a deployment supplies, in one
/// place, reachable without naming a concrete type.
///
/// It exists so that a deployment's values do not need a package of their own. Before this, a title
/// or an origin list could only be supplied by deriving the base and overriding a property, which
/// meant every deployment published a service-type package containing nothing but its own constants —
/// and a shared bundle could not carry it, because those constants belong to one host.
///
/// Every method returns this interface so a host reads as one statement, and every one sets a value
/// the option already holds rather than replacing a phase, so a host cannot lose the pipeline
/// ordering by contributing to it.
/// </remarks>
public interface IApiHostServiceType
{
    /// <summary>Sets the document's title.</summary>
    /// <param name="value">The title.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Title(string value);

    /// <summary>Sets the document's version.</summary>
    /// <param name="value">The version.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Version(string value);

    /// <summary>Sets the document's description.</summary>
    /// <remarks>Named Summary, not Description: every TypeOption already carries a Description of
    /// its own, and a setter by that name would shadow the option's identity with the document's.</remarks>
    /// <param name="value">The description.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Summary(string value);

    /// <summary>Sets the origins the document advertises.</summary>
    /// <remarks>
    /// Stated rather than derived: behind a reverse proxy Request.Scheme reports http even with
    /// forwarded-headers middleware, and a document advertising http origins gets its "try it" calls
    /// blocked as mixed content.
    /// </remarks>
    /// <param name="urls">The origins this deployment is reachable on.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Origins(params string[] urls);

    /// <summary>Adds to the generated document, after the framework's own processors have run.</summary>
    /// <remarks>
    /// Additive rather than replacing: the framework's permission processor hides operations the
    /// caller cannot invoke, and a host that replaced the document body would drop it silently.
    /// </remarks>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Documentation(Action<AspNetCoreOpenApiDocumentGeneratorSettings> method);

    /// <summary>Sets the body that adds middleware between the framework pipeline and FastEndpoints.</summary>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Pipeline(Action<IApplicationBuilder> method);

    /// <summary>Sets the body that maps routes beyond the endpoints and the framework's own.</summary>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Mapping(Action<IEndpointRouteBuilder> method);

    /// <summary>Sets the body that adjusts the FastEndpoints configuration.</summary>
    /// <param name="method">The body.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Endpoints(Action<Config> method);

    /// <summary>Sets the prefix every endpoint sits under.</summary>
    /// <param name="prefix">The route prefix.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Routing(string prefix);

    /// <summary>Sets the claim type roles are read from.</summary>
    /// <param name="claimType">The claim type.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Roles(string claimType);

    /// <summary>Sets whether the multitenancy middleware runs.</summary>
    /// <param name="enabled">Whether it runs.</param>
    /// <returns>This, for chaining.</returns>
    IApiHostServiceType Multitenancy(bool enabled);
}

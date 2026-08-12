using System;
using System.Collections.Generic;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Web.RestEndpoints.ApiServiceTypeOptions;

/// <summary>
/// Base for a domain's API surface. Drives its endpoint collections through the three phases.
/// </summary>
/// <remarks>
/// Closes IServiceType with <see cref="NullService"/> and <see cref="NullServiceFactory"/> because
/// an API domain builds nothing a caller resolves by name — its whole job happens during Configure,
/// Register and Initialize. The alternative was inventing a per-domain service and factory
/// interface that no implementation would ever satisfy.
///
/// A derived type states what its domain needs in the phase bodies and lists its collections in
/// <see cref="EndpointCollections"/>. It does not write the cycling: this base does that, so every
/// domain skips, orders and short-circuits identically, and a new domain cannot get the sweep
/// subtly wrong.
/// </remarks>
public abstract class ApiServiceTypeBase
    : ServiceTypeBase<NullService, NullServiceFactory, IServiceConfiguration>, IApiServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ApiServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The domain's name — its discriminator within the collection.</param>
    /// <param name="sectionName">The configuration section this domain binds.</param>
    /// <param name="displayName">The name shown to a human.</param>
    /// <param name="description">What this domain's API surface is.</param>
    /// <param name="category">The option's category; defaults to <c>ApiService</c>.</param>
    protected ApiServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "ApiService")
    {
    }

    /// <inheritdoc />
    public abstract IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; }


    /// <summary>
    /// Runs Configure for every endpoint collection this domain owns.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    /// <remarks>
    /// Call this from a derived type's Configuration body after whatever the domain itself needs.
    /// It is not the phase body: a domain that wants nothing extra still gets the cycle, and one
    /// that does can put its own work either side of it.
    /// </remarks>
    protected IGenericResult<IHostApplicationBuilder> ConfigureEndpoints(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        foreach (var collection in EndpointCollections ?? Array.Empty<IEndpointTypeCollection>())
        {
            var result = collection.Configure(builder);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Runs Register for every endpoint collection this domain owns.
    /// </summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    /// <remarks>
    /// Nothing is logged here. Register is the phase that reports what reached the container, and the
    /// collection is the level that can measure it — this cycle would only be able to repeat what its
    /// collections already said.
    /// </remarks>
    protected IGenericResult<IHostApplicationBuilder> RegisterEndpoints(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        // Why: a skipped collection is still driven rather than filtered out here. Register is where
        // a collection announces that it was skipped, and a filter at this level would take that
        // announcement with it — leaving a switched-off resource looking like one that never existed.
        foreach (var collection in EndpointCollections ?? Array.Empty<IEndpointTypeCollection>())
        {
            var result = collection.Register(builder, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    /// <summary>
    /// Runs Initialize for every endpoint collection this domain owns.
    /// </summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    protected IGenericResult<IHost> InitializeEndpoints(IHost host, ILoggerFactory? loggerFactory = null)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHost>.Success(host);
        }

        foreach (var collection in EndpointCollections ?? Array.Empty<IEndpointTypeCollection>())
        {
            var result = collection.Initialize(host, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHost>.Success(host);
    }
}

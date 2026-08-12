using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Abstractions;
using Fdw.Results;
using Fdw.ServiceTypes;
using Fdw.Services.Abstractions;
using Fdw.UI.ComponentTypeOptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.UI.UiServiceTypeOptions;

/// <summary>
/// Base for a domain's UI surface. Drives its component collections through the three phases.
/// </summary>
/// <remarks>
/// The counterpart of ApiServiceTypeBase, and closes IServiceType with NullService and
/// NullServiceFactory for the same reason: a UI domain builds nothing a caller resolves by name,
/// its whole job happening during Configure, Register and Initialize.
///
/// What this replaces is a host's Program.cs calling one AddXxxComponents extension per domain.
/// That shape has two problems the collections fix. It cannot be switched off — a skin wanting one
/// component of a domain took all of them — and the assemblies handed to Blazor's
/// AddAdditionalAssemblies had to be assembled by hand, so a domain registered but not listed
/// compiled fine and rendered nothing.
/// </remarks>
public abstract class UiServiceTypeBase
    : ServiceTypeBase<NullService, NullServiceFactory, IServiceConfiguration>, IUiServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UiServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The domain's name.</param>
    /// <param name="sectionName">The configuration section this domain binds.</param>
    /// <param name="displayName">The name shown to a human.</param>
    /// <param name="description">What this domain's UI surface is.</param>
    /// <param name="category">The option's category; defaults to <c>UiService</c>.</param>
    protected UiServiceTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description,
        string? category = null)
        : base(name, sectionName, displayName, description, category ?? "UiService")
    {
    }

    /// <inheritdoc />
    private readonly List<IComponentTypeCollection> _componentCollections = new();

    /// <summary>Gets the component collections this service type owns.</summary>
    /// <remarks>
    /// Added through <see cref="Components"/> in the constructor rather than declared by overriding an
    /// abstract property, for the same reason every other body is set rather than overridden: a
    /// value only reachable through an override is a second mechanism beside the one the chain
    /// actually reads, and an abstract member cannot be added to - a base contributing a component
    /// collection of its own would be replaced by the derived type rather than joined by it.
    /// </remarks>
    public IReadOnlyList<IComponentTypeCollection> ComponentCollections => _componentCollections;

    /// <summary>Adds a component collection this service type owns.</summary>
    /// <param name="collection">The collection to add.</param>
    /// <returns>This, for chaining.</returns>
    public UiServiceTypeBase Components(IComponentTypeCollection collection)
    {
        if (collection is not null)
        {
            _componentCollections.Add(collection);
        }

        return this;
    }


    /// <summary>
    /// Gets the assemblies Blazor must scan to find this domain's components.
    /// </summary>
    /// <remarks>
    /// Distinct across collections: several components in one package yield one assembly, and
    /// Blazor throws "Assembly already defined" on a duplicate — the same trap the page router hit
    /// when nineteen *.UI.Pages consolidated into one assembly.
    /// </remarks>
    public IEnumerable<Assembly> ComponentAssemblies =>
        SkipRegistration
            ? Array.Empty<Assembly>()
            : (ComponentCollections ?? Array.Empty<IComponentTypeCollection>())
                .SelectMany(c => c.ComponentAssemblies)
                .Distinct();

    /// <summary>Runs Configure for every component collection this domain owns.</summary>
    /// <param name="builder">The host builder.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    protected IGenericResult<IHostApplicationBuilder> ConfigureComponents(IHostApplicationBuilder builder)
        => Cycle(builder, (c, b) => c.Configure(b));

    /// <summary>Runs Register for every component collection this domain owns.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="loggerFactory">The logger factory, if the host has one yet.</param>
    /// <returns>The builder, or the first failure encountered.</returns>
    protected IGenericResult<IHostApplicationBuilder> RegisterComponents(
        IHostApplicationBuilder builder,
        ILoggerFactory? loggerFactory = null)
        => Cycle(builder, (c, b) => c.Register(b, loggerFactory));

    /// <summary>Runs Initialize for every component collection this domain owns.</summary>
    /// <param name="host">The built host.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The host, or the first failure encountered.</returns>
    protected IGenericResult<IHost> InitializeComponents(IHost host, ILoggerFactory? loggerFactory = null)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHost>.Success(host);
        }

        foreach (var collection in ComponentCollections ?? Array.Empty<IComponentTypeCollection>())
        {
            var result = collection.Initialize(host, loggerFactory);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHost>.Success(host);
    }

    private IGenericResult<IHostApplicationBuilder> Cycle(
        IHostApplicationBuilder builder,
        Func<IComponentTypeCollection, IHostApplicationBuilder, IGenericResult<IHostApplicationBuilder>> phase)
    {
        if (SkipRegistration)
        {
            return GenericResult<IHostApplicationBuilder>.Success(builder);
        }

        foreach (var collection in ComponentCollections ?? Array.Empty<IComponentTypeCollection>())
        {
            var result = phase(collection, builder);
            if (result.IsFailure)
            {
                return result;
            }
        }

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Fdw.Results;
using Fdw.UI.ComponentTypeOptions;
using Fdw.UI.Navigation;
using Fdw.UI.UiServiceTypeOptions;
using Fdw.Web.Http.Authentication;
using Fdw.Web.Http.Authentication.Blazor;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fdw.Hosting.UiHost;

/// <summary>
/// The Blazor Server surface every UI domain sits on: components, the pipeline, and the assemblies
/// the router has to scan.
/// </summary>
/// <remarks>
/// The counterpart of ApiHostServiceTypeBase. A skin derives from this and supplies its root
/// component and its own routes; it does not restate the pipeline, because the pipeline is
/// identical in every Blazor Server host and its ordering is the part that is easy to get wrong and
/// impossible to see from a call site.
///
/// Four constraints live in that order, and each was a comment in a host's Program.cs before it
/// lived here. Forwarded headers must precede anything reading the scheme — behind a proxy chain
/// the upstream is non-loopback, so the default KnownNetworks filter drops X-Forwarded-Proto and
/// Request.Scheme stays http. Authentication precedes authorization. Antiforgery follows both,
/// because it validates against the authenticated user. And MapRazorComponents comes last, after
/// everything it depends on.
/// </remarks>
public abstract class UiHostServiceTypeBase : UiServiceTypeBase, IUiHostServiceType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UiHostServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sectionName">The configuration section this host binds.</param>
    /// <param name="displayName">The name shown to a human.</param>
    /// <param name="description">What this host is.</param>
    protected UiHostServiceTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description)
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterUiSurface(builder));

        Initialization(InitializeUiSurface);
    }

    /// <summary>
    /// Gets the root component the router mounts — the skin's <c>App</c>.
    /// </summary>
    /// <remarks>
    /// A <see cref="Type"/> rather than a generic parameter because MapRazorComponents is generic
    /// over it and a service type cannot be. Invoked reflectively once, at Initialize.
    /// </remarks>
    protected Type? RootComponent { get; private set; }

    /// <summary>Gets the path the exception handler redirects to outside development.</summary>
    protected string ErrorPath { get; private set; } = "/Error";

    /// <summary>Sets the root component the router mounts.</summary>
    /// <remarks>
    /// Settable rather than abstract, so a skin supplies it from Program.cs instead of publishing a
    /// service-type package that exists only to name its own App class.
    /// </remarks>
    /// <param name="component">The skin's root component.</param>
    /// <returns>This, for chaining.</returns>
    public IUiHostServiceType Root(Type component)
    {
        RootComponent = component ?? throw new ArgumentNullException(nameof(component));
        return this;
    }

    /// <summary>Sets the path the exception handler redirects to outside development.</summary>
    /// <param name="path">The error path.</param>
    /// <returns>This, for chaining.</returns>
    public IUiHostServiceType Error(string path)
    {
        ErrorPath = path ?? throw new ArgumentNullException(nameof(path));
        return this;
    }

    /// <summary>
    /// Gets the collections whose components this host serves — none by default.
    /// </summary>
    /// <remarks>
    /// A host owns the surface, not the domains. Each UI domain's own service type names its
    /// collections; this one exists so the pipeline has an owner.
    /// </remarks>
    public override IReadOnlyList<IComponentTypeCollection> ComponentCollections { get; } =
        Array.Empty<IComponentTypeCollection>();

    /// <summary>Gets the body that adds middleware between the framework pipeline and the router.</summary>
    /// <remarks>
    /// A func with a gerund setter rather than a virtual method, matching Configuration, Registration
    /// and Initialization. The reason is not symmetry: the sweep invokes the funcs this option holds,
    /// so anything reachable only by an override never runs at all. Keeping every extension point in
    /// the same shape means a skin cannot pick the one that silently does nothing.
    /// </remarks>
    protected Action<IApplicationBuilder> PipelineMethod { get; private set; } = static _ => { };

    /// <summary>Gets the body that maps routes this skin serves beyond its components.</summary>
    protected Action<IEndpointRouteBuilder> MapMethod { get; private set; } = static _ => { };

    /// <summary>Sets the body that adds middleware between the framework pipeline and the router.</summary>
    /// <param name="method">The body.</param>
    /// <inheritdoc />
    public IUiHostServiceType Pipeline(Action<IApplicationBuilder> method)
    {
        PipelineMethod = method ?? throw new ArgumentNullException(nameof(method));
        return this;
    }

    /// <summary>Sets the body that maps routes this skin serves beyond its components.</summary>
    /// <param name="method">The body.</param>
    /// <inheritdoc />
    public IUiHostServiceType Mapping(Action<IEndpointRouteBuilder> method)
    {
        MapMethod = method ?? throw new ArgumentNullException(nameof(method));
        return this;
    }

    private static IGenericResult<IHostApplicationBuilder> RegisterUiSurface(IHostApplicationBuilder builder)
    {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        // Why the host registers this and not a client type: BearerTokenHandler sits on every named
        // API client and needs an IAccessTokenProvider to put a bearer on each request. The client
        // types declare that requirement; only the host knows how to satisfy it, because where the
        // token lives is a property of the hosting model. An API host reads it from HttpContext. A
        // Blazor circuit has no HttpContext after the SignalR handshake, so the token is captured
        // during the initial request and held against the circuit instead.
        builder.Services.TryAddSingleton<CircuitTokenAccessor>();
        builder.Services.AddScoped<CircuitHandler, TokenCapturingCircuitHandler>();
        builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        builder.Services.TryAddScoped<IAccessTokenProvider, BlazorServerAccessTokenProvider>();

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    private IGenericResult<IHost> InitializeUiSurface(IHost host, ILoggerFactory? loggerFactory)
    {
        if (host is not WebApplication app)
        {
            return GenericResult<IHost>.Success(host);
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler(ErrorPath);
        }

        // One structured log per request — method, path, status, elapsed. What makes a journal
        // usable for post-deploy sanity rather than just noise.
        Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(app);

        // .NET 10 replaces UseStaticFiles with MapStaticAssets for Blazor apps.
        app.MapStaticAssets();

        var forwarded = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        };

        // Behind a proxy chain the immediate peer is non-loopback, so the default filter drops
        // X-Forwarded-Proto and Request.Scheme reports http on an https request.
        forwarded.KnownIPNetworks.Clear();
        forwarded.KnownProxies.Clear();
        app.UseForwardedHeaders(forwarded);

        app.UseAuthentication();
        app.UseAuthorization();

        // After both: antiforgery validates against the authenticated user.
        app.UseAntiforgery();

        PipelineMethod(app);

        MapRootComponent(app);
        MapMethod(app);

        return GenericResult<IHost>.Success(host);
    }

    private void MapRootComponent(WebApplication app)
    {
        // Why loud: a skin that never called Root has no component for the router to mount, so every
        // page 404s while the host starts cleanly. Nothing downstream would say why.
        var root = RootComponent
            ?? throw new InvalidOperationException(
                $"{Name} has no root component. Call Root(typeof(App)) on it before the host builds.");

        // MapRazorComponents<TRoot> is generic and TRoot is only known to the deriving host, so the
        // call is made reflectively. The alternative — a generic service type — cannot work, because
        // the collection stores its members as a non-generic base.
        var mapped = typeof(RazorComponentsEndpointRouteBuilderExtensions)
            .GetMethod(nameof(RazorComponentsEndpointRouteBuilderExtensions.MapRazorComponents))!
            .MakeGenericMethod(root)
            .Invoke(null, [app]);

        if (mapped is not RazorComponentsEndpointConventionBuilder route)
        {
            return;
        }

        // Every declared page's assembly, plus every declared component's, distinct. Blazor throws
        // "Assembly already defined" on a duplicate, and several page groups routinely share one
        // assembly — the reorg that consolidated nineteen *.UI.Pages into one made that the normal
        // case rather than the exception.
        var assemblies = PageTypes.All()
            .SelectMany(p => p.PageAssemblies)
            .Concat(ComponentAssemblies)
            .Distinct()
            .ToArray();

        if (assemblies.Length > 0)
        {
            route.AddAdditionalAssemblies(assemblies);
        }

        route.AddInteractiveServerRenderMode();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using NSwag.Generation.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fdw.Results;
using Fdw.Hosting.Configuration;
using Fdw.Hosting.Extensions;
using Fdw.Hosting.Middleware;
using Fdw.SignalR;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using Fdw.Web.RestEndpoints.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors;

namespace Fdw.Hosting.ApiHost;

/// <summary>
/// The API surface every endpoint domain sits on: FastEndpoints, the context accessor, and the
/// OpenAPI document processors.
/// </summary>
/// <remarks>
/// A host derives from this and supplies its own values — a title, a description, its public server
/// URLs. It does not restate the wiring, because the wiring is identical everywhere and the two
/// parts of it that are easy to get wrong are both silent.
///
/// The first is discovery. FastEndpoints scans for endpoint types by default and routes everything
/// it finds, which would leave <c>SkipRegistration</c> decorative — an endpoint switched off in its
/// TypeOption would still be routed. Turning discovery off without supplying a filter is worse:
/// <c>DisableAutoDiscovery</c> means "scan only the assemblies I name", so with none named a host
/// starts cleanly and 404s every route. Both settings together are what make the routed set equal
/// the declared set.
///
/// The second is the two document processors that need the built provider. They are constructed
/// before Build and cannot do their work until after it, so they have a genuine post-Build step;
/// held as fields here and initialized in <c>Initialize</c>, they cannot drift apart the way a
/// local in Program.cs and a matching call eighty lines later can.
/// </remarks>
public abstract class ApiHostServiceTypeBase : ApiServiceTypeBase, IApiHostServiceType
{
    private readonly DataSetQueryDocumentProcessor _dataSetQueryProcessor = new();
    private readonly PermissionFilterDocumentProcessor _permissionFilterProcessor = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiHostServiceTypeBase"/> class.
    /// </summary>
    /// <param name="name">The option's name.</param>
    /// <param name="sectionName">The configuration section this host binds.</param>
    /// <param name="displayName">The name shown to a human.</param>
    /// <param name="description">What this host is.</param>
    protected ApiHostServiceTypeBase(string name, string sectionName, string displayName, string description)
        : base(name, sectionName, displayName, description)
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterApiSurface(builder));

        Initialization(InitializeApiSurface);
    }

    private IGenericResult<IHostApplicationBuilder> RegisterApiSurface(IHostApplicationBuilder builder)
    {
            builder.Services.AddFastEndpoints(o =>
            {
                o.DisableAutoDiscovery = true;
                o.Filter = DeclaredEndpoints.IsDeclared;
            });

            // Why: PermissionFilterDocumentProcessor reads the current user's claims to filter the
            // document, and reaches them only through the accessor. Registering it anywhere else
            // leaves that relationship stated in a comment rather than in the structure.
            builder.Services.AddHttpContextAccessor();

            builder.Services.SwaggerDocument(o =>
            {
                o.DocumentSettings = s =>
                {
                    s.Title = DocumentTitle;
                    s.Version = DocumentVersion;
                    s.Description = DocumentDescription;

                    s.DocumentProcessors.Add(_dataSetQueryProcessor);

                    // Resolves [ValuesFrom] on config DTOs to enum constraints, so a renderer shows
                    // a dropdown for a TypeCollection-backed property instead of a free-text box.
                    s.DocumentProcessors.Add(new ValuesFromSchemaDocumentProcessor());

                    // Hides operations the caller cannot invoke. Runs before any host processor so
                    // a host's own pass only ever sees operations that survived the filter.
                    s.DocumentProcessors.Add(_permissionFilterProcessor);
                    if (ServerUrls.Count > 0)
                    {
                        // Why stated rather than derived: behind a reverse proxy Request.Scheme
                        // reports http even with forwarded-headers middleware, and a document
                        // advertising http origins gets its "try it" calls blocked as mixed content.
                        s.PostProcess = doc =>
                        {
                            doc.Servers.Clear();
                            foreach (var url in ServerUrls)
                            {
                                doc.Servers.Add(new OpenApiServer { Url = url });
                            }
                        };
                    }
                };
            });

        return GenericResult<IHostApplicationBuilder>.Success(builder);
    }

    private IGenericResult<IHost> InitializeApiSurface(IHost host, ILoggerFactory? loggerFactory)
    {
            _dataSetQueryProcessor.Initialize(host.Services);
            _permissionFilterProcessor.Initialize(host.Services);

            // The HTTP pipeline is built here rather than in a host's Program.cs because its ORDER
            // is the part that matters and the part nobody can see from a call site. Forwarded
            // headers must precede anything reading the scheme; the status-code envelope must
            // precede the auth middleware whose bodyless 401/403 it wraps; FastEndpoints must come
            // after both. A host that reorders two lines gets a subtly wrong API and no error.
            if (host is IApplicationBuilder app)
            {
                var forwarded = new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                     | ForwardedHeaders.XForwardedProto
                                     | ForwardedHeaders.XForwardedHost,
                };

                // Why cleared: behind a reverse proxy the immediate peer is not a known network, and
                // leaving the defaults means the headers are ignored and Request.Scheme reports http
                // on an https request — which breaks redirect URIs and cookie security flags.
                forwarded.KnownIPNetworks.Clear();
                forwarded.KnownProxies.Clear();
                app.UseForwardedHeaders(forwarded);

                app.UseStatusCodePages(WriteAuthErrorEnvelope);

                // What UseFrameworkApplicationPipeline used to hide. Inlined because the ordering
                // below IS the pipeline — CORS before authentication or preflight fails; request
                // context after it or HttpContext.User is empty; multitenancy after that again. An
                // extension method named for the whole sequence lets a reader believe the order is
                // someone else's problem, and it is not: it is the only thing here that matters.
                if (host is WebApplication web)
                {
                    if (!web.Environment.IsDevelopment())
                    {
                        web.UseHsts();
                    }

                    // What UseFrameworkMiddleware used to hide. Same reason as the rest: the
                    // sequence is the whole content. The exception handler must be first or it
                    // cannot catch what follows; buffering must sit after it so the handler's own
                    // body gets a Content-Length, and before the security headers so every
                    // downstream response passes through it.
                    // What UseGlobalExceptionHandler used to hide.
                    web.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                    web.UseHttpsRedirection();

                    web.UseFrameworkResponseBuffering(
                        web.Configuration.GetSection("ResponseBuffering").Get<ResponseBufferingOptions>()
                        ?? new ResponseBufferingOptions());

                    web.UseMiddleware<SecurityHeadersMiddleware>(
                        web.Configuration.GetSection("SecurityHeaders").Get<SecurityHeadersOptions>()
                        ?? new SecurityHeadersOptions());

                    Serilog.SerilogApplicationBuilderExtensions.UseSerilogRequestLogging(web);

                    // Before authentication, so an OPTIONS preflight is answered rather than challenged.
                    if (web.Services.GetService<CorsOptions>()?.Enabled == true)
                    {
                        web.UseCors();
                    }

                    web.UseAuthentication();
                    web.UseAuthorization();

                    // After authentication: it reads HttpContext.User, which is empty before it.
                    // What UseRequestContext used to hide.
                    web.UseMiddleware<RequestContextMiddleware>();

                    web.UseRateLimiter();
                }

                app.UseFastEndpoints(config =>
                {
                    config.Endpoints.RoutePrefix = RoutePrefix;
                    config.Security.RoleClaimType = RoleClaimType;

                    // One error shape for the whole API. FastEndpoints' default validation body is
                    // {statusCode, message, errors:{field:[...]}}, which differs from the envelope
                    // the auth failures above write; flattened here so a caller parses one shape.
                    config.Errors.ResponseBuilder = (failures, ctx, statusCode) => new
                    {
                        errorCode = "ValidationFailed",
                        messages = failures
                            .Select(f => string.IsNullOrEmpty(f.PropertyName)
                                ? f.ErrorMessage
                                : $"{f.PropertyName}: {f.ErrorMessage}")
                            .ToArray(),
                    };
                });

                app.UseSwaggerGen();
            }

            if (host is IEndpointRouteBuilder routes)
            {
                // What MapRealTimeHubs used to hide: every declared hub maps itself.
                var hubLogger = loggerFactory?.CreateLogger("Fdw.SignalR")
                    ?? (ILogger)NullLogger.Instance;
                foreach (var hub in RealTimeHubs.All())
                {
                    hub.Map(routes);
                    SignalRLog.RealTimeHubMapped(hubLogger, hub.Name, hub.HubType.Name, hub.Route);
                }

                // What MapFrameworkHealthEndpoint used to hide. Excluded from the document because
                // a liveness probe is not part of the API a caller browses.
                routes.MapGet("/health", () => Microsoft.AspNetCore.Http.Results.Ok(new
                {
                    status = "healthy",
                    service = DocumentTitle,
                    timestamp = DateTime.UtcNow,
                })).ExcludeFromDescription();

            }

        return GenericResult<IHost>.Success(host);
    }

    /// <summary>
    /// Gets the body that adjusts the FastEndpoints configuration for this host.
    /// </summary>
    /// <remarks>
    /// A host whose endpoints do not all derive from an FDW base class registers the permission
    /// pre-processor here:
    /// <code>
    /// config.Endpoints.Configurator = ep =>
    ///     ep.PreProcessors(Order.Before, new PermissionClaimsPreProcessor());
    /// </code>
    /// Deliberately not done for every host. The FDW endpoint bases add that pre-processor
    /// themselves in their own Configure, so a host built entirely from them would register it
    /// twice. Which of the two a host needs depends on what its endpoints derive from, which only
    /// the host knows.
    /// </remarks>
    /// <summary>Gets the route prefix every endpoint sits under.</summary>
    protected string RoutePrefix { get; private set; } = "api/v1";

    /// <summary>Gets the claim type roles are read from.</summary>
    protected string RoleClaimType { get; private set; } = "roles";

    /// <summary>Gets a value indicating whether the multitenancy middleware runs.</summary>
    protected bool HasMultitenancy { get; private set; }

    /// <inheritdoc />
    public IApiHostServiceType Routing(string prefix)
    {
        RoutePrefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
        return this;
    }

    /// <inheritdoc />
    public IApiHostServiceType Roles(string claimType)
    {
        RoleClaimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
        return this;
    }

    /// <inheritdoc />
    public IApiHostServiceType Multitenancy(bool enabled)
    {
        HasMultitenancy = enabled;
        return this;
    }

    /// <remarks>
    /// A func with a gerund setter rather than a virtual method, matching Configuration, Registration
    /// and Initialization. Not for symmetry: the sweep invokes the funcs this option holds, so
    /// anything reachable only by an override never runs. One shape everywhere means a host cannot
    /// pick the extension point that silently does nothing.
    /// </remarks>
    /// <summary>
    /// Writes the framework's error envelope for bodyless 401 and 403 responses.
    /// </summary>
    /// <remarks>
    /// The auth pipeline returns those two without a body, so a caller parsing the envelope every
    /// other error uses would get nothing at all. Registered before the auth middleware because it
    /// wraps that middleware's response writes.
    /// </remarks>
    private static async Task WriteAuthErrorEnvelope(StatusCodeContext context)
    {
        var status = context.HttpContext.Response.StatusCode;
        if (status is not (401 or 403) || context.HttpContext.Response.HasStarted)
        {
            return;
        }

        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            errorCode = status == 401 ? "Unauthorized" : "Forbidden",
            messages = new[]
            {
                status == 401
                    ? "Authentication is required to access this resource."
                    : "You do not have permission to access this resource.",
            },
        }).ConfigureAwait(false);
    }

    /// <summary>Gets the title the generated document carries.</summary>
    /// <remarks>
    /// A settable value rather than an abstract property, and that is the point of this whole group.
    /// An abstract property can only be supplied by deriving, so every deployment had to publish a
    /// service-type package containing nothing but its own constants — which a shared bundle then
    /// could not carry, because those constants belong to one host. Set from Program.cs instead.
    /// </remarks>
    protected string DocumentTitle { get; private set; } = string.Empty;

    /// <summary>Gets the document version.</summary>
    protected string DocumentVersion { get; private set; } = "v1";

    /// <summary>Gets the document description.</summary>
    protected string DocumentDescription { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the public origins the document advertises. Empty leaves whatever NSwag inferred.
    /// </summary>
    protected IReadOnlyList<string> ServerUrls { get; private set; } = Array.Empty<string>();

    /// <inheritdoc />
    public IApiHostServiceType Title(string value)
    {
        DocumentTitle = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <inheritdoc />
    public IApiHostServiceType Version(string value)
    {
        DocumentVersion = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <inheritdoc />
    public IApiHostServiceType Summary(string value)
    {
        DocumentDescription = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <inheritdoc />
    public IApiHostServiceType Origins(params string[] urls)
    {
        ServerUrls = urls ?? throw new ArgumentNullException(nameof(urls));
        return this;
    }

    /// <summary>
    /// Gets the endpoint collections this host owns — none.
    /// </summary>
    /// <remarks>The host registers the API surface; each domain owns its own endpoints.</remarks>
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        Array.Empty<IEndpointTypeCollection>();
}

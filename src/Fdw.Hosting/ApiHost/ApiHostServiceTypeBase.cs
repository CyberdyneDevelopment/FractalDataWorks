using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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
using Fdw.Web.RestEndpoints.EndpointOptions;
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
public abstract class ApiHostServiceTypeBase : ApiServiceTypeBase
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

                    foreach (var processor in AdditionalDocumentProcessors)
                    {
                        s.DocumentProcessors.Add(processor);
                    }

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
                    web.UseGlobalExceptionHandler();
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
                    web.UseRequestContext();

                    web.UseRateLimiter();
                }

                ConfigurePipeline(app);

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

                MapEndpoints(routes);
            }

        return GenericResult<IHost>.Success(host);
    }

    /// <summary>Gets the route prefix every endpoint sits under.</summary>
    protected virtual string RoutePrefix => "api/v1";

    /// <summary>Gets the claim type roles are read from.</summary>
    protected virtual string RoleClaimType => "roles";

    /// <summary>Gets a value indicating whether the multitenancy middleware runs.</summary>
    protected virtual bool HasMultitenancy => false;

    /// <summary>
    /// Adds middleware between the framework pipeline and FastEndpoints.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <remarks>
    /// This is the seam for middleware that has to run before routing but after the framework's own
    /// — a host's body-shape rules, for instance, whose exempt routes are a property of that host's
    /// surface and cannot be known here.
    /// </remarks>
    protected virtual void ConfigurePipeline(IApplicationBuilder app)
    {
    }

    /// <summary>
    /// Maps routes this host serves beyond the endpoints and the framework's own.
    /// </summary>
    /// <param name="routes">The route builder.</param>
    protected virtual void MapEndpoints(IEndpointRouteBuilder routes)
    {
    }

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
    protected abstract string DocumentTitle { get; }

    /// <summary>Gets the document version.</summary>
    protected virtual string DocumentVersion => "v1";

    /// <summary>Gets the document description.</summary>
    protected abstract string DocumentDescription { get; }

    /// <summary>
    /// Gets the public origins the document advertises. Empty leaves whatever NSwag inferred.
    /// </summary>
    protected virtual IReadOnlyList<string> ServerUrls => Array.Empty<string>();

    /// <summary>
    /// Gets processors this host adds after the framework's.
    /// </summary>
    /// <remarks>
    /// Ordering is the point: these run after the permission filter, so a host pass only ever sees
    /// operations the caller can actually invoke.
    /// </remarks>
    protected virtual IReadOnlyList<IDocumentProcessor> AdditionalDocumentProcessors =>
        Array.Empty<IDocumentProcessor>();

    /// <summary>
    /// Gets the endpoint collections this host owns — none.
    /// </summary>
    /// <remarks>The host registers the API surface; each domain owns its own endpoints.</remarks>
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        Array.Empty<IEndpointTypeCollection>();
}

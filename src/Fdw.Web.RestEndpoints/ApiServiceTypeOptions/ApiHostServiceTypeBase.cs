using System;
using System.Collections.Generic;
using FastEndpoints;
using FastEndpoints.Swagger;
using Fdw.Results;
using Fdw.Web.RestEndpoints.EndpointOptions;
using Fdw.Web.RestEndpoints.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSwag;
using NSwag.Generation.Processors;

namespace Fdw.Web.RestEndpoints.ApiServiceTypeOptions;

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
        });

        Initialization((host, loggerFactory) =>
        {
            _dataSetQueryProcessor.Initialize(host.Services);
            _permissionFilterProcessor.Initialize(host.Services);
            return GenericResult<IHost>.Success(host);
        });
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

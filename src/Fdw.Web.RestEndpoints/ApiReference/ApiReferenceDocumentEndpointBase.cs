using System.Collections.Concurrent;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Results;
using Fdw.Web.RestEndpoints.ApiReference.Logging;
using Fdw.Web.RestEndpoints.ApiReference.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSwag.Generation;

namespace Fdw.Web.RestEndpoints.ApiReference;

/// <summary>
/// Serves one API reference's OpenAPI document, cut for the caller making the request.
/// </summary>
/// <typeparam name="TRequest">What the route carries; <see cref="EmptyRequest"/> when nothing.</typeparam>
/// <remarks>
/// <para>
/// The document is generated once per process and kept: what the endpoints are depends only on the
/// build. Every request then parses its own copy, the subclass cuts it with <see cref="Filter"/>,
/// schemas nothing reaches are pruned, and the result is sent as JSON.
/// </para>
/// <para>
/// This replaces serving the document through NSwag's middleware, which kept the document the FIRST
/// caller got and handed it to everyone after: an admin opening the reference after an anonymous
/// visitor saw the anonymous document (FDW-919). Nothing caller-specific runs during generation now.
/// </para>
/// </remarks>
public abstract class ApiReferenceDocumentEndpointBase<TRequest> : Endpoint<TRequest>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<string, string> Generated = new(StringComparer.Ordinal);

    private readonly ISystemRoleConfiguration _systemRoles;

    /// <summary>Initializes a new instance of the <see cref="ApiReferenceDocumentEndpointBase{TRequest}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="systemRoles">Decides whether the caller is a platform admin, who sees every operation.</param>
    protected ApiReferenceDocumentEndpointBase(
        ILogger<ApiReferenceDocumentEndpointBase<TRequest>> logger,
        ISystemRoleConfiguration systemRoles)
    {
        Logger = logger ?? NullLogger<ApiReferenceDocumentEndpointBase<TRequest>>.Instance;
        _systemRoles = systemRoles ?? throw new ArgumentNullException(nameof(systemRoles));
    }

    /// <summary>Gets the logger.</summary>
    protected new ILogger Logger { get; }

    /// <summary>Gets the name the OpenAPI document is registered under.</summary>
    protected abstract string DocumentName { get; }

    /// <summary>Gets the name of the <see cref="ApiReferencePages"/> page this document belongs to, or <see langword="null"/> for none.</summary>
    /// <remarks>
    /// Opt-in. Left <see langword="null"/>, the document is served as it always was. Named, a caller
    /// must also satisfy the policies that page has on this host - its own, or the ones set by
    /// <see cref="ApiReferencePagePolicies"/> - so the document cannot be opened by a caller the page
    /// refuses. A name no registered page has is refused, not skipped: a misspelled name must not
    /// silently remove the gate.
    /// </remarks>
    protected virtual string? PageName => null;

    /// <inheritdoc />
    public override async Task HandleAsync(TRequest req, CancellationToken ct)
    {
        var admitted = Admit(
            ApiReferenceCaller.From(User, _systemRoles),
            HttpContext.RequestServices.GetRequiredService<IConfiguration>());
        var document = admitted.IsFailure
            ? admitted.ToNewResult<ApiReferenceDocument>()
            : await Cut(req, ct).ConfigureAwait(false);
        if (document.IsFailure || document.Value is not { } cut)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(document, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var json = cut.ToJson();
        ApiReferenceLog.DocumentServed(Logger, GetType().Name, cut.OperationCount, json.Length);
        await Send.StringAsync(json, StatusCodes.Status200OK, "application/json", ct).ConfigureAwait(false);
    }

    /// <summary>Cuts the document down to what this reference shows this caller.</summary>
    /// <param name="document">This request's copy of the whole document.</param>
    /// <param name="caller">Who the document is for.</param>
    /// <param name="request">What the route carried.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>Success, or the failure the caller is answered with.</returns>
    protected abstract Task<IGenericResult> Filter(
        ApiReferenceDocument document,
        ApiReferenceCaller caller,
        TRequest request,
        CancellationToken cancellationToken);

    /// <summary>Decides whether the caller may open the document, by the policies of the page it belongs to.</summary>
    /// <param name="caller">Who the document is for.</param>
    /// <param name="configuration">The host's configuration.</param>
    /// <returns>Success, or the failure the caller is answered with: 401 when they must sign in, 403 when they lack a policy.</returns>
    protected IGenericResult Admit(ApiReferenceCaller caller, IConfiguration configuration)
    {
        if (PageName is not { } pageName)
            return GenericResult.Success();

        var pages = ApiReferencePages.All();
        if (pages.FirstOrDefault(candidate => string.Equals(candidate.Name, pageName, StringComparison.Ordinal)) is not { } page)
        {
            return GenericResult.Failure(
                ServicesResultCodes.ByName("RegistrationNotFound"), Logger,
                ResultDetails.Create(
                    "TypeName", pageName,
                    "AvailableTypes", string.Join(", ", pages.Select(candidate => candidate.Name))));
        }

        if (ApiReferencePagePolicies.Refusal(page, caller, configuration) is not { } reason)
            return GenericResult.Success();

        return GenericResult.Failure(
            ApiReferenceResultCodes.ByName(caller.IsAuthenticated ? "ReferencePageNotPermitted" : "ReferencePageSignInRequired"),
            Logger,
            ResultDetails.Create("page", pageName, "reason", reason));
    }

    private async Task<IGenericResult<ApiReferenceDocument>> Cut(TRequest req, CancellationToken ct)
    {
        var generated = await Generate().ConfigureAwait(false);
        if (generated.IsFailure || generated.Value is not { } json)
            return generated.ToNewResult<ApiReferenceDocument>();

        var document = ApiReferenceDocument.Parse(json, Logger);
        if (document.IsFailure || document.Value is not { } copy)
            return document;

        var filtered = await Filter(copy, ApiReferenceCaller.From(User, _systemRoles), req, ct).ConfigureAwait(false);
        if (filtered.IsFailure)
            return filtered.ToNewResult<ApiReferenceDocument>();

        copy.PruneTags();
        copy.PruneSchemas(Logger);
        copy.RemoveAccessStamps();

        // The document is cached once per process, so the server is the one thing cut for each request: it is the origin
        // this request reached the host by, after the host's forwarded-header middleware, so a document fetched through a
        // front end names the front end (FDW-1047).
        var server = copy.SetServer(HttpContext.Request, Logger);
        return server.IsFailure ? server.ToNewResult<ApiReferenceDocument>() : document;
    }

    // NSwag's generator takes no cancellation token; generation runs to completion once per process.
    private async Task<IGenericResult<string>> Generate()
    {
        if (Generated.TryGetValue(DocumentName, out var kept))
        {
            ApiReferenceLog.DocumentReused(Logger, DocumentName);
            return GenericResult<string>.Success(kept);
        }

        if (HttpContext.RequestServices.GetService<IOpenApiDocumentGenerator>() is not { } generator)
        {
            return GenericResult<string>.Failure(
                ServicesResultCodes.ByName("RegistrationNotFound"), Logger,
                ResultDetails.Create(
                    "TypeName", nameof(IOpenApiDocumentGenerator),
                    "AvailableTypes", "none: this host registers no OpenAPI document"));
        }

        try
        {
            var generatedDocument = await generator.GenerateAsync(DocumentName).ConfigureAwait(false);
            var unified = Unified(generatedDocument.ToJson());
            if (unified.IsFailure || unified.Value is not { } unifiedJson)
                return unified;

            var json = Generated.GetOrAdd(DocumentName, unifiedJson);
            ApiReferenceLog.DocumentGenerated(Logger, DocumentName, json.Length);
            return GenericResult<string>.Success(json);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ApiReferenceLog.DocumentGenerationFailed(Logger, exception, DocumentName);
            return GenericResult<string>.Failure(
                ApiReferenceResultCodes.ByName("DocumentGenerationFailed"), Logger,
                ResultDetails.Create("documentName", DocumentName, "reason", exception.Message));
        }
    }

    // The generator's own document with each path template spelled one way (FDW-1047). A document in which two templates
    // cannot be told apart is refused here and not cached, so the route that caused it is the thing that gets fixed.
    private IGenericResult<string> Unified(string json)
    {
        var document = ApiReferenceDocument.Parse(json, Logger);
        if (document.IsFailure || document.Value is not { } parsed)
            return document.ToNewResult<string>();

        var unified = parsed.UnifyPathTemplates(Logger);
        return unified.IsFailure ? unified.ToNewResult<string>() : GenericResult<string>.Success(parsed.ToJson());
    }
}

using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Quality;
using Fdw.Services.Quality.Configuration;
using Microsoft.AspNetCore.Http;
using Fdw.Services.Catalog.Clients.Models;
using Fdw.Web.RestEndpoints.ErrorMapping;

namespace Fdw.Services.Catalog.Endpoints;

/// <summary>Endpoint that resolves (marks as reviewed/approved) a DataSet annotation.</summary>
public abstract class ResolveDataSetAnnotationEndpointBase : Endpoint<DataSetAnnotationIdRequest, DataSetAnnotationPayload>
{
    private readonly IDataSetAnnotationConfigurationProvider _provider;

    /// <summary>Initializes a new instance of the <see cref="ResolveDataSetAnnotationEndpointBase"/> class.</summary>
    /// <param name="provider">The configuration provider for quality and catalog data.</param>
    protected ResolveDataSetAnnotationEndpointBase(IDataSetAnnotationConfigurationProvider provider)
    {
        _provider = provider;
    }

    /// <summary>Gets the authorization policy required for write operations.</summary>
    protected virtual string WritePolicy => "datastores:write";

    /// <summary>Configures the endpoint route, policies, and OpenAPI metadata.</summary>
    public override void Configure()
    {
        Post("/catalog/annotations/{AnnotationId}/resolve");
#if DEVELOP
        AllowAnonymous();
#else
        Policies(WritePolicy);
#endif
        Summary(s => s.Summary = "Resolve DataSet annotation");
    }

    /// <summary>Marks the annotation resolved by the caller and answers it. An annotation that is already resolved answers 409.</summary>
    public override async Task HandleAsync(DataSetAnnotationIdRequest req, CancellationToken ct)
    {
        var getResult = await _provider.Get(req.AnnotationId, ct).ConfigureAwait(false);

        if (!getResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(getResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (getResult.Value is not { } annotation)
        {
            await Send.NotFoundAsync(ct).ConfigureAwait(false);
            return;
        }

        var refusal = Refuse(annotation, req.AnnotationId.ToString());
        if (refusal is not null)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(refusal), HttpContext)).ConfigureAwait(false);
            return;
        }

        annotation.IsResolved = true;
        annotation.ResolvedDate = DateTimeOffset.UtcNow;
        annotation.ResolvedBy = CallerId();

        var saveResult = await _provider
            .Update(annotation, annotation.Domain, annotation.Implementation, annotation.Name, ct)
            .ConfigureAwait(false);

        if (!saveResult.IsSuccess)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(saveResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        await Send.OkAsync(MapToDto(annotation), ct).ConfigureAwait(false);
    }

    private IGenericMessage? Refuse(IDataSetAnnotationImplementationConfiguration annotation, string annotationId)
    {
        if (annotation.IsResolved)
        {
            return DataSetAnnotationLog.AlreadyResolved(Logger, annotationId);
        }

        return CallerId() is null ? DataSetAnnotationLog.ResolverUnknown(Logger, annotationId) : null;
    }

    private Guid? CallerId()
    {
        if (Guid.TryParse(HttpContext.User.FindFirstValue("sub"), out var subject))
        {
            return subject;
        }

        return Guid.TryParse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var identifier) ? identifier : null;
    }

    /// <summary>Maps a DataSetAnnotationImplementationConfiguration to its corresponding DTO.</summary>
    protected virtual DataSetAnnotationPayload MapToDto(IDataSetAnnotationImplementationConfiguration config)
    {
        return DataSetAnnotationPayloadMapper.Map(config);
    }
}

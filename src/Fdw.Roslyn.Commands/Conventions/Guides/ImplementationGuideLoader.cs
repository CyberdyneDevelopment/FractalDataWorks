using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.Extensions.Logging;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Reads the implementing document and its provenance stamp from a guides folder.
/// </summary>
/// <remarks>
/// The folder is an input and has no default: the document lives in an internal repository, and nothing
/// under <c>public/</c> may name where.
/// </remarks>
internal static class ImplementationGuideLoader
{
    private const string ContentFile = "implementing.json";
    private const string MetaFile = "implementing.meta.json";

    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Loads the guide pages under <paramref name="guidesPath"/>.
    /// </summary>
    /// <param name="guidesPath">The folder holding implementing.json and implementing.meta.json.</param>
    /// <param name="logger">The logger the translator owns.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The catalog, or a failure naming why the document could not be used.</returns>
    public static async Task<IGenericResult<ImplementationGuideCatalog>> Load(
        string guidesPath,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ImplementationGuideLoaderLog.Reading(logger, guidesPath);

        var contentPath = Path.Combine(guidesPath, ContentFile);
        var metaPath = Path.Combine(guidesPath, MetaFile);
        if (!File.Exists(contentPath) || !File.Exists(metaPath))
        {
            ImplementationGuideLoaderLog.GuideDocumentNotFound(logger, guidesPath);
            return GenericResult<ImplementationGuideCatalog>.Failure(
                RoslynResultCodes.ByName("GuideDocumentNotFound"),
                ResultDetails.Create().With("GuidesPath", guidesPath));
        }

        var content = await File.ReadAllTextAsync(contentPath, cancellationToken).ConfigureAwait(false);
        var meta = await File.ReadAllTextAsync(metaPath, cancellationToken).ConfigureAwait(false);
        return Parse(guidesPath, content, meta, logger);
    }

    private static IGenericResult<ImplementationGuideCatalog> Parse(
        string guidesPath,
        string content,
        string meta,
        ILogger logger)
    {
        List<GuidePageModel>? pages;
        GuideMetaModel? stamp;
        try
        {
            pages = JsonSerializer.Deserialize<List<GuidePageModel>>(content, Options);
            stamp = JsonSerializer.Deserialize<GuideMetaModel>(meta, Options);
        }
        catch (JsonException ex)
        {
            return Invalid(guidesPath, ex.Message, logger);
        }

        if (pages is null || stamp is null)
            return Invalid(guidesPath, "the document is empty", logger);

        var guides = pages.Where(p => p.Guide is not null).ToList();
        ImplementationGuideLoaderLog.Read(logger, guides.Count, stamp.Repo, stamp.Commit);

        return GenericResult<ImplementationGuideCatalog>.Success(new ImplementationGuideCatalog
        {
            Provenance = new GuideProvenanceInfo
            {
                Repo = stamp.Repo,
                Ref = stamp.Ref,
                Commit = stamp.Commit,
                Verified = stamp.Verified
            },
            Pages = guides
        });
    }

    private static IGenericResult<ImplementationGuideCatalog> Invalid(string guidesPath, string reason, ILogger logger)
    {
        ImplementationGuideLoaderLog.GuideDocumentInvalid(logger, guidesPath, reason);
        return GenericResult<ImplementationGuideCatalog>.Failure(
            RoslynResultCodes.ByName("GuideDocumentInvalid"),
            ResultDetails.Create().With("GuidesPath", guidesPath).With("Reason", reason));
    }
}

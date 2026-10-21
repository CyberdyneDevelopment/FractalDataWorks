using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Roslyn.Commands.Abstractions;
using Fdw.Roslyn.Commands.Abstractions.Results;
using Fdw.Roslyn.Commands.Conventions.Commands;
using Fdw.Roslyn.Commands.Conventions.Guides;
using Fdw.Roslyn.Commands.Conventions.Results;
using Fdw.Roslyn.Commands.Logging;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Translators;

/// <summary>
/// Translator for resolving the implementation guides that apply to a type or a project.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "GuideImplementation")]
public sealed class GuideImplementationTranslator
    : RoslynCommandTranslatorBase<GuideImplementationCommand, QueryResult<ImplementationGuideData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GuideImplementationTranslator"/> class.
    /// </summary>
    public GuideImplementationTranslator()
        : base("GuideImplementationTranslator", "Translates implementation guide commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<ImplementationGuideData>>> Translate(
        GuideImplementationCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.GuidesPath))
        {
            GuideImplementationTranslatorLog.GuidesPathRequired(Logger);
            return GenericResult<QueryResult<ImplementationGuideData>>.Failure(
                RoslynResultCodes.ByName("GuidesPathRequired"));
        }

        if (string.IsNullOrEmpty(command.TypeName) && string.IsNullOrEmpty(command.ProjectName))
        {
            GuideImplementationTranslatorLog.EitherTypeNameOrProjectNameRequired(Logger);
            return GenericResult<QueryResult<ImplementationGuideData>>.Failure(
                RoslynResultCodes.ByName("EitherTypeNameOrProjectNameRequired"));
        }

        GuideImplementationTranslatorLog.Resolving(Logger, command.GuidesPath);

        var catalog = await ImplementationGuideLoader.Load(command.GuidesPath, Logger, cancellationToken).ConfigureAwait(false);
        if (!catalog.IsSuccess)
            return catalog.ToNewResult<QueryResult<ImplementationGuideData>>();

        var target = await GuideTargetResolver
            .Resolve(solution, command.TypeName, command.ProjectName, Logger, cancellationToken)
            .ConfigureAwait(false);
        if (!target.IsSuccess)
            return target.ToNewResult<QueryResult<ImplementationGuideData>>();

        var guides = new List<ImplementationGuideInfo>();
        foreach (var page in catalog.Value!.Pages)
        {
            var abstractions = target.Value!.Matches(page.Guide!);
            if (abstractions.Count == 0) continue;

            guides.Add(await ImplementationGuideAssembler
                .Assemble(page, catalog.Value.Provenance, target.Value, abstractions, solution, cancellationToken)
                .ConfigureAwait(false));
        }

        if (guides.Count == 0)
        {
            GuideImplementationTranslatorLog.NoImplementationGuide(Logger, target.Value!.Label);
            return GenericResult<QueryResult<ImplementationGuideData>>.Failure(
                RoslynResultCodes.ByName("NoImplementationGuide"),
                ResultDetails.Create().With("Target", target.Value.Label));
        }

        GuideImplementationTranslatorLog.Resolved(Logger, guides.Count, target.Value!.Label);

        var data = new ImplementationGuideData
        {
            Target = target.Value.Label,
            Count = guides.Count,
            Guides = guides
        };

        return GenericResult<QueryResult<ImplementationGuideData>>.Success(
            new QueryResult<ImplementationGuideData>(
                $"Found {guides.Count} implementation guide(s) for {target.Value.Label}",
                data));
    }
}

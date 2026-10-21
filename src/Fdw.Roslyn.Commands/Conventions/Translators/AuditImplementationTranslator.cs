using System.Collections.Generic;
using System.Linq;
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
/// Translator for checking a project against the implementation guides that apply to it.
/// </summary>
[TypeOption(typeof(RoslynCommandTranslators), "AuditImplementation")]
public sealed class AuditImplementationTranslator
    : RoslynCommandTranslatorBase<AuditImplementationCommand, QueryResult<AuditImplementationData>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditImplementationTranslator"/> class.
    /// </summary>
    public AuditImplementationTranslator()
        : base("AuditImplementationTranslator", "Translates implementation audit commands")
    {
    }

    /// <inheritdoc/>
    public override async Task<IGenericResult<QueryResult<AuditImplementationData>>> Translate(
        AuditImplementationCommand command,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(command.GuidesPath))
        {
            AuditImplementationTranslatorLog.GuidesPathRequired(Logger);
            return GenericResult<QueryResult<AuditImplementationData>>.Failure(
                RoslynResultCodes.ByName("GuidesPathRequired"));
        }

        if (string.IsNullOrEmpty(command.ProjectName))
        {
            AuditImplementationTranslatorLog.ProjectNameRequired(Logger);
            return GenericResult<QueryResult<AuditImplementationData>>.Failure(
                RoslynResultCodes.ByName("ProjectNameRequired"));
        }

        AuditImplementationTranslatorLog.Auditing(Logger, command.ProjectName, command.GuidesPath);

        var catalog = await ImplementationGuideLoader.Load(command.GuidesPath, Logger, cancellationToken).ConfigureAwait(false);
        if (!catalog.IsSuccess)
            return catalog.ToNewResult<QueryResult<AuditImplementationData>>();

        var target = await GuideTargetResolver
            .Resolve(solution, command.TypeName, command.ProjectName, Logger, cancellationToken)
            .ConfigureAwait(false);
        if (!target.IsSuccess)
            return target.ToNewResult<QueryResult<AuditImplementationData>>();

        var compilation = await target.Value!.Project!.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        var referenced = compilation!.ReferencedAssemblyNames.Select(a => a.Name).ToList();

        var findings = new List<AuditFindingInfo>();
        var manual = new List<GuideRuleInfo>();
        var applied = 0;

        foreach (var page in catalog.Value!.Pages)
        {
            var abstractions = target.Value.Matches(page.Guide!);
            if (abstractions.Count == 0) continue;

            applied++;
            findings.AddRange(GuideReferenceAuditor.Audit(page.Id, page.Guide!, command.ProjectName, referenced));
            findings.AddRange(MemberFindings(page.Id, target.Value, abstractions));
            manual.AddRange(page.Guide!.Must.Concat(page.Guide.MustNot).Select(ImplementationGuideAssembler.Rule));
        }

        if (applied == 0)
        {
            AuditImplementationTranslatorLog.NoImplementationGuide(Logger, command.ProjectName);
            return GenericResult<QueryResult<AuditImplementationData>>.Failure(
                RoslynResultCodes.ByName("NoImplementationGuide"),
                ResultDetails.Create().With("Target", target.Value.Label));
        }

        AuditImplementationTranslatorLog.Audited(Logger, findings.Count, applied, command.ProjectName);

        var data = new AuditImplementationData
        {
            Project = command.ProjectName,
            GuideCount = applied,
            Findings = findings,
            ManualChecks = manual
        };

        return GenericResult<QueryResult<AuditImplementationData>>.Success(
            new QueryResult<AuditImplementationData>(
                $"{findings.Count} finding(s) for project '{command.ProjectName}' across {applied} guide(s); {manual.Count} rule(s) need review",
                data));
    }

    private static IEnumerable<AuditFindingInfo> MemberFindings(
        string guideId,
        GuideTarget target,
        IReadOnlyList<INamedTypeSymbol> abstractions)
    {
        if (target.Type is null) return Enumerable.Empty<AuditFindingInfo>();

        return GuideRequiredMembers.Unimplemented(target.Type, abstractions).Select(member => new AuditFindingInfo
        {
            Guide = guideId,
            Kind = AuditFindingKind.UnimplementedMember,
            Subject = member,
            Message = $"Type '{target.Type.Name}' leaves '{member}' unimplemented.",
            Cite = null
        });
    }
}

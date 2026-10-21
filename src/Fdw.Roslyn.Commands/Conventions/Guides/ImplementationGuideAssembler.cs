using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Roslyn.Commands.Conventions.Results;
using Microsoft.CodeAnalysis;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Builds the result objects from a guide page and the facts the loaded solution adds to it.
/// </summary>
internal static class ImplementationGuideAssembler
{
    /// <summary>
    /// Builds one guide, resolving its exemplars and reading its abstractions from the solution.
    /// </summary>
    /// <param name="page">The guide page.</param>
    /// <param name="provenance">The commit the document was verified against.</param>
    /// <param name="target">What the guide is being resolved for.</param>
    /// <param name="abstractions">The guide's types that the target reaches.</param>
    /// <param name="solution">The loaded solution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The guide with live facts.</returns>
    public static async Task<ImplementationGuideInfo> Assemble(
        GuidePageModel page,
        GuideProvenanceInfo provenance,
        GuideTarget target,
        IReadOnlyList<INamedTypeSymbol> abstractions,
        Solution solution,
        CancellationToken cancellationToken = default)
    {
        var guide = page.Guide!;
        var exemplars = new List<GuideExemplarInfo>();
        foreach (var exemplar in guide.Exemplars)
            exemplars.Add(await GuideExemplarResolver.Resolve(solution, exemplar, cancellationToken).ConfigureAwait(false));

        return new ImplementationGuideInfo
        {
            Id = page.Id,
            Label = page.Label,
            Title = page.Title,
            Provenance = provenance,
            AppliesVia = abstractions.Select(GuideTypeResolver.FullName).ToList(),
            Exemplars = exemplars,
            References = References(guide),
            Must = guide.Must.Select(Rule).ToList(),
            MustNot = guide.MustNot.Select(Rule).ToList(),
            See = guide.See.Select(s => new GuideSeeInfo { Doc = s.Doc, Page = s.Page, Why = s.Why }).ToList(),
            Documentation = GuideDocumentation.Verbatim(abstractions[0]),
            UnimplementedMembers = target.Type is null
                ? new List<string>()
                : GuideRequiredMembers.Unimplemented(target.Type, abstractions)
        };
    }

    /// <summary>
    /// Maps a must or must-not model to its result object.
    /// </summary>
    /// <param name="rule">The model.</param>
    /// <returns>The result object.</returns>
    public static GuideRuleInfo Rule(GuideRuleModel rule) =>
        new() { Head = rule.Head, Body = rule.Body, Cite = rule.Cite, Analyzer = rule.Analyzer };

    /// <summary>
    /// Maps every reference rule of a guide to its result object, requirements first.
    /// </summary>
    /// <param name="guide">The guide.</param>
    /// <returns>The reference rules.</returns>
    public static IReadOnlyList<GuideReferenceRuleInfo> References(GuideBodyModel guide) =>
        guide.References.Require.Select(r => Reference(r, GuideReferenceRuleKind.Require))
            .Concat(guide.References.Forbid.Select(r => Reference(r, GuideReferenceRuleKind.Forbid)))
            .ToList();

    private static GuideReferenceRuleInfo Reference(GuideReferenceModel rule, GuideReferenceRuleKind kind) =>
        new()
        {
            Kind = kind,
            Assembly = rule.Assembly,
            Projects = rule.Projects,
            Except = rule.Except,
            Why = rule.Why,
            Cite = rule.Cite
        };
}

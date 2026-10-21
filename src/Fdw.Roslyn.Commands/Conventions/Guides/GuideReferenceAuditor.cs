using System.Collections.Generic;
using System.Linq;
using Fdw.Roslyn.Commands.Conventions.Results;

namespace Fdw.Roslyn.Commands.Conventions.Guides;

/// <summary>
/// Evaluates a guide's require and forbid rules against the assemblies a project references.
/// </summary>
internal static class GuideReferenceAuditor
{
    /// <summary>
    /// Finds every reference rule the project departs from.
    /// </summary>
    /// <param name="guideId">The guide the rules belong to.</param>
    /// <param name="guide">The guide.</param>
    /// <param name="projectName">The project being audited.</param>
    /// <param name="referenced">The assembly names the project's compilation references, transitively.</param>
    /// <returns>One finding per forbidden assembly present and per required assembly absent.</returns>
    public static IReadOnlyList<AuditFindingInfo> Audit(
        string guideId,
        GuideBodyModel guide,
        string projectName,
        IReadOnlyCollection<string> referenced)
    {
        var findings = new List<AuditFindingInfo>();

        foreach (var rule in guide.References.Forbid.Where(r => AppliesTo(r, projectName)))
        {
            foreach (var assembly in referenced.Where(a => GuideGlob.IsMatch(rule.Assembly, a)))
            {
                findings.Add(new AuditFindingInfo
                {
                    Guide = guideId,
                    Kind = AuditFindingKind.ForbiddenReference,
                    Subject = assembly,
                    Message = $"Project '{projectName}' references '{assembly}', which this guide forbids: {rule.Why}",
                    Cite = rule.Cite
                });
            }
        }

        foreach (var rule in guide.References.Require.Where(r => AppliesTo(r, projectName)))
        {
            if (referenced.Any(a => GuideGlob.IsMatch(rule.Assembly, a))) continue;

            findings.Add(new AuditFindingInfo
            {
                Guide = guideId,
                Kind = AuditFindingKind.MissingReference,
                Subject = rule.Assembly,
                Message = $"Project '{projectName}' does not reference '{rule.Assembly}', which this guide requires: {rule.Why}",
                Cite = rule.Cite
            });
        }

        return findings;
    }

    private static bool AppliesTo(GuideReferenceModel rule, string projectName) =>
        GuideGlob.IsMatch(rule.Projects, projectName)
        && (rule.Except is null || !GuideGlob.IsMatch(rule.Except, projectName));
}

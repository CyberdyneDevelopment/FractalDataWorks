using System.Collections.Generic;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// One implementation guide, with the live facts the loaded solution adds.
/// </summary>
public sealed class ImplementationGuideInfo
{
    /// <summary>Gets the page id in the implementing document.</summary>
    public required string Id { get; init; }

    /// <summary>Gets the page label.</summary>
    public required string Label { get; init; }

    /// <summary>Gets the page title.</summary>
    public required string Title { get; init; }

    /// <summary>Gets the commit the document was verified against.</summary>
    public required GuideProvenanceInfo Provenance { get; init; }

    /// <summary>Gets the guide's implemented types that matched the target.</summary>
    public required IReadOnlyList<string> AppliesVia { get; init; }

    /// <summary>Gets the exemplars, resolved against the solution.</summary>
    public required IReadOnlyList<GuideExemplarInfo> Exemplars { get; init; }

    /// <summary>Gets the require and forbid rules.</summary>
    public required IReadOnlyList<GuideReferenceRuleInfo> References { get; init; }

    /// <summary>Gets what must be there.</summary>
    public required IReadOnlyList<GuideRuleInfo> Must { get; init; }

    /// <summary>Gets what must not be there.</summary>
    public required IReadOnlyList<GuideRuleInfo> MustNot { get; init; }

    /// <summary>Gets where the rest is documented.</summary>
    public required IReadOnlyList<GuideSeeInfo> See { get; init; }

    /// <summary>Gets the XML documentation of the first matched abstraction, verbatim; null when it carries none.</summary>
    public required string? Documentation { get; init; }

    /// <summary>Gets the members of the matched abstractions the target type leaves unimplemented; empty when the target is a project.</summary>
    public required IReadOnlyList<string> UnimplementedMembers { get; init; }
}

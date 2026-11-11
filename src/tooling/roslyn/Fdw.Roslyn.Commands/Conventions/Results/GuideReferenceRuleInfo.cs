using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>
/// A rule over the assemblies a project references.
/// </summary>
public sealed class GuideReferenceRuleInfo
{
    /// <summary>Gets whether the assembly is required or forbidden.</summary>
    [JsonConverter(typeof(GuideReferenceRuleKindJsonConverter))]
    public required IGuideReferenceRuleKind Kind { get; init; }

    /// <summary>Gets the assembly name or a glob using *.</summary>
    public required string Assembly { get; init; }

    /// <summary>Gets the glob of project names the rule applies to.</summary>
    public required string Projects { get; init; }

    /// <summary>Gets the glob of project names exempt from the rule; null when none.</summary>
    public required string? Except { get; init; }

    /// <summary>Gets the reason the rule holds.</summary>
    public required string Why { get; init; }

    /// <summary>Gets the source line the rule rests on.</summary>
    public required string Cite { get; init; }
}

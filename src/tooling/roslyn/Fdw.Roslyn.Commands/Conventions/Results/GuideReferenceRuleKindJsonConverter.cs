namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Writes a <see cref="IGuideReferenceRuleKind"/> as its name and reads it back through <c>GuideReferenceRuleKinds.ByName</c>.</summary>
public sealed class GuideReferenceRuleKindJsonConverter : Fdw.Roslyn.Commands.Json.TypeOptionNameJsonConverter<IGuideReferenceRuleKind>
{
    /// <inheritdoc />
    protected override IGuideReferenceRuleKind ByName(string name) => GuideReferenceRuleKinds.ByName(name);
}

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Writes a <see cref="IGuideExemplarResolution"/> as its name and reads it back through <c>GuideExemplarResolutions.ByName</c>.</summary>
public sealed class GuideExemplarResolutionJsonConverter : Fdw.Roslyn.Commands.Json.TypeOptionNameJsonConverter<IGuideExemplarResolution>
{
    /// <inheritdoc />
    protected override IGuideExemplarResolution ByName(string name) => GuideExemplarResolutions.ByName(name);
}

using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The file is in the solution but no longer declares the type; the guide has drifted.</summary>
[TypeOption(typeof(GuideExemplarResolutions), "TypeNotInFile")]
[ExcludeFromCodeCoverage]
public sealed class TypeNotInFileGuideExemplarResolution : GuideExemplarResolutionBase
{
    /// <summary>Initializes a new instance of the <see cref="TypeNotInFileGuideExemplarResolution"/> class.</summary>
    public TypeNotInFileGuideExemplarResolution() : base(3, "TypeNotInFile") { }
}

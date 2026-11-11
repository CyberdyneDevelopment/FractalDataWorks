using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The exemplar's file is in the solution and declares the type; location and summary are live.</summary>
[TypeOption(typeof(GuideExemplarResolutions), "Resolved")]
[ExcludeFromCodeCoverage]
public sealed class ResolvedGuideExemplarResolution : GuideExemplarResolutionBase
{
    /// <summary>Initializes a new instance of the <see cref="ResolvedGuideExemplarResolution"/> class.</summary>
    public ResolvedGuideExemplarResolution() : base(1, "Resolved") { }
}

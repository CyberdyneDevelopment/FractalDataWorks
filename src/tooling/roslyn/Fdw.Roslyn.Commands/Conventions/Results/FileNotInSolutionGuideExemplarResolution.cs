using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>The exemplar's file is not part of the loaded solution, so only the guide's stamped citation is known.</summary>
[TypeOption(typeof(GuideExemplarResolutions), "FileNotInSolution")]
[ExcludeFromCodeCoverage]
public sealed class FileNotInSolutionGuideExemplarResolution : GuideExemplarResolutionBase
{
    /// <summary>Initializes a new instance of the <see cref="FileNotInSolutionGuideExemplarResolution"/> class.</summary>
    public FileNotInSolutionGuideExemplarResolution() : base(2, "FileNotInSolution") { }
}

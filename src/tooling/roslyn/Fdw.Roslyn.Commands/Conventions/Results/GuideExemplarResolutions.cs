using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>TypeCollection of how a guide's named exemplar was found in the loaded solution.</summary>
[TypeCollection(typeof(GuideExemplarResolutionBase), typeof(IGuideExemplarResolution), typeof(GuideExemplarResolutions))]
[ExcludeFromCodeCoverage]
public abstract partial class GuideExemplarResolutions : TypeCollectionBase<GuideExemplarResolutionBase, IGuideExemplarResolution> { }

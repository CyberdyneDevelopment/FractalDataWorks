using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>One value of <see cref="GuideExemplarResolutions"/>: how a guide's named exemplar was found in the loaded solution.</summary>
public interface IGuideExemplarResolution : ITypeOption<int, GuideExemplarResolutionBase> { }

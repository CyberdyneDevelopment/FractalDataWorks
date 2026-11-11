using Fdw.Collections;

namespace Fdw.Roslyn.Commands.Conventions.Results;

/// <summary>Base of every <see cref="GuideExemplarResolutions"/> option: how a guide's named exemplar was found in the loaded solution.</summary>
public abstract class GuideExemplarResolutionBase : TypeOptionBase<GuideExemplarResolutionBase>, IGuideExemplarResolution
{
    /// <summary>Initializes a new instance of the <see cref="GuideExemplarResolutionBase"/> class.</summary>
    /// <param name="id">The option's id; ids start at 1, since 0 is the collection's not-found value.</param>
    /// <param name="name">The option's name, as the results write it.</param>
    protected GuideExemplarResolutionBase(int id, string name) : base(id, name) { }
}
